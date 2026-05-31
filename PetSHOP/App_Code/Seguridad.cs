using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

// Clase con metodos de seguridad: hash y encriptacion
public static class Seguridad
{
    // Clave para AES-256 (debe tener exactamente 32 caracteres)
    private const string CLAVE_AES = "PetSHOP_AES_Key_32Bytes_Academic";

    // Convierte un texto a su hash SHA-256 en hexadecimal
    public static string HashSHA256(string texto)
    {
        SHA256 sha = SHA256.Create();
        byte[] bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(texto));

        // Convertimos cada byte a dos caracteres hexadecimales
        StringBuilder resultado = new StringBuilder();
        for (int i = 0; i < bytes.Length; i++)
        {
            resultado.Append(bytes[i].ToString("x2"));
        }

        sha.Dispose();
        return resultado.ToString();
    }

    // Encripta un texto con AES-256 y devuelve el resultado en Base64
    // El IV aleatorio se guarda junto con el texto cifrado
    public static string Encriptar(string texto)
    {
        Aes aes = Aes.Create();
        aes.KeySize = 256;
        aes.Key = Encoding.UTF8.GetBytes(CLAVE_AES);
        aes.GenerateIV(); // IV aleatorio cada vez

        ICryptoTransform encriptador = aes.CreateEncryptor();
        MemoryStream ms = new MemoryStream();

        // Primero escribimos el IV para poder desencriptar despues
        ms.Write(aes.IV, 0, aes.IV.Length);

        CryptoStream cs = new CryptoStream(ms, encriptador, CryptoStreamMode.Write);
        StreamWriter sw = new StreamWriter(cs, Encoding.UTF8);
        sw.Write(texto);
        sw.Close();
        cs.Close();

        byte[] resultado = ms.ToArray();
        ms.Close();
        aes.Dispose();

        return Convert.ToBase64String(resultado);
    }

    // Desencripta un texto que fue encriptado con el metodo Encriptar
    public static string Desencriptar(string textoCifrado)
    {
        byte[] datos = Convert.FromBase64String(textoCifrado);

        Aes aes = Aes.Create();
        aes.KeySize = 256;
        aes.Key = Encoding.UTF8.GetBytes(CLAVE_AES);

        // Los primeros 16 bytes son el IV
        byte[] iv = new byte[16];
        Array.Copy(datos, 0, iv, 0, 16);
        aes.IV = iv;

        ICryptoTransform desencriptador = aes.CreateDecryptor();
        MemoryStream ms = new MemoryStream(datos, 16, datos.Length - 16);
        CryptoStream cs = new CryptoStream(ms, desencriptador, CryptoStreamMode.Read);
        StreamReader sr = new StreamReader(cs, Encoding.UTF8);

        string resultado = sr.ReadToEnd();

        sr.Close();
        cs.Close();
        ms.Close();
        aes.Dispose();

        return resultado;
    }
}
