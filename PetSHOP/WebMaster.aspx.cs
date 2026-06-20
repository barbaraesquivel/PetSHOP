using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Text;
using System.Web.UI.WebControls;
using BE;
using BLL;
using DAL;
using SEGURIDAD;
using SERV;

public partial class WebMaster : System.Web.UI.Page
{
    // ================================================================
    // CICLO DE VIDA
    // ================================================================

    protected void Page_Load(object sender, EventArgs e)
    {
        if (!SessionHelper.VerificarSesion(this)) return;

        if (!SessionHelper.VerificarRol(this, "WebMaster"))
        {
            pnlDenegado.Visible  = true;
            pnlContenido.Visible = false;
            pnlBloqueado.Visible = false;
            return;
        }

        if (!SessionHelper.VerificarDB(this))
        {
            pnlDenegado.Visible  = true;
            pnlContenido.Visible = false;
            pnlBloqueado.Visible = false;
            return;
        }

        lblWMUser.Text = Session["Usuario"].ToString();

        bool bloqueado = Application["SistemaBlockeado"] != null && (bool)Application["SistemaBlockeado"];

        if (bloqueado)
        {
            pnlContenido.Visible = false;
            pnlBloqueado.Visible = true;
            pnlDenegado.Visible  = false;
            ActualizarEstadoIntegridadBloqueado();
            if (!IsPostBack)
            {
                CargarListaBackups();
                Bitacora.Registrar(Session["Usuario"].ToString(), "ACCESO", "WebMaster.aspx (sistema bloqueado)");
            }
        }
        else
        {
            pnlContenido.Visible = true;
            pnlBloqueado.Visible = false;
            pnlDenegado.Visible  = false;
            ActualizarEstadoIntegridad();
            ActualizarInfoBackups();
            if (!IsPostBack)
            {
                CargarAcciones();
                CargarBitacora();
                Bitacora.Registrar(Session["Usuario"].ToString(), "ACCESO", "WebMaster.aspx");
            }
        }
    }

    // ================================================================
    // SECCION: INTEGRIDAD (modo normal)
    // ================================================================

