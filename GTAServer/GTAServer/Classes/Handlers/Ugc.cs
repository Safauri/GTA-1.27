using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Specialized;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;

#nullable disable

namespace GTAServer
{
    public class Ugc
    {
        public class Query
        {
            public List<string> Lang { get; set; }
            public string Category { get; set; }
            public string Hash { get; set; }
        }

        public static Dictionary<string, string> MissionList = new Dictionary<string, string>()
        {
            { "rstar", "bin/QueryContent1.xml" },
            { "verif", "bin/QueryContent2.xml" }
        };

        public static Dictionary<string, string> HashList = new Dictionary<string, string>()
        {
            { "fa468f46f028ab31a9cc586e1322aa8ee408211c", "bin/TheFleecaJob.xml" },
            { "574ce380eb86a5f30909a025df4145f6dff6d84d", "bin/FleecaJobScopeOut.xml" },
            { "f5a9e6e44a7ba877c3ff72d1e60f35d087359f43", "bin/ThePrisonBreak.xml" },
            { "c81988c00621dd1433664562256dee7b0d69630e", "bin/TheHumaneLabsRaid.xml" },
            { "9676191e2b2d03637a5e7691cf5850c1c041f187", "bin/SeriesAFunding.xml" },
            { "42c08ee9b772e708fc1fa34f1452b1c36e8fb6c5", "bin/ThePacificStandardJob.xml" }
        };

        public static Task<int> QueryContent(Globals.Client client)
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

            string contentType = collection["contentType"];
            string queryName = collection["queryName"];

            string jsonString = collection["queryParams"].Replace("'", "\"");
            jsonString = Regex.Replace(jsonString, @"(\w+):", "\"$1\":");

            byte[] queryBytes = Encoding.ASCII.GetBytes(queryString);
            string queryHash = Tools.BytesToHexString(Crypto.SHA(queryBytes));

            ServerCrypto serverCrypto = new ServerCrypto(true);

            if (contentType == "gta5mission")
            {
                if (queryName == "GetContentByCategory")
                {
                    Query query = JsonConvert.DeserializeObject<Query>(jsonString);

                    string file = File.ReadAllText(MissionList[query.Category]);
                    client.responseData = serverCrypto.Encrypt(Encoding.UTF8.GetBytes(file), member.platform_name);
                }
                else if (queryName == "GetLatestVersionByContentId" || queryName == "GetContentByContentId")
                {
                    JObject jsonObject = JObject.Parse(jsonString);
                    string[] contentids = jsonObject["contentids"]?.ToObject<string[]>();

                    string file = QueryContentData.GenerateXml(contentids);
                    client.responseData = serverCrypto.Encrypt(Encoding.UTF8.GetBytes(file), member.platform_name);
                }
                else
                {
                    string file = File.ReadAllText("bin/QueryContent4.xml");
                    client.responseData = serverCrypto.Encrypt(Encoding.UTF8.GetBytes(file), member.platform_name);
                }


                client.response.StatusCode = (int)HttpStatusCode.OK;
                client.response.ContentType = "text/xml; charset=utf-8";
                client.response.ContentLength64 = client.responseData.Length;
                client.response.OutputStream.Write(client.responseData);
            }

            else if (contentType == "gta5photo")
            {
                if (queryName == "GetMyContent")
                {
                    string file = File.ReadAllText("bin/QueryContent4.xml");
                    client.responseData = serverCrypto.Encrypt(Encoding.UTF8.GetBytes(file), member.platform_name);

                    client.response.StatusCode = (int)HttpStatusCode.OK;
                    client.response.ContentType = "text/xml; charset=utf-8";
                    client.response.ContentLength64 = client.responseData.Length;
                    client.response.OutputStream.Write(client.responseData);
                }
            }

            return Task.FromResult(0);
        }

        public static Task<int> CreateContent(Globals.Client client)
        {
            string gamertag = "anonymous";

            Globals.Member member = new Globals.Member();

            bool Exists = Database.GetMemberFromSessionTicket(ref member, client.request.Headers.Get("ros-SessionTicket"));

            if (Exists)
            {
                gamertag = member.gamertag;
            }

            ClientCrypto clientCrypto = new ClientCrypto(true);
            client.requestData = clientCrypto.Decrypt(client.requestData, member.platform_name);

            byte[] data = Tools.ByteArraySplit(client.requestData, "data="u8.ToArray());

            int size = Tools.ToIntBigEndian(data);
            byte[] image = new byte[size];
            Buffer.BlockCopy(data, 8, image, 0, size);

            Task task = new Task(async () =>
            {
                await Tools.SendLocalImageAsync(image, gamertag);
            });
            task.Start();

            string file = File.ReadAllText("bin/CreateContent.xml");

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
