using System.Net;
using System.Text;
using System.Xml;

#nullable disable

namespace GTAServer
{
    public class Clans
    {
        public static Task<int> GetMine(Globals.Client client)
        {
            string crew_id = "68330145";
            string crew_name = "GTAOnline";
            string crew_tag = "GTAO";
            string crew_motto = "GTA:1.27";
            string crew_color = "#FF0000";

            Globals.Member member = new Globals.Member();

            bool MemberExists = Database.GetMemberFromSessionTicket(ref member, client.request.Headers.Get("ros-SessionTicket"));

            if (MemberExists)
            {
                crew_id = member.crew_id;
                crew_tag = member.crew_tag;
            }

            Globals.Crew crew = new Globals.Crew();

            bool CrewExists = Database.GetCrewFromCrewId(ref crew, crew_id);

            if (CrewExists)
            {
                crew_name = crew.crew_name;
                crew_tag = crew.crew_tag;
                crew_motto = crew.crew_motto;
                crew_color = crew.crew_color;
            }

            XmlDocument doc = new XmlDocument();
            doc.Load("bin/ClanMembershipResponse.xml");

            doc.GetElementsByTagName("Clan")[0].Attributes["Id"].Value = crew_id;
            doc.GetElementsByTagName("Clan")[0].Attributes["Name"].Value = crew_name;
            doc.GetElementsByTagName("Clan")[0].Attributes["Tag"].Value = crew_tag;
            doc.GetElementsByTagName("Clan")[0].Attributes["Motto"].Value = crew_motto;
            doc.GetElementsByTagName("Clan")[0].Attributes["Colors"].Value = crew_color;
            string file = doc.OuterXml;

            ServerCrypto serverCrypto = new ServerCrypto(true);
            client.responseData = serverCrypto.Encrypt(Encoding.UTF8.GetBytes(file), member.platform_name);

            client.response.StatusCode = (int)HttpStatusCode.OK;
            client.response.ContentType = "text/xml; charset=utf-8";
            client.response.ContentLength64 = client.responseData.Length;
            client.response.OutputStream.Write(client.responseData);

            return Task.FromResult(0);
        }

        public static Task<int> GetInvites(Globals.Client client)
        {
            string session_ticket = client.request.Headers.Get("ros-SessionTicket");

            Globals.Member member = new Globals.Member();
            Database.GetMemberFromSessionTicket(ref member, session_ticket);

            string file = File.ReadAllText("bin/ClanInviteResponse.xml");

            ServerCrypto serverCrypto = new ServerCrypto(true);
            client.responseData = serverCrypto.Encrypt(Encoding.UTF8.GetBytes(file), member.platform_name);

            client.response.StatusCode = (int)HttpStatusCode.OK;
            client.response.ContentType = "text/xml; charset=utf-8";
            client.response.ContentLength64 = client.responseData.Length;
            client.response.OutputStream.Write(client.responseData);

            return Task.FromResult(0);
        }

        public static Task<int> GetMetadataForClan(Globals.Client client)
        {
            string session_ticket = client.request.Headers.Get("ros-SessionTicket");

            Globals.Member member = new Globals.Member();
            Database.GetMemberFromSessionTicket(ref member, session_ticket);

            string file = File.ReadAllText("bin/ClanMetadataEnumResponse.xml");

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
