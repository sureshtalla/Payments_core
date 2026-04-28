using Payments_core.Helpers;
using Payments_core.Models.BBPS;
using Payments_core.Services.BBPSService.Repository;
using Payments_core.Services.WalletService;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static Payments_core.Models.BBPS.BbpsBillerDto;
using Payments_core.Models;
using Payments_core.Services.UserDataService;
using System.Text.Json;
using System.Xml.Linq;
using Payments_core.Services.OTPService;

namespace Payments_core.Services.BBPSService
{
    public class BbpsService : IBbpsService
    {
        private readonly IConfiguration _cfg;
        private readonly IBillAvenueClient _client;
        private readonly IBbpsRepository _repo;
        private readonly IWalletService _wallet;
        private readonly IUserDataService _userDataService;
        private readonly IMSG91OTPService _msgService;

        // Round-robin counter for agent selection
        private static int _agentIndex = -1;

        public BbpsService(
            IConfiguration cfg,
            IBillAvenueClient client,
            IBbpsRepository repo,
            IWalletService wallet,
            IUserDataService userDataService,
            IMSG91OTPService msgService)
        {
            _cfg = cfg;
            _client = client;
            _repo = repo;
            _wallet = wallet;
            _userDataService = userDataService;
            _msgService = msgService;
        }

