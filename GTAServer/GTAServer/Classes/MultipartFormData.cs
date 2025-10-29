using System.Text;

#nullable disable

namespace GTAServer
{
    public class MultipartFormData
    {
        private string GetBoundary(byte[] fileData)
        {
            string fileText = Encoding.ASCII.GetString(fileData);

            int boundaryStart = fileText.IndexOf("--");

            if (boundaryStart == -1)
            {
                return string.Empty;
            }

            int boundaryEnd = fileText.IndexOf("\r\n", boundaryStart);

            if (boundaryEnd == -1)
            {
                return string.Empty;
            }

            return fileText.Substring(boundaryStart + 2, boundaryEnd - boundaryStart - 2);
        }

        private int FindBoundaryIndex(byte[] data, int startIndex, byte[] boundaryBytes)
        {
            for (int i = startIndex; i < data.Length - boundaryBytes.Length; i++)
            {
                bool match = true;
                for (int j = 0; j < boundaryBytes.Length; j++)
                {
                    if (data[i + j] != boundaryBytes[j])
                    {
                        match = false;
                        break;
                    }
                }

                if (match)
                {
                    return i;
                }
            }

            return -1;
        }

        public bool Parse(byte[] data, string folderName)
        {
            string boundary = GetBoundary(data);

            string startBoundary = "--" + boundary;
            string endBoundary = "--" + boundary + "--";

            byte[] startBoundaryBytes = Encoding.ASCII.GetBytes(startBoundary);
            byte[] endBoundaryBytes = Encoding.ASCII.GetBytes(endBoundary);

            int boundaryIndex = 0;
            int boundaryLength = startBoundaryBytes.Length;

            while (boundaryIndex < data.Length)
            {
                int startIndex = FindBoundaryIndex(data, boundaryIndex, startBoundaryBytes);

                if (startIndex == -1)
                {
                    break;
                }

                int endIndex = FindBoundaryIndex(data, startIndex + boundaryLength, endBoundaryBytes);

                if (endIndex == -1)
                {
                    break;
                }

                byte[] partData = new byte[endIndex - startIndex - boundaryLength];
                Buffer.BlockCopy(data, startIndex + boundaryLength, partData, 0, partData.Length);

                string partContent = Encoding.ASCII.GetString(partData);

                int headersEnd = partContent.IndexOf("\r\n\r\n");
                string headers = partContent.Substring(0, headersEnd);

                string fileName = string.Empty;

                if (headers.Contains("Content-Disposition"))
                {
                    int fileNameIndex = headers.IndexOf("filename=\"");

                    if (fileNameIndex != -1)
                    {
                        fileName = headers.Substring(fileNameIndex + 10);
                        fileName = fileName.Substring(0, fileName.IndexOf("\""));
                    }
                }

                if (string.IsNullOrEmpty(fileName) || string.IsNullOrEmpty(folderName))
                {
                    return false;
                }

                int contentStart = partContent.IndexOf("\r\n\r\n") + 4;
                int contentLength = partData.Length - (contentStart + 2);

                byte[] content = new byte[contentLength];
                Buffer.BlockCopy(partData, contentStart, content, 0, contentLength);

                byte[] fileHeader = new byte[0x10];
                Buffer.BlockCopy(content, 0, fileHeader, 0, 0x10);

                Crypto.AES aes = new Crypto.AES(Crypto.SaveKey);
                byte[] decFileHeader = aes.Decrypt(fileHeader);

                byte[] temp = new byte[4];
                Buffer.BlockCopy(decFileHeader, 0, temp, 0, 4);
                Array.Reverse(temp, 0, 4);

                UInt32 magic = BitConverter.ToUInt32(temp, 0);
                bool status = magic == 0x5053494E || magic == 0x5347565A; // PSIN or SGVZ

                if (!status)
                {
                    return false;
                }

                File.WriteAllBytes(Path.Combine(folderName, fileName), content);

                boundaryIndex = endIndex + boundaryLength;
            }

            return true;
        }
    }
}
