using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;

public partial class WebMaster : System.Web.UI.Page
{
    protected void Page_Load(object sender, EventArgs e)
    {
        if (!SesionHelper.VerificarSesion(this)) return;

        if (!SesionHelper.VerificarRol(this, "WebMaster"))
        {
            pnlDenegado.Visible  = true;
            pnlContenido.Visible = false;
            return;
        }

        if (!SesionHelper.VerificarDB(this))
        {
            pnlDenegado.Visible  = true;
            pnlContenido.Visible = false;
            return;
        }

        pnlContenido.Visible = true;
        pnlDenegado.Visible  = false;
        lblWMUser.Text       = Session["Usuario"].ToString();

        // Al entrar, el sistema verifica automaticamente la integridad de la BD
        ActualizarEstadoIntegridad();

        if (!IsPostBack)
            Bitacora.Registrar(Session["Usuario"].ToString(), "ACCESO", "WebMaster.aspx");
    }

    // Verifica todos los hashes y actualiza los paneles de estado
    private void ActualizarEstadoIntegridad()
    {
        try
        {
            List<ResultadoIntegridad> resultados = new List<ResultadoIntegridad>();
            bool hayCorrupcion = false;

            using (SqlConnection con = ConexionDB.ObtenerConexion())
            {
                con.Open();
                SqlCommand cmd = new SqlCommand(
                    "SELECT IdProducto, Nombre, Precio, Categoria, HashVerificador FROM Productos WHERE Activo=1", con);
                SqlDataReader reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    string  nombre       = reader["Nombre"].ToString();
                    decimal precio       = (decimal)reader["Precio"];
                    string  categoria    = reader["Categoria"].ToString();
                    string  hashGuardado = reader["HashVerificador"] == DBNull.Value ? "" : reader["HashVerificador"].ToString();
                    string  hashActual   = Seguridad.HashSHA256(nombre + precio.ToString("N2") + categoria);
                    bool    ok           = (hashActual == hashGuardado);

                    if (!ok) hayCorrupcion = true;

                    ResultadoIntegridad res = new ResultadoIntegridad();
                    res.Id             = reader["IdProducto"].ToString();
                    res.Nombre         = nombre;
                    res.Categoria      = categoria;
                    res.Precio         = precio;
                    res.HashRecalculado = hashActual;
                    res.HashGuardado   = hashGuardado;
                    res.Estado         = ok ? "OK" : "ALTERADO";
                    res.Info           = ok ? "Sin cambios" : "Modificacion externa detectada";
                    resultados.Add(res);
                }
                reader.Close();
            }

            gvIntegridad.DataSource = resultados;
            gvIntegridad.DataBind();
            gvIntegridad.Visible = (resultados.Count > 0);

            pnlCorrupto.Visible = hayCorrupcion;
            pnlEstadoOK.Visible = !hayCorrupcion;
        }
        catch (Exception ex)
        {
            MostrarMsg("Error al verificar integridad: " + ex.Message, true);
        }
    }

    // Boton 1: recalcula los HashVerificador a partir de los datos actuales
    protected void btnRecalcularHashes_Click(object sender, EventArgs e)
    {
        try
        {
            using (SqlConnection con = ConexionDB.ObtenerConexion())
            {
                con.Open();
                RecalcularHashes(con);
            }
            Bitacora.Registrar(Session["Usuario"].ToString(), "RECALCULAR_HASHES",
                "HashVerificador recalculado para todos los productos");
            MostrarMsg("Digitos verificadores recalculados y guardados correctamente.", false);
        }
        catch (Exception ex)
        {
            MostrarMsg("Error al recalcular hashes: " + ex.Message, true);
        }

        ActualizarEstadoIntegridad();
    }

    // Boton 2: restaura la BD ejecutando el stored procedure SP_RestaurarBD
    protected void btnRestaurarBD_Click(object sender, EventArgs e)
    {
        try
        {
            using (SqlConnection con = ConexionDB.ObtenerConexion())
            {
                con.Open();

                SqlCommand cmd = new SqlCommand("SP_RestaurarBD", con);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.ExecuteNonQuery();

                // Recalculamos hashes despues de restaurar
                RecalcularHashes(con);
            }

            string msg = "Restauracion exitosa desde el ultimo backup";
            Bitacora.Registrar(Session["Usuario"].ToString(), "RESTORE", msg);
            MostrarMsg(msg, false);
        }
        catch (Exception ex)
        {
            MostrarMsg("Error en la restauracion: " + ex.Message, true);
        }

        ActualizarEstadoIntegridad();
    }

    // Seccion c: llama al stored procedure SP_HacerBackup
    protected void btnBackup_Click(object sender, EventArgs e)
    {
        try
        {
            using (SqlConnection con = ConexionDB.ObtenerConexion())
            {
                con.Open();
                SqlCommand cmd = new SqlCommand("SP_HacerBackup", con);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.ExecuteNonQuery();
            }

            string msg = "Backup realizado: " + DateTime.Now.ToString("dd/MM/yyyy HH:mm");
            MostrarMsg(msg, false);
            Bitacora.Registrar(Session["Usuario"].ToString(), "BACKUP", msg);
        }
        catch (Exception ex)
        {
            MostrarMsg("Error en backup: " + ex.Message, true);
        }
    }

    // Recalcula y guarda el HashVerificador de todos los productos
    private void RecalcularHashes(SqlConnection con)
    {
        List<int>    ids    = new List<int>();
        List<string> hashes = new List<string>();

        SqlCommand cmd = new SqlCommand("SELECT IdProducto, Nombre, Precio, Categoria FROM Productos", con);
        SqlDataReader reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            ids.Add((int)reader["IdProducto"]);
            hashes.Add(Seguridad.HashSHA256(
                reader["Nombre"].ToString() +
                ((decimal)reader["Precio"]).ToString("N2") +
                reader["Categoria"].ToString()));
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

    private void MostrarMsg(string texto, bool esError)
    {
        lblMensaje.Text     = texto;
        lblMensaje.CssClass = esError ? "msg msg-err" : "msg msg-ok";
        lblMensaje.Visible  = true;
    }
}
