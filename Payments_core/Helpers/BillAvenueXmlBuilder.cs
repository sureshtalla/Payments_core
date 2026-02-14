using Newtonsoft.Json;
using Payments_core.Models.BBPS;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;

namespace Payments_core.Helpers
{
    public static class BillAvenueXmlBuilder
    {
        // =========================
        // FETCH BILL (NPCI FINAL)
        // =========================
        //public static string BuildFetchBillXml(
        //    string instituteId,
        //    string agentId,
        //    string requestId,
        //    string billerId,
        //    Dictionary<string, string> inputParams,
        //    AgentDeviceInfo deviceInfo,
        //    CustomerInfo customerInfo)
        //  {
        //      var doc = new XDocument(
        //          new XElement("billFetchRequest",
        //              new XElement("instituteId", instituteId),
        //              new XElement("agentId", agentId),


        //              new XElement("agentDeviceInfo",
        //                  new XElement("ip", deviceInfo.Ip),
        //                  new XElement("initChannel", deviceInfo.InitChannel),
        //                  new XElement("mac", deviceInfo.Mac)
        //              ),

        //              new XElement("customerInfo",
        //                  new XElement("customerMobile", customerInfo.CustomerMobile),
        //                  new XElement("customerEmail", customerInfo.CustomerEmail)
        //              ),

        //              new XElement("requestId", requestId),
        //              new XElement("billerId", billerId),

        //              new XElement("inputParams",
        //                  inputParams.Select(kv =>
        //                      new XElement("input",
        //                          new XElement("paramName", kv.Key),
        //                          new XElement("paramValue", kv.Value)
        //                      )
        //                  )
        //              )
        //          )
        //      );

        //      return doc.ToString(SaveOptions.DisableFormatting);
        //  }

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

                    new XElement("agentId", agentId),

                    new XElement("agentDeviceInfo",
                        new XElement("ip", deviceInfo.Ip),
                        new XElement("initChannel", deviceInfo.InitChannel),
                        new XElement("mac", deviceInfo.Mac)
                    ),

