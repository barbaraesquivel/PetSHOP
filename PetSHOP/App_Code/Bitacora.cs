using System;
using System.Data.SqlClient;
using System.IO;
using System.Web;

// Registra eventos en archivo de texto Y en la tabla LogBitacora
public static class Bitacora
{
    private static object candado = new object();

    public static void Registrar(string usuario, string accion, string detalle)
    {


        // Si la BD esta disponible, tambien insertamos en LogBitacora
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
        using (SqlConnection con = ConexionDB.ObtenerConexion())
        {
            con.Open();
            SqlCommand cmd = new SqlCommand(
                "INSERT INTO LogBitacora (FechaHora, NombreUsuario, Accion, Detalle) VALUES (@f, @u, @a, @d)", con);
            cmd.Parameters.AddWithValue("@f",  DateTime.Now);
            cmd.Parameters.AddWithValue("@u",  usuario);
            cmd.Parameters.AddWithValue("@a",  accion);
            cmd.Parameters.AddWithValue("@d",  detalle);
            cmd.ExecuteNonQuery();
        }
    }
}
