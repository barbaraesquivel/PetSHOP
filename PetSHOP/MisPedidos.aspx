<%@ Page Language="C#" AutoEventWireup="true" CodeFile="MisPedidos.aspx.cs" Inherits="MisPedidos" %>
<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <meta http-equiv="Content-Type" content="text/html; charset=utf-8" />
    <title>PetShop - Mis Pedidos</title>
    <style>
        body { font-family: Arial, sans-serif; background-color: #f0f0f0; margin: 0; padding: 0; }
        #cabecera { background-color: #336699; color: white; padding: 10px 15px; }
        #cabecera a { color: white; margin-left: 15px; }
        #contenido { width: 960px; margin: 15px auto; background: white; border: 1px solid #ccc; padding: 15px; }
        .tabla { width: 100%; border-collapse: collapse; margin-top: 8px; }
        .tabla th { background-color: #336699; color: white; padding: 7px; border: 1px solid #ccc; text-align: left; }
        .tabla td { border: 1px solid #ccc; padding: 7px; font-size: 13px; }
        .tabla tr:nth-child(even) { background-color: #f5f5f5; }
        .btn         { background-color: #336699; color: white; border: none; padding: 4px 10px; cursor: pointer; font-size: 12px; }
        .btn-danger  { background-color: #cc0000; color: white; border: none; padding: 4px 10px; cursor: pointer; font-size: 12px; }
        .msg         { font-weight: bold; margin: 6px 0; padding: 6px; }
        .msg-ok      { color: green; background: #e8f5e9; border: 1px solid #a5d6a7; }
        .msg-err     { color: red;   background: #ffebee; border: 1px solid #ef9a9a; }
        .alerta-retiro {
            background: #e8f5e9; border: 2px solid #2e7d32; padding: 12px 16px;
            margin-bottom: 12px; font-weight: bold; font-size: 15px; color: #1b5e20;
        }
        .sin-pedidos { color: #777; font-style: italic; margin: 20px 0; }
        .panel-detalle { background: #f9f9f9; border: 1px solid #ddd; padding: 12px; margin: 10px 0; }
        .estado-Pendiente        { color: #888; }
        .estado-Confirmado       { color: #1565C0; font-weight: bold; }
        .estado-EnPreparacion    { color: #e65100; font-weight: bold; }
        .estado-ListoParaRetirar { color: #2e7d32; font-weight: bold; background: #f1f8e9; padding: 2px 6px; }
        .estado-Retirado         { color: #333; }
        .estado-Cancelado        { color: #cc0000; text-decoration: line-through; }
    </style>
</head>
<body>
    <form id="form1" runat="server">

        <div id="cabecera">
            <strong>PetShop</strong>
            &nbsp;&nbsp;|&nbsp;&nbsp;
            Bienvenido: <asp:Label ID="lblUsuario" runat="server" />
            &nbsp;&nbsp;|&nbsp;&nbsp;
            <a href="Menu.aspx">Volver al catalogo</a>
            &nbsp;&nbsp;|&nbsp;&nbsp;
            <a href="Carrito.aspx">Ver carrito</a>
            &nbsp;&nbsp;|&nbsp;&nbsp;
            <asp:LinkButton ID="btnCerrarSesion" runat="server" ForeColor="White"
                OnClick="btnCerrarSesion_Click"
                OnClientClick="return confirm('Cerrar sesion?')">Cerrar sesion</asp:LinkButton>
        </div>

        <div id="contenido">
            <h3>Mis Pedidos</h3>

            <asp:Label ID="lblMensaje" runat="server" CssClass="msg" Visible="false" />

            <!-- Alerta pedido listo para retirar -->
            <asp:Panel ID="pnlAlertaRetiro" runat="server" Visible="false" CssClass="alerta-retiro">
                <asp:Label ID="lblAlertaRetiro" runat="server" />
            </asp:Panel>

            <!-- Sin pedidos -->
            <asp:Label ID="lblSinPedidos" runat="server" CssClass="sin-pedidos" Visible="false"
                Text="Aun no realizaste ningun pedido. Visita el catalogo para comenzar a comprar." />

            <!-- Grilla de pedidos -->
            <asp:GridView ID="gvMisPedidos" runat="server"
                AutoGenerateColumns="false" CssClass="tabla"
                DataKeyNames="IdPedido"
                OnRowCommand="gvMisPedidos_RowCommand"
                OnRowDataBound="gvMisPedidos_RowDataBound"
                Visible="false">
                <Columns>
                    <asp:BoundField DataField="IdPedido"    HeaderText="Pedido #"  ItemStyle-Width="65px" />
                    <asp:BoundField DataField="FechaPedido" HeaderText="Fecha"
                        DataFormatString="{0:dd/MM/yyyy HH:mm}" ItemStyle-Width="130px" />
                    <asp:BoundField DataField="Total"       HeaderText="Total"
                        DataFormatString="${0:N2}" ItemStyle-Width="80px" />
                    <asp:TemplateField HeaderText="Estado" ItemStyle-Width="150px">
                        <ItemTemplate>
                            <asp:Label ID="lblEstado" runat="server"
                                Text='<%# Eval("Estado") %>'
                                CssClass='<%# "estado-" + Eval("Estado") %>' />
                        </ItemTemplate>
                    </asp:TemplateField>
                    <asp:TemplateField HeaderText="Acciones" ItemStyle-Width="160px">
                        <ItemTemplate>
                            <asp:Button ID="btnVerDetalle" runat="server"
                                CommandName="VerDetalle"
                                CommandArgument='<%# Eval("IdPedido") %>'
                                Text="Ver detalle" CssClass="btn" />
                            &nbsp;
                            <asp:Button ID="btnCancelarPedido" runat="server"
                                CommandName="CancelarPedido"
                                CommandArgument='<%# Eval("IdPedido") %>'
                                Text="Cancelar" CssClass="btn-danger"
                                OnClientClick="return confirm('Cancelar este pedido?')"
                                Visible="false" />
                        </ItemTemplate>
                    </asp:TemplateField>
                </Columns>
            </asp:GridView>

            <!-- Panel detalle del pedido -->
            <asp:Panel ID="pnlDetalle" runat="server" Visible="false" CssClass="panel-detalle">
                <b><asp:Label ID="lblDetalleTitulo" runat="server" /></b>
                <asp:GridView ID="gvDetalle" runat="server"
                    AutoGenerateColumns="false" CssClass="tabla" style="margin-top:6px;">
                    <Columns>
                        <asp:BoundField DataField="NombreProducto" HeaderText="Producto" />
                        <asp:BoundField DataField="PrecioUnitario" HeaderText="Precio Unit."
                            DataFormatString="${0:N2}" ItemStyle-Width="100px" />
                        <asp:BoundField DataField="Cantidad"       HeaderText="Cantidad"  ItemStyle-Width="70px" />
                        <asp:BoundField DataField="Subtotal"       HeaderText="Subtotal"
                            DataFormatString="${0:N2}" ItemStyle-Width="90px" />
                    </Columns>
                </asp:GridView>
                <br />
                <asp:Button ID="btnCerrarDetalle" runat="server" Text="Cerrar detalle"
                    CssClass="btn" OnClick="btnCerrarDetalle_Click" />
            </asp:Panel>

        </div>
    </form>
</body>
</html>
