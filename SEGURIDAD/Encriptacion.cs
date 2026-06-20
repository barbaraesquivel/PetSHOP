using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace SEGURIDAD
{
    //hash SHA-256 y encriptacion AES-256
    public static class Encriptacion
    {
        private const string CLAVE_AES = "PetSHOP_AES_Key_32Bytes_Academic";

        // Convierte un texto a su hash SHA-256
        public static string HashSHA256(string texto)
        {
            SHA256 sha = SHA256.Create();
            byte[] bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(texto));

            StringBuilder resultado = new StringBuilder();
            for (int i = 0; i < bytes.Length; i++)
                resultado.Append(bytes[i].ToString("x2"));

            sha.Dispose();
            return resultado.ToString();
        }

        // Encripta con AES-256
        public static string Encriptar(string texto)
        {
            Aes aes = Aes.Create();
            aes.KeySize = 256;
            aes.Key     = Encoding.UTF8.GetBytes(CLAVE_AES);
            aes.GenerateIV();

            ICryptoTransform enc = aes.CreateEncryptor();
            MemoryStream ms = new MemoryStream();
            ms.Write(aes.IV, 0, aes.IV.Length);

            CryptoStream cs = new CryptoStream(ms, enc, CryptoStreamMode.Write);
            StreamWriter  sw = new StreamWriter(cs, Encoding.UTF8);
            sw.Write(texto);
            sw.Close();
            cs.Close();

            byte[] resultado = ms.ToArray();
            ms.Close();
            aes.Dispose();
            return Convert.ToBase64String(resultado);
        }

        // desencripta un texto cifrado con Encriptar
        public static string Desencriptar(string textoCifrado)
        {
            byte[] datos = Convert.FromBase64String(textoCifrado);

            Aes aes = Aes.Create();
            aes.KeySize = 256;
            aes.Key     = Encoding.UTF8.GetBytes(CLAVE_AES);

            byte[] iv = new byte[16];
            Array.Copy(datos, 0, iv, 0, 16);
            aes.IV = iv;

            ICryptoTransform dec = aes.CreateDecryptor();
            MemoryStream ms = new MemoryStream(datos, 16, datos.Length - 16);
            CryptoStream cs = new CryptoStream(ms, dec, CryptoStreamMode.Read);
            StreamReader  sr = new StreamReader(cs, Encoding.UTF8);

            string resultado = sr.ReadToEnd();
            sr.Close(); cs.Close(); ms.Close();
            aes.Dispose();
            return resultado;
        }
    }
}
