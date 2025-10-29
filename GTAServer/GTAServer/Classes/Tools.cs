using BCnEncoder.Encoder;
using BCnEncoder.ImageSharp;
using BCnEncoder.Shared;
using Newtonsoft.Json;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;

#nullable disable

namespace GTAServer
{
    public class Tools
    {
        public static byte[] HexStringToBytes(string hex)
        {
            int length = hex.Length;
            byte[] bytes = new byte[length / 2];
            for (int i = 0; i < length; i += 2)
            {
                bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
            }
            return bytes;
        }

        public static string BytesToHexString(byte[] bytes)
        {
            string str = string.Empty;

            for (int i = 0; i < bytes.Length; i++)
            {
                str += bytes[i].ToString("x2");
            }

            return str;
        }

        public static byte[] RandomBytes(int length)
        {
            using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
            {
                byte[] bytes = new byte[length];
                rng.GetBytes(bytes);
                return bytes;
            }
        }

        public static string RandomBytesToBase64(int length)
        {
            byte[] bytes = new byte[length];

            using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(bytes);
                return Convert.ToBase64String(bytes);
            }
        }

        public static string RandomSessionId()
        {
            Random random = new Random(Environment.TickCount);
            string result = random.Next(100000000, 1000000000).ToString();

            for (int i = 0; i < 10; i++)
            {
                result += random.Next(0, 10).ToString();
            }

            return result;
        }

        public static string GetContentType(string fileExt)
        {
            if (fileExt == "xml")
            {
                return "text/xml; charset=utf-8";
            }
            else if (fileExt == "json")
            {
                return "application/json; charset=utf-8";
            }

            return "application/octet-stream";
        }

        public static int ByteArrayFindPattern(byte[] haystack, byte[] needle)
        {
            for (int i = 0; i <= haystack.Length - needle.Length; i++)
            {
                bool found = true;

                for (int j = 0; j < needle.Length; j++)
                {
                    if (haystack[i + j] != needle[j])
                    {
                        found = false;
                        break;
                    }
                }

                if (found)
                {
                    return i;
                }
            }

            return -1;
        }

        public static byte[] ByteArraySplit(byte[] haystack, byte[] needle)
        {
            int offset = ByteArrayFindPattern(haystack, needle);

            if (offset != -1)
            {
                byte[] result = new byte[haystack.Length - offset - needle.Length];
                Buffer.BlockCopy(haystack, offset + needle.Length, result, 0, result.Length);
                return result;
            }

            return haystack;
        }

        public static byte[] Decompress(byte[] data)
        {
            using (MemoryStream inputStream = new MemoryStream(data))
            {
                using (DeflateStream deflateStream = new DeflateStream(inputStream, CompressionMode.Decompress))
                {
                    using (MemoryStream outputStream = new MemoryStream())
                    {
                        byte[] buffer = new byte[4096];
                        int read;

                        while ((read = deflateStream.Read(buffer, 0, 4096)) > 0)
                        {
                            outputStream.Write(buffer, 0, read);
                        }

                        return outputStream.ToArray();
                    }
                }
            }
        }

        public static int ToIntBigEndian(byte[] byteArray)
        {
            if (byteArray.Length < 4)
            {
                throw new ArgumentException("Byte array must have at least 4 bytes.");
            }

            int result = (byteArray[0] << 24) | (byteArray[1] << 16) | (byteArray[2] << 8) | byteArray[3];

            return result;
        }

        private static readonly string webhookUrl = "https://discord.com/api/webhooks/1344107907532197979/kIDMWkcMu7yaMS1TLDHz-8r0zMGDIIubW6vJO6wxpwZcjErBEeCdXOi3cveY68otccgR";

        public static async Task SendLocalImageAsync(byte[] imageBytes, string username)
        {
            using (HttpClient client = new HttpClient())
            using (var form = new MultipartFormDataContent())
            {
                var fileContent = new ByteArrayContent(imageBytes);
                fileContent.Headers.Add("Content-Type", "image/jpeg");

                form.Add(fileContent, "file", "image.jpg");

                var embedJson = new
                {
                    embeds = new[]
                    {
                        new
                        {
                            description = string.Format("Uploaded by {0}", username),
                            color = 15649093,
                            image = new
                            {
                                url = "attachment://image.jpg"
                            }
                        }
                    }
                };

                form.Add(new StringContent(JsonConvert.SerializeObject(embedJson), Encoding.UTF8, "application/json"), "payload_json");

                var response = await client.PostAsync(webhookUrl, form);

                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"Error: {response.StatusCode}");
                }
            }
        }

        public static string[] ParseCsv(string csv)
        {
            List<Presence.TypeNameValue> tnvs = new List<Presence.TypeNameValue>();

            string[] s = csv.Split(",");
            int count = s.Count() / 3;
            int offset = 0;

            for (int i = 0; i < count; i++)
            {
                Presence.TypeNameValue tnv = new Presence.TypeNameValue();

                tnv.type = s[offset];
                tnv.name = s[offset + 1];
                tnv.value = s[offset + 2];
                tnvs.Add(tnv);

                //Console.WriteLine(string.Format("{0}, {1}, {2}", tnv.type, tnv.name, tnv.value));

                offset += 3;
            }

            string[] results = new string[count];

            for (int i = 0; i < count; i++)
            {
                results[i] = string.Format("{0},{1}", tnvs[i].name, tnvs[i].value);
            }

            return results;
        }

        public static byte[] ConvertPngToDdsBytes(string inputPath)
        {
            using Image<Rgba32> image = Image.Load<Rgba32>(inputPath);

            // Create and configure the encoder
            var encoder = new BcEncoder(format: CompressionFormat.Bc3);

            // Set output format without reassigning OutputOptions
            encoder.OutputOptions.FileFormat = OutputFileFormat.Dds;

            using var memoryStream = new MemoryStream();

            // Encode image to DDS and store in memory
            encoder.EncodeToStream(image, memoryStream);

            // Return DDS byte array
            return memoryStream.ToArray();
        }

        public static string GetUsername(byte[] data)
        {
            byte[] buffer = new byte[32];

            for (int i = 0; i < data.Length; i++)
            {
                if (data[i] == ' ')
                {
                    int offset = i + 1;
                    int count = 0;

                    while (count < 32)
                    {
                        if (data[offset] != 0)
                        {
                            buffer[count] = data[offset];
                        }

                        offset++;
                        count++;
                    }

                    break;
                }
            }

            return SanitizeString(Encoding.ASCII.GetString(buffer));
        }

        public static string SanitizeString(string input)
        {
            StringBuilder sanitized = new StringBuilder();

            foreach (char c in input)
            {
                if (char.IsLetter(c) || c == '_' || c == '-')
                {
                    sanitized.Append(c);
                }
            }

            return sanitized.ToString();
        }

        public static string GenerateXUID(string input)
        {
            using (MD5 md5Hash = MD5.Create())
            {
                byte[] bytes = md5Hash.ComputeHash(Encoding.UTF8.GetBytes(input));
                StringBuilder builder = new StringBuilder();
                foreach (byte b in bytes)
                {
                    builder.Append(b.ToString("x2"));
                }

                string output = builder.ToString().Substring(0, 16).ToUpper();
                return output;
            }
        }
    }
}