        // ---------------------------------------------------------
        // FETCH BILL
        // Doc page 22: requires agentId, agentDeviceInfo (ip, initChannel, mac),
        // customerInfo (customerMobile mandatory), billerId, inputParams
        // ---------------------------------------------------------
        public async Task<BbpsFetchResponseDto> FetchBill(
            long userId,
            string billerId,
            Dictionary<string, string> inputParams,
            AgentDeviceInfo agentDeviceInfo,
            CustomerInfo customerInfo)
        {
            var cfg = _cfg.GetSection("BillAvenue");
            string requestId = BillAvenueRequestId.Generate();
            string agentId = GetNextAgentId(cfg);

            Console.WriteLine($"[BBPS][FETCH][START] RequestId={requestId}, UserId={userId}, BillerId={billerId}, AgentId={agentId}");

            try
            {
                if (customerInfo == null)
                    customerInfo = new CustomerInfo();

                // Doc page 26: customerMobile mandatory, must start with 6/7/8/9, 10 digits
                if (string.IsNullOrWhiteSpace(customerInfo.CustomerMobile))
                    customerInfo.CustomerMobile = "8004480444";

                string xml = BillAvenueXmlBuilder.BuildFetchBillXml(
                    cfg["InstituteId"],
                    agentId,
                    requestId,
                    billerId,
                    inputParams,
                    agentDeviceInfo,
                    customerInfo
                );

                string encRequest = BillAvenueCrypto.Encrypt(xml, cfg["WorkingKey"]);
                var form = BuildCommonForm(cfg, requestId, encRequest);

                string rawResponse = await _client.PostFormAsync(
                    cfg["BaseUrl"] + cfg["FetchUrl"], form);

                string decryptedXml = BillAvenueCrypto.Decrypt(rawResponse, cfg["WorkingKey"]);

                Console.WriteLine("===== FETCH DECRYPTED XML (first 300 chars) =====");
                Console.WriteLine(decryptedXml.Length > 300 ? decryptedXml.Substring(0, 300) : decryptedXml);

                var parsed = BillAvenueXmlParser.ParseFetch(decryptedXml);
                parsed.RequestId = requestId;

                Console.WriteLine("Parsed.ResponseCode = " + parsed.ResponseCode);

                if (string.IsNullOrEmpty(parsed.ResponseCode))
                    throw new Exception("BillAvenue responseCode missing in Fetch response.");

                decimal safeBillAmount = 0;
                if (!string.IsNullOrWhiteSpace(parsed.BillerResponse?.BillAmount))
                    decimal.TryParse(parsed.BillerResponse.BillAmount, out safeBillAmount);

                DateTime? safeDueDate = null;
                if (!string.IsNullOrWhiteSpace(parsed.BillerResponse?.DueDate))
                {
                    if (DateTime.TryParse(parsed.BillerResponse.DueDate, out var parsedDate))
                        safeDueDate = parsedDate;
                }

                string safeCustomerName = parsed.BillerResponse?.CustomerName ?? "";

                string effectiveBillRequestId = !string.IsNullOrWhiteSpace(parsed.BillRequestId)
                    ? parsed.BillRequestId
                    : requestId;

                await _repo.SaveFetchBill(
                    requestId,
                    effectiveBillRequestId,
                    userId,
                    agentId,
                    billerId,
                    await _repo.GetBillerCategory(billerId),
                    safeCustomerName,
                    null,
                    null,
                    safeBillAmount,
                    safeDueDate,
                    parsed.ResponseCode,
                    parsed.ResponseMessage,
                    decryptedXml
                );

                Console.WriteLine($"[BBPS][FETCH][END] RequestId={requestId}, ResponseCode={parsed.ResponseCode}, EffectiveBillRequestId={effectiveBillRequestId}");

                return new BbpsFetchResponseDto
                {
                    ResponseCode = parsed.ResponseCode,
                    ResponseMessage = parsed.ResponseMessage,
                    RequestId = requestId,
                    BillRequestId = effectiveBillRequestId,
                    InputParams = parsed.InputParams,
                    BillerResponse = parsed.BillerResponse,
                    AdditionalInfo = parsed.AdditionalInfo
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[BBPS][FETCH][ERROR] RequestId={requestId}, Error={ex}");
                throw;
            }
        }

        // ---------------------------------------------------------
        // VALIDATE BILL
        // Doc page 58: agentId + billerId + inputParams required
        // ---------------------------------------------------------
        public async Task<BbpsBillValidationResponseDto> ValidateBill(
            string billerId,
            Dictionary<string, string> inputParams)
        {
            var cfg = _cfg.GetSection("BillAvenue");
            string requestId = BillAvenueRequestId.Generate();
            string agentId = GetNextAgentId(cfg);

            // Doc page 58: validation request requires agentId
            string xml = BillAvenueXmlBuilder.BuildBillValidationXml(
                agentId, billerId, inputParams);

            string encRequest = BillAvenueCrypto.Encrypt(xml, cfg["WorkingKey"]);

            string url =
                $"{cfg["BaseUrl"]}/billpay/extBillValCntrl/billValidationRequest/xml" +
                $"?accessCode={cfg["AccessCode"]}" +
                $"&requestId={requestId}" +
                $"&ver=2.0" +
                $"&instituteId={cfg["InstituteId"]}";

            string rawResponse = await _client.PostRawAsync(url, encRequest, "text/plain");
            string decryptedXml = BillAvenueCrypto.Decrypt(rawResponse, cfg["WorkingKey"]);

            return BillAvenueXmlParser.ParseBillValidation(decryptedXml);
        }

        // ---------------------------------------------------------
        // PAY BILL
        // Doc page 23 Remitter Info: 4 tags mandatory for ALL transactions
        // Doc page 32: note 1 — same requestId for Fetch+Pay when Fetch is mandatory
        // Doc page 39: paymentMode = "Cash" for AGT channel
        // Doc page 7: all amounts in PAISE
        // Wallet: Hold before pay, Finalize on SUCCESS, Release on FAIL/exception
        // BillAvenue balance: check via Deposit Enquiry before sending payment
        // ---------------------------------------------------------
        public async Task<BbpsPayResponseDto> PayBill(
            long userId,
            string billerId,
            string? billRequestId,
            Dictionary<string, string>? inputParams,
            JsonElement? billerResponse,
            JsonElement? additionalInfo,
            decimal amount,
            string amountTag,
            string tpin,
            string customerMobile,
            string requestId)
        {
            string walletTxnId = string.Empty;

            try
            {
                // --- Validations ---
                if (string.IsNullOrWhiteSpace(tpin))
                    throw new ApplicationException("TPIN is required");

                var isValidTpin = await _userDataService.ValidateUserTpin(userId, tpin);
                if (!isValidTpin)
                    throw new ApplicationException("Invalid TPIN");

                if (string.IsNullOrWhiteSpace(customerMobile))
                    throw new ApplicationException("Customer mobile is required");

                var biller = await _repo.GetBillerById(billerId);
                if (biller == null)
                    throw new ApplicationException("Invalid Biller");

                bool isAdhoc = biller.SupportsAdhoc == 1;
                Console.WriteLine($"IsAdhoc (DB Based) = {isAdhoc}");

                var cfg = _cfg.GetSection("BillAvenue");

                // Frontend sends amount in RUPEES → convert to paise for BillAvenue
                // Doc page 7: all amounts in paise
                long amountInPaise = (long)(amount * 100);

                // ✅ FIX: Always default amountTag to BASE_BILL_AMOUNT if null/empty
                // BillAvenue requires a valid amountTag for adhoc billers (FASTag etc.)
                if (string.IsNullOrWhiteSpace(amountTag))
                    amountTag = "BASE_BILL_AMOUNT";

                Console.WriteLine($"[PAY] EffectiveAmountTag={amountTag}");

                // FIX Bug 2: Always use the same agentId that was used for fetch.
                // Try by requestId first, then by billRequestId as fallback.
                // Round-robin only if neither lookup finds a record (adhoc/quickpay).
                string agentId = await _repo.GetAgentIdByRequestId(requestId);
                if (string.IsNullOrEmpty(agentId) && !string.IsNullOrEmpty(billRequestId))
                    agentId = await _repo.GetAgentIdByRequestId(billRequestId);
                if (string.IsNullOrEmpty(agentId))
                {
                    Console.WriteLine($"[PAY][WARN] No agentId found for requestId={requestId}, falling back to round-robin");
                    agentId = GetNextAgentId(cfg);
                }
                Console.WriteLine($"[PAY] Using agentId={agentId} (from fetch record)");

                // Always generate a brand new requestId for pay —
                // BillAvenue returns responseCode 001 for duplicate requestIds
                string payRequestId = BillAvenueRequestId.Generate();

                Console.WriteLine($"[PAY] FetchRequestId={requestId}, BillRequestId={billRequestId}, NewPayRequestId={payRequestId}, AgentId={agentId}");

                // --- Amount mismatch check ---
                long fetchAmount = 0;

                if (billerResponse != null)
                {
                    var json = billerResponse.Value;

                    // Check amountOptions first if amountTag provided
                    if (!string.IsNullOrWhiteSpace(amountTag) &&
                        json.TryGetProperty("amountOptions", out var amountOptions))
                    {
                        IEnumerable<JsonElement> GetOptionsArray()
                        {
                            if (amountOptions.ValueKind == JsonValueKind.Object &&
                                amountOptions.TryGetProperty("option", out var inner) &&
                                inner.ValueKind == JsonValueKind.Array)
                                return inner.EnumerateArray();

                            if (amountOptions.ValueKind == JsonValueKind.Array)
                                return amountOptions.EnumerateArray();

                            return System.Array.Empty<JsonElement>();
                        }

                        foreach (var option in GetOptionsArray())
                        {
                            var tagName = option.GetProperty("amountName").GetString();
                            var tagValue = option.GetProperty("amountValue").GetString();
                            if (string.Equals(tagName, amountTag, StringComparison.OrdinalIgnoreCase))
                            {
                                fetchAmount = long.Parse(tagValue);
                                break;
                            }
                        }
                    }

                    // billAmount in billerResponse from frontend is in RUPEES
                    // (parser ran ConvertPaiseToRupees on it before sending to frontend)
                    // So multiply by 100 to convert back to paise for comparison
                    if (fetchAmount == 0 && json.TryGetProperty("billAmount", out var billAmt))
                    {
                        var billAmtStr = billAmt.ToString();
                        if (!string.IsNullOrWhiteSpace(billAmtStr))
                        {
                            if (decimal.TryParse(billAmtStr, out decimal inRupees))
                                fetchAmount = (long)(inRupees * 100);
                        }
                    }
                }

                Console.WriteLine($"[PAY] fetchAmount(paise)={fetchAmount}, amountInPaise={amountInPaise}");

                // ✅ FIX: For adhoc billers (FASTag, EV Recharge etc.) skip amount mismatch check.
                // User can recharge any amount. For regular billers (Electricity) still enforce.
                if (!isAdhoc && fetchAmount != amountInPaise)
                    throw new ApplicationException(
                        $"Amount mismatch. Fetch={fetchAmount}, Pay={amountInPaise}");

                // --- Build device info ---
                // Doc page 24 note: agentDeviceInfo must NOT be static.
                // IP comes from the real server. initChannel = AGT for B2B.
                var deviceInfo = new AgentDeviceInfo
                {
                    Ip = "192.168.2.73",   // replace with real server IP from env/config if available
                    InitChannel = "AGT",
                    Mac = "01-23-45-67-89-ab"
                };

                // Doc page 23: Remitter Name from config
                string remitterName = cfg["RemitterName"] ?? "MANICORE PRIVATE LIMITED";

                // --- Build payment XML ---
                string xml;

                if (isAdhoc)
                {
                    if (inputParams == null || billerResponse == null)
                        throw new ApplicationException("Adhoc payment requires inputParams & billerResponse");

                    xml = BillAvenueXmlBuilder.BuildAdhocPayXml(
                        cfg["InstituteId"],
                        payRequestId,        // new unique pay requestId
                        requestId,           // ✅ fetchRequestId — original fetch requestId
                        agentId,
                        billerId,
                        remitterName,
                        inputParams,
                        billerResponse.Value,
                        additionalInfo,
                        amountInPaise,       // ✅ custom recharge amount → amountInfo
                        fetchAmount,         // ✅ original fetch amount → billerResponse
                        amountTag,
                        customerMobile,
                        deviceInfo
                    );
                }
                else
                {
                    string effectiveBillRequestId = !string.IsNullOrWhiteSpace(billRequestId)
                        ? billRequestId
                        : requestId;

                    if (string.IsNullOrWhiteSpace(effectiveBillRequestId))
                        throw new ApplicationException("billRequestId is required for regular payment");

                    Console.WriteLine($"[PAY] Regular pay using billRequestId={effectiveBillRequestId}");

                    // FIX Bug 1: pass fetchAmount (original paise from fetch) as fetchBillAmountPaise
                    // so billerResponse in XML matches what BillAvenue stored during fetch.
                    xml = BillAvenueXmlBuilder.BuildRegularPayXml(
                        agentId,
                        billerId,
                        effectiveBillRequestId,
                        payRequestId,
                        remitterName,
                        amountInPaise,
                        fetchAmount,
                        customerMobile,
                        deviceInfo,
                        inputParams,
                        billerResponse
                    );

                    billRequestId = effectiveBillRequestId;
                }

                // ✅ Log only first 200 chars of XML to save log space
                Console.WriteLine("---------- PAY XML (first 200 chars) ----------");
                Console.WriteLine(xml.Length > 200 ? xml.Substring(0, 200) : xml);

                // --- Step 1: Hold wallet balance ---
                walletTxnId = await _wallet.HoldAsync(
                    userId, amount, "BBPS", payRequestId, "BBPS Bill Payment");

                // --- Step 2: BillAvenue balance check SKIPPED ---
                // Deposit Enquiry API times out on production and causes duplicate
                // payRequestId errors (204) when the frontend retries. Skipped for now.
                // Monitor BillAvenue account balance manually via their portal.

                // --- Step 3: Send payment to BillAvenue ---
                string encRequest = BillAvenueCrypto.Encrypt(xml, cfg["WorkingKey"]);

                // ✅ CRITICAL FIX: For adhoc MANDATORY fetch billers (FASTag),
                // the form POST requestId MUST match the fetch requestId.
                // BillAvenue looks up the fetch session using this requestId.
                // Using payRequestId here causes 204 "No fetch data found".
                var form = new Dictionary<string, string>
                {
                    { "accessCode",  cfg["AccessCode"]  },
                    { "requestId",   requestId          },  // ← always use fetch requestId
                    { "ver",         cfg["Version"]      },
                    { "instituteId", cfg["InstituteId"]  },
                    { "encRequest",  encRequest          }
                };

                string rawResponse = await _client.PostFormAsync(
                    cfg["BaseUrl"] + cfg["PayUrl"], form);

                string decryptedXml = BillAvenueCrypto.Decrypt(rawResponse, cfg["WorkingKey"]);

                // ✅ Log short summary first so it appears even if full XML gets cut off
                var errorCode = "";
                var errorMsg = "";
                try
                {
                    var xdoc = System.Xml.Linq.XDocument.Parse(decryptedXml);
                    errorCode = xdoc.Descendants("errorCode").FirstOrDefault()?.Value ?? "";
                    errorMsg = xdoc.Descendants("errorMessage").FirstOrDefault()?.Value ?? "";
                }
                catch { }
                Console.WriteLine($"[PAY][RESPONSE] CODE={decryptedXml.Substring(decryptedXml.IndexOf("<responseCode>") >= 0 ? decryptedXml.IndexOf("<responseCode>") + 14 : 0, 3)} ErrorCode={errorCode} ErrorMsg={errorMsg}");

                Console.WriteLine("---------- PAY RESPONSE (first 500 chars) ----------");
                Console.WriteLine(decryptedXml.Length > 500 ? decryptedXml.Substring(0, 500) : decryptedXml);

                var dto = BillAvenueXmlParser.ParsePay(decryptedXml);

                Console.WriteLine($"[PAY][CTRL] ResponseCode={dto.ResponseCode}, TxnRefId={dto.TxnRefId}");

                await _repo.SavePayment(
                    payRequestId,
                    billRequestId ?? "",
                    dto.TxnRefId,
                    userId,
                    amount,
                    dto.Status,
                    dto.ResponseCode,
                    dto.ResponseMessage,
                    billerId,
                    biller.BillerName ?? "",
                    "Cash",
                    decryptedXml
                );

                // --- Step 4: Finalize or release wallet ---
                if (dto.Status == "SUCCESS")
                    await _wallet.FinalizeAsync(
                        userId, amount, "BBPS", payRequestId, walletTxnId, "BBPS Success");
                else if (dto.Status == "FAILED")
                    await _wallet.ReleaseAsync(
                        userId, amount, "BBPS", payRequestId, walletTxnId, "BBPS Failed");

                return dto;
            }
            catch
            {
                if (!string.IsNullOrEmpty(walletTxnId))
                    await _wallet.ReleaseAsync(
                        userId, amount, "BBPS", requestId, walletTxnId, "BBPS Payment Exception");
                throw;
            }
        }

        // ---------------------------------------------------------
        // STATUS CHECK
        // Doc page 46: XML only needs trackType + trackValue
        // The form POST still needs accessCode, requestId, ver, instituteId
        // Doc page 48: always check txnStatus tag, not just responseCode
        // ---------------------------------------------------------
        public async Task<BbpsStatusResponseDto> CheckStatus(
            string requestId,
            string txnRefId,
            string billRequestId)
        {
            var cfg = _cfg.GetSection("BillAvenue");

            // Doc page 46: status XML — only trackType + trackValue
            string xml = BillAvenueXmlBuilder.BuildStatusXmlByTxnRef(txnRefId);

            string encRequest = BillAvenueCrypto.Encrypt(xml, cfg["WorkingKey"]);

            var form = new Dictionary<string, string>
            {
                { "accessCode",  cfg["AccessCode"]  },
                { "requestId",   requestId           },
                { "ver",         cfg["Version"]      },
                { "instituteId", cfg["InstituteId"]  },
                { "encRequest",  encRequest          }
            };

            string rawResponse = await _client.PostFormAsync(
                cfg["BaseUrl"] + cfg["StatusUrl"], form);

            string decryptedXml = BillAvenueCrypto.Decrypt(rawResponse, cfg["WorkingKey"]);

            Console.WriteLine("===== STATUS DECRYPTED XML =====");
            Console.WriteLine(decryptedXml);

            var dto = BillAvenueXmlParser.ParseStatus(decryptedXml);

            dto.Status = dto.Status?.Trim().ToUpper();

            if (string.IsNullOrWhiteSpace(dto.Status))
            {
                if (dto.ResponseCode == "000" && !string.IsNullOrWhiteSpace(dto.TxnRefId))
                    dto.Status = "SUCCESS";
                else if (dto.ResponseCode == "000")
                    dto.Status = "PENDING";
                else
                    dto.Status = "FAILED";
            }

            if (dto.Status != "SUCCESS" && dto.Status != "FAILED" && dto.Status != "PENDING")
                dto.Status = "PENDING";

            await _repo.UpdateStatus(txnRefId, billRequestId, dto.Status, decryptedXml);
            await _repo.UpdatePaymentStatus(txnRefId, dto.Status);

            if (dto.Status == "SUCCESS")
                await _wallet.FinalizeIfPending(txnRefId);
            else if (dto.Status == "FAILED")
                await _wallet.RefundIfPending(txnRefId);

            if (dto.Status == "SUCCESS" || dto.Status == "FAILED")
            {
                try
                {
                    var payment = await _repo.GetPaymentByTxnRef(txnRefId);

                    if (payment != null && payment.SmsSent == 0)
                    {
                        Console.WriteLine("========== BBPS PAYMENT SMS START ==========");

                        var user = await _userDataService.GetProfileAsync(payment.UserId);

                        if (!string.IsNullOrWhiteSpace(user?.Mobile))
                        {
                            var msgConfig = await _msgService.GetMSGOTPConfigAsync();

                            string mobile = user.Mobile.Trim();
                            string paymentMode = string.IsNullOrWhiteSpace(payment.PaymentMode)
                                ? "Cash" : payment.PaymentMode;

                            string consumerNo = "";
                            if (!string.IsNullOrWhiteSpace(payment.RawFetchXml))
                            {
                                try
                                {
                                    var doc = XDocument.Parse(payment.RawFetchXml);
                                    consumerNo = doc.Descendants("input")
                                        .Where(x => x.Element("paramName")?.Value == "Consumer No")
                                        .Select(x => x.Element("paramValue")?.Value)
                                        .FirstOrDefault() ?? "";
                                }
                                catch { }
                            }

                            Console.WriteLine("ConsumerNo: " + consumerNo);

                            bool smsResult = await _msgService.SendPaymentTemplateSmsAsync(
                                mobile,
                                payment.Amount,
                                payment.BillerName,
                                consumerNo,
                                txnRefId,
                                paymentMode,
                                msgConfig.MSGOtpAuthKey,
                                msgConfig.MSGPAYMENTSUCCESS,
                                msgConfig.MSGUrl
                            );

                            Console.WriteLine($"SMS Result: {smsResult}");

                            if (smsResult)
                                await _repo.MarkSmsSent(txnRefId);
                        }

                        Console.WriteLine("========== BBPS PAYMENT SMS END ==========");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Payment SMS Exception: " + ex.Message);
                }
            }

            return dto;
        }

        // ---------------------------------------------------------
        // SYNC BILLERS (MDM)
        // Doc page 11: MDM call limit = 15 requests per AI per 24 hours
        // IMPORTANT: Do NOT call this job more than 15 times per day
        // ---------------------------------------------------------
        public async Task SyncBillers()
        {
            var cfg = _cfg.GetSection("BillAvenue");
            var bbpsEnv = cfg["InstituteId"] == "HP59" ? "PROD" : "STG";
            var billerIds = (await _repo.GetActiveBillerIds(bbpsEnv)).ToList();

            Console.WriteLine($"MDM-ELIGIBLE BILLERS = {billerIds.Count}");
            Console.WriteLine($"WARNING: MDM call limit is 15 per AI per 24 hours (doc page 11).");

            if (!billerIds.Any())
            {
                Console.WriteLine($"No MDM-supported billers in {bbpsEnv} catalog.");
                return;
            }

            int success = 0;
            var failed = new List<string>();

            foreach (var billerId in billerIds)
            {
                string requestId = BillAvenueRequestId.GenerateForMDM();

                var xml = BillAvenueXmlBuilder.BuildBillerInfoRequest(billerId);
                string encRequest = BillAvenueCrypto.Encrypt(xml, cfg["WorkingKey"]);

                var url =
                    $"{cfg["BaseUrl"]}{cfg["MdmUrl"]}" +
                    $"?accessCode={cfg["AccessCode"]}" +
                    $"&requestId={requestId}" +
                    $"&ver={cfg["Version"]}" +
                    $"&instituteId={cfg["InstituteId"]}";

                string rawResponse = await _client.PostRawAsync(url, encRequest, "text/xml");

                string decryptedXml =
                    BillAvenueCrypto.LooksLikeHex(rawResponse)
                        ? BillAvenueCrypto.Decrypt(rawResponse, cfg["WorkingKey"])
                        : rawResponse;

                if (!decryptedXml.Contains("<responseCode>000</responseCode>"))
                {
                    failed.Add(billerId);
                    continue;
                }

                var billers = BillAvenueXmlParser.ParseBillerInfo(decryptedXml);
                foreach (var biller in billers)
                {
                    await _repo.UpsertBiller(biller);
                    success++;
                }

                await Task.Delay(300);
            }

            Console.WriteLine($"MDM Sync Done | Success={success}, Failed={failed.Count}");
        }

        // ---------------------------------------------------------
        // GET BILLERS BY CATEGORY
        // ---------------------------------------------------------
        public async Task<IEnumerable<BbpsBillerListDto>> GetBillersByCategory(string category)
        {
            return await _repo.GetBillersByCategory(category);
        }

        // ---------------------------------------------------------
        // GET BILLER PARAMS
        // ---------------------------------------------------------
        public async Task<List<BbpsBillerInputParamDto>> GetBillerParams(string billerId)
        {
            var dbParams = await _repo.GetBillerParamsFromDb(billerId);

            if (dbParams != null && dbParams.Any())
            {
                Console.WriteLine($"[MDM] Returning params from DB for {billerId}");
                return dbParams;
            }

            Console.WriteLine($"[MDM] DB miss - calling live MDM for {billerId}");

            var cfg = _cfg.GetSection("BillAvenue");
            string requestId = BillAvenueRequestId.GenerateForMDM();

            string xml = BillAvenueXmlBuilder.BuildBillerParamsRequestXml(billerId);
            string encRequest = BillAvenueCrypto.Encrypt(xml, cfg["WorkingKey"]);

            string url =
                $"{cfg["BaseUrl"]}{cfg["MdmUrl"]}" +
                $"?accessCode={cfg["AccessCode"]}" +
                $"&requestId={requestId}" +
                $"&ver={cfg["Version"]}" +
                $"&instituteId={cfg["InstituteId"]}";

            try
            {
                string rawResponse = await _client.PostRawAsync(url, encRequest, "text/plain");
                string decryptedXml = BillAvenueCrypto.Decrypt(rawResponse, cfg["WorkingKey"]);

                Console.WriteLine($"[MDM] Response: {decryptedXml}");

                if (!decryptedXml.Contains("<responseCode>000</responseCode>"))
                {
                    Console.WriteLine($"[MDM] No params for {billerId} - returning empty");
                    return new List<BbpsBillerInputParamDto>();
                }

                var parsedParams = BillAvenueXmlParser.ParseBillerInputParams(decryptedXml);

                // ✅ FIX: Save to DB cache so next call is instant — no live MDM call every time
                if (parsedParams != null && parsedParams.Any())
                {
                    try
                    {
                        await _repo.SaveBillerParams(billerId, parsedParams);
                        Console.WriteLine($"[MDM] Params saved to DB for {billerId} — future calls will use cache");
                    }
                    catch (Exception saveEx)
                    {
                        Console.WriteLine($"[MDM] SaveBillerParams failed (non-critical): {saveEx.Message}");
                    }
                }

                return parsedParams;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MDM] Error for {billerId}: {ex.Message}");
                return new List<BbpsBillerInputParamDto>();
            }
        }
        public async Task<BillerDto?> GetBillerById(string billerId)
        {
            return await _repo.GetBillerById(billerId);
        }

        // ---------------------------------------------------------
        // SEARCH TRANSACTIONS
        // ---------------------------------------------------------
        public async Task<object> SearchTransactions(
            string txnRefId,
            string mobile,
            DateTime? fromDate,
            DateTime? toDate)
        {
            var data = await _repo.SearchTransactions(txnRefId, mobile, fromDate, toDate);
            return new { success = true, count = data.Count, transactions = data };
        }

        // ---------------------------------------------------------
        // GET RECEIPT
        // ---------------------------------------------------------
        public async Task<object> GetReceipt(string txnRefId)
        {
            var payment = await _repo.GetReceiptRaw(txnRefId);

            if (payment == null)
                return new { success = false, message = "Transaction not found." };

            var dto = new BbpsReceiptDto
            {
                TxnReferenceId = payment.txn_ref_id,
                BillerId = payment.biller_id,
                BillerName = payment.biller_name,
                BillAmount = payment.amount,
                PaymentMode = payment.payment_mode,
                TransactionStatus = payment.status,
                TxnDate = payment.created_on
            };

            if (!string.IsNullOrWhiteSpace(payment.raw_pay_xml?.ToString()))
            {
                try
                {
                    string xmlString = payment.raw_pay_xml.ToString();
                    var doc = System.Xml.Linq.XDocument.Parse(xmlString);
                    var root = doc.Root;

                    string GetValue(string tagName) =>
                        root.Descendants()
                            .FirstOrDefault(e => e.Name.LocalName == tagName)
                            ?.Value;

                    dto.CustomerName = GetValue("RespCustomerName");
                    dto.BillNumber = GetValue("RespBillNumber");
                    dto.BillPeriod = GetValue("RespBillPeriod");
                    dto.BillDate = GetValue("RespBillDate");
                    dto.DueDate = GetValue("RespDueDate");
                    dto.ApprovalNumber = GetValue("approvalRefNumber");

                    // RespAmount and CustConvFee come from BillAvenue XML in paise — divide by 100
                    var respAmount = GetValue("RespAmount");
                    if (decimal.TryParse(respAmount, out decimal billAmount))
                        dto.BillAmount = billAmount / 100;

                    var ccf = GetValue("CustConvFee");
                    if (decimal.TryParse(ccf, out decimal ccfValue))
                        dto.CCF = ccfValue / 100;

                    dto.TotalAmount = dto.BillAmount + dto.CCF;
                    dto.MobileNumber = GetValue("customerMobile");
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Receipt XML Parse Error: " + ex.Message);
                }
            }

            return new { success = true, data = dto };
        }

        // ---------------------------------------------------------
        // DEPOSIT ENQUIRY — CHECK BILLAVENUE MAIN ACCOUNT BALANCE
        // Doc page 76: Deposit Enquiry API
        // Returns balance in rupees (e.g. 252000.00)
        // FIX Bug 5: throw on failure instead of returning MaxValue —
        // silently allowing payment with unknown balance causes real money loss.
        // ---------------------------------------------------------
        private async Task<decimal> GetBillAvenueBalance(
            IConfigurationSection cfg, string agentId)
        {
            try
            {
                string xml = BillAvenueXmlBuilder.BuildDepositEnquiryXml(agentId);
                string encRequest = BillAvenueCrypto.Encrypt(xml, cfg["WorkingKey"]);
                string requestId = BillAvenueRequestId.Generate();

                string url =
                    $"{cfg["BaseUrl"]}/billpay/enquireDeposit/fetchDetails/xml" +
                    $"?accessCode={cfg["AccessCode"]}" +
                    $"&requestId={requestId}" +
                    $"&ver={cfg["Version"]}" +
                    $"&instituteId={cfg["InstituteId"]}";

                string rawResponse = await _client.PostRawAsync(url, encRequest, "text/xml");
                string decryptedXml = BillAvenueCrypto.Decrypt(rawResponse, cfg["WorkingKey"]);

                Console.WriteLine("===== DEPOSIT ENQUIRY =====");
                Console.WriteLine(decryptedXml);

                var doc = XDocument.Parse(decryptedXml);
                var balStr = doc.Root?.Element("currentBalance")?.Value ?? "0";

                decimal.TryParse(balStr, out decimal balance);
                Console.WriteLine($"[DEPOSIT] BillAvenue balance = ₹{balance}");
                return balance;
            }
            catch (Exception ex)
            {
                // Deposit Enquiry API is unreliable — log and allow payment to proceed.
                // Do NOT block payment just because the enquiry call failed.
                Console.WriteLine($"[DEPOSIT ENQUIRY ERROR] {ex.Message} — allowing payment to proceed");
                return decimal.MaxValue;
            }
        }

        // ---------------------------------------------------------
        // HELPERS
        // ---------------------------------------------------------
        private Dictionary<string, string> BuildCommonForm(
            IConfigurationSection cfg,
            string requestId,
            string encRequest)
        {
            return new Dictionary<string, string>
            {
                { "accessCode",  cfg["AccessCode"]  },
                { "requestId",   requestId           },
                { "ver",         cfg["Version"]      },
                { "instituteId", cfg["InstituteId"]  },
                { "encRequest",  encRequest          }
            };
        }

        // ---------------------------------------------------------
        // ROUND ROBIN AGENT SELECTOR
        // Doc page 25: agentId must be 20 chars, alphanumeric
        // ---------------------------------------------------------
        private string GetNextAgentId(IConfigurationSection cfg)
        {
            var agentIds = new[]
            {
                System.Environment.GetEnvironmentVariable("BILLAVENUE_AGENT_ID_0"),
                System.Environment.GetEnvironmentVariable("BILLAVENUE_AGENT_ID_1"),
                System.Environment.GetEnvironmentVariable("BILLAVENUE_AGENT_ID_2"),
                System.Environment.GetEnvironmentVariable("BILLAVENUE_AGENT_ID_3"),
                System.Environment.GetEnvironmentVariable("BILLAVENUE_AGENT_ID_4"),
                System.Environment.GetEnvironmentVariable("BILLAVENUE_AGENT_ID_5")
            };

            var validIds = agentIds
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .ToArray();

            if (validIds.Length == 0)
            {
                Console.WriteLine($"[AGENT] No env AgentIds found, using config AgentId={cfg["AgentId"]}");
                return cfg["AgentId"];
            }

            int idx = Interlocked.Increment(ref _agentIndex) % validIds.Length;
            Console.WriteLine($"[AGENT] Round-robin index={idx}, AgentId={validIds[idx]}");
            return validIds[idx];
        }
    }
}