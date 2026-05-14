using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace TeruTeruServer.ManageLogic.Util
{
    public class SeedEncrypt : baseEncrypt
    {
        private const int BlockSize = 16; // 128 bits
        private const int KeySize = 16;   // 128 bits
        private const int NumRounds = 16;

        #region S-Box Tables
        private static readonly uint[] S1 = {
            169, 133, 214, 211, 84, 29, 172, 37, 93, 67, 24, 30, 81, 252, 202, 99,
            40, 68, 32, 157, 224, 226, 200, 23, 165, 143, 3, 123, 187, 19, 210, 238,
            112, 140, 63, 168, 50, 221, 246, 116, 236, 149, 11, 87, 92, 91, 189, 1,
            3, 36, 28, 115, 152, 16, 204, 242, 217, 44, 231, 114, 131, 155, 209, 134,
            201, 96, 80, 163, 235, 13, 182, 158, 79, 183, 90, 198, 120, 166, 18, 175,
            213, 97, 195, 180, 65, 82, 125, 141, 8, 31, 153, 91, 0, 25, 4, 83,
            247, 225, 253, 118, 47, 39, 176, 139, 14, 171, 162, 110, 147, 77, 105, 124,
            9, 7, 10, 191, 239, 243, 197, 135, 20, 254, 100, 222, 46, 75, 26, 6,
            33, 107, 8, 102, 2, 245, 146, 138, 12, 179, 126, 208, 122, 71, 150, 229,
            38, 128, 173, 223, 161, 48, 55, 174, 54, 21, 34, 56, 244, 167, 69, 76,
            129, 233, 132, 151, 53, 203, 206, 60, 113, 17, 199, 137, 117, 251, 218, 248,
            148, 89, 130, 196, 255, 73, 57, 103, 192, 207, 215, 184, 15, 142, 66, 35,
            145, 108, 219, 164, 52, 241, 72, 194, 111, 61, 45, 64, 190, 62, 188, 193,
            170, 186, 78, 85, 59, 220, 104, 127, 156, 216, 74, 86, 119, 160, 237, 70,
            181, 43, 101, 250, 227, 185, 177, 159, 94, 249, 230, 178, 49, 234, 109, 95,
            228, 240, 205, 136, 22, 58, 88, 212, 98, 41, 7, 51, 232, 27, 5, 121,
            144, 106, 42
        };

        private static readonly uint[] S2 = {
            56, 232, 45, 166, 207, 222, 179, 184, 175, 96, 85, 199, 68, 111, 107, 91,
            195, 98, 51, 181, 41, 160, 226, 167, 211, 145, 17, 6, 28, 188, 54, 75,
            239, 136, 108, 168, 23, 196, 22, 244, 194, 69, 225, 214, 63, 61, 142, 152,
            40, 78, 246, 62, 165, 249, 13, 223, 216, 43, 102, 122, 39, 47, 241, 114,
            66, 212, 65, 192, 115, 103, 172, 139, 247, 173, 128, 31, 202, 44, 170, 52,
            210, 11, 238, 233, 93, 148, 24, 248, 87, 174, 8, 197, 19, 205, 134, 185,
            255, 125, 193, 49, 245, 138, 106, 177, 209, 32, 215, 2, 34, 4, 104, 113,
            7, 219, 157, 153, 97, 190, 230, 89, 221, 81, 144, 220, 154, 163, 171, 208,
            129, 15, 71, 26, 227, 236, 141, 191, 150, 123, 92, 162, 161, 99, 35, 77,
            200, 158, 156, 58, 12, 46, 186, 110, 159, 90, 242, 146, 243, 73, 120, 204,
            21, 251, 112, 117, 127, 53, 16, 3, 100, 109, 198, 116, 213, 180, 234, 9,
            118, 25, 254, 64, 18, 224, 189, 5, 250, 1, 240, 42, 94, 169, 86, 67,
            133, 20, 137, 155, 176, 229, 72, 121, 151, 252, 30, 130, 33, 140, 27, 95,
            119, 84, 178, 29, 37, 79, 0, 70, 237, 88, 82, 235, 126, 218, 201, 253,
            48, 149, 101, 60, 182, 228, 187, 124, 14, 80, 57, 38, 50, 132, 105, 147,
            55, 231, 36, 164, 203, 83, 10, 135, 217, 76, 131, 143, 206, 59, 74, 183
        };
        #endregion

        private static readonly uint[] KC = {
            0x9e3779b9, 0x3c6ef373, 0x78dde6e6, 0xf1bbcdcc,
            0xe3779b99, 0xc6ef3733, 0x8dde6e67, 0x1bbcdccf,
            0x3779b99e, 0x6ef3733c, 0xdde6e678, 0xbbcdccf1,
            0x779b99e3, 0xef3733c6, 0xde6e678d, 0xbcdccf1b
        };

        public override string EncryptString(string inputText, string password)
        {
            byte[] iv = GenerateRandomIV();
            byte[] key;
            using (var keyGenerator = new Rfc2898DeriveBytes(password, salt: iv))
            {
                key = keyGenerator.GetBytes(KeySize);
            }

            byte[] inputBytes = Encoding.UTF8.GetBytes(inputText);
            byte[] paddedInput = Pad(inputBytes);
            byte[] encryptedData = EncryptCBC(paddedInput, key, iv);

            byte[] result = new byte[iv.Length + encryptedData.Length];
            Buffer.BlockCopy(iv, 0, result, 0, iv.Length);
            Buffer.BlockCopy(encryptedData, 0, result, iv.Length, encryptedData.Length);

            return Convert.ToBase64String(result);
        }

        public override string DecryptString(string inputText, string password)
        {
            byte[] fullData = Convert.FromBase64String(inputText);
            byte[] iv = new byte[BlockSize];
            Buffer.BlockCopy(fullData, 0, iv, 0, BlockSize);

            byte[] encryptedData = new byte[fullData.Length - BlockSize];
            Buffer.BlockCopy(fullData, BlockSize, encryptedData, 0, encryptedData.Length);

            byte[] key;
            using (var keyGenerator = new Rfc2898DeriveBytes(password, salt: iv))
            {
                key = keyGenerator.GetBytes(KeySize);
            }

            byte[] decryptedData = DecryptCBC(encryptedData, key, iv);
            byte[] unpaddedData = Unpad(decryptedData);

            return Encoding.UTF8.GetString(unpaddedData);
        }

        private static byte[] GenerateRandomIV()
        {
            byte[] iv = new byte[BlockSize];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(iv);
            }
            return iv;
        }

        private byte[] Pad(byte[] data)
        {
            int paddingLength = BlockSize - (data.Length % BlockSize);
            byte[] paddedData = new byte[data.Length + paddingLength];
            Buffer.BlockCopy(data, 0, paddedData, 0, data.Length);
            for (int i = data.Length; i < paddedData.Length; i++)
            {
                paddedData[i] = (byte)paddingLength;
            }
            return paddedData;
        }

        private byte[] Unpad(byte[] data)
        {
            int paddingLength = data[data.Length - 1];
            if (paddingLength < 1 || paddingLength > BlockSize)
                throw new CryptographicException("Invalid padding.");

            byte[] unpaddedData = new byte[data.Length - paddingLength];
            Buffer.BlockCopy(data, 0, unpaddedData, 0, unpaddedData.Length);
            return unpaddedData;
        }

        private byte[] EncryptCBC(byte[] data, byte[] key, byte[] iv)
        {
            uint[] roundKeys = GenerateRoundKeys(key);
            byte[] result = new byte[data.Length];
            byte[] prevBlock = (byte[])iv.Clone();

            for (int i = 0; i < data.Length; i += BlockSize)
            {
                byte[] currentBlock = new byte[BlockSize];
                for (int j = 0; j < BlockSize; j++)
                    currentBlock[j] = (byte)(data[i + j] ^ prevBlock[j]);

                byte[] encryptedBlock = EncryptBlock(currentBlock, roundKeys);
                Buffer.BlockCopy(encryptedBlock, 0, result, i, BlockSize);
                prevBlock = encryptedBlock;
            }
            return result;
        }

        private byte[] DecryptCBC(byte[] data, byte[] key, byte[] iv)
        {
            uint[] roundKeys = GenerateRoundKeys(key);
            byte[] result = new byte[data.Length];
            byte[] prevBlock = (byte[])iv.Clone();

            for (int i = 0; i < data.Length; i += BlockSize)
            {
                byte[] currentBlock = new byte[BlockSize];
                Buffer.BlockCopy(data, i, currentBlock, 0, BlockSize);

                byte[] decryptedBlock = DecryptBlock(currentBlock, roundKeys);
                for (int j = 0; j < BlockSize; j++)
                    result[i + j] = (byte)(decryptedBlock[j] ^ prevBlock[j]);

                prevBlock = currentBlock;
            }
            return result;
        }

        private uint[] GenerateRoundKeys(byte[] key)
        {
            uint[] roundKeys = new uint[NumRounds * 2];
            uint A = GetUInt32BE(key, 0);
            uint B = GetUInt32BE(key, 4);
            uint C = GetUInt32BE(key, 8);
            uint D = GetUInt32BE(key, 12);

            for (int i = 0; i < NumRounds; i++)
            {
                roundKeys[i * 2] = G(A + C - KC[i]);
                roundKeys[i * 2 + 1] = G(B - D + KC[i]);

                if (i % 2 == 0) // Odd round (1, 3, ...) in 1-based index, but i is 0-based
                {
                    uint temp = A;
                    A = (A >> 8) | (B << 24);
                    B = (B >> 8) | (temp << 24);
                }
                else
                {
                    uint temp = C;
                    C = (C << 8) | (D >> 24);
                    D = (D << 8) | (temp >> 24);
                }
            }
            return roundKeys;
        }

        private byte[] EncryptBlock(byte[] inBlock, uint[] roundKeys)
        {
            uint L = GetUInt32BE(inBlock, 0);
            uint R = GetUInt32BE(inBlock, 4);
            uint C = GetUInt32BE(inBlock, 8);
            uint D = GetUInt32BE(inBlock, 12);

            for (int i = 0; i < NumRounds; i++)
            {
                uint nextL, nextR, nextC, nextD;
                // Feistel swap
                nextL = C;
                nextR = D;

                uint F_res0, F_res1;
                F(C, D, roundKeys[i * 2], roundKeys[i * 2 + 1], out F_res0, out F_res1);

                nextC = L ^ F_res0;
                nextD = R ^ F_res1;

                L = nextL; R = nextR; C = nextC; D = nextD;
            }

            byte[] outBlock = new byte[BlockSize];
            PutUInt32BE(C, outBlock, 0);
            PutUInt32BE(D, outBlock, 4);
            PutUInt32BE(L, outBlock, 8);
            PutUInt32BE(R, outBlock, 12);
            return outBlock;
        }

        private byte[] DecryptBlock(byte[] inBlock, uint[] roundKeys)
        {
            uint L = GetUInt32BE(inBlock, 0);
            uint R = GetUInt32BE(inBlock, 4);
            uint C = GetUInt32BE(inBlock, 8);
            uint D = GetUInt32BE(inBlock, 12);

            for (int i = NumRounds - 1; i >= 0; i--)
            {
                uint nextL, nextR, nextC, nextD;
                // Reverse Feistel swap
                nextL = C;
                nextR = D;

                uint F_res0, F_res1;
                F(C, D, roundKeys[i * 2], roundKeys[i * 2 + 1], out F_res0, out F_res1);

                nextC = L ^ F_res0;
                nextD = R ^ F_res1;

                L = nextL; R = nextR; C = nextC; D = nextD;
            }

            byte[] outBlock = new byte[BlockSize];
            PutUInt32BE(C, outBlock, 0);
            PutUInt32BE(D, outBlock, 4);
            PutUInt32BE(L, outBlock, 8);
            PutUInt32BE(R, outBlock, 12);
            return outBlock;
        }

        private void F(uint C, uint D, uint Ki0, uint Ki1, out uint C_out, out uint D_out)
        {
            uint T0 = C ^ Ki0;
            uint T1 = D ^ Ki1;
            uint X = T0 ^ T1;
            uint Y = G(X);
            uint Z = G(Y + T0);
            D_out = Y + Z;
            C_out = D_out + Z;
        }

        private uint G(uint X)
        {
            uint X3 = (X >> 24) & 0xFF;
            uint X2 = (X >> 16) & 0xFF;
            uint X1 = (X >> 8) & 0xFF;
            uint X0 = X & 0xFF;

            uint Y3 = S2[X3];
            uint Y2 = S1[X2];
            uint Y1 = S2[X1];
            uint Y0 = S1[X0];

            uint m0 = 0xfc, m1 = 0xf3, m2 = 0xcf, m3 = 0x3f;

            uint Z3 = (Y0 & m3) ^ (Y1 & m0) ^ (Y2 & m1) ^ (Y3 & m2);
            uint Z2 = (Y0 & m2) ^ (Y1 & m3) ^ (Y2 & m0) ^ (Y3 & m1);
            uint Z1 = (Y0 & m1) ^ (Y1 & m2) ^ (Y2 & m3) ^ (Y3 & m0);
            uint Z0 = (Y0 & m0) ^ (Y1 & m1) ^ (Y2 & m2) ^ (Y3 & m3);

            return (Z3 << 24) | (Z2 << 16) | (Z1 << 8) | Z0;
        }

        private uint GetUInt32BE(byte[] data, int offset)
        {
            return ((uint)data[offset] << 24) | ((uint)data[offset + 1] << 16) | ((uint)data[offset + 2] << 8) | (uint)data[offset + 3];
        }

        private void PutUInt32BE(uint value, byte[] data, int offset)
        {
            data[offset] = (byte)(value >> 24);
            data[offset + 1] = (byte)(value >> 16);
            data[offset + 2] = (byte)(value >> 8);
            data[offset + 3] = (byte)value;
        }
    }
}