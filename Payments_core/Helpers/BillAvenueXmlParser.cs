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
        public static BbpsFetchResponseDto ParseFetch(string xml)
        {
            var doc = XDocument.Parse(xml);
            XNamespace ns = doc.Root.GetDefaultNamespace();
            var x = doc.Root;

            return new BbpsFetchResponseDto
            {
                ResponseCode = x.Element(ns + "responseCode")?.Value,
                ResponseMessage = x.Element(ns + "responseMessage")?.Value,
                BillRequestId = x.Element(ns + "billRequestId")?.Value,
                CustomerName = x.Element(ns + "customerName")?.Value,
                BillAmount = decimal.Parse(x.Element(ns + "billAmount")?.Value ?? "0"),
                DueDate = DateTime.Parse(x.Element(ns + "dueDate")?.Value ?? DateTime.MinValue.ToString())
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
            var doc = XDocument.Parse(xml);
            XNamespace ns = doc.Root.GetDefaultNamespace();
            var x = doc.Root;

            return new BbpsStatusResponseDto
            {
                ResponseCode = x.Element(ns + "responseCode")?.Value,
                ResponseMessage = x.Element(ns + "responseMessage")?.Value,
                TxnRefId = x.Element(ns + "txnRefId")?.Value,
                Status = x.Element(ns + "status")?.Value
            };
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