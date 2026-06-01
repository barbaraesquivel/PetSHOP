using System;
using System.Data;
using System.Data.SqlClient;
using System.Web.UI.WebControls;

public partial class Admin : System.Web.UI.Page
{
    protected void Page_Load(object sender, EventArgs e)
    {
        if (!SesionHelper.VerificarSesion(this)) return;

        string rol = Session["Rol"].ToString();

        if (rol != "Admin" && rol != "WebMaster")
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
        lblAdminUser.Text    = Session["Usuario"].ToString();

        if (rol == "WebMaster")
        {
            pnlFormUsuario.Visible  = false;
            pnlFormProducto.Visible = false;
            lblModo.Text    = "Modo solo lectura (WebMaster). Sin permisos de modificacion.";
            lblModo.Visible = true;
        }

        CargarUsuarios();
        CargarProductos();

        if (!IsPostBack)
        {
            CargarBitacora("");
            Bitacora.Registrar(Session["Usuario"].ToString(), "ACCESO", "Admin.aspx");
        }
    }

    // ===== CARGA DE GRILLAS =====

    private void CargarUsuarios()
    {
        try
        {
            using (SqlConnection con = ConexionDB.ObtenerConexion())
            {
                con.Open();
                SqlDataAdapter da = new SqlDataAdapter(
                    "SELECT IdUsuario, NombreUsuario, Rol FROM Usuarios ORDER BY NombreUsuario", con);
                DataTable dt = new DataTable();
                da.Fill(dt);
                gvUsuarios.DataSource = dt;
                gvUsuarios.DataBind();
            }
            ReaplicarSeleccionUsuario();
        }
        catch (Exception ex)
        {
            MostrarMensaje("Error al cargar usuarios: " + ex.Message, true);
        }
    }

    private void CargarProductos()
    {
        try
        {
            using (SqlConnection con = ConexionDB.ObtenerConexion())
            {
                con.Open();
                SqlDataAdapter da = new SqlDataAdapter(
                    "SELECT IdProducto, Nombre, Descripcion, Precio, Categoria, Activo FROM Productos ORDER BY Nombre", con);
                DataTable dt = new DataTable();
                da.Fill(dt);
                gvProductos.DataSource = dt;
                gvProductos.DataBind();
            }
            ReaplicarSeleccionProducto();
        }
        catch (Exception ex)
        {
            MostrarMensaje("Error al cargar productos: " + ex.Message, true);
        }
    }

    // Mantiene la fila seleccionada resaltada despues de cada rebind
    private void ReaplicarSeleccionUsuario()
    {
        int selId;
        if (!int.TryParse(hfSelectedUserId.Value, out selId) || selId <= 0) return;
        for (int i = 0; i < gvUsuarios.Rows.Count; i++)
        {
            if ((int)gvUsuarios.DataKeys[i].Value == selId)
            {
                gvUsuarios.SelectedIndex = i;
                break;
            }
        }
    }

    private void ReaplicarSeleccionProducto()
    {
        int selId;
        if (!int.TryParse(hfSelectedProdId.Value, out selId) || selId <= 0) return;
        for (int i = 0; i < gvProductos.Rows.Count; i++)
        {
            if ((int)gvProductos.DataKeys[i].Value == selId)
            {
                gvProductos.SelectedIndex = i;
                break;
            }
        }
    }

    // Hace las filas clickeables para seleccionarlas
    protected void gvUsuarios_RowDataBound(object sender, GridViewRowEventArgs e)
    {
        if (e.Row.RowType == DataControlRowType.DataRow)
        {
            e.Row.Attributes["onclick"] =
                Page.ClientScript.GetPostBackClientHyperlink(gvUsuarios, "Select$" + e.Row.RowIndex);
            e.Row.Style["cursor"] = "pointer";
            e.Row.ToolTip = "Clic para seleccionar";
        }
    }

