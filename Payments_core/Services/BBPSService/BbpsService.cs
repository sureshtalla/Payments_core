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

namespace Payments_core.Services.BBPSService
{
    public class BbpsService : IBbpsService
    {
        private readonly IConfiguration _cfg;
        private readonly IBillAvenueClient _client;
        private readonly IBbpsRepository _repo;
        private readonly IWalletService _wallet;

        public BbpsService(
            IConfiguration cfg,
            IBillAvenueClient client,
            IBbpsRepository repo,
            IWalletService wallet)
        {
            _cfg = cfg;
            _client = client;
            _repo = repo;
            _wallet = wallet;
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

              // 1️⃣ Build NPCI-safe XML
                string xml = BillAvenueXmlBuilder.BuildFetchBillXml(
                cfg["InstituteId"],
                cfg["AgentId"],
                requestId,
                billerId,
                inputParams,
                agentDeviceInfo,
                customerInfo      
            );

            // 2️⃣ Encrypt (STANDARD)
            string encRequest =
                BillAvenueCrypto.Encrypt(xml, cfg["WorkingKey"]);

            // 3️⃣ Build form
            var form =
                BuildCommonForm(cfg, requestId, encRequest);

            // 4️⃣ Call Fetch API
            string rawResponse = await _client.PostFormAsync(
                cfg["BaseUrl"] + cfg["FetchUrl"],
                form
            );

            // 5️⃣ Decrypt
            string decryptedXml =
                BillAvenueCrypto.Decrypt(rawResponse, cfg["WorkingKey"]);

            // 6️⃣ Parse
            var dto =
                BillAvenueXmlParser.ParseFetch(decryptedXml);

            // 7️⃣ Persist
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

            return dto;
        }

        // ---------------------------------------------------------
        // PAY BILL
        // ---------------------------------------------------------
        public async Task<BbpsPayResponseDto> PayBill(
         long userId,
         string billerId,
         string billRequestId,
         decimal amount,
         string tpin)
        {
            var walletTxnId =
                         await _wallet.HoldAmount(userId, amount, "BBPS Bill Payment");

            var cfg = _cfg.GetSection("BillAvenue");
            string requestId = BillAvenueRequestId.Generate();

            long amountInPaise = (long)(amount * 100);

            // 1️⃣ Build NPCI-safe XML
            string xml = BillAvenueXmlBuilder.BuildPayBillXml(
                cfg["InstituteId"],
                requestId,
                billRequestId,
                amountInPaise,
                cfg["AgentId"]
            );

            // 2️⃣ Encrypt
            string encRequest =
                BillAvenueCrypto.Encrypt(xml, cfg["WorkingKey"]);

            var form =
                BuildCommonForm(cfg, requestId, encRequest);

            // 3️⃣ Call Pay API
            string rawResponse = await _client.PostFormAsync(
                cfg["BaseUrl"] + cfg["PayUrl"],
                form
            );

            // 4️⃣ Decrypt
            string decryptedXml =
                BillAvenueCrypto.Decrypt(rawResponse, cfg["WorkingKey"]);

            var dto =
                BillAvenueXmlParser.ParsePay(decryptedXml);

            await _repo.SavePayment(
               billRequestId,
               dto.TxnRefId,
               userId,          
               amount,
               dto.Status,
               dto.ResponseCode,
               dto.ResponseMessage,
               decryptedXml
           );

            if (dto.Status == "SUCCESS")
                await _wallet.DebitFromHold(
                    userId, amount, dto.TxnRefId, "BBPS Bill Payment");
            else
                await _wallet.ReleaseHold(
                    userId, amount, dto.TxnRefId, "BBPS Payment Failed");

            return dto;
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

            await _repo.UpdateStatus(
                txnRefId,
                billRequestId,
                dto.Status,
                decryptedXml
            );

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