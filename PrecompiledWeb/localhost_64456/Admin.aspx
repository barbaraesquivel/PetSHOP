<%@ page language="C#" autoeventwireup="true" inherits="Admin, App_Web_hagjagd0" %>
<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <meta http-equiv="Content-Type" content="text/html; charset=utf-8" />
    <title>PetShop - Panel Admin</title>
    <style>
        body { font-family: Arial, sans-serif; background-color: #f0f0f0; margin: 0; padding: 0; }
        #cabecera { background-color: #336699; color: white; padding: 10px 15px; }
        #cabecera a { color: white; margin-left: 10px; }
        #contenido { width: 980px; margin: 15px auto; background: white; border: 1px solid #ccc; padding: 15px; }
        .tabla { width: 100%; border-collapse: collapse; margin-top: 8px; }
        .tabla th { background-color: #336699; color: white; padding: 7px; border: 1px solid #ccc; text-align: left; }
        .tabla td { border: 1px solid #ccc; padding: 7px; font-size: 13px; }
        .tabla tr:nth-child(even) { background-color: #f5f5f5; }
        .tabla tr[data-fila]:hover { background-color: #e3f2fd; cursor: pointer; }
        .form-inline td { padding: 5px 8px; }
        .btn         { background-color: #336699; color: white; border: none; padding: 5px 12px; cursor: pointer; }
        .btn-agregar { background-color: #2e7d32; color: white; border: none; padding: 5px 12px; cursor: pointer; }
        .btn-editar  { background-color: #e65100; color: white; border: none; padding: 5px 12px; cursor: pointer; }
        .btn-danger  { background-color: #cc0000; color: white; border: none; padding: 5px 12px; cursor: pointer; }
        .msg { font-weight: bold; margin: 6px 0; padding: 6px; }
        .msg-ok  { color: green; background: #e8f5e9; border: 1px solid #a5d6a7; }
        .msg-err { color: red;   background: #ffebee; border: 1px solid #ef9a9a; }
        .modo-lectura { color: #e65100; font-weight: bold; }
        .denegado { color: red; font-size: 18px; font-weight: bold; margin: 30px; }
        .panel-form { background: #f9f9f9; border: 1px solid #ddd; padding: 12px; margin: 8px 0; }
        .inactivo { color: #cc0000; }
        .activo   { color: #2e7d32; }
        .hint-seleccion { font-size: 12px; color: #777; margin: 4px 0 0 0; }
        .fila-seleccionada { background-color: #cce5ff !important; font-weight: bold; }
        input[type=text], input[type=password], select { padding: 4px; border: 1px solid #aaa; }
    </style>
    <script type="text/javascript">
        function seleccionarFila(fila, id, hfId) {
            // Quitar seleccion anterior en la misma grilla
            var grilla = fila.parentNode;
            while (grilla && grilla.tagName !== 'TABLE') grilla = grilla.parentNode;
            if (grilla) {
                var filas = grilla.querySelectorAll('tr');
                for (var i = 0; i < filas.length; i++)
                    filas[i].classList.remove('fila-seleccionada');
            }
            fila.classList.add('fila-seleccionada');
            document.getElementById(hfId).value = id;
        }
    </script>
</head>
<body>
    <form id="form1" runat="server">

        <asp:HiddenField ID="hfSelectedUserId" runat="server" Value="0" />
        <asp:HiddenField ID="hfSelectedProdId" runat="server" Value="0" />

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

                <!-- Formulario agregar + botones de accion sobre seleccionado -->
                <asp:Panel ID="pnlFormUsuario" runat="server" CssClass="panel-form">
                    <table class="form-inline">
                        <tr>
                            <td><b>Agregar usuario:</b></td>
                            <td>Nombre:</td>
                            <td><asp:TextBox ID="txtNombreU" runat="server" MaxLength="50" Width="120px" /></td>
                            <td>Contrasena:</td>
                            <td><asp:TextBox ID="txtPassU" runat="server" TextMode="Password" MaxLength="100" Width="120px" /></td>
                            <td>Rol:</td>
                            <td>
                                <asp:DropDownList ID="ddlRolU" runat="server">
                                    <asp:ListItem Value="Usuario">Usuario</asp:ListItem>
                                    <asp:ListItem Value="Admin">Admin</asp:ListItem>
                                    <asp:ListItem Value="WebMaster">WebMaster</asp:ListItem>
                                </asp:DropDownList>
                            </td>
                            <td>
                                <asp:Button ID="btnAgregarUsuario" runat="server" Text="Agregar"
                                    CssClass="btn-agregar" OnClick="btnAgregarUsuario_Click" />
                            </td>
                            <td style="padding-left:20px; border-left:1px solid #ccc;">
                                <asp:Button ID="btnEditarUsuario" runat="server" Text="Editar"
                                    CssClass="btn-editar" OnClick="btnEditarUsuario_Click" />
                                &nbsp;
                                <asp:Button ID="btnEliminarUsuario" runat="server" Text="Eliminar"
                                    CssClass="btn-danger" OnClick="btnEliminarUsuario_Click"
                                    OnClientClick="return confirm('Eliminar el usuario seleccionado?')" />
                            </td>
                        </tr>
                    </table>
                </asp:Panel>

                <!-- Formulario editar usuario -->
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
                                &nbsp;
                                <asp:Button ID="btnCancelarEditUser" runat="server" Text="Cancelar" CssClass="btn"         OnClick="btnCancelarEditUser_Click" />
                            </td>
                        </tr>
                    </table>
                </asp:Panel>

                <p class="hint-seleccion">&#8593; Haga clic en una fila para seleccionarla, luego use Editar o Eliminar.</p>

                <!-- Grilla de usuarios — sin columna Acciones, filas clickeables -->
                <asp:GridView ID="gvUsuarios" runat="server"
                    AutoGenerateColumns="false" CssClass="tabla"
                    DataKeyNames="IdUsuario"
                    OnRowDataBound="gvUsuarios_RowDataBound">
                    <Columns>
                        <asp:BoundField DataField="IdUsuario"     HeaderText="ID"      ItemStyle-Width="40px" />
                        <asp:BoundField DataField="NombreUsuario" HeaderText="Usuario" ItemStyle-Width="150px" />
                        <asp:BoundField DataField="Rol"           HeaderText="Rol" />
                    </Columns>
                </asp:GridView>

                <hr />
                <!-- ===== SECCION B: PRODUCTOS ===== -->
                <h4>b) Gestion de Productos y Precios</h4>

                <!-- Formulario agregar + botones de accion sobre seleccionado -->
                <asp:Panel ID="pnlFormProducto" runat="server" CssClass="panel-form">
                    <table class="form-inline">
                        <tr>
                            <td><b>Agregar producto:</b></td>
                            <td>Nombre:</td>
                            <td><asp:TextBox ID="txtNombreP" runat="server" MaxLength="100" Width="140px" /></td>
                            <td>Precio:</td>
                            <td><asp:TextBox ID="txtPrecioP" runat="server" MaxLength="10" Width="70px" /></td>
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
                            <td></td>
                            <td>Descripcion:</td>
                            <td colspan="4"><asp:TextBox ID="txtDescP" runat="server" MaxLength="250" Width="360px" /></td>
                            <td>
                                <asp:Button ID="btnAgregarProducto" runat="server" Text="Agregar"
                                    CssClass="btn-agregar" OnClick="btnAgregarProducto_Click" />
                            </td>
                        </tr>
                        <tr>
                            <td colspan="6" style="padding-top:8px; border-top:1px solid #ddd;">
                                <asp:Button ID="btnEditarProducto" runat="server" Text="Editar seleccionado"
                                    CssClass="btn-editar" OnClick="btnEditarProducto_Click" />
                                &nbsp;
                                <asp:Button ID="btnEliminarProducto" runat="server" Text="Desactivar seleccionado"
                                    CssClass="btn-danger" OnClick="btnEliminarProducto_Click"
                                    OnClientClick="return confirm('Desactivar el producto seleccionado?')" />
                            </td>
                        </tr>
                    </table>
                </asp:Panel>

                <!-- Formulario editar producto -->
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
                                &nbsp;
                                <asp:Button ID="btnCancelarEditProd" runat="server" Text="Cancelar"        CssClass="btn"         OnClick="btnCancelarEditProd_Click" />
                                <small>&nbsp; El HashVerificador se recalcula al guardar.</small>
                            </td>
                        </tr>
                    </table>
                </asp:Panel>

                <p class="hint-seleccion">&#8593; Haga clic en una fila para seleccionarla, luego use Editar o Desactivar.</p>

                <!-- Grilla de productos — sin columna Acciones, filas clickeables -->
                <asp:GridView ID="gvProductos" runat="server"
                    AutoGenerateColumns="false" CssClass="tabla"
                    DataKeyNames="IdProducto"
                    OnRowDataBound="gvProductos_RowDataBound">
                    <Columns>
                        <asp:BoundField DataField="IdProducto"  HeaderText="ID"          ItemStyle-Width="40px" />
                        <asp:BoundField DataField="Nombre"      HeaderText="Nombre"       ItemStyle-Width="160px" />
                        <asp:BoundField DataField="Descripcion" HeaderText="Descripcion" />
                        <asp:BoundField DataField="Precio"      HeaderText="Precio"       DataFormatString="${0:N2}" ItemStyle-Width="80px" />
                        <asp:BoundField DataField="Categoria"   HeaderText="Categoria"    ItemStyle-Width="90px" />
                        <asp:TemplateField HeaderText="Activo" ItemStyle-Width="50px">
                            <ItemTemplate>
                                <asp:Label runat="server"
                                    Text='<%# DataBinder.Eval(Container.DataItem,"Activo").ToString()=="True" ? "Si" : "No" %>'
                                    CssClass='<%# DataBinder.Eval(Container.DataItem,"Activo").ToString()=="True" ? "activo" : "inactivo" %>' />
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
                    <asp:Button ID="btnFiltrarBit" runat="server" Text="Filtrar"  CssClass="btn" OnClick="btnFiltrarBit_Click" />
                    <asp:Button ID="btnVerTodoBit" runat="server" Text="Ver todo" CssClass="btn" OnClick="btnVerTodoBit_Click" />
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
