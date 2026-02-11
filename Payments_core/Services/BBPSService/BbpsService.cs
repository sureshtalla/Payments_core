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

namespace Payments_core.Services.BBPSService
{
    public class BbpsService : IBbpsService
    {
        private readonly IConfiguration _cfg;
        private readonly IBillAvenueClient _client;
        private readonly IBbpsRepository _repo;
        private readonly IWalletService _wallet;
        private readonly IUserDataService _userDataService;

        public BbpsService(
            IConfiguration cfg,
            IBillAvenueClient client,
            IBbpsRepository repo,
            IWalletService wallet,
            IUserDataService userDataService)
        {
            _cfg = cfg;
            _client = client;
            _repo = repo;
            _wallet = wallet;
            _userDataService = userDataService;
        }

        // ---------------------------------------------------------
        // FETCH BILL
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
                    Console.WriteLine($"[BBPS][FETCH] customerInfo was NULL | RequestId={requestId}");
                }

                // -------------------------------
                // Mobile enforcement (BBPS runtime rule)
                // -------------------------------
                if (string.IsNullOrWhiteSpace(customerInfo.CustomerMobile))
                {
                    customerInfo.CustomerMobile = "8004480444"; // UAT fallback
                    Console.WriteLine($"[BBPS][FETCH] customerMobile missing, fallback applied | RequestId={requestId}");
                }

                // -------------------------------
                // Build XML
                // -------------------------------
                Console.WriteLine($"[BBPS][FETCH] Building XML | RequestId={requestId}");

                string xml = BillAvenueXmlBuilder.BuildFetchBillXml(
                    cfg["InstituteId"],
                    cfg["AgentId"],
                    requestId,
                    billerId,
                    inputParams,
                    agentDeviceInfo,
                    customerInfo
                );

                // -------------------------------
                // Encrypt
                // -------------------------------
                Console.WriteLine($"[BBPS][FETCH] Encrypting request | RequestId={requestId}");

                string encRequest =
                    BillAvenueCrypto.Encrypt(xml, cfg["WorkingKey"]);

                var form =
                    BuildCommonForm(cfg, requestId, encRequest);

                // -------------------------------
                // Call BillAvenue
                // -------------------------------
                Console.WriteLine($"[BBPS][FETCH] Calling BillAvenue API | RequestId={requestId}");

                var startTime = DateTime.UtcNow;

                string rawResponse = await _client.PostFormAsync(
                    cfg["BaseUrl"] + cfg["FetchUrl"],
                    form
                );

                var elapsedMs = (DateTime.UtcNow - startTime).TotalMilliseconds;

                Console.WriteLine($"[BBPS][FETCH] BillAvenue response received | RequestId={requestId}, TimeMs={elapsedMs}");

                // -------------------------------
                // Decrypt
                // -------------------------------
                Console.WriteLine($"[BBPS][FETCH] Decrypting response | RequestId={requestId}");

                string decryptedXml =
                    BillAvenueCrypto.Decrypt(rawResponse, cfg["WorkingKey"]);

                // -------------------------------
                // Parse
                // -------------------------------
                var dto =
                    BillAvenueXmlParser.ParseFetch(decryptedXml);

                Console.WriteLine(
                    $"[BBPS][FETCH][END] RequestId={requestId}, ResponseCode={dto.ResponseCode}, Message={dto.ResponseMessage}");

                // -------------------------------
                // Persist
                // -------------------------------
                Console.WriteLine($"[BBPS][FETCH] Saving to DB | RequestId={requestId}");

                await _repo.SaveFetchBill(
                    requestId,
                    dto.BillRequestId,
                    userId,
                    cfg["AgentId"],
                    billerId,
                    await _repo.GetBillerCategory(billerId),
                    dto.CustomerName,
                    null,
                    inputParams.ContainsKey("Vehicle Registration Number")
                        ? inputParams["Vehicle Registration Number"]
                        : null,
                    dto.BillAmount,
                    dto.DueDate == DateTime.MinValue ? null : dto.DueDate,
                    dto.ResponseCode,
                    dto.ResponseMessage,
                    decryptedXml
                );

                Console.WriteLine($"[BBPS][FETCH] DB save completed | RequestId={requestId}");

