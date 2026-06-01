using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;

public partial class WebMaster : System.Web.UI.Page
{
    protected void Page_Load(object sender, EventArgs e)
    {
        if (!SessionHelper.VerificarSesion(this)) return;

        if (!SessionHelper.VerificarRol(this, "WebMaster"))
        {
            pnlDenegado.Visible  = true;
            pnlContenido.Visible = false;
            return;
        }

        if (!SessionHelper.VerificarDB(this))
        {
            pnlDenegado.Visible  = true;
            pnlContenido.Visible = false;
            return;
        }

        pnlContenido.Visible = true;
        pnlDenegado.Visible  = false;
        lblWMUser.Text       = Session["Usuario"].ToString();


        ActualizarEstadoIntegridad();

        if (!IsPostBack)
            Bitacora.Registrar(Session["Usuario"].ToString(), "ACCESO", "WebMaster.aspx");
    }


    private void ActualizarEstadoIntegridad()
    {

        pnlCorrupto.Visible         = false;
        pnlEstadoOK.Visible         = false;
        pnlErrorVerificacion.Visible = false;

        try
        {
            List<ResultadoIntegridad> resultados = new List<ResultadoIntegridad>();
            bool hayCorrupcion = false;

            using (SqlConnection con = ConexionBD.ObtenerConexion())
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
                    string  hashActual   = Encriptacion.HashSHA256(nombre + precio.ToString("F2", System.Globalization.CultureInfo.InvariantCulture) + categoria);
                    bool    ok           = (hashActual == hashGuardado);

                    if (!ok) hayCorrupcion = true;

                    ResultadoIntegridad res = new ResultadoIntegridad();
                    res.Id              = reader["IdProducto"].ToString();
                    res.Nombre          = nombre;
                    res.Categoria       = categoria;
                    res.Precio          = precio;
                    res.HashRecalculado = hashActual;
                    res.HashGuardado    = hashGuardado;
                    res.Estado          = ok ? "OK" : "ALTERADO";
                    res.Info            = ok ? "Sin cambios" : "Modificacion externa detectada";
                    resultados.Add(res);
                }
                reader.Close();
            }


            pnlCorrupto.Visible = hayCorrupcion;
            pnlEstadoOK.Visible = !hayCorrupcion;

            if (hayCorrupcion)
                Bitacora.Registrar(Session["Usuario"].ToString(), "INTEGRIDAD_ALERTA",
                    "Se detectaron productos con hashVerificador no coincidente");

            try
            {
                gvIntegridad.DataSource = resultados;
                gvIntegridad.DataBind();
                gvIntegridad.Visible = (resultados.Count > 0);
            }
            catch
            {
                gvIntegridad.Visible = false;
            }
        }
        catch (Exception ex)
        {
            lblErrorVerificacion.Text    = "No se pudo verificar la integridad: " + ex.Message;
            pnlErrorVerificacion.Visible = true;
            Bitacora.Registrar(Session["Usuario"].ToString(), "ERROR_INTEGRIDAD", ex.Message);
        }
    }

    protected void btnRecalcularHashes_Click(object sender, EventArgs e)
    {
        try
        {
            using (SqlConnection con = ConexionBD.ObtenerConexion())
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

    protected void btnRestaurarBD_Click(object sender, EventArgs e)
    {
        try
        {
            using (SqlConnection con = ConexionBD.ObtenerConexion())
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


    protected void btnBackup_Click(object sender, EventArgs e)
    {
        try
        {
            using (SqlConnection con = ConexionBD.ObtenerConexion())
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

    private void RecalcularHashes(SqlConnection con)
    {
        List<int>    ids    = new List<int>();
        List<string> hashes = new List<string>();

        SqlCommand cmd = new SqlCommand("SELECT IdProducto, Nombre, Precio, Categoria FROM Productos", con);
        SqlDataReader reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            ids.Add((int)reader["IdProducto"]);
            hashes.Add(Encriptacion.HashSHA256(
                reader["Nombre"].ToString() +
                ((decimal)reader["Precio"]).ToString("F2", System.Globalization.CultureInfo.InvariantCulture) +
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

    protected void gvIntegridad_RowDataBound(object sender, System.Web.UI.WebControls.GridViewRowEventArgs e)
    {
        if (e.Row.RowType != System.Web.UI.WebControls.DataControlRowType.DataRow) return;
        ResultadoIntegridad res = (ResultadoIntegridad)e.Row.DataItem;
        if (res != null && res.Estado == "ALTERADO")
            e.Row.Cells[4].CssClass = "alterado";
        else
            e.Row.Cells[4].CssClass = "ok";
    }

    private void MostrarMsg(string texto, bool esError)
    {
        lblMensaje.Text     = texto;
        lblMensaje.CssClass = esError ? "msg msg-err" : "msg msg-ok";
        lblMensaje.Visible  = true;
    }
}
