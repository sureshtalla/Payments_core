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
            Dictionary<string, string> inputParams)
        {
            var cfg = _cfg.GetSection("BillAvenue");
            string requestId = BillAvenueRequestId.Generate();

            string xml = BillAvenueXmlBuilder.BuildFetchBillXml(
                  cfg["AgentId"],
                  billerId,
                  inputParams
              );

            // ✅ PHP-matched crypto
            string encRequest = BillAvenueCrypto.Encrypt(xml, cfg["WorkingKey"]);

            var form = BuildCommonForm(cfg, requestId, encRequest);

            string rawResponse = await _client.PostFormAsync(
             cfg["BaseUrl"] + cfg["FetchUrl"],
             BuildCommonForm(cfg, requestId, encRequest)
         );

            string decryptedXml =
                BillAvenueCrypto.Decrypt(rawResponse, cfg["WorkingKey"]);

            var dto = BillAvenueXmlParser.ParseFetch(decryptedXml);

            await _repo.SaveFetchBill(
                dto.BillRequestId,
                userId,
                billerId,
                dto.CustomerName,
                dto.BillAmount,
                dto.DueDate,
                dto.ResponseCode,
                dto.ResponseMessage,
                decryptedXml);

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
            var walletTxnId = await _wallet.HoldAmount(
                userId, amount, "BBPS Bill Payment");

            var cfg = _cfg.GetSection("BillAvenue");
            string requestId = BillAvenueRequestId.Generate();

            // ✅ Correct XML
            var xml = BillAvenueXmlBuilder.BuildPayBillXml(
                cfg["AgentId"],
                billerId,
                billRequestId,
                (long)(amount * 100)
            );

            string encRequest =
                BillAvenueCrypto.Encrypt(xml, cfg["WorkingKey"]);

            var form =
                BuildCommonForm(cfg, requestId, encRequest);

            string rawResponse = await _client.PostFormAsync(
                cfg["BaseUrl"] + cfg["PayUrl"],
                form
            );

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
                    userId, amount, walletTxnId, "BBPS Payment Failed");

            return dto;
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

            // 1️⃣ Build correct XML
            string xml =
                BillAvenueXmlBuilder.BuildStatusXmlByTxnRef(txnRefId);

            // 2️⃣ Encrypt FIRST
            string encRequest =
                BillAvenueCrypto.Encrypt(xml, cfg["WorkingKey"]);

            // 3️⃣ Build form
            var form =
                BuildCommonForm(cfg, requestId, encRequest);

            // 4️⃣ Call correct STATUS URL
            string rawResponse = await _client.PostFormAsync(
                cfg["BaseUrl"] + cfg["StatusUrl"],
                form
            );

            // 5️⃣ Decrypt
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
        //public async Task SyncBillers()
        //{
        //    var cfg = _cfg.GetSection("BillAvenue");
        //    string requestId = BillAvenueRequestId.GenerateForMDM();

        //    var xml = BillAvenueXmlBuilder.BuildBillerInfoRequest(
        //        cfg["InstituteId"], requestId, "1.0");

        //    string encRequest = BillAvenueCrypto.Encrypt(xml, cfg["WorkingKey"]);

        //    var form = new Dictionary<string, string>
        //    {
        //        { "accessCode", cfg["AccessCode"] },
        //        { "requestId", requestId },
        //        { "ver", "1.0" },
        //        { "instituteId", cfg["InstituteId"] },
        //        { "encRequest", encRequest }
        //    };

        //    string rawResponse = await _client.PostFormAsync(
        //        cfg["BaseUrl"] + cfg["MdmUrl"], form);


        //    string decryptedXml =
        //        BillAvenueCrypto.Decrypt(rawResponse, cfg["WorkingKey"]);

        //    var billers = BillAvenueXmlParser.ParseBillerInfo(decryptedXml);
        //    foreach (var biller in billers)
        //    {
        //        Console.WriteLine($"➡ Calling Upsert for {biller.BillerId}");
        //        await _repo.UpsertBiller(biller);
        //    }

        //    Console.WriteLine("===== FULL DECRYPTED XML =====");
        //    Console.WriteLine(decryptedXml);
        //    Console.WriteLine("===== END XML =====");
        //}

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

            // 1️⃣ Build XML
            string xml =
                BillAvenueXmlBuilder.BuildBillerParamsRequestXml(billerId);

            Console.WriteLine("========== BBPS MDM BILLER PARAMS ==========");
            Console.WriteLine($"RequestId  : {requestId}");
            Console.WriteLine($"BillerId   : {billerId}");
            Console.WriteLine("Request XML:");
            Console.WriteLine(xml);

            // 2️⃣ Encrypt
            //string encRequest =
            //    BillAvenueCrypto.Encrypt(xml, cfg["WorkingKey"]);

            string encRequest =
             BillAvenueCrypto.EncryptForMdm(xml, cfg["WorkingKey"]);

            Console.WriteLine("Encrypted Request (HEX):");
            Console.WriteLine(encRequest);

            // 3️⃣ Build FORM
            var form = new Dictionary<string, string>
            {
                { "accessCode", cfg["AccessCode"] },
                { "requestId", requestId },
                { "ver", cfg["Version"] },
                { "instituteId", cfg["InstituteId"] },
                { "encRequest", encRequest }
            };

            string rawResponse = null;
            string decryptedXml = null;

            try
            {
                // 4️⃣ POST FORM
                rawResponse = await _client.PostFormAsync(
                    cfg["BaseUrl"] + cfg["MdmUrl"],
                    form
                );

                Console.WriteLine("Raw Response:");
                Console.WriteLine(rawResponse);

                // 5️⃣ Decrypt
                decryptedXml =
                BillAvenueCrypto.LooksLikeHex(rawResponse)
                    ? BillAvenueCrypto.Decrypt(rawResponse, cfg["WorkingKey"]) // MD5-based
                    : rawResponse;

                //decryptedXml =
                //BillAvenueCrypto.LooksLikeHex(rawResponse)
                //    ? BillAvenueCrypto.DecryptForMdm(rawResponse, cfg["WorkingKey"])
                //    : rawResponse;

                Console.WriteLine("Decrypted XML:");
                Console.WriteLine(decryptedXml);

                // 6️⃣ Validate response
                //if (!decryptedXml.Contains("<responseCode>000</responseCode>"))
                //{
                //    Console.WriteLine("❌ MDM FAILED: responseCode is not 000");
                //    throw new Exception("BBPS MDM (Biller Params) failed");
                //}

                if (!decryptedXml.Contains("<responseCode>000</responseCode>"))
                {
                    Console.WriteLine("⚠️ MDM params not available for this biller. Falling back to Fetch Bill.");
                    return new List<BbpsBillerInputParamDto>();
                }

                Console.WriteLine("✅ MDM SUCCESS");

                // 7️⃣ Parse params
                return BillAvenueXmlParser.ParseBillerInputParams(decryptedXml);
            }
            catch (Exception ex)
            {
                Console.WriteLine("🔥 EXCEPTION IN BBPS MDM");
                Console.WriteLine($"Message: {ex.Message}");
                Console.WriteLine("Raw/Decrypted Response:");
                Console.WriteLine(decryptedXml ?? rawResponse);
                throw;
            }
            finally
            {
                Console.WriteLine("========== END BBPS MDM ==========\n");
            }
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