    private void ActualizarEstadoIntegridad()
    {
        pnlCorrupto.Visible          = false;
        pnlEstadoOK.Visible          = false;
        pnlErrorVerificacion.Visible = false;

        try
        {
            List<ResultadoIntegridad> resultados = new List<ResultadoIntegridad>();
            bool hayCorrupcion = false;

            using (SqlConnection con = ConexionBD.ObtenerConexion())
            {
                con.Open();
                SqlCommand cmd = new SqlCommand(
                    "SELECT IdProducto, Nombre, Descripcion, Precio, Categoria, HashVerificador " +
                    "FROM Productos WHERE Activo=1", con);
                SqlDataReader reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    int     idProd      = (int)reader["IdProducto"];
                    string  nombre      = reader["Nombre"].ToString();
                    string  desc        = reader["Descripcion"] == DBNull.Value ? "" : reader["Descripcion"].ToString();
                    decimal precio      = (decimal)reader["Precio"];
                    string  categoria   = reader["Categoria"].ToString();
                    string  hashGuardado = reader["HashVerificador"] == DBNull.Value ? "" : reader["HashVerificador"].ToString();
                    string  hashActual  = Catalogo.CalcularHash(idProd, nombre, desc, precio, categoria);
                    bool    ok          = (hashActual == hashGuardado);
                    if (!ok) hayCorrupcion = true;

                    ResultadoIntegridad res = new ResultadoIntegridad();
                    res.Id              = idProd.ToString();
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
            catch { gvIntegridad.Visible = false; }
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
            bool todoOk;
            using (SqlConnection con = ConexionBD.ObtenerConexion())
            {
                con.Open();
                RecalcularHashes(con);
                todoOk = VerificarTodosOK(con);
            }

            if (todoOk)
            {
                string carpeta = Server.MapPath("~/App_Data/backups/");
                if (!Directory.Exists(carpeta)) Directory.CreateDirectory(carpeta);
                string nombreArchivo = "backup_" + DateTime.Now.ToString("yyyyMMdd_HHmm") + ".sql";
                BackupService.GuardarBackupEnArchivo(Path.Combine(carpeta, nombreArchivo));
                BackupService.AplicarRetencion(carpeta);
                Bitacora.Registrar(Session["Usuario"].ToString(), "RECALCULAR_HASHES",
                    "Hashes OK - backup automatico guardado: " + nombreArchivo);
                MostrarMsg("Digitos verificadores recalculados. Backup automatico guardado: " + nombreArchivo, false);
            }
            else
            {
                Bitacora.Registrar(Session["Usuario"].ToString(), "RECALCULAR_HASHES",
                    "Hashes recalculados pero se detectaron inconsistencias");
                MostrarMsg("Digitos verificadores recalculados pero se detectaron inconsistencias.", true);
            }
        }
        catch (Exception ex)
        {
            MostrarMsg("Error al recalcular hashes: " + ex.Message, true);
        }

        ActualizarEstadoIntegridad();
        ActualizarInfoBackups();
    }

    // ================================================================
    // SECCION: GESTION DE BACKUPS (modo normal)
    // ================================================================

    private void ActualizarInfoBackups()
    {
        try
        {
            string carpeta = Server.MapPath("~/App_Data/backups/");
            List<InfoBackup> lista = BackupService.ObtenerInfoBackups(carpeta);

            if (lista.Count == 0)
            {
                lblInfoBackup.Text   = "Ultimo backup: (ninguno) &nbsp;|&nbsp; Backups disponibles: 0";
                lblNoBackups.Visible = true;
                gvBackups.Visible    = false;
            }
            else
            {
                InfoBackup ultimo = lista[0];
                lblInfoBackup.Text   = "Ultimo backup: <strong>"
                                     + ultimo.FechaHora.ToString("dd/MM/yyyy HH:mm")
                                     + "</strong> &nbsp;|&nbsp; Backups disponibles: <strong>"
                                     + lista.Count + " de 7</strong>";
                lblNoBackups.Visible = false;
                gvBackups.DataSource = lista;
                gvBackups.DataBind();
                gvBackups.Visible    = true;
            }
        }
        catch (Exception ex)
        {
            lblInfoBackup.Text = "Error al leer backups: " + ex.Message;
        }
    }

    protected void btnGenerarBackup_Click(object sender, EventArgs e)
    {
        try
        {
            string carpeta = Server.MapPath("~/App_Data/backups/");
            if (!Directory.Exists(carpeta)) Directory.CreateDirectory(carpeta);
            string nombreArchivo = "backup_" + DateTime.Now.ToString("yyyyMMdd_HHmm") + ".sql";
            BackupService.GuardarBackupEnArchivo(Path.Combine(carpeta, nombreArchivo));
            BackupService.AplicarRetencion(carpeta);
            Bitacora.Registrar(Session["Usuario"].ToString(), "BACKUP",
                "Backup manual generado: " + nombreArchivo);
            lblGenBackupMsg.Text    = "Backup generado: " + nombreArchivo;
            lblGenBackupMsg.CssClass = "msg msg-ok";
            lblGenBackupMsg.Visible  = true;
            ActualizarInfoBackups();
        }
        catch (Exception ex)
        {
            lblGenBackupMsg.Text    = "Error al generar backup: " + ex.Message;
            lblGenBackupMsg.CssClass = "msg msg-err";
            lblGenBackupMsg.Visible  = true;
        }
    }

    protected void gvBackups_RowCommand(object sender, GridViewCommandEventArgs e)
    {
        if (e.CommandName != "Restaurar") return;

        string nombreArchivo = e.CommandArgument.ToString();
        string carpeta       = Server.MapPath("~/App_Data/backups/");
        string rutaCompleta  = Path.Combine(carpeta, nombreArchivo);

        if (!File.Exists(rutaCompleta))
        {
            lblGenBackupMsg.Text    = "El archivo no existe: " + nombreArchivo + ". Actualiza la lista.";
            lblGenBackupMsg.CssClass = "msg msg-err";
            lblGenBackupMsg.Visible  = true;
            return;
        }

        try
        {
            string contenido = File.ReadAllText(rutaCompleta, Encoding.UTF8);
            BackupService.RestaurarDesdeContenido(contenido);
            Bitacora.Registrar(Session["Usuario"].ToString(), "RESTORE_SQL",
                "Restaurado desde backup del servidor: " + nombreArchivo);
            lblGenBackupMsg.Text    = "Restauracion exitosa desde \"" + nombreArchivo + "\".";
            lblGenBackupMsg.CssClass = "msg msg-ok";
            lblGenBackupMsg.Visible  = true;
            ActualizarEstadoIntegridad();
            ActualizarInfoBackups();
        }
        catch (Exception ex)
        {
            Bitacora.Registrar(Session["Usuario"].ToString(), "RESTORE_ERROR",
                "Fallo al restaurar " + nombreArchivo + ": " + ex.Message);
            lblGenBackupMsg.Text    = "Error en la restauracion (rollback aplicado): " + ex.Message;
            lblGenBackupMsg.CssClass = "msg msg-err";
            lblGenBackupMsg.Visible  = true;
        }
    }

    protected void btnRestaurar_Click(object sender, EventArgs e)
    {
        if (!fuRestore.HasFile)
        {
            MostrarRestoreMsg("Selecciona un archivo .sql para restaurar.", true);
            return;
        }

        string ext = Path.GetExtension(fuRestore.FileName).ToLower();
        if (ext != ".sql")
        {
            MostrarRestoreMsg("Solo se aceptan archivos con extension .sql.", true);
            return;
        }

        if (fuRestore.PostedFile.ContentLength == 0)
        {
            MostrarRestoreMsg("El archivo esta vacio.", true);
            return;
        }

        string contenido;
        try
        {
            using (var sr = new StreamReader(fuRestore.PostedFile.InputStream, Encoding.UTF8))
                contenido = sr.ReadToEnd();
        }
        catch (Exception ex)
        {
            MostrarRestoreMsg("No se pudo leer el archivo: " + ex.Message, true);
            return;
        }

        try
        {
            BackupService.RestaurarDesdeContenido(contenido);
            Bitacora.Registrar(Session["Usuario"].ToString(), "RESTORE_SQL",
                "Restaurado desde archivo externo: " + fuRestore.FileName);
            MostrarRestoreMsg("Restauracion exitosa desde \"" + fuRestore.FileName + "\".", false);
            ActualizarEstadoIntegridad();
            ActualizarInfoBackups();
        }
        catch (Exception ex)
        {
            Bitacora.Registrar(Session["Usuario"].ToString(), "RESTORE_ERROR",
                "Fallo al restaurar archivo externo " + fuRestore.FileName + ": " + ex.Message);
            MostrarRestoreMsg("Error en la restauracion (rollback aplicado): " + ex.Message, true);
        }
    }

    // ================================================================
    // SECCION: BITACORA (modo normal)
    // ================================================================

    private void CargarAcciones()
    {
        try
        {
            ddlAccion.Items.Clear();
            ddlAccion.Items.Add(new ListItem("(todas)", ""));
            foreach (string acc in BitacoraService.ObtenerAccionesDistintas())
                ddlAccion.Items.Add(new ListItem(acc, acc));
        }
        catch { }
    }

    private void CargarBitacora()
    {
        DateTime? desde  = null;
        DateTime? hasta  = null;
        string    usuario = txtFiltroUsuario.Text.Trim();
        string    accion  = ddlAccion.SelectedValue;

        DateTime d;
        if (!string.IsNullOrEmpty(txtDesde.Text) && DateTime.TryParse(txtDesde.Text, out d))
            desde = d;
        DateTime h;
        if (!string.IsNullOrEmpty(txtHasta.Text) && DateTime.TryParse(txtHasta.Text, out h))
            hasta = h;

        try
        {
            DataTable dt = BitacoraService.ObtenerRegistros(desde, hasta, usuario, accion);
            lblConteoLog.Text    = dt.Rows.Count + " registro(s) encontrado(s).";
            lblConteoLog.Visible = true;
            gvBitacora.DataSource = dt;
            gvBitacora.DataBind();
            gvBitacora.Visible = (dt.Rows.Count > 0);
        }
        catch (Exception ex)
        {
            lblConteoLog.Text    = "Error al cargar la bitacora: " + ex.Message;
            lblConteoLog.Visible = true;
            gvBitacora.Visible   = false;
        }
    }

    protected void btnFiltrar_Click(object sender, EventArgs e)
    {
        gvBitacora.PageIndex = 0;
        CargarBitacora();
    }

    protected void btnLimpiarFiltros_Click(object sender, EventArgs e)
    {
        txtDesde.Text          = "";
        txtHasta.Text          = "";
        txtFiltroUsuario.Text  = "";
        ddlAccion.SelectedIndex = 0;
        gvBitacora.PageIndex   = 0;
        CargarBitacora();
    }

    protected void gvBitacora_PageIndexChanging(object sender, GridViewPageEventArgs e)
    {
        gvBitacora.PageIndex = e.NewPageIndex;
        CargarBitacora();
    }

    // ================================================================
    // MODO BLOQUEADO
    // ================================================================

    private void ActualizarEstadoIntegridadBloqueado()
    {
        pnlCorruptoBloqueado.Visible = false;
        pnlEstadoOKBloqueado.Visible = false;

        try
        {
            List<ResultadoIntegridad> resultados = new List<ResultadoIntegridad>();
            bool hayCorrupcion = false;

            using (SqlConnection con = ConexionBD.ObtenerConexion())
            {
                con.Open();
                SqlCommand cmd = new SqlCommand(
                    "SELECT IdProducto, Nombre, Descripcion, Precio, Categoria, HashVerificador " +
                    "FROM Productos WHERE Activo=1", con);
                SqlDataReader reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    int     idProd      = (int)reader["IdProducto"];
                    string  nombre      = reader["Nombre"].ToString();
                    string  desc        = reader["Descripcion"] == DBNull.Value ? "" : reader["Descripcion"].ToString();
                    decimal precio      = (decimal)reader["Precio"];
                    string  categoria   = reader["Categoria"].ToString();
                    string  hashGuardado = reader["HashVerificador"] == DBNull.Value ? "" : reader["HashVerificador"].ToString();
                    string  hashActual  = Catalogo.CalcularHash(idProd, nombre, desc, precio, categoria);
                    bool    ok          = (hashActual == hashGuardado);
                    if (!ok) hayCorrupcion = true;

                    ResultadoIntegridad res = new ResultadoIntegridad();
                    res.Id              = idProd.ToString();
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

            pnlCorruptoBloqueado.Visible = hayCorrupcion;
            pnlEstadoOKBloqueado.Visible = !hayCorrupcion;

            try
            {
                gvIntBloqueado.DataSource = resultados;
                gvIntBloqueado.DataBind();
                gvIntBloqueado.Visible = (resultados.Count > 0);
            }
            catch { gvIntBloqueado.Visible = false; }
        }
        catch (Exception ex)
        {
            Bitacora.Registrar(Session["Usuario"].ToString(), "ERROR_INTEGRIDAD", ex.Message);
        }
    }

    protected void btnRecalcularBloqueado_Click(object sender, EventArgs e)
    {
        try
        {
            bool todoOk;
            using (SqlConnection con = ConexionBD.ObtenerConexion())
            {
                con.Open();
                RecalcularHashes(con);
                todoOk = VerificarTodosOK(con);
            }

            if (todoOk)
            {
                string carpeta = Server.MapPath("~/App_Data/backups/");
                if (!Directory.Exists(carpeta)) Directory.CreateDirectory(carpeta);
                string nombreArchivo = "backup_" + DateTime.Now.ToString("yyyyMMdd_HHmm") + ".sql";
                BackupService.GuardarBackupEnArchivo(Path.Combine(carpeta, nombreArchivo));
                BackupService.AplicarRetencion(carpeta);
                Application["SistemaBlockeado"] = false;
                Bitacora.Registrar(Session["Usuario"].ToString(), "RECALCULAR_HASHES",
                    "Sistema desbloqueado - backup guardado: " + nombreArchivo);
                Response.Redirect("WebMaster.aspx", false);
            }
            else
            {
                lblRecalcMsg.Text    = "Se recalcularon los hashes pero aun hay alteraciones. El sistema sigue bloqueado.";
                lblRecalcMsg.CssClass = "msg msg-err";
                lblRecalcMsg.Visible  = true;
                ActualizarEstadoIntegridadBloqueado();
            }
        }
        catch (Exception ex)
        {
            lblRecalcMsg.Text    = "Error al recalcular: " + ex.Message;
            lblRecalcMsg.CssClass = "msg msg-err";
            lblRecalcMsg.Visible  = true;
        }
    }

    protected void btnActualizarLista_Click(object sender, EventArgs e)
    {
        CargarListaBackups();
        ActualizarEstadoIntegridadBloqueado();
    }

    protected void btnRestaurarDesdeArchivo_Click(object sender, EventArgs e)
    {
        if (string.IsNullOrEmpty(ddlBackups.SelectedValue))
        {
            lblRestaurarMsg.Text    = "Selecciona un archivo de backup de la lista.";
            lblRestaurarMsg.CssClass = "msg msg-err";
            lblRestaurarMsg.Visible  = true;
            return;
        }

        string carpeta     = Server.MapPath("~/App_Data/backups/");
        string rutaArchivo = Path.Combine(carpeta, ddlBackups.SelectedValue);

        if (!File.Exists(rutaArchivo))
        {
            lblRestaurarMsg.Text    = "El archivo seleccionado no existe. Actualiza la lista.";
            lblRestaurarMsg.CssClass = "msg msg-err";
            lblRestaurarMsg.Visible  = true;
            return;
        }

        try
        {
            string contenido = File.ReadAllText(rutaArchivo, Encoding.UTF8);
            BackupService.RestaurarDesdeContenido(contenido);
            Application["SistemaBlockeado"] = false;
            Bitacora.Registrar(Session["Usuario"].ToString(), "RESTORE_SQL",
                "Archivo: " + ddlBackups.SelectedValue + " - Sistema desbloqueado");
            Response.Redirect("WebMaster.aspx", false);
        }
        catch (Exception ex)
        {
            Bitacora.Registrar(Session["Usuario"].ToString(), "RESTORE_ERROR",
                "Fallo al restaurar " + ddlBackups.SelectedValue + ": " + ex.Message);
            lblRestaurarMsg.Text    = "Error en la restauracion (rollback aplicado): " + ex.Message;
            lblRestaurarMsg.CssClass = "msg msg-err";
            lblRestaurarMsg.Visible  = true;
        }
    }

    private void CargarListaBackups()
    {
        string carpeta = Server.MapPath("~/App_Data/backups/");
        ddlBackups.Items.Clear();

        if (!Directory.Exists(carpeta) || Directory.GetFiles(carpeta, "*.sql").Length == 0)
        {
            ddlBackups.Items.Add(new ListItem("(ninguno disponible)", ""));
            return;
        }

        string[] archivos = Directory.GetFiles(carpeta, "*.sql");
        Array.Sort(archivos);
        Array.Reverse(archivos);

        ddlBackups.Items.Add(new ListItem("-- Seleccionar backup --", ""));
        foreach (string arch in archivos)
            ddlBackups.Items.Add(new ListItem(Path.GetFileName(arch), Path.GetFileName(arch)));
    }

    // ================================================================
    // HELPERS COMPARTIDOS
    // ================================================================

    private void RecalcularHashes(SqlConnection con)
    {
        List<int>    ids    = new List<int>();
        List<string> hashes = new List<string>();

        SqlCommand    cmd    = new SqlCommand("SELECT IdProducto, Nombre, Descripcion, Precio, Categoria FROM Productos", con);
        SqlDataReader reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            int id = (int)reader["IdProducto"];
            ids.Add(id);
            hashes.Add(Catalogo.CalcularHash(
                id,
                reader["Nombre"].ToString(),
                reader["Descripcion"] == DBNull.Value ? "" : reader["Descripcion"].ToString(),
                (decimal)reader["Precio"],
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

    private bool VerificarTodosOK(SqlConnection con)
    {
        SqlCommand    cmd = new SqlCommand(
            "SELECT IdProducto, Nombre, Descripcion, Precio, Categoria, HashVerificador " +
            "FROM Productos WHERE Activo=1", con);
        SqlDataReader r   = cmd.ExecuteReader();
        bool ok = true;
        while (r.Read())
        {
            int     id     = (int)r["IdProducto"];
            string  nombre = r["Nombre"].ToString();
            string  desc   = r["Descripcion"] == DBNull.Value ? "" : r["Descripcion"].ToString();
            decimal precio = (decimal)r["Precio"];
            string  cat    = r["Categoria"].ToString();
            string  hash   = r["HashVerificador"] == DBNull.Value ? "" : r["HashVerificador"].ToString();
            if (Catalogo.CalcularHash(id, nombre, desc, precio, cat) != hash)
            {
                ok = false;
                break;
            }
        }
        r.Close();
        return ok;
    }

    protected void gvIntegridad_RowDataBound(object sender, GridViewRowEventArgs e)
    {
        if (e.Row.RowType != DataControlRowType.DataRow) return;
        ResultadoIntegridad res = (ResultadoIntegridad)e.Row.DataItem;
        e.Row.Cells[4].CssClass = (res != null && res.Estado == "ALTERADO") ? "alterado" : "ok";
    }

    protected void gvIntBloqueado_RowDataBound(object sender, GridViewRowEventArgs e)
    {
        if (e.Row.RowType != DataControlRowType.DataRow) return;
        ResultadoIntegridad res = (ResultadoIntegridad)e.Row.DataItem;
        e.Row.Cells[4].CssClass = (res != null && res.Estado == "ALTERADO") ? "alterado" : "ok";
    }

    private void MostrarMsg(string texto, bool esError)
    {
        lblMensaje.Text     = texto;
        lblMensaje.CssClass = esError ? "msg msg-err" : "msg msg-ok";
        lblMensaje.Visible  = true;
    }

    private void MostrarRestoreMsg(string texto, bool esError)
    {
        lblRestoreMsg.Text     = texto;
        lblRestoreMsg.CssClass = esError ? "msg msg-err" : "msg msg-ok";
        lblRestoreMsg.Visible  = true;
    }
}
