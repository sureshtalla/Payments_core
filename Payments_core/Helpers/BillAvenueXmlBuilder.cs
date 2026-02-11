using Newtonsoft.Json;
using Payments_core.Models.BBPS;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;

namespace Payments_core.Helpers
{
    public static class BillAvenueXmlBuilder
    {
        // =========================
        // FETCH BILL (NPCI FINAL)
        // =========================
      public static string BuildFetchBillXml(
          string instituteId,
          string agentId,
          string requestId,
          string billerId,
          Dictionary<string, string> inputParams,
          AgentDeviceInfo deviceInfo,
          CustomerInfo customerInfo)
        {
            var doc = new XDocument(
                new XElement("billFetchRequest",
                    new XElement("instituteId", instituteId),
                    new XElement("agentId", agentId),

                    // ✅ REQUIRED FOR FASTag
                    new XElement("agentDeviceInfo",
                        new XElement("ip", deviceInfo.Ip),
                        new XElement("initChannel", deviceInfo.InitChannel),
                        new XElement("mac", deviceInfo.Mac)
                    ),

                    new XElement("customerInfo",
                        new XElement("customerMobile", customerInfo.CustomerMobile),
                        new XElement("customerEmail", customerInfo.CustomerEmail)
                    ),

                    new XElement("requestId", requestId),
                    new XElement("billerId", billerId),

                    new XElement("inputParams",
                        inputParams.Select(kv =>
                            new XElement("input",
                                new XElement("paramName", kv.Key),
                                new XElement("paramValue", kv.Value)
                            )
                        )
                    )
                )
            );

            return doc.ToString(SaveOptions.DisableFormatting);
        }

        // =========================
        // PAY BILL (NPCI FINAL)
        // =========================
        //public static string BuildPayBillXml(
        //        string instituteId,
        //        string requestId,
        //        string billRequestId,
        //        long amountInPaise,
        //        string agentId,
        //        string customerMobile
        //    )
        //{
        //    var doc = new XDocument(
        //        new XElement("billPaymentRequest",
        //            new XElement("instituteId", instituteId),
        //            new XElement("requestId", requestId),
        //            new XElement("billRequestId", billRequestId),
        //            new XElement("agentId", agentId),
        //            new XElement("amountInfo",
        //                new XElement("amount", amountInPaise),
        //                new XElement("currency", "356"),
        //                new XElement("custConvFee", "0")
        //            )
        //        )
        //    );

        //    return doc.ToString(SaveOptions.DisableFormatting);
        //}

        public static string BuildAdhocPayXml(
            string instituteId,
            string requestId,
            string agentId,
            string billerId,
            Dictionary<string, string> inputParams,
            string billerResponseJson,
            long amountInPaise,
            string customerMobile
        )
                {
                    var billerResponse = JsonConvert.DeserializeObject<dynamic>(billerResponseJson);

                    var inputXml = new StringBuilder();
                    foreach (var item in inputParams)
                    {
                        inputXml.Append($@"
                    <input>
                        <paramName>{item.Key}</paramName>
                        <paramValue>{item.Value}</paramValue>
                    </input>");
                    }

                    string xml = $@"
        <billPaymentRequest>
            <agentId>{agentId}</agentId>
            <agentDeviceInfo>
                <ip>192.168.2.73</ip>
                <initChannel>AGT</initChannel>
                <mac>01-23-45-67-89-ab</mac>
            </agentDeviceInfo>

            <billerAdhoc>true</billerAdhoc>
            <billerId>{billerId}</billerId>

            <customerInfo>
                <customerMobile>{customerMobile}</customerMobile>
            </customerInfo>

            <inputParams>
                {inputXml}
            </inputParams>

            <billerResponse>
                <billAmount>{billerResponse.billAmount}</billAmount>
                <billDate>{billerResponse.billDate}</billDate>
                <billNumber>{billerResponse.billNumber}</billNumber>
                <billPeriod>{billerResponse.billPeriod}</billPeriod>
                <customerName>{billerResponse.customerName}</customerName>
                <dueDate>{billerResponse.dueDate}</dueDate>
            </billerResponse>

            <paymentRefId>{Guid.NewGuid().ToString("N")}</paymentRefId>

            <amountInfo>
                <amount>{amountInPaise}</amount>
                <currency>356</currency>
                <custConvFee>0</custConvFee>
            </amountInfo>

            <paymentMethod>
                <paymentMode>Cash</paymentMode>
                <quickPay>N</quickPay>
                <splitPay>N</splitPay>
            </paymentMethod>
        </billPaymentRequest>";

                    return xml;
         }

        // =========================
        // STATUS (NPCI FINAL)
        // =========================
        public static string BuildStatusXmlByTxnRef(
            string instituteId,
            string requestId,
            string txnRefId)
        {
            var doc = new XDocument(
                new XElement("transactionStatusReq",
                    new XElement("instituteId", instituteId),
                    new XElement("requestId", requestId),
                    new XElement("trackType", "TRANS_REF_ID"),
                    new XElement("trackValue", txnRefId)
                )
            );

            return doc.ToString(SaveOptions.DisableFormatting);
        }

        // =========================
        // MDM (LEAVE AS-IS)
        // =========================
        public static string BuildBillerInfoRequest(string billerId)
        {
            return
                "<billerInfoRequest>" +
                $"<billerId>{billerId}</billerId>" +
                "</billerInfoRequest>";
        }


        public static string BuildBillerParamsRequestXml(string billerId)
        {
            return
                "<billerInfoRequest>" +
                $"<billerId>{billerId}</billerId>" +
                "</billerInfoRequest>";
        }

        // =========================
        // BILL VALIDATION (NPCI)
        // =========================
        public static string BuildBillValidationXml(
            string billerId,
            Dictionary<string, string> inputParams)
        {
            var inputs = new XElement("inputParams");

            foreach (var kv in inputParams)
            {
                inputs.Add(
                    new XElement("input",
                        new XElement("paramName", kv.Key),
                        new XElement("paramValue", kv.Value)
                    )
                );
            }

            var doc = new XDocument(
                new XElement("billValidationRequest",
                    new XElement("billerId", billerId),
                    inputs
                )
            );

            return doc.ToString(SaveOptions.DisableFormatting);
        }
    }
}