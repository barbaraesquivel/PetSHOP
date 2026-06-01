<%@ Page Language="C#" AutoEventWireup="true" CodeFile="Carrito.aspx.cs" Inherits="Carrito" %>
<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <meta http-equiv="Content-Type" content="text/html; charset=utf-8" />
    <title>PetShop - Carrito</title>
    <style>
        body {
            font-family: Arial, sans-serif;
            background-color: #f0f0f0;
            margin: 0;
            padding: 0;
        }
        #cabecera {
            background-color: #336699;
            color: white;
            padding: 10px 15px;
        }
        #cabecera a {
            color: white;
        }
        #contenido {
            width: 800px;
            margin: 15px auto;
            background-color: white;
            border: 1px solid #cccccc;
            padding: 15px;
        }
        .tabla-carrito {
            width: 100%;
            border-collapse: collapse;
        }
        .tabla-carrito th {
            background-color: #336699;
            color: white;
            padding: 8px;
            border: 1px solid #cccccc;
            text-align: left;
        }
        .tabla-carrito td {
            border: 1px solid #cccccc;
            padding: 8px;
        }
        .tabla-carrito tr:nth-child(even) {
            background-color: #f5f5f5;
        }
        .btn-cantidad {
            background-color: #336699;
            color: white;
            border: none;
            padding: 2px 8px;
            cursor: pointer;
            font-weight: bold;
        }
        .btn-eliminar {
            background-color: #cc0000;
            color: white;
            border: none;
            padding: 3px 8px;
            cursor: pointer;
        }
        .fila-total {
            font-weight: bold;
            font-size: 16px;
            margin: 10px 0;
        }
        .btn-confirmar {
            background-color: #2e7d32;
            color: white;
            border: none;
            padding: 8px 20px;
            cursor: pointer;
            font-size: 14px;
        }
        .btn-volver {
            background-color: #336699;
            color: white;
            border: none;
            padding: 8px 20px;
            cursor: pointer;
            font-size: 14px;
        }
        .mensaje-ok {
            color: green;
            font-weight: bold;
        }
        .mensaje-vacio {
            color: gray;
        }
        .precio {
            font-weight: bold;
            color: #2e7d32;
        }
    </style>
</head>
<body>
    <form id="form1" runat="server">

        <!-- Cabecera -->
        <div id="cabecera">
            <strong>PetShop</strong>
            &nbsp;&nbsp;|&nbsp;&nbsp;
            <a href="Menu.aspx"> Volver al catalogo</a>
        </div>

        <div id="contenido">
            <h3>Mi Carrito</h3>

            <!-- Panel visible cuando la BD no esta disponible -->
            <asp:Panel ID="pnlDBError" runat="server" Visible="false">
                <p style="color:red; font-weight:bold;">No esta disponible el sistema. Base de datos no disponible.</p>
                <p><a href="Default.aspx">Volver al inicio</a></p>
            </asp:Panel>

            <!-- Mensaje de confirmacion de pedido -->
            <asp:Label ID="lblConfirmacion" runat="server" CssClass="mensaje-ok" Visible="false" />

            <!-- Mensaje cuando el carrito esta vacio -->
            <asp:Panel ID="pnlVacio" runat="server" Visible="false">
                <p class="mensaje-vacio">El carrito esta vacio.</p>
                <asp:Button ID="btnIrCatalogo" runat="server" Text="Ver productos"
                    CssClass="btn-volver" OnClick="btnVolverCatalogo_Click" />
            </asp:Panel>

            <!-- Panel con los productos del carrito -->
            <asp:Panel ID="pnlCarrito" runat="server" Visible="false">

                <!-- Tabla con los items -->
                <asp:GridView ID="gvCarrito" runat="server"
                    AutoGenerateColumns="false"
                    CssClass="tabla-carrito"
                    OnRowCommand="gvCarrito_RowCommand">
                    <Columns>
                        <asp:BoundField DataField="Nombre"   HeaderText="Producto"    />
                        <asp:BoundField DataField="Precio"   HeaderText="Precio unit."
                            DataFormatString="${0:N2}" ItemStyle-CssClass="precio" ItemStyle-Width="90px" />
                        <asp:TemplateField HeaderText="Cantidad" ItemStyle-Width="100px" ItemStyle-HorizontalAlign="Center">
                            <ItemTemplate>
                                <asp:LinkButton runat="server" CommandName="Restar"
                                    CommandArgument='<%# Eval("IdProducto") %>' CssClass="btn-cantidad">-</asp:LinkButton>
                                &nbsp;<%# Eval("Cantidad") %>&nbsp;
                                <asp:LinkButton runat="server" CommandName="Sumar"
                                    CommandArgument='<%# Eval("IdProducto") %>' CssClass="btn-cantidad">+</asp:LinkButton>
                            </ItemTemplate>
                        </asp:TemplateField>
                        <asp:BoundField DataField="Subtotal" HeaderText="Subtotal"
                            DataFormatString="${0:N2}" ItemStyle-CssClass="precio" ItemStyle-Width="90px" />
                        <asp:TemplateField HeaderText="Quitar" ItemStyle-Width="70px" ItemStyle-HorizontalAlign="Center">
                            <ItemTemplate>
                                <asp:LinkButton runat="server" CommandName="Eliminar"
                                    CommandArgument='<%# Eval("IdProducto") %>' CssClass="btn-eliminar"
                                    OnClientClick="return confirm('¿Eliminar este producto?')">X</asp:LinkButton>
                            </ItemTemplate>
                        </asp:TemplateField>
                    </Columns>
                </asp:GridView>

                <!-- Total y botones de accion -->
                <br />
                <p class="fila-total">
                    Total: <asp:Label ID="lblTotal" runat="server" CssClass="precio" />
                </p>

                <asp:Button ID="btnVolver2" runat="server" Text="Seguir comprando"
                    CssClass="btn-volver" OnClick="btnVolverCatalogo_Click" />
                &nbsp;&nbsp;
                <asp:Button ID="btnConfirmar" runat="server" Text="Confirmar pedido"
                    CssClass="btn-confirmar" OnClick="btnConfirmar_Click"
                    OnClientClick="return confirm('¿Confirmar el pedido?')" />

            </asp:Panel>
        </div>

    </form>
</body>
</html>
