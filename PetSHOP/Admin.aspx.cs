using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Web.UI.WebControls;
using BE;
using BLL;
using DAL;
using SEGURIDAD;
using SERV;

public partial class Admin : System.Web.UI.Page
{
    protected void Page_Load(object sender, EventArgs e)
    {
        if (!SessionHelper.VerificarSesion(this)) return;

        string rol = Session["Rol"].ToString();

        if (rol != "Admin" && rol != "WebMaster")
        {
            pnlDenegado.Visible  = true;
            pnlContenido.Visible = false;
            return;
        }

        bool bloqueado = Application["SistemaBlockeado"] != null && (bool)Application["SistemaBlockeado"];
        if (bloqueado)
        {
            if (rol == "WebMaster")
                Response.Redirect("WebMaster.aspx", false);
            else
                Response.Redirect("Error.aspx?motivo=integridad", false);
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
        CargarClientes();
        CargarAlertaStock();
        CargarPedidos();

        if (!IsPostBack)
            Bitacora.Registrar(Session["Usuario"].ToString(), "ACCESO", "Admin.aspx");
    }

    // ===== CARGA DE GRILLAS =====

    private void CargarUsuarios()
    {
        try
        {
            using (SqlConnection con = ConexionBD.ObtenerConexion())
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
            using (SqlConnection con = ConexionBD.ObtenerConexion())
            {
                con.Open();
                SqlDataAdapter da = new SqlDataAdapter(
                    "SELECT IdProducto, Nombre, Descripcion, Precio, Categoria, Stock, Activo FROM Productos ORDER BY Nombre", con);
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

    // vuelve a resaltar la fila seleccionada despues de cada postback
    private void ReaplicarSeleccionUsuario()
    {
        int selId;
        if (!int.TryParse(hfSelectedUserId.Value, out selId) || selId <= 0) return;
        for (int i = 0; i < gvUsuarios.Rows.Count; i++)
        {
            if ((int)gvUsuarios.DataKeys[i].Value == selId)
            {
                string rowClientId = gvUsuarios.Rows[i].ClientID;
                Page.ClientScript.RegisterStartupScript(GetType(), "reselUser",
                    "var r=document.getElementById('" + rowClientId + "'); if(r) r.classList.add('fila-seleccionada');", true);
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
                string rowClientId = gvProductos.Rows[i].ClientID;
                Page.ClientScript.RegisterStartupScript(GetType(), "reselProd",
                    "var r=document.getElementById('" + rowClientId + "'); if(r) r.classList.add('fila-seleccionada');", true);
                break;
            }
        }
    }

    // Hace las filas clickeables: JS puro actualiza el HiddenField sin postback
    protected void gvUsuarios_RowDataBound(object sender, GridViewRowEventArgs e)
    {
        if (e.Row.RowType == DataControlRowType.DataRow)
        {
            string id = gvUsuarios.DataKeys[e.Row.RowIndex].Value.ToString();
            e.Row.Attributes["onclick"] = "seleccionarFila(this,'" + id + "','hfSelectedUserId')";
            e.Row.Style["cursor"] = "pointer";
            e.Row.ToolTip = "Clic para seleccionar";
        }
    }

    protected void gvProductos_RowDataBound(object sender, GridViewRowEventArgs e)
    {
        if (e.Row.RowType == DataControlRowType.DataRow)
        {
            string id = gvProductos.DataKeys[e.Row.RowIndex].Value.ToString();
            e.Row.Attributes["onclick"] = "seleccionarFila(this,'" + id + "','hfSelectedProdId')";
            e.Row.Style["cursor"] = "pointer";
            e.Row.ToolTip = "Clic para seleccionar";
        }
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
            string hash = Encriptacion.HashSHA256(pass);
            using (SqlConnection con = ConexionBD.ObtenerConexion())
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
            using (SqlConnection con = ConexionBD.ObtenerConexion())
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
            using (SqlConnection con = ConexionBD.ObtenerConexion())
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
            using (SqlConnection con = ConexionBD.ObtenerConexion())
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
            using (SqlConnection con = ConexionBD.ObtenerConexion())
            {
                con.Open();
                // Paso 1: insertar y obtener el IdProducto generado
                SqlCommand cmd = new SqlCommand(
                    @"INSERT INTO Productos (Nombre, Descripcion, Precio, Categoria, Activo, HashVerificador)
                      OUTPUT INSERTED.IdProducto
                      VALUES (@n, @d, @p, @c, 1, '')", con);
                cmd.Parameters.AddWithValue("@n", nombre);
                cmd.Parameters.AddWithValue("@d", desc == "" ? (object)DBNull.Value : (object)desc);
                cmd.Parameters.AddWithValue("@p", precio);
                cmd.Parameters.AddWithValue("@c", categoria);
                int idNuevo = (int)cmd.ExecuteScalar();

                // Paso 2: calcular hash con el id real y actualizar
                string hash = Catalogo.CalcularHash(idNuevo, nombre, desc, precio, categoria);
                SqlCommand upd = new SqlCommand(
                    "UPDATE Productos SET HashVerificador=@h WHERE IdProducto=@id", con);
                upd.Parameters.AddWithValue("@h",  hash);
                upd.Parameters.AddWithValue("@id", idNuevo);
                upd.ExecuteNonQuery();
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
            using (SqlConnection con = ConexionBD.ObtenerConexion())
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
            using (SqlConnection con = ConexionBD.ObtenerConexion())
            {
                con.Open();
                SqlCommand cmdGet = new SqlCommand(
                    "SELECT Nombre FROM Productos WHERE IdProducto=@id", con);
                cmdGet.Parameters.AddWithValue("@id", id);
                nombreProd = (string)cmdGet.ExecuteScalar();

                // Baja logica: Activo=0 y Eliminado=1 (sin tabla Eliminados)
                SqlCommand cmdUpd = new SqlCommand(
                    "UPDATE Productos SET Activo=0, Eliminado=1 WHERE IdProducto=@id", con);
                cmdUpd.Parameters.AddWithValue("@id", id);
                cmdUpd.ExecuteNonQuery();
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
            string hash = Catalogo.CalcularHash(id, nombre, desc, precio, categoria);
            using (SqlConnection con = ConexionBD.ObtenerConexion())
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

    // ===== STOCK =====

    protected void btnGestionarStock_Click(object sender, EventArgs e)
    {
        int id;
        if (!int.TryParse(hfSelectedProdId.Value, out id) || id <= 0)
        {
            MostrarMensaje("Seleccione un producto de la tabla primero.", true);
            return;
        }

        try
        {
            using (SqlConnection con = ConexionBD.ObtenerConexion())
            {
                con.Open();
                SqlCommand cmd = new SqlCommand(
                    "SELECT Nombre, Stock FROM Productos WHERE IdProducto=@id", con);
                cmd.Parameters.AddWithValue("@id", id);
                SqlDataReader r = cmd.ExecuteReader();
                if (r.Read())
                {
                    hfIdStockEdit.Value        = id.ToString();
                    lblNombreStock.Text        = r["Nombre"].ToString();
                    lblStockActual.Text        = r["Stock"].ToString();
                    txtNuevoStock.Text         = r["Stock"].ToString();
                    pnlActualizarStock.Visible = true;
                    Page.ClientScript.RegisterStartupScript(GetType(), "scrollStock",
                        "document.getElementById('" + pnlActualizarStock.ClientID + "').scrollIntoView({behavior:'smooth'});", true);
                }
                r.Close();
            }
        }
        catch (Exception ex)
        {
            MostrarMensaje("Error al cargar stock: " + ex.Message, true);
        }
    }

    protected void btnActualizarStock_Click(object sender, EventArgs e)
    {
        int id = int.Parse(hfIdStockEdit.Value);
        int nuevoStock;
        if (!int.TryParse(txtNuevoStock.Text.Trim(), out nuevoStock) || nuevoStock < 0)
        {
            MostrarMensaje("Stock invalido. Debe ser un numero entero mayor o igual a 0.", true);
            return;
        }

        try
        {
            string nombreProd    = "";
            int    stockAnterior = 0;

            using (SqlConnection con = ConexionBD.ObtenerConexion())
            {
                con.Open();
                SqlCommand cmdGet = new SqlCommand(
                    "SELECT Nombre, Stock FROM Productos WHERE IdProducto=@id", con);
                cmdGet.Parameters.AddWithValue("@id", id);
                SqlDataReader r = cmdGet.ExecuteReader();
                if (r.Read())
                {
                    nombreProd    = r["Nombre"].ToString();
                    stockAnterior = (int)r["Stock"];
                }
                r.Close();

                SqlCommand cmdUpd = new SqlCommand(
                    "UPDATE Productos SET Stock=@s WHERE IdProducto=@id", con);
                cmdUpd.Parameters.AddWithValue("@s",  nuevoStock);
                cmdUpd.Parameters.AddWithValue("@id", id);
                cmdUpd.ExecuteNonQuery();
            }

            Bitacora.Registrar(Session["Usuario"].ToString(), "STOCK_ACTUALIZADO",
                "Producto: " + nombreProd + " | Anterior: " + stockAnterior + " | Nuevo: " + nuevoStock);

            pnlActualizarStock.Visible = false;
            MostrarMensaje("Stock de '" + nombreProd + "' actualizado: " + stockAnterior + " -> " + nuevoStock + ".", false);
            CargarProductos();
            CargarAlertaStock();
        }
        catch (Exception ex)
        {
            MostrarMensaje("Error al actualizar stock: " + ex.Message, true);
        }
    }

    protected void btnCancelarStock_Click(object sender, EventArgs e)
    {
        pnlActualizarStock.Visible = false;
    }

    private void CargarAlertaStock()
    {
        try
        {
            using (SqlConnection con = ConexionBD.ObtenerConexion())
            {
                con.Open();
                SqlDataAdapter da = new SqlDataAdapter(
                    "SELECT IdProducto, Nombre, Stock FROM Productos WHERE Activo=1 AND Eliminado=0 AND Stock <= 5 ORDER BY Stock, Nombre", con);
                DataTable dt = new DataTable();
                da.Fill(dt);
                pnlAlertaStock.Visible = dt.Rows.Count > 0;
                if (dt.Rows.Count > 0)
                {
                    gvAlertaStock.DataSource = dt;
                    gvAlertaStock.DataBind();
                }
            }
        }
        catch { }
    }

    // ===== CLIENTES =====

    private void CargarClientes()
    {
        try
        {
            List<Cliente> clientes = ClienteBLL.GetAll();
            gvClientes.DataSource = clientes;
            gvClientes.DataBind();
        }
        catch (Exception ex)
        {
            MostrarMensaje("Error al cargar clientes: " + ex.Message, true);
        }
    }

    // ===== PEDIDOS =====

    private void CargarPedidos()
    {
        try
        {
            DataTable dt = PedidoBLL.GetAllAdmin();
            gvPedidos.DataSource = dt;
            gvPedidos.DataBind();
        }
        catch (Exception ex)
        {
            MostrarMensaje("Error al cargar pedidos: " + ex.Message, true);
        }
    }

    protected void gvPedidos_RowDataBound(object sender, GridViewRowEventArgs e)
    {
        if (e.Row.RowType != DataControlRowType.DataRow) return;

        DataRowView drv    = (DataRowView)e.Row.DataItem;
        string      estado = drv["Estado"].ToString();

        Button btnAvanzar  = (Button)e.Row.FindControl("btnAvanzarEstado");
        Button btnCancelar = (Button)e.Row.FindControl("btnCancelarPedidoAdmin");

        if (btnAvanzar != null)
        {
            string siguiente = PedidoBLL.GetSiguienteEstado(estado);
            btnAvanzar.Text    = siguiente != null ? PedidoBLL.GetEtiquetaAvance(estado) : "-";
            btnAvanzar.Enabled = siguiente != null;
        }

        if (btnCancelar != null)
            btnCancelar.Visible = PedidoBLL.PuedeCancelarAdmin(estado);
    }

    protected void gvPedidos_RowCommand(object sender, GridViewCommandEventArgs e)
    {
        int idPedido = int.Parse(e.CommandArgument.ToString());

        if (e.CommandName == "AvanzarEstado")
        {
            try
            {
                string usr        = Session["Usuario"].ToString();
                string nuevoEstado = PedidoBLL.AvanzarEstado(idPedido, usr);
                Bitacora.Registrar(usr, "PEDIDO_AVANCE", "Pedido #" + idPedido + " -> " + nuevoEstado);
                MostrarMensaje("Pedido #" + idPedido + " avanzado a: " + nuevoEstado, false);
                CargarPedidos();
                CargarAlertaStock();
            }
            catch (Exception ex)
            {
                MostrarMensaje("Error al avanzar estado: " + ex.Message, true);
            }
        }
        else if (e.CommandName == "CancelarPedido")
        {
            try
            {
                string usr          = Session["Usuario"].ToString();
                string estadoPrevio = PedidoBLL.Cancelar(idPedido, usr, esAdmin: true);
                Bitacora.Registrar(usr, "PEDIDO_CANCELADO", "Pedido #" + idPedido + ": " + estadoPrevio + " -> Cancelado");
                MostrarMensaje("Pedido #" + idPedido + " cancelado. Stock restaurado.", false);
                pnlDetallePedidoAdmin.Visible = false;
                CargarPedidos();
                CargarAlertaStock();
            }
            catch (Exception ex)
            {
                MostrarMensaje("Error al cancelar: " + ex.Message, true);
            }
        }
        else if (e.CommandName == "VerDetalle")
        {
            try
            {
                DataTable dt = PedidoBLL.GetDetalleByPedido(idPedido);
                lblDetallePedidoTitulo.Text   = "Detalle del pedido #" + idPedido;
                gvDetallePedidoAdmin.DataSource = dt;
                gvDetallePedidoAdmin.DataBind();
                pnlDetallePedidoAdmin.Visible = true;
                Page.ClientScript.RegisterStartupScript(GetType(), "scrollDetalle",
                    "document.getElementById('" + pnlDetallePedidoAdmin.ClientID + "').scrollIntoView({behavior:'smooth'});", true);
            }
            catch (Exception ex)
            {
                MostrarMensaje("Error al ver detalle: " + ex.Message, true);
            }
        }
    }

    protected void btnCerrarDetallePedido_Click(object sender, EventArgs e)
    {
        pnlDetallePedidoAdmin.Visible = false;
    }

    private void MostrarMensajePedido(string texto, bool esError)
    {
        lblMensajePedido.Text     = texto;
        lblMensajePedido.CssClass = esError ? "msg msg-err" : "msg msg-ok";
        lblMensajePedido.Visible  = true;
    }

    // ===== MENSAJES =====

    private void MostrarMensaje(string texto, bool esError)
    {
        lblMensaje.Text     = texto;
        lblMensaje.CssClass = esError ? "msg msg-err" : "msg msg-ok";
        lblMensaje.Visible  = true;
    }
}
