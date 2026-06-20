<%@ Page Language="C#" AutoEventWireup="true" CodeFile="Menu.aspx.cs" Inherits="Menu" %>
<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <meta http-equiv="Content-Type" content="text/html; charset=utf-8" />
    <title>PetShop - Catalogo</title>
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
            margin-left: 15px;
        }
        #contenido {
            width: 900px;
            margin: 15px auto;
            background-color: white;
            border: 1px solid #cccccc;
            padding: 15px;
        }
        .btn-categoria {
            background-color: #336699;
            color: white;
            border: none;
            padding: 5px 12px;
            margin: 2px;
            cursor: pointer;
        }
        .btn-categoria-activo {
            background-color: #1a4d80;
            color: white;
            border: 2px solid #ffcc00;
            padding: 4px 11px;
            margin: 2px;
            cursor: pointer;
            font-weight: bold;
        }
        .tabla-productos {
            width: 100%;
            border-collapse: collapse;
            margin-top: 10px;
        }
        .tabla-productos th {
            background-color: #336699;
            color: white;
            padding: 8px;
            text-align: left;
            border: 1px solid #cccccc;
        }
        .tabla-productos td {
            border: 1px solid #cccccc;
            padding: 8px;
        }
        .tabla-productos tr:nth-child(even) {
            background-color: #f5f5f5;
        }
        .btn-agregar {
            background-color: #2e7d32;
            color: white;
            border: none;
            padding: 4px 10px;
            cursor: pointer;
        }
        .mensaje-ok {
            color: green;
            font-weight: bold;
            margin: 5px 0;
        }
        .mensaje-error {
            color: red;
            font-weight: bold;
            margin: 5px 0;
        }
        .precio {
            font-weight: bold;
            color: #2e7d32;
        }
        .stock-ok   { color: #2e7d32; font-weight: bold; }
        .stock-bajo { color: #e65100; font-weight: bold; }
        .stock-cero { color: #cc0000; font-weight: bold; }
        .sin-stock  { color: #cc0000; font-style: italic; font-size: 12px; }
    </style>
</head>
<body>
    <form id="form1" runat="server">

        <!-- Cabecera con bienvenida y navegacion -->
        <div id="cabecera">
            <strong>PetShop</strong>
            &nbsp;&nbsp;|&nbsp;&nbsp;
            Bienvenido: <asp:Label ID="lblUsuario" runat="server" />
            &nbsp;&nbsp;|&nbsp;&nbsp;
            <a href="Carrito.aspx">Ver carrito (<asp:Label ID="lblCantCarrito" runat="server" Text="0" />)</a>
            &nbsp;&nbsp;|&nbsp;&nbsp;
            <a href="MisPedidos.aspx">Mis Pedidos</a>
            &nbsp;&nbsp;|&nbsp;&nbsp;
            <a href="MiPerfil.aspx">Mi Perfil</a>
            &nbsp;&nbsp;|&nbsp;&nbsp;
            <asp:HyperLink ID="lnkAdmin" runat="server" NavigateUrl="Admin.aspx"
                ForeColor="White" Visible="false">Panel Admin</asp:HyperLink>
            &nbsp;
            <asp:HyperLink ID="lnkWebMaster" runat="server" NavigateUrl="WebMaster.aspx"
                ForeColor="White" Visible="false">Panel WebMaster</asp:HyperLink>
            &nbsp;&nbsp;|&nbsp;&nbsp;
            <asp:LinkButton ID="btnCerrarSesion" runat="server" ForeColor="White"
                OnClick="btnCerrarSesion_Click"
                OnClientClick="return confirm('Cerrar sesion?')">Cerrar sesion</asp:LinkButton>
        </div>

        <!-- Panel visible cuando la BD no esta disponible -->
        <asp:Panel ID="pnlDBError" runat="server" Visible="false">
            <div id="contenido">
                <p style="color:red; font-weight:bold;">No esta disponible el sistema. Base de datos no disponible.</p>
                <p><a href="Default.aspx">Volver al inicio</a></p>
            </div>
        </asp:Panel>

        <asp:Panel ID="pnlContenido" runat="server">
        <div id="contenido">
            <h3>Catalogo de productos</h3>

            <!-- Botones de filtro por categoria -->
            <p>
                <strong>Filtrar por categoria:</strong><br />
                <asp:Button ID="btnTodos"      runat="server" Text="Todos"       CommandArgument=""           OnClick="btnFiltro_Click" />
                <asp:Button ID="btnPerros"     runat="server" Text="Perros"      CommandArgument="Perros"     OnClick="btnFiltro_Click" />
                <asp:Button ID="btnGatos"      runat="server" Text="Gatos"       CommandArgument="Gatos"      OnClick="btnFiltro_Click" />
                <asp:Button ID="btnJuguetes"   runat="server" Text="Juguetes"    CommandArgument="Juguetes"   OnClick="btnFiltro_Click" />
                <asp:Button ID="btnAccesorios" runat="server" Text="Accesorios"  CommandArgument="Accesorios" OnClick="btnFiltro_Click" />
                <asp:Button ID="btnSalud"      runat="server" Text="Salud"       CommandArgument="Salud"      OnClick="btnFiltro_Click" />
            </p>

            <!-- Mensaje cuando se agrega un producto -->
            <asp:Label ID="lblMensaje" runat="server" CssClass="mensaje-ok" Visible="false" />

            <!-- Tabla con los productos -->
            <asp:GridView ID="gvProductos" runat="server"
                AutoGenerateColumns="false"
                CssClass="tabla-productos"
                OnRowCommand="gvProductos_RowCommand">
                <Columns>
                    <asp:BoundField DataField="Nombre"      HeaderText="Producto"    />
                    <asp:BoundField DataField="Descripcion" HeaderText="Descripcion" />
                    <asp:BoundField DataField="Categoria"   HeaderText="Categoria"   ItemStyle-Width="90px" />
                    <asp:BoundField DataField="Precio"      HeaderText="Precio"
                        DataFormatString="${0:N2}"
                        ItemStyle-CssClass="precio"
                        ItemStyle-Width="80px" />
                    <asp:TemplateField HeaderText="Stock" ItemStyle-Width="60px" ItemStyle-HorizontalAlign="Center">
                        <ItemTemplate>
                            <asp:Label runat="server"
                                Text='<%# Eval("Stock") %>'
                                CssClass='<%# (int)DataBinder.Eval(Container.DataItem,"Stock") == 0 ? "stock-cero" : ((int)DataBinder.Eval(Container.DataItem,"Stock") <= 5 ? "stock-bajo" : "stock-ok") %>' />
                        </ItemTemplate>
                    </asp:TemplateField>
                    <asp:TemplateField HeaderText="Accion" ItemStyle-Width="100px">
                        <ItemTemplate>
                            <asp:LinkButton ID="lnkAgregar" runat="server"
                                CommandName="AgregarAlCarrito"
                                CommandArgument='<%# Eval("IdProducto") %>'
                                CssClass="btn-agregar"
                                Visible='<%# (int)DataBinder.Eval(Container.DataItem,"Stock") > 0 %>'>
                                + Agregar
                            </asp:LinkButton>
                            <asp:Label ID="lblSinStock" runat="server"
                                CssClass="sin-stock" Text="Sin stock"
                                Visible='<%# (int)DataBinder.Eval(Container.DataItem,"Stock") == 0 %>' />
                        </ItemTemplate>
                    </asp:TemplateField>
                </Columns>
            </asp:GridView>
        </div>
        </asp:Panel>

    </form>
</body>
</html>
