using System.Collections.Specialized;
using System.Net;
using System.Text;
using System.Web;
using System.Xml;

#nullable disable

namespace GTAServer
{
    public class Auth
    {
        public static Task<int> CreateTicketXbl2(Globals.Client client)
        {
            ClientCrypto clientCrypto = new ClientCrypto();
            client.requestData = clientCrypto.Decrypt(client.requestData);

            int userInfoLength = client.requestData.Length - 0x14;
            byte[] userInfoBytes = new byte[userInfoLength];
            Buffer.BlockCopy(client.requestData, 0, userInfoBytes, 0, userInfoLength);

            string userInfo = Encoding.ASCII.GetString(userInfoBytes);
            NameValueCollection collection = HttpUtility.ParseQueryString(userInfo);

            string platformName = collection["platformName"];
            string xuid = collection["xuid"];
            string gamertag = collection["gamertag"];

            if (platformName == null || xuid == null || gamertag == null)
            {
                client.response.StatusCode = (int)HttpStatusCode.InternalServerError;
                return Task.FromResult(1);
            }

            string sessionId = Tools.RandomSessionId();
            string sessionKey = Tools.RandomBytesToBase64(18);
            string sessionTicket = Tools.RandomBytesToBase64(60);

            Globals.Member member = new Globals.Member();

            bool Exists = Database.GetMemberByXuid(ref member, xuid);

            bool Banned = false;

            if (Exists)
            {
                member.last_online = DateTime.Now;
                member.session_ticket = sessionTicket;
                member.session_key = sessionKey;
                member.platform_name = platformName;
                Database.UpdateMember(ref member);
            }
            else
            {
                member.id = (int)(Database.GetMemberCount() + 1);
                member.xuid = xuid;
                member.gamertag = gamertag;
                member.crew_id = "68330145";
                member.crew_tag = "GTAO";
                member.expires = DateTime.Now.AddDays(7);
                member.last_online = DateTime.Now;
                member.session_ticket = sessionTicket;
                member.session_key = sessionKey;
                member.platform_name = platformName;
                Database.AddMember(ref member);
            }

            Banned = member.banned == 1 ? true : false;

            if (Banned)
                return Task.FromResult(0);

            XmlDocument doc = new XmlDocument();
            doc.Load("bin/CreateTicketResponse.xml");

            string posixTime = ((DateTimeOffset)DateTime.UtcNow).ToUnixTimeSeconds().ToString();

            doc.GetElementsByTagName("PosixTime")[0].InnerText = posixTime;
            doc.GetElementsByTagName("PlayerAccountId")[0].InnerText = member.id.ToString();
            doc.GetElementsByTagName("SessionId")[0].InnerText = sessionId;
            //doc.GetElementsByTagName("SessionKey")[0].InnerText = sessionKey; // Static for now
            doc.GetElementsByTagName("SessionTicket")[0].InnerText = sessionTicket;
            doc.GetElementsByTagName("RockstarId")[0].InnerText = member.id.ToString();
            doc.GetElementsByTagName("Age")[0].InnerText = "21";
            doc.GetElementsByTagName("Nickname")[0].InnerText = gamertag;
            string file = doc.OuterXml;

            ServerCrypto serverCrypto = new ServerCrypto();
            client.responseData = serverCrypto.Encrypt(Encoding.UTF8.GetBytes(file));

            client.response.StatusCode = (int)HttpStatusCode.OK;
            client.response.ContentType = "text/xml; charset=utf-8";
            client.response.ContentLength64 = client.responseData.Length;
            client.response.OutputStream.Write(client.responseData);

            if (!Directory.Exists(string.Format("bin/members/{0}", xuid)))
            {
                Directory.CreateDirectory(string.Format("bin/members/{0}", xuid));
            }

            if (!File.Exists(string.Format("bin/members/{0}/mpstats.json", xuid)))
            {
                File.Copy("bin/mpstats.json", string.Format("bin/members/{0}/mpstats.json", xuid));
            }

            return Task.FromResult(0);
        }

