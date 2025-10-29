using System.Net;
using System.Text;

#nullable disable

namespace GTAServer
{
    class Program
    {
        private static HttpListener listener;

        private static byte[] GetRequestData(HttpListenerRequest request)
        {
            try
            {
                byte[] buffer = new byte[4096];
                int count;

                List<byte> data = new List<byte>();

                while ((count = request.InputStream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    byte[] temp = new byte[count];
                    Buffer.BlockCopy(buffer, 0, temp, 0, count);
                    data.AddRange(temp);
                }

                return data.ToArray();
            }
            catch (Exception ex)
            {
                Console.WriteLine(string.Format("[DEBUG] [GetRequestData] Exception: {0}", ex.Message));
            }

            return null;
        }

        private static void HandlePostRequest(Globals.Client client)
        {
            try
            {
                int functionNameOffset = client.path.LastIndexOf('/') + 1;
                string functionName = string.Empty;

                if (functionNameOffset != -1)
                {
                    functionName = client.path.Substring(functionNameOffset);
                }

                if (Globals.PostList.TryGetValue(functionName, out Func<Globals.Client, Task<int>> function))
                {
                    function(client);
                }
                else
                {
                    client.response.StatusCode = (int)HttpStatusCode.NotFound;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(string.Format("[DEBUG] [HandlePostRequest] Exception: {0}", ex.Message));

                client.response.StatusCode = (int)HttpStatusCode.InternalServerError;
            }
            finally
            {
                client.response.Close();
            }
        }

        private static void HandleGetRequest(Globals.Client client)
        {
            try
            {
                int fileNameOffset = client.path.LastIndexOf('/') + 1;
                string fileName = string.Empty;

                if (fileNameOffset != -1)
                {
                    fileName = client.path.Substring(fileNameOffset);
                }

                int fileExtOffset = client.path.LastIndexOf('.') + 1;
                string fileExt = string.Empty;

                if (fileExtOffset != -1)
                {
                    fileExt = client.path.Substring(fileExtOffset);
                }

                if (fileName == string.Empty || fileExt == string.Empty)
                {
                    client.response.StatusCode = (int)HttpStatusCode.InternalServerError;
                    return;
                }

                if (Globals.GetList.Contains(fileName))
                {
                    string session_ticket = client.request.Headers.Get("ros-SessionTicket");

                    Globals.Member member = new Globals.Member();
                    Database.GetMemberFromSessionTicket(ref member, session_ticket);

                    if (client.path.Contains("members"))
                    {
                        string filePath = string.Empty;
                        string memberFilePath = Members.GetPath(client.path, member.platform_name);

                        if (memberFilePath != string.Empty)
                        {
                            filePath = memberFilePath == "DoesNotExist" ? Path.Combine("bin", "DoesNotExist.xml") : memberFilePath;
                        }

                        if (filePath == string.Empty)
                        {
                            client.response.StatusCode = (int)HttpStatusCode.InternalServerError;
                            return;
                        }

                        client.responseData = File.ReadAllBytes(filePath);

                        ServerCrypto serverCrypto = new ServerCrypto(true);
                        client.responseData = serverCrypto.Encrypt(client.responseData, member.platform_name);

                        client.response.StatusCode = filePath.Contains("DoesNotExist") ? (int)HttpStatusCode.NotFound : (int)HttpStatusCode.OK;
                        client.response.ContentType = Tools.GetContentType(fileExt);
                        client.response.ContentLength64 = client.responseData.Length;
                        client.response.OutputStream.Write(client.responseData, 0, client.responseData.Length);
                    }
                    else if (client.path.Contains("gta5mission"))
                    {
                        string filePath = Path.Combine("bin", fileName);
                        string missionFilePath = Missions.GetPath(client.path);

                        if (missionFilePath != string.Empty && missionFilePath != "DoesNotExist")
                        {
                            filePath = missionFilePath;
                        }

                        client.responseData = File.ReadAllBytes(filePath);

                        ServerCrypto serverCrypto = new ServerCrypto(true);
                        client.responseData = serverCrypto.Encrypt(client.responseData, member.platform_name);

                        client.response.StatusCode = (int)HttpStatusCode.OK;
                        client.response.ContentType = Tools.GetContentType(fileExt);
                        client.response.ContentLength64 = client.responseData.Length;
                        client.response.OutputStream.Write(client.responseData, 0, client.responseData.Length);
                    }
                    else
                    {
                        if (client.path.Count(c => c == '/') > 1)
                        {
                            client.responseData = Encoding.UTF8.GetBytes(string.Format("Found. Redirecting to http://tunables.gtao.me/{0}", fileName));
                            client.response.Redirect(string.Format("http://tunables.gtao.me/{0}", fileName));
                            client.response.ContentLength64 = client.responseData.Length;
                            client.response.OutputStream.Write(client.responseData, 0, client.responseData.Length);
                        }
                        else if (client.path.EndsWith("emblem_128.dds"))
                        {
                            string crew_id = "68330145";
                            byte[] crew_emblem = File.ReadAllBytes(Path.Combine("bin", fileName));

                            bool MemberExists = Database.GetMemberFromSessionTicket(ref member, client.request.Headers.Get("ros-SessionTicket"));

                            if (MemberExists)
                            {
                                crew_id = member.crew_id;

                                string crewPath = string.Format("bin/crews/{0}/", crew_id);
                                string fileNamePattern = "emblem_128";
                                string[] extensions = { ".png", ".jpg", ".webp" };

                                bool fileExists = extensions.Any(ext => File.Exists(Path.Combine(crewPath, $"{fileNamePattern}{ext}")));
                                if (fileExists)
                                {
                                    string foundFile = extensions.Select(ext => Path.Combine(crewPath, $"{fileNamePattern}{ext}")).FirstOrDefault(File.Exists);
                                    crew_emblem = Tools.ConvertPngToDdsBytes(foundFile);
                                }
                            }

                            client.responseData = crew_emblem;

                            client.response.StatusCode = (int)HttpStatusCode.OK;
                            client.response.ContentType = Tools.GetContentType(fileExt);
                            client.response.ContentLength64 = client.responseData.Length;
                            client.response.OutputStream.Write(client.responseData, 0, client.responseData.Length);
                        }
                        else
                        {
                            client.responseData = File.ReadAllBytes(Path.Combine("bin", fileName));

                            client.response.StatusCode = (int)HttpStatusCode.OK;
                            client.response.ContentType = Tools.GetContentType(fileExt);
                            client.response.ContentLength64 = client.responseData.Length;
                            client.response.OutputStream.Write(client.responseData, 0, client.responseData.Length);
                        }
                    }
                }
                else
                {
                    client.response.StatusCode = (int)HttpStatusCode.NotFound;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(string.Format("[DEBUG] [HandleGetRequest] Exception: {0}", ex.Message));

                client.response.StatusCode = (int)HttpStatusCode.InternalServerError;
            }
            finally
            {
                client.response.Close();
            }
        }

        private static void HandleRequest(HttpListenerContext context)
        {
            try
            {
                Globals.Client client = new Globals.Client()
                {
                    request = context.Request,
                    requestData = null,

                    response = context.Response,
                    responseData = null,

                    endPoint = context.Request.RemoteEndPoint,

                    method = context.Request.HttpMethod,
                    path = context.Request.Url.AbsolutePath
                };

                Console.WriteLine(string.Format("[{0}] {1} {2}", client.endPoint.ToString(), client.method, client.path));

                client.requestData = GetRequestData(client.request);

                Globals.Member member = new Globals.Member();

                bool Exists = Database.GetMemberFromSessionTicket(ref member, client.request.Headers.Get("ros-SessionTicket"));

                if (Exists)
                {
                    member.last_online = DateTime.Now;
                    member.session_ticket = client.request.Headers.Get("ros-SessionTicket");
                    Database.UpdateLastOnline(ref member);
                }

                switch (client.method)
                {
                    case "POST":
                        HandlePostRequest(client);
                        break;
                    case "GET":
                        HandleGetRequest(client);
                        break;
                    default:
                        client.response.StatusCode = (int)HttpStatusCode.InternalServerError;
                        client.response.Close();
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(string.Format("[DEBUG] [HandleRequest] Exception: {0}", ex.Message));
            }
        }

        private static void Listen()
        {
            while (true)
            {
                try
                {
                    HttpListenerContext context = listener.GetContext();

                    new Thread(new ThreadStart(() => HandleRequest(context))).Start();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(string.Format("[DEBUG] [Listen] Exception: {0}", ex.Message));
                }
            }
        }

        private static void Main(string[] args)
        {
            try
            {
                Console.Title = "GTAServer";

                listener = new HttpListener();
                listener.Prefixes.Add("http://+:80/");
                listener.Start();

                new Thread(new ThreadStart(Listen)).Start();
            }
            catch (Exception ex)
            {
                Console.WriteLine(string.Format("[DEBUG] [Main] Exception: {0}", ex.Message));
            }
        }
    }
}
