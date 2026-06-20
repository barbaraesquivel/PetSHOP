using System;
using System.Data.SqlClient;
using System.IO;
using System.Web;

namespace DAL
{
    public static class ConexionBD
    {
        private const string CadenaConexion =
            "Server=159.112.151.168;Database=PetShop;User Id=sa;Password=Barby846!;" +
            "TrustServerCertificate=True;Encrypt=True;";

        private static string ObtenerCadena() => CadenaConexion;

        public static SqlConnection ObtenerConexion()
        {
            return new SqlConnection(ObtenerCadena());
        }

        // prueba si se puede conectar a la BD
        public static bool EstaDisponible()
        {
            try
            {
                using (SqlConnection con = ObtenerConexion())
                {
                    con.Open();
                    RegistrarEnLog("sistema", "DB_OK", "Conexion a la base de datos exitosa");
                    return true;
                }
            }
            catch (Exception ex)
            {
                RegistrarEnLog("sistema", "DB_ERROR", "No se pudo conectar a la BD: " + ex.Message);
                return false;
            }
        }

        // escribe directo al archivo sin pasar por bitacora
        private static void RegistrarEnLog(string usuario, string accion, string detalle)
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
                             + usuario + " | " + accion + " | " + detalle;

                System.IO.File.AppendAllText(Path.Combine(carpeta, "bitacora.txt"), linea + Environment.NewLine);
            }
            catch { }
        }
    }
}
