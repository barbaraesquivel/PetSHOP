using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Web.UI.WebControls;

public partial class Carrito : System.Web.UI.Page
{
    protected void Page_Load(object sender, EventArgs e)
    {
        if (!SessionHelper.VerificarRol(this, "Usuario")) return;

        if (!SessionHelper.VerificarDB(this))
        {
            pnlDBError.Visible = true;
            pnlVacio.Visible   = false;
            pnlCarrito.Visible = false;
            return;
        }

        CargarCarrito();
    }

    private Dictionary<int, ItemCarrito> ObtenerCarrito()
    {
        if (Session["Carrito"] == null)
            Session["Carrito"] = new Dictionary<int, ItemCarrito>();
        return (Dictionary<int, ItemCarrito>)Session["Carrito"];
    }

    private void CargarCarrito()
    {
        Dictionary<int, ItemCarrito> carrito = ObtenerCarrito();

        if (carrito.Count == 0)
        {
            pnlVacio.Visible   = true;
            pnlCarrito.Visible = false;
            return;
        }

        pnlVacio.Visible   = false;
        pnlCarrito.Visible = true;

        // Proyectamos el diccionario a una lista anonima para el GridView
        // La clave (IdProducto) va como campo para los comandos + - eliminar
        var items = new System.Collections.ArrayList();
        foreach (var kvp in carrito)
        {
            items.Add(new
            {
                IdProducto = kvp.Key,
                Nombre     = kvp.Value.Nombre,
                Precio     = kvp.Value.Precio,
                Cantidad   = kvp.Value.Cantidad,
                Subtotal   = kvp.Value.Subtotal
            });
        }

        gvCarrito.DataSource = items;
        gvCarrito.DataBind();

        decimal total = 0;
        foreach (ItemCarrito item in carrito.Values)
            total += item.Subtotal;

        lblTotal.Text = "$" + total.ToString("N2");
    }

    protected void gvCarrito_RowCommand(object sender, GridViewCommandEventArgs e)
    {
        int idProducto                       = int.Parse(e.CommandArgument.ToString());
        Dictionary<int, ItemCarrito> carrito = ObtenerCarrito();

        if (!carrito.ContainsKey(idProducto)) return;

        if (e.CommandName == "Sumar")
        {
            carrito[idProducto].Cantidad++;
        }
        else if (e.CommandName == "Restar")
        {
            carrito[idProducto].Cantidad--;
            if (carrito[idProducto].Cantidad <= 0)
                carrito.Remove(idProducto);
        }
        else if (e.CommandName == "Eliminar")
        {
            carrito.Remove(idProducto);
        }

        Session["Carrito"] = carrito;
        CargarCarrito();
    }

    protected void btnConfirmar_Click(object sender, EventArgs e)
    {
        Dictionary<int, ItemCarrito> carrito = ObtenerCarrito();

        decimal total     = 0;
        int     cantItems = 0;
        foreach (ItemCarrito item in carrito.Values)
        {
            total     += item.Subtotal;
            cantItems += item.Cantidad;
        }

        try
        {
            using (SqlConnection con = ConexionBD.ObtenerConexion())
            {
                con.Open();

                // Insertamos el pedido y obtenemos su IdPedido
                // Columnas reales: IdPedido, IdUsuario, FechaPedido, Total, Estado
                SqlCommand cmdPedido = new SqlCommand(
                    @"INSERT INTO Pedidos (IdUsuario, FechaPedido, Total, Estado)
                      OUTPUT INSERTED.IdPedido
                      VALUES (@idUsuario, GETDATE(), @total, 'Activo')", con);
                cmdPedido.Parameters.AddWithValue("@idUsuario", Session["IdUsuario"]);
                cmdPedido.Parameters.AddWithValue("@total",     total);
                int idPedido = (int)cmdPedido.ExecuteScalar();

                // Insertamos cada item en DetallePedido
                // Columnas reales: IdDetalle, IdPedido, NombreProducto, PrecioUnitario, Cantidad, Subtotal
                foreach (var kvp in carrito)
                {
                    SqlCommand cmdDetalle = new SqlCommand(
                        @"INSERT INTO DetallePedido (IdPedido, NombreProducto, PrecioUnitario, Cantidad, Subtotal)
                          VALUES (@idPedido, @nombre, @precio, @cantidad, @subtotal)", con);
                    cmdDetalle.Parameters.AddWithValue("@idPedido",  idPedido);
                    cmdDetalle.Parameters.AddWithValue("@nombre",    kvp.Value.Nombre);
                    cmdDetalle.Parameters.AddWithValue("@precio",    kvp.Value.Precio);
                    cmdDetalle.Parameters.AddWithValue("@cantidad",  kvp.Value.Cantidad);
                    cmdDetalle.Parameters.AddWithValue("@subtotal",  kvp.Value.Subtotal);
                    cmdDetalle.ExecuteNonQuery();
                }
            }

            Bitacora.Registrar(Session["Usuario"].ToString(), "PEDIDO",
                cantItems + " items - Total: $" + total.ToString("N2"));

            Session["Carrito"] = new Dictionary<int, ItemCarrito>();

            lblConfirmacion.Text    = "Pedido confirmado! Total: $" + total.ToString("N2") + " - Gracias por tu compra!";
            lblConfirmacion.Visible = true;
            pnlCarrito.Visible      = false;
            pnlVacio.Visible        = false;
        }
        catch (Exception ex)
        {
            lblConfirmacion.Text    = "Error al confirmar el pedido: " + ex.Message;
            lblConfirmacion.Visible = true;
        }
    }

    protected void btnVolverCatalogo_Click(object sender, EventArgs e)
    {
        Response.Redirect("Menu.aspx");
    }
}
