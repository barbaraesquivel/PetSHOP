using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Web;

public partial class Global : System.Web.HttpApplication
{
    protected void Application_Start(object sender, EventArgs e)
    {
        bool disponible = ConexionDB.EstaDisponible();
        Application["DBDisponible"] = disponible;

        if (disponible)
        {
            // Inicializamos los HashVerificador de los productos que aun no tienen
            InicializarHashesNulos();
           Bitacora.Registrar("sistema", "APP_START", "Aplicacion iniciada con BD disponible");
        }
        else
        {
            Bitacora.Registrar("sistema", "APP_START", "Aplicacion iniciada SIN BD disponible");
        }
    }

    // Solo calcula y guarda el hash en productos que tienen HashVerificador = NULL
    // Si el hash ya existe (fue puesto por el Admin), no lo tocamos
    // Esto es importante para que la deteccion de cambios externos funcione correctamente
    private void InicializarHashesNulos()
    {
        try
        {
            List<int>    ids    = new List<int>();
            List<string> hashes = new List<string>();

            using (SqlConnection con = ConexionDB.ObtenerConexion())
            {
                con.Open();

                SqlCommand cmd = new SqlCommand(
                    "SELECT IdProducto, Nombre, Precio, Categoria FROM Productos WHERE HashVerificador IS NULL", con);
                SqlDataReader reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    string  nombre   = reader["Nombre"].ToString();
                    decimal precio   = (decimal)reader["Precio"];
                    string  categoria = reader["Categoria"].ToString();
                    string  hash     = Seguridad.HashSHA256(nombre + precio.ToString("N2") + categoria);
                    ids.Add((int)reader["IdProducto"]);
                    hashes.Add(hash);
                }
                reader.Close();

                for (int i = 0; i < ids.Count; i++)
                {
                    SqlCommand upd = new SqlCommand(
                        "UPDATE Productos SET HashVerificador=@hash WHERE IdProducto=@id", con);
                    upd.Parameters.AddWithValue("@hash", hashes[i]);
                    upd.Parameters.AddWithValue("@id",   ids[i]);
                    upd.ExecuteNonQuery();
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
