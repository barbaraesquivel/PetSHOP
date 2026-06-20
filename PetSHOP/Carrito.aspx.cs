using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Web.UI.WebControls;
using BE;
using BLL;
using DAL;
using SERV;

public partial class Carrito : System.Web.UI.Page
{
    protected void Page_Load(object sender, EventArgs e)
    {
        if (!SessionHelper.VerificarRol(this, "Usuario")) return;

        bool bloqueado = Application["SistemaBlockeado"] != null && (bool)Application["SistemaBlockeado"];
        if (bloqueado)
        {
            string rol = Session["Rol"] != null ? Session["Rol"].ToString() : "";
            if (rol == "WebMaster")
                Response.Redirect("WebMaster.aspx", false);
            else
                Response.Redirect("Error.aspx?motivo=integridad", false);
            return;
        }

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

        int idUsuario = (int)Session["IdUsuario"];
        string nombreUsuario = Session["Usuario"].ToString();

        try
        {
            using (SqlConnection con = ConexionBD.ObtenerConexion())
            {
                con.Open();
                SqlTransaction tx = con.BeginTransaction();
                try
                {
                    // Obtener o crear el registro de Cliente
                    int idCliente;
                    Cliente clienteExistente = ClienteBLL.GetByIdUsuario(idUsuario);

                    if (clienteExistente != null)
                    {
                        idCliente = clienteExistente.IdCliente;
                    }
                    else
                    {
                        // Leer datos personales guardados en Usuarios al registrarse
                        SqlCommand cmdDatos = new SqlCommand(
                            "SELECT Nombre, Apellido, Email, Telefono, Direccion FROM Usuarios WHERE IdUsuario = @id",
                            con, tx);
                        cmdDatos.Parameters.AddWithValue("@id", idUsuario);
                        SqlDataReader rDatos = cmdDatos.ExecuteReader();
                        string nombre = "", apellido = "", email = "", telefono = "", direccion = "";
                        if (rDatos.Read())
                        {
                            nombre    = rDatos["Nombre"]    == DBNull.Value ? "" : rDatos["Nombre"].ToString();
                            apellido  = rDatos["Apellido"]  == DBNull.Value ? "" : rDatos["Apellido"].ToString();
                            email     = rDatos["Email"]     == DBNull.Value ? "" : rDatos["Email"].ToString();
                            telefono  = rDatos["Telefono"]  == DBNull.Value ? "" : rDatos["Telefono"].ToString();
                            direccion = rDatos["Direccion"] == DBNull.Value ? "" : rDatos["Direccion"].ToString();
                        }
                        rDatos.Close();

                        if (nombre == "" || apellido == "" || email == "")
                        {
                            lblConfirmacion.Text    = "Antes de confirmar el pedido completa tus datos personales en <a href='MiPerfil.aspx'>Mi Perfil</a>.";
                            lblConfirmacion.Visible = true;
                            tx.Rollback();
                            return;
                        }

                        idCliente = ClienteBLL.Crear(con, tx, idUsuario, nombre, apellido, email, telefono, direccion);
                        Bitacora.Registrar(nombreUsuario, "CLIENTE_NUEVO", "Primera compra - cliente registrado Id:" + idCliente);
                    }

                    // Verificar y descontar stock atomicamente
                    foreach (var kvp in carrito)
                    {
                        int idProd   = kvp.Key;
                        int cantidad = kvp.Value.Cantidad;

                        SqlCommand cmdStock = new SqlCommand(
                            "UPDATE Productos SET Stock = Stock - @cant WHERE IdProducto = @id AND Stock >= @cant",
                            con, tx);
                        cmdStock.Parameters.AddWithValue("@cant", cantidad);
                        cmdStock.Parameters.AddWithValue("@id",   idProd);
                        int afectadas = (int)cmdStock.ExecuteNonQuery();

                        if (afectadas == 0)
                        {
                            SqlCommand cmdGetStock = new SqlCommand(
                                "SELECT Nombre, Stock FROM Productos WHERE IdProducto = @id", con, tx);
                            cmdGetStock.Parameters.AddWithValue("@id", idProd);
                            SqlDataReader rStock = cmdGetStock.ExecuteReader();
                            string msgStock = "Stock insuficiente";
                            if (rStock.Read())
                                msgStock = "Stock insuficiente para '" + rStock["Nombre"] + "'. Disponible: " + rStock["Stock"] + ", solicitado: " + cantidad;
                            rStock.Close();
                            throw new Exception(msgStock);
                        }
                    }

                    // Insertar pedido usando IdCliente
                    SqlCommand cmdPedido = new SqlCommand(
                        @"INSERT INTO Pedidos (IdCliente, FechaPedido, Total, Estado)
                          OUTPUT INSERTED.IdPedido
                          VALUES (@idCliente, GETDATE(), @total, 'Pendiente')",
                        con, tx);
                    cmdPedido.Parameters.AddWithValue("@idCliente", idCliente);
                    cmdPedido.Parameters.AddWithValue("@total",     total);
                    int idPedido = (int)cmdPedido.ExecuteScalar();

                    foreach (var kvp in carrito)
                    {
                        SqlCommand cmdDetalle = new SqlCommand(
                            @"INSERT INTO DetallePedido (IdPedido, NombreProducto, PrecioUnitario, Cantidad, Subtotal, IdProducto)
                              VALUES (@idPedido, @nombre, @precio, @cantidad, @subtotal, @idProducto)",
                            con, tx);
                        cmdDetalle.Parameters.AddWithValue("@idPedido",   idPedido);
                        cmdDetalle.Parameters.AddWithValue("@nombre",     kvp.Value.Nombre);
                        cmdDetalle.Parameters.AddWithValue("@precio",     kvp.Value.Precio);
                        cmdDetalle.Parameters.AddWithValue("@cantidad",   kvp.Value.Cantidad);
                        cmdDetalle.Parameters.AddWithValue("@subtotal",   kvp.Value.Subtotal);
                        cmdDetalle.Parameters.AddWithValue("@idProducto", kvp.Key);
                        cmdDetalle.ExecuteNonQuery();
                    }

                    tx.Commit();
                }
                catch
                {
                    tx.Rollback();
                    throw;
                }
            }

            Bitacora.Registrar(nombreUsuario, "PEDIDO",
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
