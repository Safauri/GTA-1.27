using System.Collections.Specialized;
using System.Net;
using System.Text;
using System.Web;
using System.Xml;

#nullable disable

namespace GTAServer
{
    public class Presence
    {
        public struct TypeNameValue
        {
            public string type;
            public string name;
            public string value;
        }

        public static Task<int> GetPresenceServers(Globals.Client client)
        {
            string session_ticket = client.request.Headers.Get("ros-SessionTicket");

            Globals.Member member = new Globals.Member();
            Database.GetMemberFromSessionTicket(ref member, session_ticket);

            string file = File.ReadAllText("bin/GetPresenceServersResponse.xml");

            ServerCrypto serverCrypto = new ServerCrypto(true);
            client.responseData = serverCrypto.Encrypt(Encoding.UTF8.GetBytes(file), member.platform_name);

            client.response.StatusCode = (int)HttpStatusCode.OK;
            client.response.ContentType = "text/xml; charset=utf-8";
            client.response.ContentLength64 = client.responseData.Length;
            client.response.OutputStream.Write(client.responseData);

            return Task.FromResult(0);
        }

        public static Task<int> GetAttributes(Globals.Client client)
        {
            string session_ticket = client.request.Headers.Get("ros-SessionTicket");

            Member member = new Member();
            Database.GetMemberFromSessionTicket(ref member, session_ticket);

            //ClientCrypto clientCrypto = new ClientCrypto(true);
            //client.requestData = clientCrypto.Decrypt(client.requestData, member.platform_name);
            //File.WriteAllBytes(string.Format("bin/dump/pres_get_{0}.bin", Environment.TickCount.ToString()), client.requestData);

            string file = File.ReadAllText("bin/GetAttributes.xml");

            ServerCrypto serverCrypto = new ServerCrypto(true);
            client.responseData = serverCrypto.Encrypt(Encoding.UTF8.GetBytes(file), member.platform_name);

            client.response.StatusCode = (int)HttpStatusCode.OK;
            client.response.ContentType = "text/xml; charset=utf-8";
            client.response.ContentLength64 = client.responseData.Length;
            client.response.OutputStream.Write(client.responseData);

            return Task.FromResult(0);
        }

        public static Task<int> SetAttributes(Globals.Client client)
        {
            string session_ticket = client.request.Headers.Get("ros-SessionTicket");

            Globals.Member member = new Globals.Member();
            Database.GetMemberFromSessionTicket(ref member, session_ticket);

            ClientCrypto clientCrypto = new ClientCrypto(true);
            client.requestData = clientCrypto.Decrypt(client.requestData, member.platform_name);

            int queryDataLength = client.requestData.Length - 0x14;
            byte[] queryData = new byte[queryDataLength];
            Buffer.BlockCopy(client.requestData, 0, queryData, 0, queryDataLength);

            string queryString = Encoding.UTF8.GetString(queryData);
            NameValueCollection collection = HttpUtility.ParseQueryString(queryString);

            string csvString = string.Empty;

            if (collection["typeNameValueCsv"] != string.Empty)
            {
                csvString = collection["typeNameValueCsv"];
            }

            string[] results = [];

            if (csvString != string.Empty)
            {
                results = Tools.ParseCsv(csvString);

                foreach (string result in results)
                {
                    int x = result.IndexOf(",");
                    string name = result.Substring(0, x).Replace(",", "");
                    string value = result.Substring(x).Replace(",", "");

                    if (Database.AttributeExists(name))
                    {
                        Database.SetAttribute(name, value, session_ticket);
                    }
                }
            }

            string file = File.ReadAllText("bin/ServicesSuccess.xml");

            ServerCrypto serverCrypto = new ServerCrypto(true);
            client.responseData = serverCrypto.Encrypt(Encoding.UTF8.GetBytes(file), member.platform_name);

            client.response.StatusCode = (int)HttpStatusCode.OK;
            client.response.ContentType = "text/xml; charset=utf-8";
            client.response.ContentLength64 = client.responseData.Length;
            client.response.OutputStream.Write(client.responseData);

            return Task.FromResult(0);
        }

        public static Task<int> ReplaceAttributes(Globals.Client client)
        {
            string session_ticket = client.request.Headers.Get("ros-SessionTicket");

            Globals.Member member = new Globals.Member();
            Database.GetMemberFromSessionTicket(ref member, session_ticket);

            //ClientCrypto clientCrypto = new ClientCrypto(true);
            //client.requestData = clientCrypto.Decrypt(client.requestData, member.platform_name);
            //File.WriteAllBytes(string.Format("bin/dump/pres_replace_{0}.bin", Environment.TickCount.ToString()), client.requestData);

            string file = File.ReadAllText("bin/ServicesSuccess.xml");

            ServerCrypto serverCrypto = new ServerCrypto(true);
            client.responseData = serverCrypto.Encrypt(Encoding.UTF8.GetBytes(file), member.platform_name);

            client.response.StatusCode = (int)HttpStatusCode.OK;
            client.response.ContentType = "text/xml; charset=utf-8";
            client.response.ContentLength64 = client.responseData.Length;
            client.response.OutputStream.Write(client.responseData);

            return Task.FromResult(0);
        }

