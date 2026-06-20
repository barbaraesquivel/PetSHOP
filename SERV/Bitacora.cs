using System.Data.SqlClient;
using System.Web;
using DAL;

namespace SERV
{
    public static class Bitacora
    {
        public static void Registrar(string usuario, string accion, string detalle)
        {
            try
            {
                bool dbDisponible = false;
                if (HttpContext.Current != null && HttpContext.Current.Application["DBDisponible"] != null)
                    dbDisponible = (bool)HttpContext.Current.Application["DBDisponible"];

                if (dbDisponible)
                    RegistrarEnDB(usuario, accion, detalle);
            }
            catch { }
        }

        private static void RegistrarEnDB(string usuario, string accion, string detalle)
        {
            using (SqlConnection con = ConexionBD.ObtenerConexion())
            {
                con.Open();
                SqlCommand cmd = new SqlCommand(
                    "INSERT INTO LogBitacora (FechaHora, NombreUsuario, Accion, Detalle) VALUES (@f, @u, @a, @d)", con);
                cmd.Parameters.AddWithValue("@f", System.DateTime.Now);
                cmd.Parameters.AddWithValue("@u", usuario);
                cmd.Parameters.AddWithValue("@a", accion);
                cmd.Parameters.AddWithValue("@d", detalle);
                cmd.ExecuteNonQuery();
            }
        }
    }
}
