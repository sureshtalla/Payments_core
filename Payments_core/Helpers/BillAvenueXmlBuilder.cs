using Payments_core.Models.BBPS;
using System.Collections.Generic;
using System.Text.Json;
using System.Xml.Linq;

namespace Payments_core.Helpers
{
    public static class BillAvenueXmlBuilder
    {
        // =============================================================
        // FETCH BILL
        // =============================================================
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

        // =============================================================
        // ADHOC PAY  (billerAdhoc = true)
        // =============================================================
        public static string BuildAdhocPayXml(
            string instituteId,
            string requestId,
            string agentId,
            string billerId,
            string remitterName,
            Dictionary<string, string> inputParams,
            JsonElement billerResponse,
            JsonElement? additionalInfo,
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

            void AddIfPresent(string name)
            {
                if (billerResponse.TryGetProperty(name, out var val))
                {
                    var v = val.ToString();
                    if (!string.IsNullOrWhiteSpace(v))
                        billerResponseXml.Add(new XElement(name, v));
                }
            }

            billerResponseXml.Add(new XElement("billAmount", amountInPaise));
            AddIfPresent("billDate");
            AddIfPresent("billNumber");
            AddIfPresent("billPeriod");
            AddIfPresent("customerName");
            AddIfPresent("dueDate");

            if (billerResponse.TryGetProperty("amountOptions", out var amountOptions))
            {
                var amountOptionsXml = new XElement("amountOptions");

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
                    amountOptionsXml.Add(
                        new XElement("option",
                            new XElement("amountName",
                                option.GetProperty("amountName").ToString()),
                            new XElement("amountValue",
                                option.GetProperty("amountValue").ToString())
                        )
                    );
                }

                billerResponseXml.Add(amountOptionsXml);
            }

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

            string effectiveAmountTag = string.IsNullOrWhiteSpace(amountTag)
                ? "BASE_BILL_AMOUNT"
                : amountTag;

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
                        new XElement("customerMobile", customerMobile)
                    ),

                    new XElement("billerId", billerId),
                    inputElements,
                    billerResponseXml,
                    additionalInfoXml,

                    new XElement("amountInfo",
                        new XElement("amount", amountInPaise),
                        new XElement("currency", "356"),
                        new XElement("custConvFee", "0"),
                        new XElement("amountTags",
                            new XElement("amountTag", effectiveAmountTag),
                            new XElement("value", amountInPaise.ToString())
                        )
                    ),

                    new XElement("paymentMethod",
                        new XElement("paymentMode", "Cash"),
                        new XElement("quickPay", "N"),
                        new XElement("splitPay", "N")
                    ),

