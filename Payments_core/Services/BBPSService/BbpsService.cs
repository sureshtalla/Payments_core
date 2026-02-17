using Payments_core.Helpers;
using Payments_core.Models.BBPS;
using Payments_core.Services.BBPSService.Repository;
using Payments_core.Services.WalletService;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
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
        // FETCH BILL (FINAL CORRECT STRUCTURE)
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

            Console.WriteLine($"[BBPS][FETCH][START] RequestId={requestId}, UserId={userId}, BillerId={billerId}");

            try
            {
                // -------------------------------
                // Ensure customerInfo
                // -------------------------------
                if (customerInfo == null)
                {
                    customerInfo = new CustomerInfo();
                }

                if (string.IsNullOrWhiteSpace(customerInfo.CustomerMobile))
                {
                    customerInfo.CustomerMobile = "8004480444"; // UAT fallback
                }

                // -------------------------------
                // Build XML
                // -------------------------------
                string xml = BillAvenueXmlBuilder.BuildFetchBillXml(
                    cfg["InstituteId"],
                    cfg["AgentId"],
                    requestId,
                    billerId,
                    inputParams,
                    agentDeviceInfo,
                    customerInfo
                );

                string encRequest =
                    BillAvenueCrypto.Encrypt(xml, cfg["WorkingKey"]);

                var form = BuildCommonForm(cfg, requestId, encRequest);

                string rawResponse = await _client.PostFormAsync(
                    cfg["BaseUrl"] + cfg["FetchUrl"],
                    form
                );

                string decryptedXml =
                    BillAvenueCrypto.Decrypt(rawResponse, cfg["WorkingKey"]);

                // -------------------------------
                // Parse FULL STRUCTURE
                // -------------------------------
                var parsed =
                    BillAvenueXmlParser.ParseFetch(decryptedXml);

                parsed.RequestId = requestId;

                // ===============================
                // 🔥 DEBUG + VALIDATION (NEW)
                // ===============================
                Console.WriteLine("===== FETCH DECRYPTED XML =====");
                Console.WriteLine(decryptedXml);

                Console.WriteLine("Parsed.ResponseCode = " + parsed.ResponseCode);

                if (string.IsNullOrEmpty(parsed.ResponseCode))
                {
                    throw new Exception(
                        "BillAvenue responseCode missing in Fetch response. Check decrypted XML."
                    );
                }

                // -------------------------------
                // Persist minimal fields only
                // -------------------------------
                await _repo.SaveFetchBill(
                    requestId,
                    parsed.BillRequestId,
                    userId,
                    cfg["AgentId"],
                    billerId,
                    await _repo.GetBillerCategory(billerId),
                    parsed.CustomerName,
                    null,
                    null,
                    parsed.BillAmount,
                    parsed.DueDate == DateTime.MinValue ? null : parsed.DueDate,
                    parsed.ResponseCode,
                    parsed.ResponseMessage,
                    decryptedXml
                );

                Console.WriteLine($"[BBPS][FETCH][END] RequestId={requestId}, ResponseCode={parsed.ResponseCode}");

                // 🔥 RETURN COMPLETE OBJECT (CRITICAL FIX)
                return new BbpsFetchResponseDto
                {
                    ResponseCode = parsed.ResponseCode,
                    ResponseMessage = parsed.ResponseMessage,
                    RequestId = requestId,
                    BillRequestId = parsed.BillRequestId,

                    // 🔥 IMPORTANT — RAW STRUCTURE FOR PAY
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

        public async Task<BbpsBillValidationResponseDto> ValidateBill(
        string billerId,
        Dictionary<string, string> inputParams)
        {
            var cfg = _cfg.GetSection("BillAvenue");
            string requestId = BillAvenueRequestId.Generate();

            string xml = BillAvenueXmlBuilder.BuildBillValidationXml(
                billerId,
                inputParams
            );

            string encRequest =
                BillAvenueCrypto.Encrypt(xml, cfg["WorkingKey"]);

            string url =
                $"{cfg["BaseUrl"]}/billpay/extBillValCntrl/billValidationRequest/xml" +
                $"?accessCode={cfg["AccessCode"]}" +
                $"&requestId={requestId}" +
                $"&ver=2.0" +
                $"&instituteId={cfg["InstituteId"]}";

            string rawResponse =
                await _client.PostRawAsync(
                    url,
                    encRequest,
                    "text/plain"
                );

            string decryptedXml =
                BillAvenueCrypto.Decrypt(rawResponse, cfg["WorkingKey"]);

            return BillAvenueXmlParser.ParseBillValidation(decryptedXml);
        }

        // ---------------------------------------------------------
        // PAY BILL (NPCI READY – ADHOC + REGULAR SUPPORTED)
        // ---------------------------------------------------------
       public async Task<BbpsPayResponseDto> PayBill(
        long userId,
        string billerId,
        string? billRequestId,
        Dictionary<string, string>? inputParams,
        JsonElement? billerResponse,
         JsonElement? AdditionalInfo,
        decimal amount,
        string amountTag,
        string tpin,
        string customerMobile,
        string requestId
    )
{
    string walletTxnId = string.Empty;

    try
    {
        // --------------------------------------------------
        // BASIC VALIDATION
        // --------------------------------------------------
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

        long amountInPaise = (long)amount;

        // ==========================================================
        // 🔥 SAFE AMOUNT VALIDATION (ARRAY + OBJECT SUPPORTED)
        // ==========================================================

        long fetchAmount = 0;

        if (billerResponse != null)
        {
            var json = billerResponse.Value;

            if (!string.IsNullOrWhiteSpace(amountTag) &&
                json.TryGetProperty("amountOptions", out var amountOptions))
            {
                JsonElement optionsArray;

                // Case 1: amountOptions is { option: [...] }
                if (amountOptions.ValueKind == JsonValueKind.Object &&
                    amountOptions.TryGetProperty("option", out optionsArray))
                {
                    foreach (var option in optionsArray.EnumerateArray())
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
                // Case 2: amountOptions is directly an array
                else if (amountOptions.ValueKind == JsonValueKind.Array)
                {
                    foreach (var option in amountOptions.EnumerateArray())
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
            }

            // Fallback to billAmount
            if (fetchAmount == 0 &&
                json.TryGetProperty("billAmount", out var billAmt))
            {
                fetchAmount = long.Parse(billAmt.ToString());
            }
        }

        if (fetchAmount != amountInPaise)
        {
            throw new ApplicationException(
                $"Amount mismatch. Fetch={fetchAmount}, Pay={amountInPaise}"
            );
        }

        // --------------------------------------------------
        // DEVICE INFO
        // --------------------------------------------------
        var deviceInfo = new AgentDeviceInfo
        {
            Ip = "192.168.2.73",
            InitChannel = "AGT",
            Mac = "01-23-45-67-89-ab"
        };

        string xml;

        // --------------------------------------------------
        // BUILD XML
        // --------------------------------------------------
        if (isAdhoc)
        {
            if (inputParams == null || billerResponse == null)
                throw new ApplicationException("Adhoc payment requires inputParams & billerResponse");

            xml = BillAvenueXmlBuilder.BuildAdhocPayXml(
                cfg["InstituteId"],
                requestId,
                cfg["AgentId"],
                billerId,
                inputParams,
                billerResponse.Value,
                  AdditionalInfo,
                amountInPaise,
                amountTag,
                customerMobile,
                deviceInfo
            );
        }
        else
        {
            if (string.IsNullOrWhiteSpace(billRequestId))
                throw new ApplicationException("billRequestId is required for regular payment");

                    xml = BillAvenueXmlBuilder.BuildRegularPayXml(
                    cfg["AgentId"],
                    billRequestId,
                    requestId,
                    amountInPaise,
                    customerMobile,
                    deviceInfo
                    );
                }

        Console.WriteLine("---------- PAY XML ----------");
        Console.WriteLine(xml);

        // --------------------------------------------------
        // HOLD WALLET
        // --------------------------------------------------
        walletTxnId = await _wallet.HoldAmount(userId, amount, "BBPS Bill Payment");

        string encRequest = BillAvenueCrypto.Encrypt(xml, cfg["WorkingKey"]);

        var form = new Dictionary<string, string>
        {
            { "accessCode", cfg["AccessCode"] },
            { "requestId", requestId },
            { "ver", cfg["Version"] },
            { "instituteId", cfg["InstituteId"] },
            { "encRequest", encRequest }
        };

        string rawResponse = await _client.PostFormAsync(
            cfg["BaseUrl"] + cfg["PayUrl"],
            form
        );

        string decryptedXml =
            BillAvenueCrypto.Decrypt(rawResponse, cfg["WorkingKey"]);

        Console.WriteLine("---------- PAY RESPONSE ----------");
        Console.WriteLine(decryptedXml);

        var dto = BillAvenueXmlParser.ParsePay(decryptedXml);

                //    await _repo.SavePayment(
                //    requestId,
                //    billRequestId ?? requestId,
                //    dto.TxnRefId,
                //    userId,
                //    amount,
                //    "PENDING",   // 🔥 FORCE PENDING
                //    dto.ResponseCode,
                //    dto.ResponseMessage,
                //    decryptedXml
                //); 

                await _repo.SavePayment(
                requestId,
                billRequestId ?? "",
                dto.TxnRefId,
                userId,
                amount,
                dto.Status,
                dto.ResponseCode,
                dto.ResponseMessage,
                billerId,                                   // 🔥 ADD
                biller.BillerName ?? "",                    // 🔥 ADD
                "Cash",                                     // 🔥 ADD (or dynamic if needed)
                decryptedXml
                );

                // --------------------------------------------------
                // WALLET FINALIZATION
                // --------------------------------------------------
                if (dto.Status == "SUCCESS")
            await _wallet.DebitFromHold(userId, amount, walletTxnId, "BBPS Bill Payment");
        else
            await _wallet.ReleaseHold(userId, amount, walletTxnId, "BBPS Payment Failed");

        return dto;
    }
    catch
    {
        if (!string.IsNullOrEmpty(walletTxnId))
            await _wallet.ReleaseHold(userId, amount, walletTxnId, "BBPS Payment Exception");

        throw;
    }
}
        // ---------------------------------------------------------
        // STATUS
        // ---------------------------------------------------------
        public async Task<BbpsStatusResponseDto> CheckStatus(
            string requestId,
            string txnRefId,
            string billRequestId)
        {
            var cfg = _cfg.GetSection("BillAvenue");

            // 1️⃣ Build XML
            string xml = BillAvenueXmlBuilder.BuildStatusXmlByTxnRef(
                cfg["InstituteId"],
                requestId,
                txnRefId
            );

            // 2️⃣ Encrypt
            string encRequest =
                BillAvenueCrypto.Encrypt(xml, cfg["WorkingKey"]);

            var form = new Dictionary<string, string>
    {
        { "accessCode", cfg["AccessCode"] },
        { "requestId", requestId },
        { "ver", cfg["Version"] },
        { "instituteId", cfg["InstituteId"] },
        { "encRequest", encRequest }
    };

            // 3️⃣ Call Status API
            string rawResponse = await _client.PostFormAsync(
                cfg["BaseUrl"] + cfg["StatusUrl"],
                form
            );

            // 4️⃣ Decrypt
            string decryptedXml =
                BillAvenueCrypto.Decrypt(rawResponse, cfg["WorkingKey"]);

            Console.WriteLine("===== STATUS DECRYPTED XML =====");
            Console.WriteLine(decryptedXml);

            var dto = BillAvenueXmlParser.ParseStatus(decryptedXml);

            // ---------------------------------------------------------
            // 🔥 STRICT STATUS NORMALIZATION
            // ---------------------------------------------------------

            dto.Status = dto.Status?.Trim().ToUpper();

            if (string.IsNullOrWhiteSpace(dto.Status))
            {
                // STG sometimes returns responseCode only
                if (dto.ResponseCode == "000" &&
                    !string.IsNullOrWhiteSpace(dto.TxnRefId))
                {
                    dto.Status = "SUCCESS";
                }
                else if (dto.ResponseCode == "000")
                {
                    dto.Status = "PENDING";
                }
                else
                {
                    dto.Status = "FAILED";
                }
            }

            // Safety: ensure only valid states returned
            if (dto.Status != "SUCCESS" &&
                dto.Status != "FAILED" &&
                dto.Status != "PENDING")
            {
                dto.Status = "PENDING";
            }

            // ---------------------------------------------------------
            // 5️⃣ Update DB
            // ---------------------------------------------------------
            await _repo.UpdateStatus(
                txnRefId,
                billRequestId,
                dto.Status,
                decryptedXml
            );

            // 🔥 ALSO UPDATE PAYMENT TABLE
            await _repo.UpdatePaymentStatus(
                txnRefId,
                dto.Status
            );

            // ---------------------------------------------------------
            // 6️⃣ Wallet Finalization
            // ---------------------------------------------------------
            if (dto.Status == "SUCCESS")
            {
                await _wallet.FinalizeIfPending(txnRefId);
            }
            else if (dto.Status == "FAILED")
            {
                await _wallet.RefundIfPending(txnRefId);
            }

            // ===================================================
            // ✅ SEND PAYMENT SMS (SUCCESS / FAILED)
            // ===================================================

            if (dto.Status == "SUCCESS" || dto.Status == "FAILED")
            {
                try
                {
                    var payment = await _repo.GetPaymentByTxnRef(txnRefId);

                    if (payment != null && payment.SmsSent == 0)
                    {
                        Console.WriteLine("========== BBPS PAYMENT SMS START ==========");
                        Console.WriteLine($"TxnRefId: {txnRefId}");
                        Console.WriteLine($"Status: {dto.Status}");

                        var user = await _userDataService.GetProfileAsync(payment.UserId);

                        if (!string.IsNullOrWhiteSpace(user?.Mobile))
                        {
                            var msgConfig = await _msgService.GetMSGOTPConfigAsync();

                            string mobile = user.Mobile.Trim();

                            string paymentMode = string.IsNullOrWhiteSpace(payment.PaymentMode)
                                ? "Cash"
                                : payment.PaymentMode;

                            Console.WriteLine($"Sending SMS to: {mobile}");

                            bool smsResult = await _msgService.SendPaymentFlowSmsAsync(
                                mobile,
                                txnRefId,
                                payment.Amount,
                                payment.BillerName,
                                payment.BillerId,
                                paymentMode,
                                dto.Status,
                                msgConfig.MSGOtpAuthKey,
                                msgConfig.MSGOtpTemplateId,
                                msgConfig.MSGUrl
                            );

                            Console.WriteLine($"SMS Result: {smsResult}");

                            if (smsResult)
                            {
                                await _repo.MarkSmsSent(txnRefId);
                            }
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
        // ---------------------------------------------------------
        public async Task SyncBillers()
        {
            var cfg = _cfg.GetSection("BillAvenue");

            var billerIds = (await _repo.GetActiveBillerIds("STG")).ToList();

            Console.WriteLine($"🔎 MDM-ELIGIBLE BILLERS = {billerIds.Count}");

            if (!billerIds.Any())
            {
                Console.WriteLine("ℹ️ No MDM-supported billers in STG (expected for sandbox)");
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

                string rawResponse = await _client.PostRawAsync(
                    url,
                    encRequest,
                    "text/xml"
                );

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

            Console.WriteLine($"✅ MDM Sync Done | Success={success}, Failed={failed.Count}");
        }


        public async Task<IEnumerable<BbpsBillerListDto>> GetBillersByCategory(string category)
        {
            return await _repo.GetBillersByCategory(category);
        }


        public async Task<List<BbpsBillerInputParamDto>> GetBillerParams(string billerId)
        {
            var cfg = _cfg.GetSection("BillAvenue");
            string requestId = BillAvenueRequestId.GenerateForMDM();

            // 1️⃣ XML
            string xml = BillAvenueXmlBuilder.BuildBillerParamsRequestXml(billerId);

            // 2️⃣ Encrypt (MDM style)
            string encRequest = BillAvenueCrypto.Encrypt(xml, cfg["WorkingKey"]);

            // 3️⃣ URL — EXACTLY LIKE BILLAVENUE CURL
            string url =
                $"{cfg["BaseUrl"]}{cfg["MdmUrl"]}" +
                $"?accessCode={cfg["AccessCode"]}" +
                $"&requestId={requestId}" +
                $"&ver={cfg["Version"]}" +
                $"&instituteId={cfg["InstituteId"]}";

            // 4️⃣ POST RAW HEX (NOT FORM)
            string rawResponse = await _client.PostRawAsync(
                url,
                encRequest,
                "text/plain"
            );

            // 5️⃣ Decrypt
            //string decryptedXml =
            //    BillAvenueCrypto.LooksLikeHex(rawResponse)
            //        ? BillAvenueCrypto.DecryptForMdm(rawResponse, cfg["WorkingKey"])
            //        : rawResponse;

            //string decryptedXml =
            //    BillAvenueCrypto.LooksLikeHex(rawResponse)
            //    ? BillAvenueCrypto.Decrypt(rawResponse, cfg["WorkingKey"]) // ✅ MD5 BASED
            //    : rawResponse;

            string decryptedXml = BillAvenueCrypto.Decrypt(rawResponse, cfg["WorkingKey"]);

            // 6️⃣ STG BEHAVIOUR (EXPECTED)
            if (!decryptedXml.Contains("<responseCode>000</responseCode>"))
            {
                Console.WriteLine("⚠️ MDM Params not enabled for this biller (STG)");
                return new List<BbpsBillerInputParamDto>();
            }

            return BillAvenueXmlParser.ParseBillerInputParams(decryptedXml);
        }

        public async Task<BillerDto?> GetBillerById(string billerId)
        {
            return await _repo.GetBillerById(billerId);
        }


        private Dictionary<string, string> BuildCommonForm(
            IConfigurationSection cfg,
            string requestId,
            string encRequest)
        {
            return new Dictionary<string, string>
            {
                { "accessCode", cfg["AccessCode"] },
                { "requestId", requestId },
                { "ver", cfg["Version"] },
                { "instituteId", cfg["InstituteId"] },
                { "encRequest", encRequest }
            };
        }

        public async Task<object> SearchTransactions(
        string txnRefId,
        string mobile,
        DateTime? fromDate,
        DateTime? toDate)
        {
            var data = await _repo.SearchTransactions(
                txnRefId,
                mobile,
                fromDate,
                toDate);

            return new
            {
                success = true,
                count = data.Count,
                transactions = data
            };
        }
    }
}