using Newtonsoft.Json;
using System.Xml.Linq;

#nullable disable

namespace GTAServer
{
    public class QueryContentData
    {
        public class Root
        {
            public List<Record> r { get; set; }
        }

        public class Record
        {
            public Mission m { get; set; }
            public Ratings r { get; set; }
            public string _c { get; set; }
            public string s { get; set; }
        }

        public class Mission
        {
            public string da { get; set; }
            public string de { get; set; }
            public string _ca { get; set; }
            public string _cd { get; set; }
            public string _f1 { get; set; }
            public string _f2 { get; set; }
            public string _n { get; set; }
            public string _l { get; set; }
            public string _ld { get; set; }
            public string _pd { get; set; }
            public string _rci { get; set; }
            public string _ud { get; set; }
            public string _un { get; set; }
            public string _v { get; set; }
        }

        public class Ratings
        {
            public string _a { get; set; }
            public string _u { get; set; }
            public string _n { get; set; }
            public string _p { get; set; }
        }

        public static string GenerateXml(string[] contentids)
        {
            //foreach (var id in contentids)
            //Console.WriteLine(id);

            string json = File.ReadAllText("json.txt");

            Root root = JsonConvert.DeserializeObject<Root>(json);

            int Total = root.r.Count(record => Array.Exists(contentids, id => id == record._c));

            XNamespace ns = "QueryContent";

            XDocument fullXml = new XDocument(
                new XDeclaration("1.0", "utf-8", null),
                new XElement(ns + "Response",
                    new XAttribute(XNamespace.Xmlns + "xsd", "http://www.w3.org/2001/XMLSchema"),
                    new XAttribute(XNamespace.Xmlns + "xsi", "http://www.w3.org/2001/XMLSchema-instance"),
                    new XElement(ns + "Status", "1"),
                    new XElement(ns + "Result",
                        new XAttribute("Count", Total),
                        new XAttribute("Total", Total),
                        new XAttribute("Hash", "0")
                    )
                )
            );

            XElement resultElement = fullXml.Root.Element(ns + "Result");

            foreach (var record in root.r)
            {
                if (Array.Exists(contentids, id => id == record._c || id == record.m._rci))
                {
                    var xml = new XElement(ns + "r",
                        new XAttribute("c", record._c)
                    );

                    // Add <m> element with conditional attributes
                    XElement mElement = new XElement(ns + "m");

                    if (!string.IsNullOrEmpty(record.m._ca))
                        mElement.Add(new XAttribute("ca", record.m._ca));
                    if (!string.IsNullOrEmpty(record.m._cd))
                        mElement.Add(new XAttribute("cd", record.m._cd));
                    if (!string.IsNullOrEmpty(record.m._f1))
                        mElement.Add(new XAttribute("f1", record.m._f1));
                    if (!string.IsNullOrEmpty(record.m._f2))
                        mElement.Add(new XAttribute("f2", record.m._f2));
                    if (!string.IsNullOrEmpty(record.m._n))
                        mElement.Add(new XAttribute("n", record.m._n));
                    if (!string.IsNullOrEmpty(record.m._l))
                        mElement.Add(new XAttribute("l", record.m._l));
                    if (!string.IsNullOrEmpty(record.m._ld))
                        mElement.Add(new XAttribute("ld", record.m._ld));
                    if (!string.IsNullOrEmpty(record.m._pd))
                        mElement.Add(new XAttribute("pd", record.m._pd));
                    if (!string.IsNullOrEmpty(record.m._rci))
                        mElement.Add(new XAttribute("rci", record.m._rci));
                    if (!string.IsNullOrEmpty(record.m._ud))
                        mElement.Add(new XAttribute("ud", record.m._ud));
                    if (!string.IsNullOrEmpty(record.m._un))
                        mElement.Add(new XAttribute("un", record.m._un));
                    if (!string.IsNullOrEmpty(record.m._v))
                        mElement.Add(new XAttribute("v", record.m._v));

                    // Add the <da> and <de> elements only if they are not null/empty
                    if (!string.IsNullOrEmpty(record.m.da))
                        mElement.Add(new XElement(ns + "da", new XCData(record.m.da)));
                    if (!string.IsNullOrEmpty(record.m.de))
                        mElement.Add(new XElement(ns + "de", new XCData(record.m.de)));

                    xml.Add(mElement);

                    // Add <r> element with conditional attributes
                    XElement rElement = new XElement(ns + "r");

                    if (!string.IsNullOrEmpty(record.r._a))
                        rElement.Add(new XAttribute("a", record.r._a));
                    if (!string.IsNullOrEmpty(record.r._u))
                        rElement.Add(new XAttribute("u", record.r._u));
                    if (!string.IsNullOrEmpty(record.r._n))
                        rElement.Add(new XAttribute("n", record.r._n));
                    if (!string.IsNullOrEmpty(record.r._p))
                        rElement.Add(new XAttribute("p", record.r._p));

                    xml.Add(rElement);

                    // Add <s> element only if it is not null/empty
                    if (!string.IsNullOrEmpty(record.s))
                        xml.Add(new XElement(ns + "s", new XCData(record.s)));

                    // Add the generated XML to the full XML document
                    resultElement.Add(xml);
                }
            }

            //File.WriteAllText(string.Format("QueryContent_{0}.xml", DateTime.Now.ToString("yyyy-dd-MM_hh-mm-ss")), fullXml.ToString());

            // Return the full XML document
            return fullXml.ToString();
        }
    }
}
