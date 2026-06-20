<%@ Page Language="C#" AutoEventWireup="true" CodeFile="MiPerfil.aspx.cs" Inherits="MiPerfil" %>
<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <meta http-equiv="Content-Type" content="text/html; charset=utf-8" />
    <title>PetShop - Mi Perfil</title>
    <style>
        body { font-family: Arial, sans-serif; background-color: #f0f0f0; margin: 0; padding: 0; }
        #cabecera { background-color: #336699; color: white; padding: 10px 15px; }
        #cabecera a { color: white; margin-left: 15px; }
        #contenido { width: 500px; margin: 30px auto; background: white; border: 1px solid #ccc; padding: 20px; }
        .form-tabla td { padding: 7px 10px; vertical-align: middle; }
        .form-tabla td:first-child { font-weight: bold; width: 120px; }
        input[type=text], input[type=email] { width: 250px; padding: 5px; border: 1px solid #aaa; }
        .btn-guardar { background-color: #336699; color: white; border: none; padding: 7px 20px; cursor: pointer; }
        .btn-volver  { background-color: #888; color: white; border: none; padding: 7px 20px; cursor: pointer; margin-left: 8px; }
        .msg    { font-weight: bold; padding: 8px; margin: 8px 0; }
        .msg-ok  { color: green; background: #e8f5e9; border: 1px solid #a5d6a7; }
        .msg-err { color: red;   background: #ffebee; border: 1px solid #ef9a9a; }
        .aviso { background: #fff3e0; border: 1px solid #ff9800; padding: 8px; margin-bottom: 12px; color: #e65100; }
    </style>
</head>
<body>
    <form id="form1" runat="server">

        <div id="cabecera">
            <strong>PetShop</strong>
            &nbsp;&nbsp;|&nbsp;&nbsp;
            <a href="Menu.aspx">Volver al catalogo</a>
            &nbsp;&nbsp;|&nbsp;&nbsp;
            <a href="Carrito.aspx">Mi carrito</a>
        </div>

        <div id="contenido">
            <h3>Mi Perfil</h3>

            <asp:Panel ID="pnlAviso" runat="server" Visible="false" CssClass="aviso">
                Completa tus datos para poder confirmar pedidos.
            </asp:Panel>

            <asp:Label ID="lblMensaje" runat="server" CssClass="msg" Visible="false" />

            <table class="form-tabla">
                <tr>
                    <td>Usuario:</td>
                    <td><asp:Label ID="lblNombreUsuario" runat="server" /></td>
                </tr>
                <tr>
                    <td>Nombre:</td>
                    <td><asp:TextBox ID="txtNombre"   runat="server" MaxLength="100" /></td>
                </tr>
                <tr>
                    <td>Apellido:</td>
                    <td><asp:TextBox ID="txtApellido" runat="server" MaxLength="100" /></td>
                </tr>
                <tr>
                    <td>Email:</td>
                    <td><asp:TextBox ID="txtEmail"    runat="server" MaxLength="200" TextMode="Email" /></td>
                </tr>
                <tr>
                    <td>Telefono:</td>
                    <td><asp:TextBox ID="txtTelefono" runat="server" MaxLength="50" /></td>
                </tr>
                <tr>
                    <td>Direccion:</td>
                    <td><asp:TextBox ID="txtDireccion" runat="server" MaxLength="300" /></td>
                </tr>
                <tr>
                    <td></td>
                    <td style="padding-top:12px;">
                        <asp:Button ID="btnGuardar" runat="server" Text="Guardar datos"
                            CssClass="btn-guardar" OnClick="btnGuardar_Click" />
                        <asp:Button ID="btnVolver" runat="server" Text="Cancelar"
                            CssClass="btn-volver" OnClick="btnVolver_Click" CausesValidation="false" />
                    </td>
                </tr>
            </table>
        </div>

    </form>
</body>
</html>