        public static Task<int> CreateTicketNp2(Globals.Client client)
        {
            ClientCrypto clientCrypto = new ClientCrypto();
            client.requestData = clientCrypto.Decrypt(client.requestData, "ps3");

            int userInfoLength = client.requestData.Length - 0x14;
            byte[] userInfoBytes = new byte[userInfoLength];
            Buffer.BlockCopy(client.requestData, 0, userInfoBytes, 0, userInfoLength);

            string userInfo = Encoding.ASCII.GetString(userInfoBytes);
            NameValueCollection collection = HttpUtility.ParseQueryString(userInfo);

            string platformName = collection["platformName"];
            byte[] npTicket = Convert.FromBase64String(collection["npTicket"]);
            string gamertag = Tools.GetUsername(npTicket);
            string xuid = Tools.GenerateXUID(gamertag);

            if (platformName == null || xuid == null || gamertag == null)
            {
                client.response.StatusCode = (int)HttpStatusCode.InternalServerError;
                return Task.FromResult(1);
            }

            string sessionId = Tools.RandomSessionId();
            string sessionKey = Tools.RandomBytesToBase64(18);
            string sessionTicket = Tools.RandomBytesToBase64(60);

            Globals.Member member = new Globals.Member();

            bool Exists = Database.GetMemberByXuid(ref member, xuid);

            bool Banned = false;

            if (Exists)
            {
                member.last_online = DateTime.Now;
                member.session_ticket = sessionTicket;
                member.session_key = sessionKey;
                member.platform_name = platformName;
                Database.UpdateMember(ref member);
            }
            else
            {
                member.id = (int)(Database.GetMemberCount() + 1);
                member.xuid = xuid;
                member.gamertag = gamertag;
                member.crew_id = "68330145";
                member.crew_tag = "GTAO";
                member.expires = DateTime.Now.AddDays(7);
                member.last_online = DateTime.Now;
                member.session_ticket = sessionTicket;
                member.session_key = sessionKey;
                member.platform_name = platformName;
                Database.AddMember(ref member);
            }

            Banned = member.banned == 1 ? true : false;

            if (Banned)
                return Task.FromResult(0);

            XmlDocument doc = new XmlDocument();
            doc.Load("bin/CreateTicketResponse.xml");

            string posixTime = ((DateTimeOffset)DateTime.UtcNow).ToUnixTimeSeconds().ToString();

            doc.GetElementsByTagName("PosixTime")[0].InnerText = posixTime;
            doc.GetElementsByTagName("PlayerAccountId")[0].InnerText = member.id.ToString();
            doc.GetElementsByTagName("SessionId")[0].InnerText = sessionId;
            //doc.GetElementsByTagName("SessionKey")[0].InnerText = sessionKey; // Static for now
            doc.GetElementsByTagName("SessionTicket")[0].InnerText = sessionTicket;
            doc.GetElementsByTagName("RockstarId")[0].InnerText = member.id.ToString();
            doc.GetElementsByTagName("Age")[0].InnerText = "21";
            doc.GetElementsByTagName("Nickname")[0].InnerText = gamertag;
            string file = doc.OuterXml;

            ServerCrypto serverCrypto = new ServerCrypto();
            client.responseData = serverCrypto.Encrypt(Encoding.UTF8.GetBytes(file), "ps3");

            client.response.StatusCode = (int)HttpStatusCode.OK;
            client.response.ContentType = "text/xml; charset=utf-8";
            client.response.ContentLength64 = client.responseData.Length;
            client.response.OutputStream.Write(client.responseData);

            if (!Directory.Exists(string.Format("bin/members/{0}", xuid)))
            {
                Directory.CreateDirectory(string.Format("bin/members/{0}", xuid));
            }

            if (!File.Exists(string.Format("bin/members/{0}/mpstats.json", xuid)))
            {
                File.Copy("bin/mpstats.json", string.Format("bin/members/{0}/mpstats.json", xuid));
            }

            return Task.FromResult(0);
        }
    }
}