        public static Task<int> Query(Globals.Client client)
        {
            string session_ticket = client.request.Headers.Get("ros-SessionTicket");

            Globals.Member member = new Globals.Member();
            Database.GetMemberFromSessionTicket(ref member, session_ticket);

            ClientCrypto clientCrypto = new ClientCrypto(true);
            client.requestData = clientCrypto.Decrypt(client.requestData, member.platform_name);
            //File.WriteAllBytes(string.Format("bin/dump/pres_query_{0}.bin", Environment.TickCount.ToString()), client.requestData);

            int queryDataLength = client.requestData.Length - 0x14;
            byte[] queryData = new byte[queryDataLength];
            Buffer.BlockCopy(client.requestData, 0, queryData, 0, queryDataLength);

            string queryString = Encoding.UTF8.GetString(queryData);
            //Console.WriteLine(queryString);

            NameValueCollection collection = HttpUtility.ParseQueryString(queryString);

            string queryName = collection["queryName"];

            string file = string.Empty;

            if (queryName == "CrewmateSessions")
            {
                string[] results = Database.FindSession(session_ticket, member.platform_name);

                string Xuid = results[0];
                string Info = results[1];

                if (Xuid != string.Empty && Info != string.Empty)
                {
                    //byte[] statsResponseBytes = File.ReadAllBytes("bin/ReadStatsResponse1.xml");

                    //Buffer.BlockCopy(Encoding.ASCII.GetBytes(Xuid), 0, statsResponseBytes, 0xDD, 0xD);
                    //Buffer.BlockCopy(Encoding.ASCII.GetBytes(Info), 0, statsResponseBytes, 0x112, 0x50);

                    //file = Encoding.ASCII.GetString(statsResponseBytes);

                    XmlDocument doc = new XmlDocument();

                    XmlDeclaration declaration = doc.CreateXmlDeclaration("1.0", "utf-8", null);
                    doc.AppendChild(declaration);

                    XmlElement responseElement = doc.CreateElement(string.Empty, "Response", "ReadStatsResponse");
                    responseElement.SetAttribute("xmlns:xsd", "http://www.w3.org/2001/XMLSchema");
                    responseElement.SetAttribute("xmlns:xsi", "http://www.w3.org/2001/XMLSchema-instance");
                    doc.AppendChild(responseElement);

                    XmlElement statusElement = doc.CreateElement("Status");
                    statusElement.InnerText = "1";
                    responseElement.AppendChild(statusElement);

                    XmlElement resultsElement = doc.CreateElement("Results");
                    resultsElement.SetAttribute("Count", results.Length.ToString());
                    responseElement.AppendChild(resultsElement);

                    XmlElement rElement = doc.CreateElement("r");
                    rElement.SetAttribute("gh", member.platform_name == "ps3" ? $"{Xuid}" : $"XBL {Xuid}");

                    string json = $"{{\"gsinfo\":\"{Info}\"}}";
                    rElement.InnerText = json;

                    resultsElement.AppendChild(rElement);

                    file = doc.OuterXml;
                }
                else
                {
                    file = File.ReadAllText("bin/OK.txt");
                }
            }
            //else if (queryName == "FindMatchedGamers")
            //{
            //    byte[] statsResponseBytes = File.ReadAllBytes("bin/ReadStatsResponse2.xml");
            //    file = Encoding.ASCII.GetString(statsResponseBytes);
            //}
            //else if (queryName == "SessionByGamerHandle")
            //{
            //    byte[] statsResponseBytes = File.ReadAllBytes("bin/ReadStatsResponse2.xml");
            //    file = Encoding.ASCII.GetString(statsResponseBytes);
            //}
            else
            {
                file = File.ReadAllText("bin/OK.txt");
            }

            ServerCrypto serverCrypto = new ServerCrypto(true);
            client.responseData = serverCrypto.Encrypt(Encoding.UTF8.GetBytes(file), member.platform_name);

            client.response.StatusCode = (int)HttpStatusCode.OK;
            client.response.ContentType = "text/xml; charset=utf-8";
            client.response.ContentLength64 = client.responseData.Length;
            client.response.OutputStream.Write(client.responseData);

            return Task.FromResult(0);
        }

        public static Task<int> Subscribe(Globals.Client client)
        {
            string session_ticket = client.request.Headers.Get("ros-SessionTicket");

            Globals.Member member = new Globals.Member();
            Database.GetMemberFromSessionTicket(ref member, session_ticket);

            //ClientCrypto clientCrypto = new ClientCrypto(true);
            //client.requestData = clientCrypto.Decrypt(client.requestData, member.platform_name);
            //File.WriteAllBytes(string.Format("bin/dump/pres_subscribe_{0}.bin", Environment.TickCount.ToString()), client.requestData);

            string file = File.ReadAllText("bin/OK.txt");

            ServerCrypto serverCrypto = new ServerCrypto(true);
            client.responseData = serverCrypto.Encrypt(Encoding.UTF8.GetBytes(file), member.platform_name);

            client.response.StatusCode = (int)HttpStatusCode.OK;
            client.response.ContentType = "text/xml; charset=utf-8";
            client.response.ContentLength64 = client.responseData.Length;
            client.response.OutputStream.Write(client.responseData);

            return Task.FromResult(0);
        }

        public static Task<int> MultiPostMessage(Globals.Client client)
        {
            string session_ticket = client.request.Headers.Get("ros-SessionTicket");

            Globals.Member member = new Globals.Member();
            Database.GetMemberFromSessionTicket(ref member, session_ticket);

            string file = File.ReadAllText("bin/OK.txt");

            ServerCrypto serverCrypto = new ServerCrypto(true);
            client.responseData = serverCrypto.Encrypt(Encoding.UTF8.GetBytes(file), member.platform_name);

            client.response.StatusCode = (int)HttpStatusCode.OK;
            client.response.ContentType = "text/xml; charset=utf-8";
            client.response.ContentLength64 = client.responseData.Length;
            client.response.OutputStream.Write(client.responseData);

            return Task.FromResult(0);
        }
    }
}
