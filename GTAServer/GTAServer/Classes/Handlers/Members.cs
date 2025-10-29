using System.Net;
using System.Text;
using System.Text.RegularExpressions;

using Newtonsoft.Json;

#nullable disable

namespace GTAServer
{
    public class Members
    {
        public static Task<int> MpStats(Globals.Client client)
        {
            string session_ticket = client.request.Headers.Get("ros-SessionTicket");

            Globals.Member member = new Globals.Member();
            Database.GetMemberFromSessionTicket(ref member, session_ticket);

            ClientCrypto clientCrypto = new ClientCrypto(true);
            client.requestData = clientCrypto.Decrypt(client.requestData, member.platform_name);

            MultipartFormData multipartFormData = new MultipartFormData();
            bool result = multipartFormData.Parse(client.requestData, GetPath(client.path, member.platform_name));

            if (result)
            {
                Console.WriteLine(string.Format("[DEBUG] MpStats: {0} success", GetPath(client.path, member.platform_name)));
            }

            string file = File.ReadAllText(result ? "bin/Success.xml" : "bin/OK.txt");

            ServerCrypto serverCrypto = new ServerCrypto(true);
            client.responseData = serverCrypto.Encrypt(Encoding.UTF8.GetBytes(file), member.platform_name);

            client.response.StatusCode = (int)HttpStatusCode.OK;
            client.response.ContentType = "text/xml; charset=utf-8";
            client.response.ContentLength64 = client.responseData.Length;
            client.response.OutputStream.Write(client.responseData);

            return Task.FromResult(0);
        }

        private static readonly Regex XblPathFormat = new Regex(@"^/cloud/11/cloudservices/members/xbl/\d+/GTA5/saves/mpstats$", RegexOptions.IgnoreCase);
        private static readonly Regex XblFilePathFormat = new Regex(@"^/cloud/11/cloudservices/members/xbl/\d+/GTA5/saves/mpstats/(save_default0000.save|save_char0001.save|save_char0002.save)$", RegexOptions.IgnoreCase);
        
        private static readonly Regex NpPathFormat = new Regex(@"^/cloud/11/cloudservices/members/np/[^/]+/GTA5/saves/mpstats$", RegexOptions.IgnoreCase);
        private static readonly Regex NpFilePathFormat = new Regex(@"^/cloud/11/cloudservices/members/np/[^/]+/GTA5/saves/mpstats/(save_default0000.save|save_char0001.save|save_char0002.save)$", RegexOptions.IgnoreCase);

        public static string GetPath(string absolutePath, string platformName = "xbox360")
        {
            bool isFile = absolutePath.EndsWith(".save");

            switch (platformName.ToLower())
            {
                case "xbox360":
                    {
                        Match match = isFile ? XblFilePathFormat.Match(absolutePath) : XblPathFormat.Match(absolutePath);

                        if (match.Success)
                        {
                            string filePath = string.Format("bin/members/{0}", isFile
                                ? absolutePath.Replace("/cloud/11/cloudservices/members/xbl/", "").Replace("/GTA5/saves/mpstats/", "/")
                                : absolutePath.Replace("/cloud/11/cloudservices/members/xbl/", "").Replace("/GTA5/saves/mpstats", ""));

                            if (isFile ? File.Exists(filePath) : Directory.Exists(filePath))
                            {
                                return filePath;
                            }

                            return "DoesNotExist";
                        }
                    }
                    break;
                case "ps3":
                    {
                        Match match = isFile ? NpFilePathFormat.Match(absolutePath) : NpPathFormat.Match(absolutePath);

                        if (match.Success)
                        {
                            string path = absolutePath.Replace("/cloud/11/cloudservices/members/np/", "").Replace("/GTA5/saves/mpstats/", "/");
                            string[] parts = path.Split('/');

                            string filePath = string.Format("bin/members/{0}", isFile
                                ? Tools.GenerateXUID(parts[0]) + "/" + parts[1]
                                : Tools.GenerateXUID(absolutePath.Replace("/cloud/11/cloudservices/members/np/", "").Replace("/GTA5/saves/mpstats", "")));

                            if (isFile ? File.Exists(filePath) : Directory.Exists(filePath))
                            {
                                return filePath;
                            }

                            return "DoesNotExist";
                        }
                    } 
                    break;
            }

            return string.Empty;
        }

