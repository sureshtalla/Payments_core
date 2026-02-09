using System.Collections.Generic;
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
            string requestId,
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
                new XElement("billFetchRequest",
                    new XElement("instituteId", instituteId),
                    new XElement("requestId", requestId),
                    new XElement("billerId", billerId),
                    inputs
                )
            );

            return doc.ToString(SaveOptions.DisableFormatting);
        }

        // =========================
        // PAY BILL (NPCI FINAL)
        // =========================
        public static string BuildPayBillXml(
            string instituteId,
            string requestId,
            string billRequestId,
            long amountInPaise,
            string agentId)
        {
            var doc = new XDocument(
                new XElement("billPaymentRequest",
                    new XElement("instituteId", instituteId),
                    new XElement("requestId", requestId),
                    new XElement("billRequestId", billRequestId),
                    new XElement("agentId", agentId),
                    new XElement("amountInfo",
                        new XElement("amount", amountInPaise),
                        new XElement("currency", "356"),
                        new XElement("custConvFee", "0")
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

    }
}