using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using Payments_core.Models.BBPS;

namespace Payments_core.Helpers
{
    public static class BillAvenueXmlParser
    {
        // ---------------- FETCH ----------------
        //public static BbpsFetchResponseDto ParseFetch(string xml)
        //{
        //    var doc = XDocument.Parse(xml);
        //    XNamespace ns = doc.Root.GetDefaultNamespace();
        //    var x = doc.Root;

        //    return new BbpsFetchResponseDto
        //    {
        //        ResponseCode = x.Element(ns + "responseCode")?.Value,
        //        ResponseMessage = x.Element(ns + "responseMessage")?.Value,
        //        BillRequestId = x.Element(ns + "billRequestId")?.Value,
        //        CustomerName = x.Element(ns + "customerName")?.Value,
        //        BillAmount = decimal.Parse(x.Element(ns + "billAmount")?.Value ?? "0"),
        //        DueDate = DateTime.Parse(x.Element(ns + "dueDate")?.Value ?? DateTime.MinValue.ToString())
        //    };
        //}

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

            // ==============================
            // 🔥 Parse InputParams (DTO)
            // ==============================
            var inputParams = root
                .Element("inputParams")?
                .Elements("input")
                .Select(x => new InputParamDto
                {
                    ParamName = x.Element("paramName")?.Value,
                    ParamValue = x.Element("paramValue")?.Value
                })
                .ToList();

            // ==============================
            // 🔥 Parse AdditionalInfo (DTO)
            // ==============================
            var additionalInfo = root
                .Element("additionalInfo")?
                .Elements("info")
                .Select(x => new AdditionalInfoDto
                {
                    InfoName = x.Element("infoName")?.Value,
                    InfoValue = x.Element("infoValue")?.Value
                })
                .ToList();

            // ==============================
            // 🔥 Parse AmountOptions (DTO)
            // ==============================
            List<AmountOptionDto> amountOptions = null;

            var amountOptionsElement =
                billerResponseElement?.Element("amountOptions");

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

            // ==============================
            // 🔥 Build BillerResponse DTO
            // ==============================
            var billerResponse = new BillerResponseDto
            {
                BillAmount = billerResponseElement?.Element("billAmount")?.Value,
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
        public static BbpsPayResponseDto ParsePay(string xml)
        {
            var doc = XDocument.Parse(xml);
            XNamespace ns = doc.Root.GetDefaultNamespace();
            var x = doc.Root;

            return new BbpsPayResponseDto
            {
                ResponseCode = x.Element(ns + "responseCode")?.Value,
                ResponseMessage = x.Element(ns + "responseMessage")?.Value,
                TxnRefId = x.Element(ns + "txnRefId")?.Value,
                Status = x.Element(ns + "status")?.Value
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

            var root = doc.Root;   // 🔥 safer than Element()

            if (root == null)
                return dto;

            dto.ResponseCode = root.Element("responseCode")?.Value?.Trim();
            dto.ResponseMessage = root.Element("responseReason")?.Value?.Trim();

            var txnNode = root.Element("txnList");

            if (txnNode != null)
            {
                dto.TxnRefId = txnNode.Element("txnReferenceId")?.Value?.Trim();
                dto.Status = txnNode.Element("txnStatus")?.Value?.Trim()?.ToUpper();
            }

            Console.WriteLine($"[PARSED] Code={dto.ResponseCode}, TxnRef={dto.TxnRefId}, Status={dto.Status}");

            if (string.IsNullOrWhiteSpace(dto.Status))
            {
                if (dto.ResponseCode == "000" &&
                    !string.IsNullOrWhiteSpace(dto.TxnRefId))
                {
                    dto.Status = "SUCCESS";
                }
                else
                {
                    dto.Status = "PENDING";
                }
            }

            return dto;
        }

        // ---------------- MDM BILLERS (FIXED) ----------------
        // File: Payments_core/Helpers/BillAvenueXmlParser.cs
        public static List<BbpsBillerMaster> ParseBillerInfo(string xml)
        {
            var doc = XDocument.Parse(xml);
            XNamespace ns = doc.Root.GetDefaultNamespace();

            // ✅ BBPS uses <biller>
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
                    SupportsAdhoc =
                    string.Equals(
                        x.Element(ns + "billerAdhoc")?.Value,
                        "true",
                        StringComparison.OrdinalIgnoreCase
                    ),
                    CreatedOn = DateTime.UtcNow

                })
                .Where(b => !string.IsNullOrWhiteSpace(b.BillerId))
                .ToList();
            Console.WriteLine("Parsed biller count = " + billers.Count);
            Console.WriteLine("=================================");

            return billers;
        }

        //public static List<BbpsBillerInputParamDto> ParseBillerInputParams(string xml)
        //{
        //    var doc = XDocument.Parse(xml);
        //    XNamespace ns = doc.Root.GetDefaultNamespace();

        //    return doc
        //        .Descendants(ns + "paramInfo")
        //        .Select(x => new BbpsBillerInputParamDto
        //        {
        //            ParamName = x.Element(ns + "paramName")?.Value,
        //            DataType = x.Element(ns + "dataType")?.Value,
        //            IsOptional =
        //                string.Equals(
        //                    x.Element(ns + "isOptional")?.Value,
        //                    "true",
        //                    StringComparison.OrdinalIgnoreCase),
        //            MinLength = int.Parse(x.Element(ns + "minLength")?.Value ?? "0"),
        //            MaxLength = int.Parse(x.Element(ns + "maxLength")?.Value ?? "0"),
        //            Visibility =
        //                string.Equals(
        //                    x.Element(ns + "visibility")?.Value,
        //                    "true",
        //                    StringComparison.OrdinalIgnoreCase)
        //        })
        //        .Where(p => !string.IsNullOrWhiteSpace(p.ParamName))
        //        .ToList();
        //}

        public static List<BbpsBillerInputParamDto> ParseBillerInputParams(string xml)
        {
            var doc = XDocument.Parse(xml);
            XNamespace ns = doc.Root.GetDefaultNamespace();

            return doc
                .Descendants(ns + "paramInfo")
                .Select(x => new BbpsBillerInputParamDto
                {
                    ParamName = x.Element(ns + "paramName")?.Value?.Trim(),
                    DataType = x.Element(ns + "dataType")?.Value,
                    IsOptional = string.Equals(
                        x.Element(ns + "isOptional")?.Value,
                        "true",
                        StringComparison.OrdinalIgnoreCase),
                    MinLength = int.Parse(x.Element(ns + "minLength")?.Value ?? "0"),
                    MaxLength = int.Parse(x.Element(ns + "maxLength")?.Value ?? "0"),
                    Visibility = string.Equals(
                        x.Element(ns + "visibility")?.Value,
                        "true",
                        StringComparison.OrdinalIgnoreCase)
                })
                // ✅ ONLY VALID INPUT FIELDS
                .Where(p =>
                    p.Visibility &&
                    !string.IsNullOrWhiteSpace(p.ParamName) &&
                    !string.IsNullOrWhiteSpace(p.DataType) &&
                    p.MaxLength > 0
                )
                // ✅ REMOVE DUPLICATES
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
                ResponseCode =
                    root.Element(ns + "responseCode")?.Value,
                ResponseMessage =
                    root.Element(ns + "responseMessage")?.Value
            };
        }
    }
}