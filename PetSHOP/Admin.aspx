<%@ Page Language="C#" AutoEventWireup="true" CodeFile="Admin.aspx.cs" Inherits="Admin" %>
<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <meta http-equiv="Content-Type" content="text/html; charset=utf-8" />
    <title>PetShop - Panel Admin</title>
    <style>
        body { font-family: Arial, sans-serif; background-color: #f0f0f0; margin: 0; padding: 0; }
        #cabecera { background-color: #336699; color: white; padding: 10px 15px; }
        #cabecera a { color: white; margin-left: 10px; }
        #contenido { width: 950px; margin: 15px auto; background: white; border: 1px solid #ccc; padding: 15px; }
        .tabla { width: 100%; border-collapse: collapse; margin-top: 8px; }
        .tabla th { background-color: #336699; color: white; padding: 7px; border: 1px solid #ccc; text-align: left; }
        .tabla td { border: 1px solid #ccc; padding: 7px; font-size: 13px; }
        .tabla tr:nth-child(even) { background-color: #f5f5f5; }
        .form-inline td { padding: 5px 8px; }
        .btn         { background-color: #336699; color: white; border: none; padding: 4px 10px; cursor: pointer; }
        .btn-agregar { background-color: #2e7d32; color: white; border: none; padding: 4px 10px; cursor: pointer; }
        .btn-editar  { background-color: #e65100; color: white; border: none; padding: 3px 8px; cursor: pointer; }
        .btn-danger  { background-color: #cc0000; color: white; border: none; padding: 3px 8px; cursor: pointer; }
        .msg { font-weight: bold; margin: 6px 0; padding: 6px; }
        .msg-ok  { color: green; background: #e8f5e9; border: 1px solid #a5d6a7; }
        .msg-err { color: red;   background: #ffebee; border: 1px solid #ef9a9a; }
        .modo-lectura { color: #e65100; font-weight: bold; }
        .denegado { color: red; font-size: 18px; font-weight: bold; margin: 30px; }
        .panel-form { background: #f9f9f9; border: 1px solid #ddd; padding: 12px; margin-bottom: 12px; }
        .inactivo { color: #cc0000; }
        .activo   { color: #2e7d32; }
        input[type=text], input[type=password], select { padding: 4px; border: 1px solid #aaa; }
    </style>
</head>
<body>
    <form id="form1" runat="server">

        <div id="cabecera">
            <strong>PetShop - Panel Admin</strong>
            &nbsp;|&nbsp;
            <a href="Menu.aspx">Volver al catalogo</a>
            &nbsp;|&nbsp;
            Usuario: <asp:Label ID="lblAdminUser" runat="server" />
        </div>

        <asp:Panel ID="pnlDenegado" runat="server" Visible="false">
            <p class="denegado">No esta disponible el sistema para su usuario.</p>
            <p style="margin-left:30px"><a href="Menu.aspx">Volver al catalogo</a></p>
        </asp:Panel>

        <asp:Panel ID="pnlContenido" runat="server" Visible="false">
            <div id="contenido">
                <h3>Panel de Administracion</h3>
                <asp:Label ID="lblModo"    runat="server" CssClass="modo-lectura" Visible="false" />
                <asp:Label ID="lblMensaje" runat="server" CssClass="msg"          Visible="false" />

                <hr />
                <!-- ===== SECCION A: USUARIOS ===== -->
                <h4>a) Gestion de Usuarios</h4>

                <!-- Formulario agregar (solo Admin) -->
                <asp:Panel ID="pnlFormUsuario" runat="server" CssClass="panel-form">
                    <b>Agregar nuevo usuario:</b>
                    <table class="form-inline">
                        <tr>
                            <td>Nombre de usuario:</td>
                            <td><asp:TextBox ID="txtNombreU" runat="server" MaxLength="50" /></td>
                            <td>Contrasena:</td>
                            <td><asp:TextBox ID="txtPassU" runat="server" TextMode="Password" MaxLength="100" /></td>
                            <td>Rol:</td>
                            <td>
                                <asp:DropDownList ID="ddlRolU" runat="server">
                                    <asp:ListItem Value="Usuario">Usuario</asp:ListItem>
                                    <asp:ListItem Value="Admin">Admin</asp:ListItem>
                                    <asp:ListItem Value="WebMaster">WebMaster</asp:ListItem>
                                </asp:DropDownList>
                            </td>
                        </tr>
                        <tr>
                            <td colspan="6">
                                <asp:Button ID="btnAgregarUsuario" runat="server" Text="Agregar usuario"
                                    CssClass="btn-agregar" OnClick="btnAgregarUsuario_Click" />
                            </td>
                        </tr>
                    </table>
                </asp:Panel>

                <!-- Formulario editar usuario (oculto por defecto) -->
                <asp:Panel ID="pnlEditarUsuario" runat="server" Visible="false" CssClass="panel-form">
                    <asp:HiddenField ID="hfIdUserEdit" runat="server" />
                    <b>Editando usuario: </b><asp:Label ID="lblNombreUserEdit" runat="server" /><br /><br />
                    <table class="form-inline">
                        <tr>
                            <td>Nuevo Rol:</td>
                            <td>
                                <asp:DropDownList ID="ddlEditRol" runat="server">
                                    <asp:ListItem Value="Usuario">Usuario</asp:ListItem>
                                    <asp:ListItem Value="Admin">Admin</asp:ListItem>
                                    <asp:ListItem Value="WebMaster">WebMaster</asp:ListItem>
                                </asp:DropDownList>
                            </td>
                            <td>
                                <asp:Button ID="btnGuardarUsuario"   runat="server" Text="Guardar"  CssClass="btn-agregar" OnClick="btnGuardarUsuario_Click" />
                                <asp:Button ID="btnCancelarEditUser" runat="server" Text="Cancelar" CssClass="btn"         OnClick="btnCancelarEditUser_Click" />
                            </td>
                        </tr>
                    </table>
                </asp:Panel>

                <!-- Grilla de usuarios (solo Id, Nombre, Rol — la BD no tiene Email ni Activo) -->
                <asp:GridView ID="gvUsuarios" runat="server"
                    AutoGenerateColumns="false" CssClass="tabla"
                    OnRowCommand="gvUsuarios_RowCommand"
                    OnRowDataBound="gvUsuarios_RowDataBound">
                    <Columns>
                        <asp:BoundField DataField="IdUsuario"     HeaderText="ID"      ItemStyle-Width="40px" />
                        <asp:BoundField DataField="NombreUsuario" HeaderText="Usuario" ItemStyle-Width="140px" />
                        <asp:BoundField DataField="Rol"           HeaderText="Rol"     ItemStyle-Width="100px" />
                        <asp:TemplateField HeaderText="Acciones" ItemStyle-Width="80px">
                            <ItemTemplate>
                                <asp:Panel ID="pnlAccionesUser" runat="server">
                                    <asp:LinkButton CommandName="EditarUser"
                                        CommandArgument='<%# Eval("IdUsuario") %>'
                                        CssClass="btn-editar">Editar</asp:LinkButton>
                                </asp:Panel>
                            </ItemTemplate>
                        </asp:TemplateField>
                    </Columns>
                </asp:GridView>

                <hr />
                <!-- ===== SECCION B: PRODUCTOS ===== -->
                <h4>b) Gestion de Productos y Precios</h4>

                <!-- Formulario agregar producto (solo Admin) -->
                <asp:Panel ID="pnlFormProducto" runat="server" CssClass="panel-form">
                    <b>Agregar nuevo producto:</b>
                    <table class="form-inline">
                        <tr>
                            <td>Nombre:</td>
                            <td><asp:TextBox ID="txtNombreP" runat="server" MaxLength="100" Width="180px" /></td>
                            <td>Precio ($):</td>
                            <td><asp:TextBox ID="txtPrecioP" runat="server" MaxLength="10" Width="80px" /></td>
                            <td>Categoria:</td>
                            <td>
                                <asp:DropDownList ID="ddlCatP" runat="server">
                                    <asp:ListItem>Perros</asp:ListItem>
                                    <asp:ListItem>Gatos</asp:ListItem>
                                    <asp:ListItem>Juguetes</asp:ListItem>
                                    <asp:ListItem>Accesorios</asp:ListItem>
                                    <asp:ListItem>Salud</asp:ListItem>
                                </asp:DropDownList>
                            </td>
                        </tr>
                        <tr>
                            <td>Descripcion:</td>
                            <td colspan="5"><asp:TextBox ID="txtDescP" runat="server" MaxLength="250" Width="400px" /></td>
                        </tr>
                        <tr>
                            <td colspan="6">
                                <asp:Button ID="btnAgregarProducto" runat="server" Text="Agregar producto"
                                    CssClass="btn-agregar" OnClick="btnAgregarProducto_Click" />
                            </td>
                        </tr>
                    </table>
                </asp:Panel>

                <!-- Formulario editar producto (oculto por defecto) -->
                <asp:Panel ID="pnlEditarProducto" runat="server" Visible="false" CssClass="panel-form">
                    <asp:HiddenField ID="hfIdProdEdit" runat="server" />
                    <b>Editando producto ID: </b><asp:Label ID="lblIdProdEdit" runat="server" /><br /><br />
                    <table class="form-inline">
                        <tr>
                            <td>Nombre:</td>
                            <td><asp:TextBox ID="txtEditNombreP" runat="server" MaxLength="100" Width="180px" /></td>
                            <td>Precio ($):</td>
                            <td><asp:TextBox ID="txtEditPrecioP" runat="server" MaxLength="10"  Width="80px" /></td>
                            <td>Categoria:</td>
                            <td>
                                <asp:DropDownList ID="ddlEditCatP" runat="server">
                                    <asp:ListItem>Perros</asp:ListItem>
                                    <asp:ListItem>Gatos</asp:ListItem>
                                    <asp:ListItem>Juguetes</asp:ListItem>
                                    <asp:ListItem>Accesorios</asp:ListItem>
                                    <asp:ListItem>Salud</asp:ListItem>
                                </asp:DropDownList>
                            </td>
                        </tr>
                        <tr>
                            <td>Descripcion:</td>
                            <td colspan="5"><asp:TextBox ID="txtEditDescP" runat="server" MaxLength="250" Width="400px" /></td>
                        </tr>
                        <tr>
                            <td colspan="6">
                                <asp:Button ID="btnGuardarProducto"  runat="server" Text="Guardar cambios" CssClass="btn-agregar" OnClick="btnGuardarProducto_Click" />
                                <asp:Button ID="btnCancelarEditProd" runat="server" Text="Cancelar"        CssClass="btn"         OnClick="btnCancelarEditProd_Click" />
                                <small>&nbsp; El HashVerificador se recalcula automaticamente al guardar.</small>
                            </td>
                        </tr>
                    </table>
                </asp:Panel>

                <!-- Grilla de productos -->
                <asp:GridView ID="gvProductos" runat="server"
                    AutoGenerateColumns="false" CssClass="tabla"
                    OnRowCommand="gvProductos_RowCommand"
                    OnRowDataBound="gvProductos_RowDataBound">
                    <Columns>
                        <asp:BoundField DataField="IdProducto"  HeaderText="ID"          ItemStyle-Width="40px" />
                        <asp:BoundField DataField="Nombre"      HeaderText="Nombre" />
                        <asp:BoundField DataField="Descripcion" HeaderText="Descripcion" />
                        <asp:BoundField DataField="Precio"      HeaderText="Precio"      DataFormatString="${0:N2}" ItemStyle-Width="80px" />
                        <asp:BoundField DataField="Categoria"   HeaderText="Categoria"   ItemStyle-Width="90px" />
                        <asp:TemplateField HeaderText="Activo" ItemStyle-Width="50px">
                            <ItemTemplate>
                                <asp:Label runat="server"
                                    Text='<%# DataBinder.Eval(Container.DataItem,"Activo").ToString()=="True" ? "Si" : "No" %>'
                                    CssClass='<%# DataBinder.Eval(Container.DataItem,"Activo").ToString()=="True" ? "activo" : "inactivo" %>' />
                            </ItemTemplate>
                        </asp:TemplateField>
                        <asp:TemplateField HeaderText="Acciones" ItemStyle-Width="150px">
                            <ItemTemplate>
                                <asp:Panel ID="pnlAccionesProd" runat="server">
                                    <asp:LinkButton CommandName="EditarProd"
                                        CommandArgument='<%# Eval("IdProducto") %>'
                                        CssClass="btn-editar">Editar</asp:LinkButton>
                                    &nbsp;
                                    <asp:LinkButton CommandName="DesactivarProd"
                                        CommandArgument='<%# Eval("IdProducto") %>'
                                        CssClass="btn-danger"
                                        OnClientClick="return confirm('Desactivar este producto?')">Desactivar</asp:LinkButton>
                                </asp:Panel>
                            </ItemTemplate>
                        </asp:TemplateField>
                    </Columns>
                </asp:GridView>

                <hr />
                <!-- ===== SECCION C: BITACORA ===== -->
                <h4>c) Bitacora del sistema</h4>
                <p>
                    Filtrar por usuario:&nbsp;
                    <asp:TextBox ID="txtFiltroBit" runat="server" MaxLength="50" Width="150px" />
                    &nbsp;
                    <asp:Button ID="btnFiltrarBit"  runat="server" Text="Filtrar"  CssClass="btn" OnClick="btnFiltrarBit_Click" />
                    <asp:Button ID="btnVerTodoBit"  runat="server" Text="Ver todo" CssClass="btn" OnClick="btnVerTodoBit_Click" />
                </p>

                <asp:GridView ID="gvBitacora" runat="server"
                    AutoGenerateColumns="false" CssClass="tabla">
                    <Columns>
                        <asp:BoundField DataField="FechaHora"     HeaderText="Fecha/Hora"
                            DataFormatString="{0:dd/MM/yyyy HH:mm:ss}" ItemStyle-Width="140px" />
                        <asp:BoundField DataField="NombreUsuario" HeaderText="Usuario"  ItemStyle-Width="80px" />
                        <asp:BoundField DataField="Accion"        HeaderText="Accion"   ItemStyle-Width="120px" />
                        <asp:BoundField DataField="Detalle"       HeaderText="Detalle" />
                    </Columns>
                </asp:GridView>

            </div>
        </asp:Panel>

    </form>
</body>
</html>
