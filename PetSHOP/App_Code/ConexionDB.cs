using System;
using System.Configuration;
using System.Data.SqlClient;
using System.IO;
using System.Web;

// Clase para obtener conexiones a la base de datos
public static class ConexionDB
{
    // Lee la cadena de conexion del Web.config (name="PetShopDB")
    private static string ObtenerCadena()
    {
        return ConfigurationManager.ConnectionStrings["PetShopDB"].ConnectionString;
    }

    // Devuelve una nueva SqlConnection sin abrir
    // Usar siempre con using() para cerrarla automaticamente
    public static SqlConnection ObtenerConexion()
    {
        return new SqlConnection(ObtenerCadena());
    }

    // Prueba si se puede conectar a la BD
    // Devuelve true si esta disponible, false si hubo error
    public static bool EstaDisponible()
    {
        try
        {
            using (SqlConnection con = ObtenerConexion())
            {
                con.Open();
                RegistrarEnLog("sistema", "DB_OK", "Conexion a la base de datos exitosa", "servidor");
                return true;
            }
        }
        catch (Exception ex)
        {
            RegistrarEnLog("sistema", "DB_ERROR", "No se pudo conectar a la BD: " + ex.Message, "servidor");
            return false;
        }
    }

    // Escribe directo al archivo sin pasar por Bitacora
    // (se usa en Application_Start donde HttpContext puede ser null)
    private static void RegistrarEnLog(string usuario, string accion, string detalle, string ip)
    {
        try
        {
            string carpeta;
            if (HttpContext.Current != null)
                carpeta = HttpContext.Current.Server.MapPath("~/App_Data");
            else
                carpeta = Path.Combine(System.Web.HttpRuntime.AppDomainAppPath, "App_Data");

            if (!Directory.Exists(carpeta))
                Directory.CreateDirectory(carpeta);

            string linea = "[" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "] | "
                         + usuario + " | " + accion + " | " + detalle + " | " + ip;

            File.AppendAllText(Path.Combine(carpeta, "bitacora.txt"), linea + Environment.NewLine);
        }
        catch { }
    }
}