        public class StatItem
        {
            public string HashKey { get; set; }
            public string Type { get; set; }
            public object Value { get; set; }
        }

        public static byte[] GetStats(string filePath)
        {
            string jsonString = File.ReadAllText(filePath);

            List<StatItem> statItems = JsonConvert.DeserializeObject<List<StatItem>>(jsonString);

            List<byte> data = new List<byte>();

            foreach (StatItem statItem in statItems)
            {
                byte[] hashKeyBytes = Convert.FromHexString(statItem.HashKey);

                byte typeByte = statItem.Type switch
                {
                    "int64" => (byte)0,
                    "int32" => (byte)4,
                    "float" => (byte)3,
                    _ => (byte)0xFF
                };

                data.AddRange(hashKeyBytes);
                data.Add(typeByte);

                switch (statItem.Type)
                {
                    case "int64":
                        {
                            byte[] valueBytes = BitConverter.GetBytes(Convert.ToInt64(statItem.Value));
                            Array.Reverse(valueBytes);

                            data.AddRange(valueBytes);
                        }
                        break;
                    case "int32":
                    case "float":
                        {
                            byte[] valueBytes = BitConverter.GetBytes(Convert.ToInt32(statItem.Value));
                            Array.Reverse(valueBytes);

                            data.AddRange(valueBytes);
                        }
                        break;
                }
            }

            return data.ToArray();
        }

        public static int UpdateStats(string filePath, byte[] compressedStats)
        {
            string jsonString = File.ReadAllText(filePath);

            List<StatItem> statItems = JsonConvert.DeserializeObject<List<StatItem>>(jsonString);

            byte[] decompressedStats = Tools.Decompress(compressedStats);

            byte[] buffer = new byte[decompressedStats.Length - 6];
            Buffer.BlockCopy(decompressedStats, 6, buffer, 0, decompressedStats.Length - 6);

            int offset = 0;
            int count = 0;

            while (offset < buffer.Length)
            {
                byte[] hashKeyBytes = new byte[4];
                Buffer.BlockCopy(buffer, offset, hashKeyBytes, 0, 4);
                string hashKey = Convert.ToHexString(hashKeyBytes);

                StatItem statItem = statItems.FirstOrDefault(item => item.HashKey == hashKey);

                if (statItem != null)
                {
                    offset += 4;

                    if (statItem.Type == "int64")
                    {
                        byte[] valueBytes = new byte[8];
                        Buffer.BlockCopy(buffer, offset, valueBytes, 0, 8);
                        Array.Reverse(valueBytes);

                        statItem.Value = BitConverter.ToInt64(valueBytes, 0);

                        offset += 8;
                        count++;
                    }
                    else if (statItem.Type == "int32" || statItem.Type == "float")
                    {
                        byte[] valueBytes = new byte[4];
                        Buffer.BlockCopy(buffer, offset, valueBytes, 0, 4);
                        Array.Reverse(valueBytes);

                        statItem.Value = BitConverter.ToInt32(valueBytes, 0);

                        offset += 4;
                        count++;
                    }
                }
                else
                {
                    offset += 4;
                }
            }

            string newJsonString = JsonConvert.SerializeObject(statItems, Formatting.Indented);

            string tempFilePath = Path.GetTempFileName();
            File.WriteAllText(tempFilePath, newJsonString);

            try
            {
                File.Replace(tempFilePath, filePath, filePath + ".bak");
            }
            catch (Exception ex)
            {
                Console.WriteLine(string.Format("[DEBUG] [UpdateStats] Exception: {0}", ex.Message));

                return -1;
            }

            return count;
        }

