using Payments_core.Models;
using Payments_core.Models.BBPS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace Payments_core.Helpers
{
    public static class BillAvenueXmlParser
    {
        // ---------------- FETCH ----------------
        public static BbpsFetchResponseDto ParseFetch(string xml)
        {
            var doc = XDocument.Parse(xml);
            var root = doc.Root;

            var responseCode = root.Element("responseCode")?.Value;

            if (responseCode != "000")
            {
                var errorMessage =
                    root.Element("errorInfo")
                        ?.Element("error")
                        ?.Element("errorMessage")
                        ?.Value;

                return new BbpsFetchResponseDto
                {
                    ResponseCode = responseCode,
                    ResponseMessage = errorMessage ?? "Fetch failed"
                };
            }

            var billerResponseElement = root.Element("billerResponse");

            var inputParams = root
                .Element("inputParams")?
                .Elements("input")
                .Select(x => new InputParamDto
                {
                    ParamName = x.Element("paramName")?.Value,
                    ParamValue = x.Element("paramValue")?.Value
                })
                .ToList();

            var additionalInfo = root
                .Element("additionalInfo")?
                .Elements("info")
                .Select(x => new AdditionalInfoDto
                {
                    InfoName = x.Element("infoName")?.Value,
                    InfoValue = x.Element("infoValue")?.Value
                })
                .ToList();

            List<AmountOptionDto> amountOptions = null;
            var amountOptionsElement = billerResponseElement?.Element("amountOptions");
            if (amountOptionsElement != null)
            {
                amountOptions = amountOptionsElement
                    .Elements("option")
                    .Select(o => new AmountOptionDto
                    {
                        AmountName = o.Element("amountName")?.Value,
                        AmountValue = o.Element("amountValue")?.Value
                    })
                    .ToList();
            }

            var billerResponse = new BillerResponseDto
            {
                BillAmount = ConvertPaiseToRupees(billerResponseElement?.Element("billAmount")?.Value),
                BillDate = billerResponseElement?.Element("billDate")?.Value,
                BillNumber = billerResponseElement?.Element("billNumber")?.Value,
                BillPeriod = billerResponseElement?.Element("billPeriod")?.Value,
                CustomerName = billerResponseElement?.Element("customerName")?.Value,
                DueDate = billerResponseElement?.Element("dueDate")?.Value,
                AmountOptions = amountOptions
            };

            return new BbpsFetchResponseDto
            {
                ResponseCode = "000",
                ResponseMessage = "SUCCESS",
                BillRequestId = root.Element("billRequestId")?.Value,
                RequestId = root.Element("requestId")?.Value,
                InputParams = inputParams,
                BillerResponse = billerResponse,
                AdditionalInfo = additionalInfo
            };
        }

        // ---------------- PAY ----------------
        // FIX: BillAvenue Pay response has NO <status> or <responseMessage> tags.
        // Correct tag is <responseReason>. Status must be derived from responseCode + txnRefId.
        public static BbpsPayResponseDto ParsePay(string xml)
        {
            var doc = XDocument.Parse(xml);
            var root = doc.Root;

            var responseCode = root.Element("responseCode")?.Value;
            var responseReason = root.Element("responseReason")?.Value;
            var txnRefId = root.Element("txnRefId")?.Value;

            string status;
            if (responseCode == "000" && !string.IsNullOrWhiteSpace(txnRefId))
                status = "SUCCESS";
            else if (responseCode == "001")
                status = "FAILED";
            else if (responseCode == "204" || responseCode == "205" || string.IsNullOrWhiteSpace(responseCode))
                status = "PENDING";
            else if (!string.IsNullOrWhiteSpace(txnRefId))
                status = "PENDING";
            else
                status = "PENDING";

            return new BbpsPayResponseDto
            {
                ResponseCode = responseCode,
                ResponseMessage = responseReason,
                TxnRefId = txnRefId,
                Status = status
            };
        }

        // ---------------- STATUS ----------------
        public static BbpsStatusResponseDto ParseStatus(string xml)
        {
            var dto = new BbpsStatusResponseDto();

            if (string.IsNullOrWhiteSpace(xml))
                return dto;

            dto.RawXml = xml;

            var doc = XDocument.Parse(xml);
            var root = doc.Element("transactionStatusResp");
            if (root == null)
                return dto;

            dto.ResponseCode = root.Element("responseCode")?.Value?.Trim();
            dto.ResponseMessage = root.Element("responseReason")?.Value?.Trim();

            var txnNode = root.Element("txnList");
            if (txnNode != null)
            {
                dto.TxnRefId = txnNode.Element("txnReferenceId")?.Value?.Trim();
                dto.Status = txnNode.Element("txnStatus")?.Value?.Trim()?.ToUpper();
                dto.CustomerName = txnNode.Element("respCustomerName")?.Value?.Trim();
                dto.PaidAmount = txnNode.Element("amount")?.Value?.Trim();
                dto.ApprovalRefNumber = txnNode.Element("approvalRefNumber")?.Value?.Trim();
                dto.BillNumber = txnNode.Element("respBillNumber")?.Value?.Trim();
                dto.DueDate = txnNode.Element("respDueDate")?.Value?.Trim();
            }

            if (string.IsNullOrWhiteSpace(dto.Status))
            {
                if (dto.ResponseCode == "000" && !string.IsNullOrWhiteSpace(dto.TxnRefId))
                    dto.Status = "SUCCESS";
                else
                    dto.Status = "PENDING";
            }

            return dto;
        }

        // ---------------- MDM BILLERS ----------------
        public static List<BbpsBillerMaster> ParseBillerInfo(string xml)
        {
            var doc = XDocument.Parse(xml);
            XNamespace ns = doc.Root.GetDefaultNamespace();

            var billerNodes = doc.Descendants(ns + "biller").ToList();

            Console.WriteLine("====== BILLER PARSER DEBUG ======");
            Console.WriteLine("XML Length = " + xml.Length);
            Console.WriteLine("biller nodes found = " + billerNodes.Count);

            var billers = billerNodes
                .Select(x => new BbpsBillerMaster
                {
                    BillerId = x.Element(ns + "billerId")?.Value,
                    BillerName = x.Element(ns + "billerName")?.Value,
                    Category = x.Element(ns + "billerCategory")?.Value,
                    FetchRequirement = x.Element(ns + "billerFetchRequiremet")?.Value,
                    PaymentAmountExactness = x.Element(ns + "paymentAmountExactness")?.Value?.ToUpper(),
                    SupportsAdhoc = string.Equals(
                                                x.Element(ns + "billerAdhoc")?.Value,
                                                "true",
                                                StringComparison.OrdinalIgnoreCase),
                    BillerStatus = x.Element(ns + "billerStatus")?.Value ?? "ACTIVE",
                    CreatedOn = DateTime.UtcNow
                })
                .Where(b => !string.IsNullOrWhiteSpace(b.BillerId))
                .ToList();

            Console.WriteLine("Parsed biller count = " + billers.Count);
            Console.WriteLine("=================================");

            return billers;
        }

        public static List<BbpsBillerInputParamDto> ParseBillerInputParams(string xml)
        {
            var doc = XDocument.Parse(xml);
            XNamespace ns = doc.Root.GetDefaultNamespace();

            return doc
                .Descendants(ns + "paramInfo")
                .Select(x =>
                {
                    // ✅ FIX: Default visibility to TRUE if <visibility> tag is missing
                    // Some billers like ManipalCigna Health Insurance do not include
                    // the <visibility> tag at all in their MDM response.
                    // Old code: string.Equals(visValue, "true") → returns false when tag missing
                    // New code: if tag is missing or empty → treat as visible (true)
                    var visibilityStr = x.Element(ns + "visibility")?.Value;
                    bool isVisible = string.IsNullOrWhiteSpace(visibilityStr)
                        ? true  // ← missing tag = show the field
                        : string.Equals(visibilityStr, "true", StringComparison.OrdinalIgnoreCase);

                    return new BbpsBillerInputParamDto
                    {
                        ParamName = x.Element(ns + "paramName")?.Value?.Trim(),
                        DataType = x.Element(ns + "dataType")?.Value,
                        IsOptional = string.Equals(
                                         x.Element(ns + "isOptional")?.Value,
                                         "true",
                                         StringComparison.OrdinalIgnoreCase),
                        MinLength = int.TryParse(x.Element(ns + "minLength")?.Value, out var mn) ? mn : 0,
                        MaxLength = int.TryParse(x.Element(ns + "maxLength")?.Value, out var mx) ? mx : 0,
                        Regex = x.Element(ns + "regEx")?.Value,
                        Visibility = isVisible
                    };
                })
                // ✅ FIX: Removed p.Visibility filter — visibility now defaults to true above
                // ✅ FIX: Removed p.MaxLength > 0 filter — some billers set maxLength=0
                //         meaning "no limit". We still want to show those fields.
                //         Frontend validator only adds maxLength rule if maxLength > 0.
                .Where(p =>
                    !string.IsNullOrWhiteSpace(p.ParamName) &&
                    !string.IsNullOrWhiteSpace(p.DataType))
                .GroupBy(p => p.ParamName)
                .Select(g => g.First())
                .ToList();
        }

        // ---------------- BILL VALIDATION ----------------
        public static BbpsBillValidationResponseDto ParseBillValidation(string xml)
        {
            var doc = XDocument.Parse(xml);
            XNamespace ns = doc.Root.GetDefaultNamespace();
            var root = doc.Root;

            return new BbpsBillValidationResponseDto
            {
                ResponseCode = root.Element(ns + "responseCode")?.Value,
                ResponseMessage = root.Element(ns + "responseMessage")?.Value
            };
        }

        // ---------------- HELPERS ----------------
        private static string? ConvertPaiseToRupees(string? paiseValue)
        {
            if (string.IsNullOrWhiteSpace(paiseValue)) return paiseValue;
            if (decimal.TryParse(paiseValue, out decimal paise))
                return (paise / 100m).ToString("0.##");
            return paiseValue;
        }
    }
}