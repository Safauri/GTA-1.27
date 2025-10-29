using System.Security.Cryptography;

#nullable disable

namespace GTAServer
{
    public class Crypto
    {
        public static byte[] XorKey = null;
        public static byte[] HashKey = null;

        public static byte[] SaltKey = { 0x21, 0x91, 0x24, 0xF8, 0xBA, 0x88, 0x4A, 0xDA, 0x18, 0x9B, 0x8F, 0x9F, 0xDE, 0xEC, 0xC1, 0x1B };
        public static byte[] SessionKey = { 0x41, 0x55, 0xE4, 0xA5, 0x74, 0x56, 0x2A, 0xC4, 0x8C, 0x0F, 0x71, 0xC1, 0x6B, 0xF0, 0x7D, 0x52 };

        public static byte[] SaveKey =
        {
            0x66, 0xC0, 0xD6, 0x9E, 0xCE, 0x49, 0xCA, 0x45, 0x76, 0x22, 0xB5, 0x85, 0x8F, 0x29, 0xAC, 0xB0,
            0x3C, 0xBF, 0xFB, 0x0B, 0x76, 0x14, 0x37, 0x23, 0xA1, 0xC2, 0x63, 0xA6, 0x2A, 0xE9, 0x68, 0xEC
        };

        public class RC4
        {
            public int[] S = new int[256];
            public int[] T = new int[256];

            public int i;
            public int j;

            public RC4(byte[] key)
            {
                for (int x = 0; x < 256; x++)
                {
                    S[x] = x;
                }

                if (key.Length == 256)
                {
                    Buffer.BlockCopy(key, 0, T, 0, key.Length);
                }
                else
                {
                    for (int x = 0; x < 256; x++)
                    {
                        T[x] = key[x % key.Length];
                    }
                }

                i = 0;
                j = 0;

                for (i = 0; i < 256; i++)
                {
                    j = (j + S[i] + T[i]) % 256;

                    int temp = S[i];
                    S[i] = S[j];
                    S[j] = temp;
                }

                i = 0;
                j = 0;
            }

            public byte[] Encrypt(byte[] data)
            {
                int dataLength = data.Length;
                byte[] result = new byte[dataLength];

                for (int x = 0; x < dataLength; x++)
                {
                    i = (i + 1) % 256;
                    j = (j + S[i]) % 256;

                    int temp = S[i];
                    S[i] = S[j];
                    S[j] = temp;

                    int K = S[(S[i] + S[j]) % 256];

                    result[x] = (byte)(data[x] ^ K);
                }

                return result;
            }

            public byte[] Decrypt(byte[] data)
            {
                return Encrypt(data);
            }
        }

        public class AES
        {
            private Aes aes;

            public AES(byte[] key, int keySize = 256, int blockSize = 128)
            {
                aes = Aes.Create();

                aes.Mode = CipherMode.ECB;
                aes.Padding = PaddingMode.None;

                aes.KeySize = keySize;
                aes.BlockSize = blockSize;

                aes.Key = key;
            }

            public byte[] Encrypt(byte[] data)
            {
                byte[] buffer = new byte[data.Length];

                int blockSize = aes.BlockSize / 8;

                for (int i = 0; i < data.Length; i += blockSize)
                {
                    aes.CreateEncryptor().TransformBlock(data, i, blockSize, buffer, i);
                }

                return buffer;
            }

            public byte[] Decrypt(byte[] data)
            {
                byte[] buffer = new byte[data.Length];

                int blockSize = aes.BlockSize / 8;

                for (int i = 0; i < data.Length; i += blockSize)
                {
                    aes.CreateDecryptor().TransformBlock(data, i, blockSize, buffer, i);
                }

                return buffer;
            }
        }

        public static byte[] SHA(byte[] data)
        {
            using (SHA1 sha = SHA1.Create())
            {
                return sha.ComputeHash(data);
            }
        }
    }

    public class ClientCrypto
    {
        private bool UseSessionKey;

        public ClientCrypto(bool useSessionKey = false)
        {
            UseSessionKey = useSessionKey;
        }