        public static Task<int> MpChars(Globals.Client client)
        {
            string session_ticket = client.request.Headers.Get("ros-SessionTicket");

            Globals.Member member = new Globals.Member();
            Database.GetMemberFromSessionTicket(ref member, session_ticket);

            ClientCrypto clientCrypto = new ClientCrypto(true);
            client.requestData = clientCrypto.Decrypt(client.requestData, member.platform_name);

            MultipartFormData multipartFormData = new MultipartFormData();
            bool result = multipartFormData.Parse(client.requestData, GetPortrait(client.path, member.platform_name));

            if (result)
            {
                Console.WriteLine(string.Format("[DEBUG] MpChars: {0} success", GetPortrait(client.path, member.platform_name)));
            }

            string file = File.ReadAllText(result ? "bin/Success.xml" : "bin/OK.txt");

            ServerCrypto serverCrypto = new ServerCrypto(true);
            client.responseData = serverCrypto.Encrypt(Encoding.UTF8.GetBytes(file), member.platform_name);

            client.response.StatusCode = (int)HttpStatusCode.OK;
            client.response.ContentType = "text/xml; charset=utf-8";
            client.response.ContentLength64 = client.responseData.Length;
            client.response.OutputStream.Write(client.responseData);

            return Task.FromResult(0);
        }

        private static readonly Regex XblPortraitFormat = new Regex(@"^/cloud/11/cloudservices/members/xbl/\d+/share/gta5/mpchars$", RegexOptions.IgnoreCase);
        private static readonly Regex XblPortraitPathFormat = new Regex(@"^/cloud/11/cloudservices/members/xbl/\d+/share/gta5/mpchars/(0.dds|1.dds)$", RegexOptions.IgnoreCase);

        private static readonly Regex NpPortraitFormat = new Regex(@"^/cloud/11/cloudservices/members/xbl/\d+/share/gta5/mpchars$", RegexOptions.IgnoreCase);
        private static readonly Regex NpPortraitPathFormat = new Regex(@"^/cloud/11/cloudservices/members/xbl/\d+/share/gta5/mpchars/(0.dds|1.dds)$", RegexOptions.IgnoreCase);

        public static string GetPortrait(string absolutePath, string platformName = "xbox360")
        {
            bool isFile = absolutePath.EndsWith(".dds");

            switch (platformName.ToLower())
            {
                case "xbox360":
                    {
                        Match match = isFile ? XblPortraitPathFormat.Match(absolutePath) : XblPortraitFormat.Match(absolutePath);

                        if (match.Success)
                        {
                            string filePath = string.Format("bin/members/{0}", isFile
                                ? absolutePath.Replace("/cloud/11/cloudservices/members/xbl/", "").Replace("/share/gta5/mpchars/", "/")
                                : absolutePath.Replace("/cloud/11/cloudservices/members/xbl/", "").Replace("/share/gta5/mpchars", ""));

                            if (isFile ? File.Exists(filePath) : Directory.Exists(filePath))
                            {
                                return filePath;
                            }

                            return string.Empty;
                        }
                    }
                    break;
                case "ps3":
                    {
                        Match match = isFile ? NpPortraitPathFormat.Match(absolutePath) : NpPortraitFormat.Match(absolutePath);

                        if (match.Success)
                        {
                            string filePath = string.Format("bin/members/{0}", isFile
                                ? absolutePath.Replace("/cloud/11/cloudservices/members/np/", "").Replace("/share/gta5/mpchars/", "/")
                                : absolutePath.Replace("/cloud/11/cloudservices/members/np/", "").Replace("/share/gta5/mpchars", ""));

                            if (isFile ? File.Exists(filePath) : Directory.Exists(filePath))
                            {
                                return filePath;
                            }

                            return string.Empty;
                        }
                    }
                    break;
            }

            return string.Empty;
        }
    }
}