    protected void gvProductos_RowDataBound(object sender, GridViewRowEventArgs e)
    {
        if (e.Row.RowType == DataControlRowType.DataRow)
        {
            e.Row.Attributes["onclick"] =
                Page.ClientScript.GetPostBackClientHyperlink(gvProductos, "Select$" + e.Row.RowIndex);
            e.Row.Style["cursor"] = "pointer";
            e.Row.ToolTip = "Clic para seleccionar";
        }
    }

    // Guarda el ID seleccionado en el HiddenField para que persista entre postbacks
    protected void gvUsuarios_SelectedIndexChanged(object sender, EventArgs e)
    {
        if (gvUsuarios.SelectedDataKey != null)
            hfSelectedUserId.Value = gvUsuarios.SelectedDataKey.Value.ToString();
    }

    protected void gvProductos_SelectedIndexChanged(object sender, EventArgs e)
    {
        if (gvProductos.SelectedDataKey != null)
            hfSelectedProdId.Value = gvProductos.SelectedDataKey.Value.ToString();
    }

    // ===== ACCIONES USUARIOS =====

    protected void btnAgregarUsuario_Click(object sender, EventArgs e)
    {
        string nombre = txtNombreU.Text.Trim().ToLower();
        string pass   = txtPassU.Text;
        string rol    = ddlRolU.SelectedValue;

        if (nombre == "" || pass == "")
        {
            MostrarMensaje("El nombre y la contrasena son obligatorios.", true);
            return;
        }

        try
        {
            string hash = Seguridad.HashSHA256(pass);
            using (SqlConnection con = ConexionDB.ObtenerConexion())
            {
                con.Open();
                SqlCommand cmd = new SqlCommand(
                    "INSERT INTO Usuarios (NombreUsuario, PasswordHash, Rol) VALUES (@n, @h, @r)", con);
                cmd.Parameters.AddWithValue("@n", nombre);
                cmd.Parameters.AddWithValue("@h", hash);
                cmd.Parameters.AddWithValue("@r", rol);
                cmd.ExecuteNonQuery();
            }
            Bitacora.Registrar(Session["Usuario"].ToString(), "AGREGAR_USUARIO", "Nuevo usuario: " + nombre);
            txtNombreU.Text = "";
            txtPassU.Text   = "";
            MostrarMensaje("Usuario '" + nombre + "' agregado correctamente.", false);
            CargarUsuarios();
        }
        catch (Exception ex)
        {
            MostrarMensaje("Error al agregar usuario: " + ex.Message, true);
        }
    }

    protected void btnEditarUsuario_Click(object sender, EventArgs e)
    {
        int id;
        if (!int.TryParse(hfSelectedUserId.Value, out id) || id <= 0)
        {
            MostrarMensaje("Seleccione un usuario de la tabla primero.", true);
            return;
        }

        try
        {
            using (SqlConnection con = ConexionDB.ObtenerConexion())
            {
                con.Open();
                SqlCommand cmd = new SqlCommand(
                    "SELECT NombreUsuario, Rol FROM Usuarios WHERE IdUsuario=@id", con);
                cmd.Parameters.AddWithValue("@id", id);
                SqlDataReader reader = cmd.ExecuteReader();

                if (reader.Read())
                {
                    hfIdUserEdit.Value       = id.ToString();
                    lblNombreUserEdit.Text   = reader["NombreUsuario"].ToString();
                    ddlEditRol.SelectedValue = reader["Rol"].ToString();
                    pnlEditarUsuario.Visible = true;
                    Page.ClientScript.RegisterStartupScript(GetType(), "scrollUser",
                        "document.getElementById('pnlEditarUsuario').scrollIntoView({behavior:'smooth'});", true);
                }
                reader.Close();
            }
        }
        catch (Exception ex)
        {
            MostrarMensaje("Error al cargar usuario: " + ex.Message, true);
        }
    }