        public byte[] Decrypt(byte[] data, string platformName = "xbox360")
        {
            switch (platformName.ToLower())
            {
                case "xbox360":
                    {
                        Crypto.XorKey = new byte[] { 0x20, 0x93, 0x27, 0xFC, 0xBF, 0x8E, 0x4D, 0xD2, 0x11, 0x91, 0x84, 0x93, 0xD3, 0xE2, 0xCE, 0x0B };
                    }
                    break;
                case "ps3":
                    {
                        Crypto.XorKey = new byte[] { 0xBA, 0x10, 0x6F, 0xDF, 0x12, 0x84, 0xFD, 0xFC, 0xC2, 0x11, 0x92, 0x83, 0x02, 0x65, 0xE2, 0x8F };
                    }
                    break;
                default:
                    throw new ArgumentException("Unsupported platformName.");
            }

            byte[] saltKey = new byte[0x10];
            Buffer.BlockCopy(data, 0, saltKey, 0, 0x10);

            if (UseSessionKey)
            {
                for (int i = 0; i < 0x10; i++)
                {
                    saltKey[i] ^= Crypto.SessionKey[i];
                }
            }

            byte[] rc4Key = new byte[0x10];
            Buffer.BlockCopy(saltKey, 0, rc4Key, 0, 0x10);

            for (int i = 0; i < 0x10; i++)
            {
                rc4Key[i] ^= Crypto.XorKey[i];
            }

            int encDataLength = data.Length - 0x10;
            byte[] encData = new byte[encDataLength];
            Buffer.BlockCopy(data, 0x10, encData, 0, encDataLength);

            return new Crypto.RC4(rc4Key).Encrypt(encData);
        }
    }

    public class ServerCrypto
    {
        private bool UseSessionKey;

        public ServerCrypto(bool useSessionKey = false)
        {
            UseSessionKey = useSessionKey;
        }

        public byte[] Encrypt(byte[] data, string platformName = "xbox360")
        {
            switch (platformName.ToLower())
            {
                case "xbox360":
                    {
                        Crypto.XorKey = new byte[] { 0x20, 0x93, 0x27, 0xFC, 0xBF, 0x8E, 0x4D, 0xD2, 0x11, 0x91, 0x84, 0x93, 0xD3, 0xE2, 0xCE, 0x0B };
                        Crypto.HashKey = new byte[] { 0x84, 0xAA, 0x50, 0x71, 0x60, 0x0F, 0x1E, 0x84, 0xFE, 0xB9, 0xBD, 0xB4, 0x98, 0x9E, 0x1D, 0x2C };
                    }
                    break;
                case "ps3":
                    {
                        Crypto.XorKey = new byte[] { 0xBA, 0x10, 0x6F, 0xDF, 0x12, 0x84, 0xFD, 0xFC, 0xC2, 0x11, 0x92, 0x83, 0x02, 0x65, 0xE2, 0x8F };
                        Crypto.HashKey = new byte[] { 0x86, 0x0B, 0x0F, 0xFB, 0x22, 0x7E, 0xA2, 0xD2, 0x20, 0x9D, 0x72, 0xAF, 0x4A, 0xB1, 0x8E, 0x8F };
                    }
                    break;
                default:
                    throw new ArgumentException("Unsupported platformName.");
            }

            byte[] saltKey = new byte[0x10];
            Buffer.BlockCopy(Crypto.SaltKey, 0, saltKey, 0, 0x10);

            if (UseSessionKey)
            {
                for (int i = 0; i < 0x10; i++)
                {
                    saltKey[i] ^= Crypto.SessionKey[i];
                }
            }

            byte[] rc4Key = new byte[0x10];
            Buffer.BlockCopy(saltKey, 0, rc4Key, 0, 0x10);

            for (int i = 0; i < 0x10; i++)
            {
                if (UseSessionKey)
                {
                    rc4Key[i] ^= Crypto.SessionKey[i];
                }

                rc4Key[i] ^= Crypto.XorKey[i];
            }

            List<byte> result = new List<byte>();

            int blockSize = 0x400;
            byte[] blockSizeBytes = BitConverter.GetBytes(blockSize);
            Array.Reverse(blockSizeBytes, 0, 4);

            Crypto.RC4 rc4 = new Crypto.RC4(rc4Key);
            blockSizeBytes = rc4.Encrypt(blockSizeBytes);

            result.AddRange(saltKey);
            result.AddRange(blockSizeBytes);

            int dataLength = data.Length;
            int startOffset = 0;

            while (startOffset < dataLength)
            {
                int endOffset = Math.Min(dataLength, startOffset + blockSize);
                int currentBlockSize = endOffset - startOffset;

                byte[] blockData = new byte[currentBlockSize];
                Buffer.BlockCopy(data, startOffset, blockData, 0, currentBlockSize);
                blockData = rc4.Encrypt(blockData);

                byte[] tempData = new byte[blockData.Length + 0x10];
                Buffer.BlockCopy(blockData, 0, tempData, 0, blockData.Length);
                Buffer.BlockCopy(Crypto.HashKey, 0, tempData, blockData.Length, 0x10);
                byte[] blockHash = Crypto.SHA(tempData);

                result.AddRange(blockData);
                result.AddRange(blockHash);

                startOffset = endOffset;
            }

            return result.ToArray();
        }
    }
}
