using System.Collections.Generic;
using System.Xml.Linq;

namespace Payments_core.Helpers
{
    public static class BillAvenueXmlBuilder
    {
        // =========================
        // FETCH BILL (NPCI SAFE)
        // =========================
        public static string BuildFetchBillXml(
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
                new XElement("billFetchRequest",
                    new XElement("agentId", agentId),
                    new XElement("billerId", billerId),
                    inputs
                )
            );

            return doc.ToString(SaveOptions.DisableFormatting);
        }

        // =========================
        // PAY BILL (MINIMAL – YOU WILL EXTEND)
        // =========================
        public static string BuildPayBillXml(
            string agentId,
            string billerId,
            string billRequestId,
            long amountInPaise)
        {
            var doc = new XDocument(
                new XElement("billPaymentRequest",
                    new XElement("agentId", agentId),
                    new XElement("billerId", billerId),
                    new XElement("billRequestId", billRequestId),
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
        // STATUS (NPCI CORRECT)
        // =========================
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

        // =========================
        // MDM (CORRECT)
        // =========================
        public static string BuildBillerInfoRequest(string billerId)
        {
            return
                "<?xml version=\"1.0\" encoding=\"UTF-8\"?>" +
                "<billerInfoRequest>" +
                $"<billerId>{billerId}</billerId>" +
                "</billerInfoRequest>";
        }


        //public static string BuildBillerParamsRequestXml(string billerId)
        //{
        //    return
        //        "<?xml version=\"1.0\" encoding=\"UTF-8\"?>" +
        //        "<billerInfoRequest>" +
        //        $"<billerId>{billerId}</billerId>" +
        //        "</billerInfoRequest>";
        //}

        public static string BuildBillerParamsRequestXml(string billerId)
        {
            // ❌ NO XML DECLARATION FOR MDM
            return
                "<billerInfoRequest>" +
                $"<billerId>{billerId}</billerId>" +
                "</billerInfoRequest>";
        }
    }
}