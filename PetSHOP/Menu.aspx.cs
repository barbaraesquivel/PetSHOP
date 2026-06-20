using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Web.UI.WebControls;
using BE;
using DAL;
using SERV;

public partial class Menu : System.Web.UI.Page
{
    protected void Page_Load(object sender, EventArgs e)
    {
        if (!SessionHelper.VerificarRol(this, "Usuario")) return;

        bool bloqueado = Application["SistemaBlockeado"] != null && (bool)Application["SistemaBlockeado"];
        if (bloqueado)
        {
            string rolActual = Session["Rol"] != null ? Session["Rol"].ToString() : "";
            if (rolActual == "WebMaster")
                Response.Redirect("WebMaster.aspx", false);
            else
                Response.Redirect("Error.aspx?motivo=integridad", false);
            return;
        }

        lblUsuario.Text    = Session["Usuario"].ToString();
        lblMensaje.Visible = false;

        if (!SessionHelper.VerificarDB(this))
        {
            pnlDBError.Visible   = true;
            pnlContenido.Visible = false;
            return;
        }

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
            using (SqlConnection con = ConexionBD.ObtenerConexion())
            {
                con.Open();

                string sql = "SELECT IdProducto, Nombre, Descripcion, Precio, Categoria, Stock FROM Productos WHERE Activo=1 AND Eliminado=0";
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
            using (SqlConnection con = ConexionBD.ObtenerConexion())
            {
                con.Open();
                SqlCommand cmd = new SqlCommand(
                    "SELECT Nombre, Precio, Stock FROM Productos WHERE IdProducto=@id AND Activo=1 AND Eliminado=0", con);
                cmd.Parameters.AddWithValue("@id", idProducto);
                SqlDataReader reader = cmd.ExecuteReader();

                if (!reader.Read()) { reader.Close(); return; }

                string  nombre = reader["Nombre"].ToString();
                decimal precio = (decimal)reader["Precio"];
                int     stock  = (int)reader["Stock"];
                reader.Close();

                Dictionary<int, ItemCarrito> carrito = ObtenerCarrito();
                int cantidadActual = carrito.ContainsKey(idProducto) ? carrito[idProducto].Cantidad : 0;

                if (stock <= cantidadActual)
                {
                    lblMensaje.Text    = "Stock insuficiente para '" + nombre + "'. Disponible: " + stock + ", ya en carrito: " + cantidadActual + ".";
                    lblMensaje.CssClass = "mensaje-error";
                    lblMensaje.Visible = true;
                    string catActual = Session["CategoriaFiltro"] != null ? Session["CategoriaFiltro"].ToString() : "";
                    CargarProductos(catActual);
                    ActualizarContadorCarrito();
                    return;
                }

                if (carrito.ContainsKey(idProducto))
                    carrito[idProducto].Cantidad++;
                else
                    carrito[idProducto] = new ItemCarrito { Nombre = nombre, Precio = precio, Cantidad = 1 };

                Session["Carrito"] = carrito;
                lblMensaje.Text    = "Se agrego '" + nombre + "' al carrito.";
                lblMensaje.CssClass = "mensaje-ok";
                lblMensaje.Visible = true;
                ActualizarContadorCarrito();
            }
        }
        catch (Exception ex)
        {
            lblMensaje.Text    = "Error al agregar: " + ex.Message;
            lblMensaje.CssClass = "mensaje-error";
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
