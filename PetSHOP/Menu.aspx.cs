using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Web.UI.WebControls;

public partial class Menu : System.Web.UI.Page
{
    protected void Page_Load(object sender, EventArgs e)
    {
        if (!SesionHelper.VerificarRol(this, "Usuario")) return;

        lblUsuario.Text    = Session["Usuario"].ToString();
        lblMensaje.Visible = false;

        if (!SesionHelper.VerificarDB(this))
        {
            pnlDBError.Visible   = true;
            pnlContenido.Visible = false;
            return;
        }

        // Mostramos links segun el rol
        string rol = Session["Rol"].ToString();
        if (rol == "Admin" || rol == "WebMaster")
            lnkAdmin.Visible = true;
        if (rol == "WebMaster")
            lnkWebMaster.Visible = true;

        string categoria = Session["CategoriaFiltro"] != null ? Session["CategoriaFiltro"].ToString() : "";
        CargarProductos(categoria);
        ActualizarContadorCarrito();

        if (!IsPostBack)
            Bitacora.Registrar(Session["Usuario"].ToString(), "ACCESO", "Entro al catalogo");
    }

    private void CargarProductos(string categoria)
    {
        try
        {
            using (SqlConnection con = ConexionDB.ObtenerConexion())
            {
                con.Open();

                // La BD usa IdProducto como clave primaria en Productos
                string sql = "SELECT IdProducto, Nombre, Descripcion, Precio, Categoria FROM Productos WHERE Activo=1";
                if (categoria != "")
                    sql += " AND Categoria=@categoria";
                sql += " ORDER BY Nombre";

                SqlCommand cmd = new SqlCommand(sql, con);
                if (categoria != "")
                    cmd.Parameters.AddWithValue("@categoria", categoria);

                SqlDataAdapter da = new SqlDataAdapter(cmd);
                DataTable dt = new DataTable();
                da.Fill(dt);

                gvProductos.DataSource = dt;
                gvProductos.DataBind();
            }

            ActualizarEstiloBotones(categoria);
        }
        catch (Exception ex)
        {
            lblMensaje.Text    = "Error al cargar productos: " + ex.Message;
            lblMensaje.Visible = true;
        }
    }

    private void ActualizarEstiloBotones(string cat)
    {
        btnTodos.CssClass      = cat == ""           ? "btn-categoria-activo" : "btn-categoria";
        btnPerros.CssClass     = cat == "Perros"     ? "btn-categoria-activo" : "btn-categoria";
        btnGatos.CssClass      = cat == "Gatos"      ? "btn-categoria-activo" : "btn-categoria";
        btnJuguetes.CssClass   = cat == "Juguetes"   ? "btn-categoria-activo" : "btn-categoria";
        btnAccesorios.CssClass = cat == "Accesorios" ? "btn-categoria-activo" : "btn-categoria";
        btnSalud.CssClass      = cat == "Salud"      ? "btn-categoria-activo" : "btn-categoria";
    }

    private void ActualizarContadorCarrito()
    {
        Dictionary<int, ItemCarrito> carrito = ObtenerCarrito();
        int total = 0;
        foreach (ItemCarrito item in carrito.Values)
            total += item.Cantidad;
        lblCantCarrito.Text = total.ToString();
    }

    private Dictionary<int, ItemCarrito> ObtenerCarrito()
    {
        if (Session["Carrito"] == null)
            Session["Carrito"] = new Dictionary<int, ItemCarrito>();
        return (Dictionary<int, ItemCarrito>)Session["Carrito"];
    }

    protected void btnFiltro_Click(object sender, EventArgs e)
    {
        string categoria = ((Button)sender).CommandArgument;
        Session["CategoriaFiltro"] = categoria;
        CargarProductos(categoria);
        ActualizarContadorCarrito();
    }

    protected void gvProductos_RowCommand(object sender, GridViewCommandEventArgs e)
    {
        if (e.CommandName != "AgregarAlCarrito") return;

        int idProducto = int.Parse(e.CommandArgument.ToString());

        try
        {
            // Buscamos el producto en la BD para obtener nombre y precio
            using (SqlConnection con = ConexionDB.ObtenerConexion())
            {
                con.Open();
                SqlCommand cmd = new SqlCommand(
                    "SELECT Nombre, Precio FROM Productos WHERE IdProducto=@id AND Activo=1", con);
                cmd.Parameters.AddWithValue("@id", idProducto);
                SqlDataReader reader = cmd.ExecuteReader();

                if (!reader.Read()) { reader.Close(); return; }

                string  nombre = reader["Nombre"].ToString();
                decimal precio = (decimal)reader["Precio"];
                reader.Close();

                // Usamos Dictionary<int, ItemCarrito> con el Id como clave
                Dictionary<int, ItemCarrito> carrito = ObtenerCarrito();

                if (carrito.ContainsKey(idProducto))
                    carrito[idProducto].Cantidad++;
                else
                    carrito[idProducto] = new ItemCarrito { Nombre = nombre, Precio = precio, Cantidad = 1 };

                Session["Carrito"] = carrito;
                lblMensaje.Text    = "Se agrego '" + nombre + "' al carrito.";
                lblMensaje.Visible = true;
                ActualizarContadorCarrito();
            }
        }
        catch (Exception ex)
        {
            lblMensaje.Text    = "Error al agregar: " + ex.Message;
            lblMensaje.Visible = true;
        }
    }

    protected void btnCerrarSesion_Click(object sender, EventArgs e)
    {
        Bitacora.Registrar(Session["Usuario"].ToString(), "LOGOUT", "Cerro sesion");
        Session.Clear();
        Session.Abandon();
        Response.Redirect("Default.aspx");
    }
}
