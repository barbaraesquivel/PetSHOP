using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Web;
using BLL;
using DAL;
using SERV;

public partial class Global : System.Web.HttpApplication
{
    protected void Application_Start(object sender, EventArgs e)
    {
        bool disponible = ConexionBD.EstaDisponible();
        Application["DBDisponible"] = disponible;

        if (disponible)
        {
            InicializarHashesNulos();
            VerificarIntegridadSistema();
            GenerarBackupInicialSiNecesario();
            Bitacora.Registrar("sistema", "APP_START", "Aplicacion iniciada con BD disponible");
        }
        else
        {
            Application["SistemaBlockeado"] = false;
            Bitacora.Registrar("sistema", "APP_START", "Aplicacion iniciada SIN BD disponible");
        }
    }

    protected void Session_Start(object sender, EventArgs e)
    {
        VerificarIntegridadSistema();
    }

    // ----------------------------------------------------------------

    private void VerificarIntegridadSistema()
    {
        if (Application["DBDisponible"] == null || !(bool)Application["DBDisponible"]) return;

        try
        {
            bool hayAlterado = false;

            using (SqlConnection con = ConexionBD.ObtenerConexion())
            {
                con.Open();
                SqlCommand cmd = new SqlCommand(
                    "SELECT IdProducto, Nombre, Descripcion, Precio, Categoria, HashVerificador " +
                    "FROM Productos WHERE Activo=1", con);
                SqlDataReader reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    int     id     = (int)reader["IdProducto"];
                    string  nombre = reader["Nombre"].ToString();
                    string  desc   = reader["Descripcion"] == DBNull.Value ? "" : reader["Descripcion"].ToString();
                    decimal precio = (decimal)reader["Precio"];
                    string  cat    = reader["Categoria"].ToString();
                    string  hash   = reader["HashVerificador"] == DBNull.Value ? "" : reader["HashVerificador"].ToString();

                    if (Catalogo.CalcularHash(id, nombre, desc, precio, cat) != hash)
                    {
                        hayAlterado = true;
                        break;
                    }
                }
                reader.Close();
            }

            Application["SistemaBlockeado"] = hayAlterado;

            if (hayAlterado)
                Bitacora.Registrar("sistema", "INTEGRIDAD_ALERTA", "Sistema bloqueado: integridad comprometida");
        }
        catch (Exception ex)
        {
            Bitacora.Registrar("sistema", "ERROR_INTEGRIDAD", "Error al verificar integridad: " + ex.Message);
            Application["SistemaBlockeado"] = false;
        }
    }

    private void GenerarBackupInicialSiNecesario()
    {
        if (Application["SistemaBlockeado"] != null && (bool)Application["SistemaBlockeado"]) return;

        try
        {
            string carpeta = Path.Combine(HttpRuntime.AppDomainAppPath, "App_Data", "backups");
            if (!Directory.Exists(carpeta)) Directory.CreateDirectory(carpeta);

            if (Directory.GetFiles(carpeta, "*.sql").Length == 0)
            {
                string nombre = "backup_" + DateTime.Now.ToString("yyyyMMdd_HHmm") + ".sql";
                BackupService.GuardarBackupEnArchivo(Path.Combine(carpeta, nombre));
                BackupService.AplicarRetencion(carpeta);
                Bitacora.Registrar("sistema", "BACKUP_AUTO", "Backup inicial generado: " + nombre);
            }
        }
        catch (Exception ex)
        {
            Bitacora.Registrar("sistema", "BACKUP_ERROR", "Error al generar backup inicial: " + ex.Message);
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
                    "SELECT IdProducto, Nombre, Descripcion, Precio, Categoria " +
                    "FROM Productos WHERE HashVerificador IS NULL", con);
                SqlDataReader reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    int     idProd    = (int)reader["IdProducto"];
                    string  nombre    = reader["Nombre"].ToString();
                    string  desc      = reader["Descripcion"] == DBNull.Value ? "" : reader["Descripcion"].ToString();
                    decimal precio    = (decimal)reader["Precio"];
                    string  categoria = reader["Categoria"].ToString();
                    ids.Add(idProd);
                    hashes.Add(Catalogo.CalcularHash(idProd, nombre, desc, precio, categoria));
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
            string usuario = "sistema";
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