                    new XElement("customerInfo",
                        new XElement("customerMobile", customerInfo.CustomerMobile ?? ""),
                        new XElement("customerEmail", customerInfo.CustomerEmail ?? "")
                    ),

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
     JsonElement billerResponse,
     JsonElement? additionalInfo,   // 🔥 NEW
     long amountInPaise,
     string amountTag,
     string customerMobile,
     AgentDeviceInfo deviceInfo
 )
        {
            var inputElements = new XElement("inputParams");

            foreach (var kv in inputParams)
            {
                inputElements.Add(
                    new XElement("input",
                        new XElement("paramName", kv.Key),
                        new XElement("paramValue", kv.Value)
                    )
                );
            }

            var billerResponseXml = new XElement("billerResponse");

            void Add(string name)
            {
                if (billerResponse.TryGetProperty(name, out var val))
                {
                    var v = val.ToString();
                    if (!string.IsNullOrWhiteSpace(v))
                        billerResponseXml.Add(new XElement(name, v));
                }
            }

            Add("billAmount");
            Add("billDate");
            Add("billNumber");
            Add("billPeriod");
            Add("customerName");
            Add("dueDate");

            // amountOptions
            if (billerResponse.TryGetProperty("amountOptions", out var amountOptions))
            {
                var amountOptionsXml = new XElement("amountOptions");

                JsonElement optionsArray;

                if (amountOptions.ValueKind == JsonValueKind.Object &&
                    amountOptions.TryGetProperty("option", out optionsArray))
                {
                    foreach (var option in optionsArray.EnumerateArray())
                    {
                        amountOptionsXml.Add(
                            new XElement("option",
                                new XElement("amountName",
                                    option.GetProperty("amountName").ToString()),
                                new XElement("amountValue",
                                    option.GetProperty("amountValue").ToString())
                            )
                        );
                    }
                }
                else if (amountOptions.ValueKind == JsonValueKind.Array)
                {
                    foreach (var option in amountOptions.EnumerateArray())
                    {
                        amountOptionsXml.Add(
                            new XElement("option",
                                new XElement("amountName",
                                    option.GetProperty("amountName").ToString()),
                                new XElement("amountValue",
                                    option.GetProperty("amountValue").ToString())
                            )
                        );
                    }
                }

                billerResponseXml.Add(amountOptionsXml);
            }

            // 🔥 ADDITIONAL INFO BLOCK (CRITICAL FIX)
            XElement additionalInfoXml = null;

            if (additionalInfo.HasValue &&
                additionalInfo.Value.ValueKind == JsonValueKind.Object &&
                additionalInfo.Value.TryGetProperty("info", out var infoArray))
            {
                additionalInfoXml = new XElement("additionalInfo");

                foreach (var info in infoArray.EnumerateArray())
                {
                    additionalInfoXml.Add(
                        new XElement("info",
                            new XElement("infoName",
                                info.GetProperty("infoName").ToString()),
                            new XElement("infoValue",
                                info.GetProperty("infoValue").ToString())
                        )
                    );
                }
            }

            var doc = new XDocument(
                new XElement("billPaymentRequest",

                    new XElement("agentId", agentId),
                    new XElement("billerAdhoc", "true"),

                    new XElement("agentDeviceInfo",
                        new XElement("ip", deviceInfo.Ip),
                        new XElement("initChannel", deviceInfo.InitChannel),
                        new XElement("mac", deviceInfo.Mac)
                    ),

                    new XElement("customerInfo",
                        new XElement("REMITTER_NAME", "ABCABC"),
                        new XElement("customerMobile", customerMobile),
                        new XElement("customerEmail", "kishor.anand@avenues.info"),
                        new XElement("customerAdhaar", "548550008000"),
                        new XElement("customerPan", "HJAUI4588H")
                    ),

                    new XElement("billerId", billerId),
                    inputElements,
                    billerResponseXml,
                    additionalInfoXml,   // 🔥 ADD HERE

                    new XElement("paymentRefId", requestId),

                    new XElement("amountInfo",
                        new XElement("amount", amountInPaise),
                        new XElement("currency", "356"),
                        new XElement("custConvFee", "0"),

                        string.IsNullOrWhiteSpace(amountTag)
                            ? null
                            : new XElement("amountTags",
                                new XElement("amountTag", amountTag),
                                new XElement("value", amountInPaise)
                            )
                    ),

                    new XElement("paymentMethod",
                        new XElement("paymentMode", "Cash"),
                        new XElement("quickPay", "N"),
                        new XElement("splitPay", "N")
                    ),

                    new XElement("paymentInfo",
                        new XElement("info",
                            new XElement("infoName", "Remarks"),
                            new XElement("infoValue", "Received")
                        )
                    )
                )
            );

            return doc.ToString(SaveOptions.DisableFormatting);
        }
        // =========================
        // REGULAR PAY (WITH billRequestId)
        // =========================
        public static string BuildRegularPayXml(
         string instituteId,
         string requestId,
         string agentId,
         string billRequestId,
         long amountInPaise,
         string customerMobile,
         AgentDeviceInfo deviceInfo
     )
        {
            var doc = new XDocument(
                new XElement("billPaymentRequest",

                    new XElement("agentId", agentId),

                    new XElement("agentDeviceInfo",
                        new XElement("ip", deviceInfo.Ip),
                        new XElement("initChannel", deviceInfo.InitChannel),
                        new XElement("mac", deviceInfo.Mac)
                    ),

                    new XElement("customerInfo",
                        new XElement("customerMobile", customerMobile),
                        new XElement("customerEmail", "")
                    ),

                    new XElement("billRequestId", billRequestId),

                    new XElement("paymentRefId", Guid.NewGuid().ToString("N")),

                    new XElement("amountInfo",
                        new XElement("amount", amountInPaise),
                        new XElement("currency", "356"),
                        new XElement("custConvFee", "0")
                    ),

                    new XElement("paymentMethod",
                        new XElement("paymentMode", "Wallet"),
                        new XElement("quickPay", "N"),
                        new XElement("splitPay", "N")
                    ),

                    new XElement("paymentInfo",
                        new XElement("info",
                            new XElement("infoName", "Payment Account Info"),
                            new XElement("infoValue", "Wallet")
                        )
                    )
                )
            );

            return doc.ToString(SaveOptions.DisableFormatting);
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

        public static string BuildStandardPayXml(
            string agentId,
            string billerId,
            Dictionary<string, string> inputParams,
            JsonElement billerResponse,
            long amountInPaise,
            string customerMobile,
            AgentDeviceInfo deviceInfo,
            string paymentMode,
            string paymentAccountInfo
        )
        {
            string GetSafe(string name)
            {
                return billerResponse.TryGetProperty(name, out var val)
                    ? val.ToString()
                    : "";
            }

            var inputElements = new XElement("inputParams");

            foreach (var kv in inputParams)
            {
                inputElements.Add(
                    new XElement("input",
                        new XElement("paramName", kv.Key),
                        new XElement("paramValue", kv.Value)
                    )
                );
            }

            var billerResponseXml = new XElement("billerResponse",
                new XElement("billAmount", GetSafe("billAmount")),
                new XElement("billDate", GetSafe("billDate")),
                new XElement("billNumber", GetSafe("billNumber")),
                new XElement("billPeriod", GetSafe("billPeriod")),
                new XElement("customerName", GetSafe("customerName")),
                new XElement("dueDate", GetSafe("dueDate"))
            );

            var doc = new XDocument(
                new XElement("billPaymentRequest",

                    new XElement("agentId", agentId),

                    new XElement("agentDeviceInfo",
                        new XElement("ip", deviceInfo.Ip),
                        new XElement("initChannel", deviceInfo.InitChannel),
                        new XElement("mac", deviceInfo.Mac)
                    ),

                    new XElement("customerInfo",
                        new XElement("REMITTER_NAME", "ABC"),   // pass dynamically later
                        new XElement("customerMobile", customerMobile),
                        new XElement("customerEmail", "kishor.anand@avenues.info"),
                        new XElement("customerAdhaar", "548550008000"),
                        new XElement("customerPan", "HJAUI4588H")
                    ),

                    new XElement("billerId", billerId),

                    inputElements,

                    billerResponseXml,

                    new XElement("paymentRefId", Guid.NewGuid().ToString("N")),

                    new XElement("amountInfo",
                        new XElement("amount", amountInPaise),
                        new XElement("currency", "356"),
                        new XElement("custConvFee", "0")
                    ),

                    new XElement("paymentMethod",
                        new XElement("paymentMode", paymentMode),
                        new XElement("quickPay", "N"),
                        new XElement("splitPay", "N")
                    ),

                    new XElement("paymentInfo",
                        new XElement("info",
                            new XElement("infoName", "Payment Account Info"),
                            new XElement("infoValue", paymentAccountInfo)
                        )
                    )
                )
            );

            return doc.ToString(SaveOptions.DisableFormatting);
        }

    }
}