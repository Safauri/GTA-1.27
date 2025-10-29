using System.Net;
using System.Text;
using System.Xml;

#nullable disable

namespace GTAServer
{
    public class ProfileStats
    {
        public static Task<int> ReadStatsByGamer2(Globals.Client client)
        {
            string session_ticket = client.request.Headers.Get("ros-SessionTicket");

            Globals.Member member = new Globals.Member();
            Database.GetMemberFromSessionTicket(ref member, session_ticket);

            string file = File.ReadAllText("bin/ReadStatsResponse.xml");

            ServerCrypto serverCrypto = new ServerCrypto(true);
            client.responseData = serverCrypto.Encrypt(Encoding.UTF8.GetBytes(file), member.platform_name);

            client.response.StatusCode = (int)HttpStatusCode.OK;
            client.response.ContentType = "text/xml; charset=utf-8";
            client.response.ContentLength64 = client.responseData.Length;
            client.response.OutputStream.Write(client.responseData);

            return Task.FromResult(0);
        }

        public static Task<int> ReadStatsByGroups(Globals.Client client)
        {
            string sessionTicket = client.request.Headers.Get("ROS-SESSIONTICKET");
            string xuid = string.Empty;

            Globals.Member member = new Globals.Member();

            if (Database.GetMemberFromSessionTicket(ref member, sessionTicket))
            {
                xuid = member.xuid;
            }
            else
            {
                client.response.StatusCode = (int)HttpStatusCode.InternalServerError;
                return Task.FromResult(1);
            }

            if (xuid == string.Empty)
            {
                client.response.StatusCode = (int)HttpStatusCode.InternalServerError;
                return Task.FromResult(1);
            }

            byte[] values = Members.GetStats(string.Format("bin/members/{0}/mpstats.json", xuid));

            XmlDocument doc = new XmlDocument();
            doc.Load("bin/ReadStatsByGroupsResponse.xml");

            doc.GetElementsByTagName("Values")[0].InnerText = Convert.ToBase64String(values);
            string file = doc.OuterXml;

            ServerCrypto serverCrypto = new ServerCrypto(true);
            client.responseData = serverCrypto.Encrypt(Encoding.UTF8.GetBytes(file), member.platform_name);

            client.response.StatusCode = (int)HttpStatusCode.OK;
            client.response.ContentType = "text/xml; charset=utf-8";
            client.response.ContentLength64 = client.responseData.Length;
            client.response.OutputStream.Write(client.responseData);

            return Task.FromResult(0);
        }

        public static Task<int> WriteStats(Globals.Client client)
        {
            string sessionTicket = client.request.Headers.Get("ROS-SESSIONTICKET");
            string xuid = string.Empty;

            Globals.Member member = new Globals.Member();

            if (Database.GetMemberFromSessionTicket(ref member, sessionTicket))
            {
                xuid = member.xuid;
            }
            else
            {
                client.response.StatusCode = (int)HttpStatusCode.InternalServerError;
                return Task.FromResult(1);
            }

            if (xuid == string.Empty)
            {
                client.response.StatusCode = (int)HttpStatusCode.InternalServerError;
                return Task.FromResult(1);
            }

            ClientCrypto clientCrypto = new ClientCrypto(true);
            client.requestData = clientCrypto.Decrypt(client.requestData, member.platform_name);

            client.requestData = Tools.ByteArraySplit(client.requestData, Encoding.ASCII.GetBytes("data="));

            byte[] buffer = new byte[client.requestData.Length - 1];
            Buffer.BlockCopy(client.requestData, 1, buffer, 0, client.requestData.Length - 1);

            string filePath = string.Format("bin/members/{0}/mpstats.json", xuid);
            string file = string.Empty;

            if (File.Exists(filePath))
            {
                int count = Members.UpdateStats(filePath, buffer);

                XmlDocument doc = new XmlDocument();
                doc.Load("bin/WriteStatsResponse.xml");
                doc.GetElementsByTagName("Status")[0].InnerText = count > -1 ? "1" : "0";
                doc.GetElementsByTagName("NumWritten")[0].InnerText = count.ToString();
                file = doc.OuterXml;
            }
            else
            {
                file = File.ReadAllText("bin/WriteStatsResponse.xml");
            }

            ServerCrypto serverCrypto = new ServerCrypto(true);
            client.responseData = serverCrypto.Encrypt(Encoding.UTF8.GetBytes(file), member.platform_name);

            client.response.StatusCode = (int)HttpStatusCode.OK;
            client.response.ContentType = "text/xml; charset=utf-8";
            client.response.ContentLength64 = client.responseData.Length;
            client.response.OutputStream.Write(client.responseData);

            return Task.FromResult(0);
        }

        public static Task<int> ResetStats(Globals.Client client)
        {
            return Task.FromResult(0);
        }
    }
}