    protected void btnEliminarUsuario_Click(object sender, EventArgs e)
    {
        int id;
        if (!int.TryParse(hfSelectedUserId.Value, out id) || id <= 0)
        {
            MostrarMensaje("Seleccione un usuario de la tabla primero.", true);
            return;
        }

        if (id == (int)Session["IdUsuario"])
        {
            MostrarMensaje("No puede eliminarse a si mismo.", true);
            return;
        }

        try
        {
            string nombre = "";
            using (SqlConnection con = ConexionDB.ObtenerConexion())
            {
                con.Open();
                SqlCommand cmdGet = new SqlCommand(
                    "SELECT NombreUsuario FROM Usuarios WHERE IdUsuario=@id", con);
                cmdGet.Parameters.AddWithValue("@id", id);
                nombre = (string)cmdGet.ExecuteScalar();

                SqlCommand cmdDel = new SqlCommand(
                    "DELETE FROM Usuarios WHERE IdUsuario=@id", con);
                cmdDel.Parameters.AddWithValue("@id", id);
                cmdDel.ExecuteNonQuery();
            }

            hfSelectedUserId.Value = "0";
            Bitacora.Registrar(Session["Usuario"].ToString(), "ELIMINAR_USUARIO", "Usuario eliminado: " + nombre);
            MostrarMensaje("Usuario '" + nombre + "' eliminado.", false);
            CargarUsuarios();
        }
        catch (Exception ex)
        {
            string msg = ex.Message.Contains("REFERENCE") || ex.Message.Contains("FK")
                ? "No se puede eliminar: el usuario tiene pedidos asociados."
                : "Error al eliminar: " + ex.Message;
            MostrarMensaje(msg, true);
        }
    }

    protected void btnGuardarUsuario_Click(object sender, EventArgs e)
    {
        int    id  = int.Parse(hfIdUserEdit.Value);
        string rol = ddlEditRol.SelectedValue;

        try
        {
            using (SqlConnection con = ConexionDB.ObtenerConexion())
            {
                con.Open();
                SqlCommand cmd = new SqlCommand(
                    "UPDATE Usuarios SET Rol=@r WHERE IdUsuario=@id", con);
                cmd.Parameters.AddWithValue("@r",  rol);
                cmd.Parameters.AddWithValue("@id", id);
                cmd.ExecuteNonQuery();
            }
            Bitacora.Registrar(Session["Usuario"].ToString(), "EDITAR_USUARIO", "Id:" + id + " nuevo rol: " + rol);
            pnlEditarUsuario.Visible = false;
            MostrarMensaje("Usuario actualizado correctamente.", false);
            CargarUsuarios();
        }
        catch (Exception ex)
        {
            MostrarMensaje("Error al guardar: " + ex.Message, true);
        }
    }

    protected void btnCancelarEditUser_Click(object sender, EventArgs e)
    {
        pnlEditarUsuario.Visible = false;
    }

    // ===== ACCIONES PRODUCTOS =====

