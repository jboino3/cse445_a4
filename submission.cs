using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ConsoleApp1
{
    public class Program
    {
        public static string xmlURL = "https://YOUR_USER.github.io/path/Hotels.xml";        // Q1.2
        public static string xmlErrorURL = "https://YOUR_USER.github.io/path/HotelsErrors.xml";  // Q1.3
        public static string xsdURL = "https://YOUR_USER.github.io/path/Hotels.xsd";        // Q1.1

        public static void Main(string[] args)
        {
            // Q3(1): Verify the valid XML
            string result = Verification(xmlURL, xsdURL);
            Console.WriteLine(result);

            // Q3(2): Verify the invalid XML and print all errors
            result = Verification(xmlErrorURL, xsdURL);
            Console.WriteLine(result);

            // Q3(3): Convert valid XML to JSON (must be de-serializable by JsonConvert.DeserializeXmlNode)
            result = Xml2Json(xmlURL);
            Console.WriteLine(result);
        }

        // Q2.1: Validate XML against XSD and return either "No errors are found" or the collected error messages.
        public static string Verification(string xmlUrl, string xsdUrl)
        {
            try
            {
                var settings = new XmlReaderSettings();
                settings.Schemas.Add(null, xsdUrl);
                settings.ValidationType = ValidationType.Schema;

                var errors = new List<string>();
                settings.ValidationEventHandler += (sender, e) =>
                {
                    // Include severity and line info when available
                    var ex = e.Exception;
                    string where = (ex != null)
                        ? $"(Line {ex.LineNumber}, Position {ex.LinePosition})"
                        : "";
                    errors.Add($"{e.Severity}: {e.Message} {where}".Trim());
                };

                using (var reader = XmlReader.Create(xmlUrl, settings))
                {
                    while (reader.Read())
                    {
                        // Walk document to trigger validation
                    }
                }

                if (errors.Count == 0)
                    return "No errors are found";

                return string.Join(Environment.NewLine, errors);
            }
            catch (Exception ex)
            {
                // Return exception message so the autograder/user sees what failed (network, parse, etc.)
                return ex.Message;
            }
        }

        // Q2.2: Convert the valid XML into the EXACT JSON shape required by the spec.
        // Shape:
        // {
        //   "Hotels": {
        //     "Hotel": [
        //       {
        //         "Name": "Westin",
        //         "Phone": ["480-...", "800-..."],
        //         "Address": { "Number": "...", "Street": "...", "City": "...", "State": "...", "Zip": "...", "NearestAirport": "..." },
        //         "_Rating": "4.2"   // ONLY if <Rating> exists; omit entirely if missing
        //       },
        //       ...
        //     ]
        //   }
        // }
        public static string Xml2Json(string xmlUrl)
        {
            try
            {
                var xml = new XmlDocument();
                xml.Load(xmlUrl);

                var root = xml.DocumentElement;
                if (root == null || !string.Equals(root.Name, "Hotels", StringComparison.Ordinal))
                    throw new InvalidDataException("Root element must be 'Hotels'.");

                var hotelNodes = root.SelectNodes("Hotel");
                if (hotelNodes == null)
                    throw new InvalidDataException("No 'Hotel' elements found.");

                var hotelsArray = new JArray();

                foreach (XmlNode hotel in hotelNodes)
                {
                    if (hotel.NodeType != XmlNodeType.Element) continue;

                    var jHotel = new JObject();

                    // Name
                    var name = hotel.SelectSingleNode("Name")?.InnerText?.Trim();
                    if (!string.IsNullOrEmpty(name))
                        jHotel["Name"] = name;

                    // Phone (multiple allowed)
                    var phoneNodes = hotel.SelectNodes("Phone");
                    if (phoneNodes != null && phoneNodes.Count > 0)
                    {
                        var phones = new JArray();
                        foreach (XmlNode p in phoneNodes)
                        {
                            var v = (p.InnerText ?? "").Trim();
                            if (!string.IsNullOrEmpty(v))
                                phones.Add(v);
                        }
                        if (phones.Count > 0)
                            jHotel["Phone"] = phones;
                    }

                    // Address object
                    var addrNode = hotel.SelectSingleNode("Address");
                    if (addrNode != null)
                    {
                        var jAddr = new JObject();

                        Func<string, string> get = tag => addrNode.SelectSingleNode(tag)?.InnerText?.Trim();
                        Action<string> add = tag =>
                        {
                            var val = get(tag);
                            if (!string.IsNullOrEmpty(val))
                                jAddr[tag] = val;
                        };

                        add("Number");
                        add("Street");
                        add("City");
                        add("State");
                        add("Zip");
                        add("NearestAirport");

                        if (jAddr.Properties().Any())
                            jHotel["Address"] = jAddr;
                    }

                    // Optional Rating -> JSON key must be "_Rating" (with underscore) and only when present
                    var ratingText = hotel.SelectSingleNode("Rating")?.InnerText?.Trim();
                    if (!string.IsNullOrEmpty(ratingText))
                        jHotel["_Rating"] = ratingText;

                    hotelsArray.Add(jHotel);
                }

                var rootObj = new JObject(
                    new JProperty("Hotels",
                        new JObject(new JProperty("Hotel", hotelsArray)))
                );

                // Must be de-serializable by Newtonsoft.Json (assignment requirement)
                JsonConvert.DeserializeXmlNode(rootObj.ToString());

                return rootObj.ToString(Newtonsoft.Json.Formatting.Indented);
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }
    }
}