                return dto;
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    $"[BBPS][FETCH][ERROR] RequestId={requestId}, UserId={userId}, BillerId={billerId}, Error={ex}");

                throw;
            }
        }
        // ---------------------------------------------------------
        // PAY BILL
        // ---------------------------------------------------------
        public async Task<BbpsPayResponseDto> PayBill(
           long userId,
           string billerId,
           Dictionary<string, string> inputParams,
           string billerResponseJson,
           decimal amount,
           string tpin,
           string customerMobile
       )
        {
            string requestId = string.Empty;
            string walletTxnId = string.Empty;

            try
            {
                Console.WriteLine($"[BBPS][PAY][START] UserId={userId}, BillerId={billerId}, Amount={amount}");

                // -------------------------------------------------
                // 0️⃣ Validate TPIN (BEFORE wallet hold)
                // -------------------------------------------------
                Console.WriteLine($"[BBPS][PAY] Validating TPIN for UserId={userId}");

                if (string.IsNullOrWhiteSpace(tpin))
                {
                    Console.WriteLine("[BBPS][PAY] TPIN missing");
                    throw new ApplicationException("TPIN is required");
                }

                var isValidTpin = await _userDataService.ValidateUserTpin(userId, tpin);

                if (!isValidTpin)
                {
                    Console.WriteLine($"[BBPS][PAY] Invalid TPIN for UserId={userId}");
                    throw new ApplicationException("Invalid TPIN");
                }

                Console.WriteLine("[BBPS][PAY] TPIN validated successfully");

                // -------------------------------------------------
                // 1️⃣ Ensure customer mobile (BBPS requirement)
                // -------------------------------------------------
                if (string.IsNullOrWhiteSpace(customerMobile))
                {
                    customerMobile = "9892506507"; // UAT fallback
                    Console.WriteLine("[BBPS][PAY] customerMobile missing, fallback applied");
                }

                // -------------------------------------------------
                // 2️⃣ Hold wallet amount
                // -------------------------------------------------
                walletTxnId = await _wallet.HoldAmount(
                    userId,
                    amount,
                    "BBPS Bill Payment"
                );

                Console.WriteLine($"[BBPS][PAY] Wallet hold success | WalletTxnId={walletTxnId}");

                var cfg = _cfg.GetSection("BillAvenue");
                requestId = BillAvenueRequestId.Generate();

                Console.WriteLine($"[BBPS][PAY] Generated RequestId={requestId}");

                long amountInPaise = (long)(amount * 100);
                Console.WriteLine($"[BBPS][PAY] AmountInPaise={amountInPaise}");

                // -------------------------------------------------
                // 3️⃣ Build Pay XML
                // -------------------------------------------------
                Console.WriteLine($"[BBPS][PAY] Building Pay XML | RequestId={requestId}");

                string xml = BillAvenueXmlBuilder.BuildAdhocPayXml(
                    instituteId: cfg["InstituteId"],
                    requestId: requestId,
                    agentId: cfg["AgentId"],
                    billerId: billerId,
                    inputParams: inputParams,
                    billerResponseJson: billerResponseJson,
                    amountInPaise: amountInPaise,
                    customerMobile: customerMobile
                );

                // -------------------------------------------------
                // 4️⃣ Encrypt
                // -------------------------------------------------
                Console.WriteLine($"[BBPS][PAY] Encrypting request | RequestId={requestId}");

                string encRequest =
                    BillAvenueCrypto.Encrypt(xml, cfg["WorkingKey"]);

                var form =
                    BuildCommonForm(cfg, requestId, encRequest);

                // -------------------------------------------------
                // 5️⃣ Call BillAvenue Pay API
                // -------------------------------------------------
                Console.WriteLine($"[BBPS][PAY] Calling BillAvenue Pay API | RequestId={requestId}");

                string rawResponse = await _client.PostFormAsync(
                    cfg["BaseUrl"] + cfg["PayUrl"],
                    form
                );

                // -------------------------------------------------
                // 6️⃣ Decrypt response
                // -------------------------------------------------
                Console.WriteLine($"[BBPS][PAY] Decrypting response | RequestId={requestId}");

                string decryptedXml =
                    BillAvenueCrypto.Decrypt(rawResponse, cfg["WorkingKey"]);

                // -------------------------------------------------
                // 7️⃣ Parse response
                // -------------------------------------------------
                var dto =
                    BillAvenueXmlParser.ParsePay(decryptedXml);

                Console.WriteLine($"[BBPS][PAY][RESPONSE] Status={dto.Status}, Code={dto.ResponseCode}");

                // -------------------------------------------------
                // 8️⃣ Save payment
                // -------------------------------------------------
                await _repo.SavePayment(
                     requestId,
                    dto.TxnRefId,
                    userId,
                    amount,
                    dto.Status,
                    dto.ResponseCode,
                    dto.ResponseMessage,
                    decryptedXml
                );

                // -------------------------------------------------
                // 9️⃣ Wallet finalize
                // -------------------------------------------------
                if (dto.Status == "SUCCESS")
                {
                    Console.WriteLine("[BBPS][PAY] Debit from hold");

                    await _wallet.DebitFromHold(
                        userId,
                        amount,
                        walletTxnId, // ✅ internal wallet reference
                        "BBPS Bill Payment"
                    );
                }
                else
                {
                    Console.WriteLine("[BBPS][PAY] Release hold due to failure");

                    await _wallet.ReleaseHold(
                        userId,
                        amount,
                        walletTxnId, // ✅ correct release reference
                        "BBPS Payment Failed"
                    );
                }

                Console.WriteLine($"[BBPS][PAY][END] RequestId={requestId}");
                return dto;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[BBPS][PAY][ERROR] RequestId={requestId} | {ex}");

                // -------------------------------------------------
                // Wallet safety rollback
                // -------------------------------------------------
                if (!string.IsNullOrEmpty(walletTxnId))
                {
                    try
                    {
                        Console.WriteLine("[BBPS][PAY] Releasing wallet hold due to exception");

                        await _wallet.ReleaseHold(
                            userId,
                            amount,
                            walletTxnId,   // ✅ FIXED
                            "BBPS Payment Exception"
                        );
                    }
                    catch (Exception walletEx)
                    {
                        Console.WriteLine($"[BBPS][PAY][ERROR] Wallet release failed | {walletEx}");
                    }
                }

                throw;
            }
        }

        // ---------------------------------------------------------
        // BILL VALIDATION
        // ---------------------------------------------------------
        public async Task<BbpsBillValidationResponseDto> ValidateBill(
            string billerId,
            Dictionary<string, string> inputParams)
        {
            var cfg = _cfg.GetSection("BillAvenue");
            string requestId = BillAvenueRequestId.Generate();

            // 1️⃣ Build XML
            string xml = BillAvenueXmlBuilder.BuildBillValidationXml(
                billerId,
                inputParams
            );

            // 2️⃣ Encrypt (MD5-based – SAME as Fetch/Pay)
            string encRequest =
                BillAvenueCrypto.Encrypt(xml, cfg["WorkingKey"]);

            // 3️⃣ URL
            string url =
                $"{cfg["BaseUrl"]}/billpay/extBillValCntrl/billValidationRequest/xml" +
                $"?accessCode={cfg["AccessCode"]}" +
                $"&requestId={requestId}" +
                $"&ver=2.0" +
                $"&instituteId={cfg["InstituteId"]}";

            // 4️⃣ POST RAW
            string rawResponse =
                await _client.PostRawAsync(
                    url,
                    encRequest,
                    "text/plain"
                );

            // 5️⃣ Decrypt
            string decryptedXml =
                BillAvenueCrypto.Decrypt(rawResponse, cfg["WorkingKey"]);

            return BillAvenueXmlParser.ParseBillValidation(decryptedXml);
        }

        // ---------------------------------------------------------
        // STATUS
        // ---------------------------------------------------------
        public async Task<BbpsStatusResponseDto> CheckStatus(
        string txnRefId,
        string billRequestId)
        {
            var cfg = _cfg.GetSection("BillAvenue");
            string requestId = BillAvenueRequestId.Generate();

            // 1️⃣ Build XML
            string xml = BillAvenueXmlBuilder.BuildStatusXmlByTxnRef(
                cfg["InstituteId"],
                requestId,
                txnRefId
            );

            // 2️⃣ Encrypt
            string encRequest =
                BillAvenueCrypto.Encrypt(xml, cfg["WorkingKey"]);

            var form =
                BuildCommonForm(cfg, requestId, encRequest);

            // 3️⃣ Call Status API
            string rawResponse = await _client.PostFormAsync(
                cfg["BaseUrl"] + cfg["StatusUrl"],
                form
            );

            // 4️⃣ Decrypt
            string decryptedXml =
                BillAvenueCrypto.Decrypt(rawResponse, cfg["WorkingKey"]);

            var dto =
                BillAvenueXmlParser.ParseStatus(decryptedXml);

            // 5️⃣ Update DB
            await _repo.UpdateStatus(
                txnRefId,
                billRequestId,
                dto.Status,
                decryptedXml
            );

            // 🔴 ADD THIS BLOCK (Wallet Finalization)
            if (dto.Status == "SUCCESS")
            {
                await _wallet.FinalizeIfPending(txnRefId);
            }
            else if (dto.Status == "FAILED")
            {
                await _wallet.RefundIfPending(txnRefId);
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
    }
}