                    new XElement("paymentInfo",
                        new XElement("info",
                            new XElement("infoName", "Remitter Name"),
                            new XElement("infoValue", remitterName)
                        ),
                        new XElement("info",
                            new XElement("infoName", "Payment Account Info"),
                            new XElement("infoValue", "Cash Payment")
                        ),
                        new XElement("info",
                            new XElement("infoName", "PaymentRefId"),
                            new XElement("infoValue", requestId)
                        ),
                        new XElement("info",
                            new XElement("infoName", "Payment mode"),
                            new XElement("infoValue", "Cash")
                        )
                    )
                )
            );

            return doc.ToString(SaveOptions.DisableFormatting);
        }

        // =============================================================
        // REGULAR PAY  (billerAdhoc = false)
        // FIX Bug 1: added fetchBillAmountPaise parameter — billerResponse
        // in the payment XML must contain the original paise value that
        // BillAvenue stored during fetch, not the user-selected amount.
        // Without this BillAvenue returns E211 billerResponse value mismatch.
        // =============================================================
        public static string BuildRegularPayXml(
            string agentId,
            string billerId,
            string billRequestId,
            string paymentRefId,
            string remitterName,
            long amount,
            long fetchBillAmountPaise,
            string customerMobile,
            AgentDeviceInfo deviceInfo,
            Dictionary<string, string>? inputParams,
            JsonElement? billerResponse
        )
        {
            var inputElements = new XElement("inputParams");
            if (inputParams != null)
                foreach (var kv in inputParams)
                    inputElements.Add(new XElement("input",
                        new XElement("paramName", kv.Key),
                        new XElement("paramValue", kv.Value)));

            // FIX Bug 1: use fetchBillAmountPaise here, not amount.
            // BillAvenue validates this value against their stored fetch record.
            var billerResponseXml = new XElement("billerResponse",
                new XElement("billAmount", fetchBillAmountPaise));

            if (billerResponse.HasValue)
            {
                void AddIfPresent(string name)
                {
                    if (billerResponse.Value.TryGetProperty(name, out var val))
                    {
                        var v = val.ToString();
                        if (!string.IsNullOrWhiteSpace(v))
                            billerResponseXml.Add(new XElement(name, v));
                    }
                }
                AddIfPresent("billDate");
                AddIfPresent("billNumber");
                AddIfPresent("billPeriod");
                AddIfPresent("customerName");
                AddIfPresent("dueDate");
            }

            var doc = new XDocument(
                new XElement("billPaymentRequest",

                    new XElement("agentId", agentId),
                    new XElement("billerAdhoc", "false"),

                    new XElement("agentDeviceInfo",
                        new XElement("ip", deviceInfo.Ip),
                        new XElement("initChannel", deviceInfo.InitChannel),
                        new XElement("mac", deviceInfo.Mac)
                    ),

                    new XElement("customerInfo",
                        new XElement("customerMobile", customerMobile)
                    ),

                    new XElement("billerId", billerId),
                    inputElements,
                    billerResponseXml,

                    new XElement("amountInfo",
                        new XElement("amount", amount),
                        new XElement("currency", "356"),
                        new XElement("custConvFee", "0")
                    ),

                    new XElement("paymentMethod",
                        new XElement("paymentMode", "Cash"),
                        new XElement("quickPay", "N"),
                        new XElement("splitPay", "N")
                    ),

                    new XElement("paymentInfo",
                        new XElement("info",
                            new XElement("infoName", "Remitter Name"),
                            new XElement("infoValue", remitterName)
                        ),
                        new XElement("info",
                            new XElement("infoName", "Payment Account Info"),
                            new XElement("infoValue", "Cash Payment")
                        ),
                        new XElement("info",
                            new XElement("infoName", "PaymentRefId"),
                            new XElement("infoValue", paymentRefId)
                        ),
                        new XElement("info",
                            new XElement("infoName", "Payment mode"),
                            new XElement("infoValue", "Cash")
                        )
                    )
                )
            );

            return doc.ToString(SaveOptions.DisableFormatting);
        }

        // =============================================================
        // TRANSACTION STATUS CHECK
        // =============================================================
        public static string BuildStatusXmlByTxnRef(string txnRefId)
        {
            var doc = new XDocument(
                new XElement("transactionStatusReq",
                    new XElement("trackType", "TRANS_REF_ID"),
                    new XElement("trackValue", txnRefId)
                )
            );

            return doc.ToString(SaveOptions.DisableFormatting);
        }

        // =============================================================
        // MDM — BILLER INFO REQUEST
        // =============================================================
        public static string BuildBillerInfoRequest(string billerId)
        {
            return
                "<billerInfoRequest>" +
                $"<billerId>{billerId}</billerId>" +
                "</billerInfoRequest>";
        }

        // =============================================================
        // MDM — BILLER PARAMS REQUEST
        // =============================================================
        public static string BuildBillerParamsRequestXml(string billerId)
        {
            return
                "<billerInfoRequest>" +
                $"<billerId>{billerId}</billerId>" +
                "</billerInfoRequest>";
        }

        // =============================================================
        // BILL VALIDATION
        // =============================================================
        public static string BuildBillValidationXml(
            string agentId,
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
                    new XElement("agentId", agentId),
                    new XElement("billerId", billerId),
                    inputs
                )
            );

            return doc.ToString(SaveOptions.DisableFormatting);
        }

        // =============================================================
        // DEPOSIT ENQUIRY
        // =============================================================
        public static string BuildDepositEnquiryXml(string agentId)
        {
            return
                "<depositDetailsRequest>" +
                "<agents>" +
                $"<agentId>{agentId}</agentId>" +
                "</agents>" +
                "</depositDetailsRequest>";
        }
    }
}