    protected void btnAgregarProducto_Click(object sender, EventArgs e)
    {
        string  nombre    = txtNombreP.Text.Trim();
        string  desc      = txtDescP.Text.Trim();
        string  precioStr = txtPrecioP.Text.Trim().Replace(",", ".");
        string  categoria = ddlCatP.SelectedValue;
        decimal precio;

        if (nombre == "" || !decimal.TryParse(precioStr, System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out precio))
        {
            MostrarMensaje("Nombre y precio son obligatorios. El precio debe ser numerico.", true);
            return;
        }

        try
        {
            string hash = Seguridad.HashSHA256(nombre + precio.ToString("N2") + categoria);
            using (SqlConnection con = ConexionDB.ObtenerConexion())
            {
                con.Open();
                SqlCommand cmd = new SqlCommand(
                    @"INSERT INTO Productos (Nombre, Descripcion, Precio, Categoria, Activo, HashVerificador)
                      VALUES (@n, @d, @p, @c, 1, @h)", con);
                cmd.Parameters.AddWithValue("@n", nombre);
                cmd.Parameters.AddWithValue("@d", desc == "" ? (object)DBNull.Value : (object)desc);
                cmd.Parameters.AddWithValue("@p", precio);
                cmd.Parameters.AddWithValue("@c", categoria);
                cmd.Parameters.AddWithValue("@h", hash);
                cmd.ExecuteNonQuery();
            }
            Bitacora.Registrar(Session["Usuario"].ToString(), "AGREGAR_PRODUCTO", "Producto: " + nombre);
            txtNombreP.Text = "";
            txtDescP.Text   = "";
            txtPrecioP.Text = "";
            MostrarMensaje("Producto '" + nombre + "' agregado correctamente.", false);
            CargarProductos();
        }
        catch (Exception ex)
        {
            MostrarMensaje("Error al agregar producto: " + ex.Message, true);
        }
    }

    protected void btnEditarProducto_Click(object sender, EventArgs e)
    {
        int id;
        if (!int.TryParse(hfSelectedProdId.Value, out id) || id <= 0)
        {
            MostrarMensaje("Seleccione un producto de la tabla primero.", true);
            return;
        }

        try
        {
            using (SqlConnection con = ConexionDB.ObtenerConexion())
            {
                con.Open();
                SqlCommand cmd = new SqlCommand(
                    "SELECT Nombre, Descripcion, Precio, Categoria FROM Productos WHERE IdProducto=@id", con);
                cmd.Parameters.AddWithValue("@id", id);
                SqlDataReader reader = cmd.ExecuteReader();

                if (reader.Read())
                {
                    hfIdProdEdit.Value        = id.ToString();
                    lblIdProdEdit.Text        = id.ToString();
                    txtEditNombreP.Text       = reader["Nombre"].ToString();
                    txtEditDescP.Text         = reader["Descripcion"] == DBNull.Value ? "" : reader["Descripcion"].ToString();
                    txtEditPrecioP.Text       = ((decimal)reader["Precio"]).ToString("N2");
                    ddlEditCatP.SelectedValue = reader["Categoria"].ToString();
                    pnlEditarProducto.Visible = true;
                    Page.ClientScript.RegisterStartupScript(GetType(), "scrollProd",
                        "document.getElementById('pnlEditarProducto').scrollIntoView({behavior:'smooth'});", true);
                }
                reader.Close();
            }
        }
        catch (Exception ex)
        {
            MostrarMensaje("Error al cargar producto: " + ex.Message, true);
        }
    }

    protected void btnEliminarProducto_Click(object sender, EventArgs e)
    {
        int id;
        if (!int.TryParse(hfSelectedProdId.Value, out id) || id <= 0)
        {
            MostrarMensaje("Seleccione un producto de la tabla primero.", true);
            return;
        }

        try
        {
            string nombreProd = "";
            using (SqlConnection con = ConexionDB.ObtenerConexion())
            {
                con.Open();
                SqlCommand cmdGet = new SqlCommand(
                    "SELECT Nombre FROM Productos WHERE IdProducto=@id", con);
                cmdGet.Parameters.AddWithValue("@id", id);
                nombreProd = (string)cmdGet.ExecuteScalar();

                SqlCommand cmdUpd = new SqlCommand(
                    "UPDATE Productos SET Activo=0 WHERE IdProducto=@id", con);
                cmdUpd.Parameters.AddWithValue("@id", id);
                cmdUpd.ExecuteNonQuery();

                SqlCommand cmdElim = new SqlCommand(
                    @"INSERT INTO Eliminados (Tipo, Descripcion, RealizadoPor, FechaHora, EsExterno)
                      VALUES ('Producto', @desc, @usuario, GETDATE(), 0)", con);
                cmdElim.Parameters.AddWithValue("@desc",    "Producto desactivado: " + nombreProd + " (Id=" + id + ")");
                cmdElim.Parameters.AddWithValue("@usuario", Session["Usuario"].ToString());
                cmdElim.ExecuteNonQuery();
            }

            hfSelectedProdId.Value = "0";
            Bitacora.Registrar(Session["Usuario"].ToString(), "DESACTIVAR_PRODUCTO", "Producto: " + nombreProd);
            MostrarMensaje("Producto '" + nombreProd + "' desactivado.", false);
            CargarProductos();
        }
        catch (Exception ex)
        {
            MostrarMensaje("Error: " + ex.Message, true);
        }
    }

    protected void btnGuardarProducto_Click(object sender, EventArgs e)
    {
        int     id        = int.Parse(hfIdProdEdit.Value);
        string  nombre    = txtEditNombreP.Text.Trim();
        string  desc      = txtEditDescP.Text.Trim();
        string  precioStr = txtEditPrecioP.Text.Trim().Replace(",", ".");
        string  categoria = ddlEditCatP.SelectedValue;
        decimal precio;

        if (nombre == "" || !decimal.TryParse(precioStr, System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out precio))
        {
            MostrarMensaje("Nombre y precio son obligatorios. El precio debe ser numerico.", true);
            return;
        }

        try
        {
            string hash = Seguridad.HashSHA256(nombre + precio.ToString("N2") + categoria);
            using (SqlConnection con = ConexionDB.ObtenerConexion())
            {
                con.Open();
                SqlCommand cmd = new SqlCommand(
                    @"UPDATE Productos
                      SET Nombre=@n, Descripcion=@d, Precio=@p, Categoria=@c, HashVerificador=@h
                      WHERE IdProducto=@id", con);
                cmd.Parameters.AddWithValue("@n",  nombre);
                cmd.Parameters.AddWithValue("@d",  desc == "" ? (object)DBNull.Value : (object)desc);
                cmd.Parameters.AddWithValue("@p",  precio);
                cmd.Parameters.AddWithValue("@c",  categoria);
                cmd.Parameters.AddWithValue("@h",  hash);
                cmd.Parameters.AddWithValue("@id", id);
                cmd.ExecuteNonQuery();
            }
            Bitacora.Registrar(Session["Usuario"].ToString(), "EDITAR_PRODUCTO", "Id:" + id + " - " + nombre);
            pnlEditarProducto.Visible = false;
            MostrarMensaje("Producto actualizado. Hash recalculado.", false);
            CargarProductos();
        }
        catch (Exception ex)
        {
            MostrarMensaje("Error al guardar: " + ex.Message, true);
        }
    }

    protected void btnCancelarEditProd_Click(object sender, EventArgs e)
    {
        pnlEditarProducto.Visible = false;
    }

    // ===== BITACORA =====

    private void CargarBitacora(string filtroUsuario)
    {
        try
        {
            using (SqlConnection con = ConexionDB.ObtenerConexion())
            {
                con.Open();
                string sql = "SELECT FechaHora, NombreUsuario, Accion, Detalle FROM LogBitacora";
                if (!string.IsNullOrEmpty(filtroUsuario))
                    sql += " WHERE NombreUsuario = @usuario";
                sql += " ORDER BY FechaHora DESC";

                SqlCommand cmd = new SqlCommand(sql, con);
                if (!string.IsNullOrEmpty(filtroUsuario))
                    cmd.Parameters.AddWithValue("@usuario", filtroUsuario);

                SqlDataAdapter da = new SqlDataAdapter(cmd);
                DataTable dt = new DataTable();
                da.Fill(dt);
                gvBitacora.DataSource = dt;
                gvBitacora.DataBind();
            }
        }
        catch (Exception ex)
        {
            MostrarMensaje("Error al cargar bitacora: " + ex.Message, true);
        }
    }

    protected void btnFiltrarBit_Click(object sender, EventArgs e)
    {
        CargarBitacora(txtFiltroBit.Text.Trim());
    }

    protected void btnVerTodoBit_Click(object sender, EventArgs e)
    {
        txtFiltroBit.Text = "";
        CargarBitacora("");
    }

    private void MostrarMensaje(string texto, bool esError)
    {
        lblMensaje.Text     = texto;
        lblMensaje.CssClass = esError ? "msg msg-err" : "msg msg-ok";
        lblMensaje.Visible  = true;
    }
}
