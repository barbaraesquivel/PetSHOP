using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Web;

public partial class Global : System.Web.HttpApplication
{
    protected void Application_Start(object sender, EventArgs e)
    {
        bool disponible = ConexionBD.EstaDisponible();
        Application["DBDisponible"] = disponible;

        if (disponible)
        {
            InicializarHashesNulos();
           Bitacora.Registrar("sistema", "APP_START", "Aplicacion iniciada con BD disponible");
        }
        else
        {
            Bitacora.Registrar("sistema", "APP_START", "Aplicacion iniciada SIN BD disponible");
        }
    }


    private void InicializarHashesNulos()
    {
        try
        {
            List<int>    ids    = new List<int>();
            List<string> hashes = new List<string>();

            using (SqlConnection con = ConexionBD.ObtenerConexion())
            {
                con.Open();

                SqlCommand cmd = new SqlCommand(
                    "SELECT IdProducto, Nombre, Precio, Categoria FROM Productos WHERE HashVerificador IS NULL", con);
                SqlDataReader reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    string  nombre    = reader["Nombre"].ToString();
                    decimal precio    = (decimal)reader["Precio"];
                    string  categoria = reader["Categoria"].ToString();
                    string  hash      = Encriptacion.HashSHA256(nombre + precio.ToString("F2", System.Globalization.CultureInfo.InvariantCulture) + categoria);
                    ids.Add((int)reader["IdProducto"]);
                    hashes.Add(hash);
                }
                reader.Close();

                if (ids.Count > 0)
                {
                    for (int i = 0; i < ids.Count; i++)
                    {
                        SqlCommand upd = new SqlCommand(
                            "UPDATE Productos SET HashVerificador=@hash WHERE IdProducto=@id", con);
                        upd.Parameters.AddWithValue("@hash", hashes[i]);
                        upd.Parameters.AddWithValue("@id",   ids[i]);
                        upd.ExecuteNonQuery();
                    }
                    Bitacora.Registrar("sistema", "INIT_HASHES",
                        ids.Count + " producto(s) inicializados con HashVerificador desde datos actuales");
                }
            }
        }
        catch (Exception ex)
        {
            Bitacora.Registrar("sistema", "ERROR", "Error al inicializar hashes: " + ex.Message);
        }
    }

    protected void Application_Error(object sender, EventArgs e)
    {
        Exception error = Server.GetLastError();
        if (error != null)
        {
            string ip      = "?";
            string usuario = "sistema";
            try { ip = Request.UserHostAddress; } catch { }
            try
            {
                if (HttpContext.Current.Session != null && HttpContext.Current.Session["Usuario"] != null)
                    usuario = HttpContext.Current.Session["Usuario"].ToString();
            }
            catch { }
            Bitacora.Registrar(usuario, "ERROR_APP", error.Message);
        }
        Server.ClearError();
        try { Response.Redirect("~/Error.aspx"); } catch { }
    }
}
