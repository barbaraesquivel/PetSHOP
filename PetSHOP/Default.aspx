<%@ Page Language="C#" AutoEventWireup="true" CodeFile="Default.aspx.cs" Inherits="_Default" %>
<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <meta http-equiv="Content-Type" content="text/html; charset=utf-8" />
    <title>PetShop - Login</title>
    <style>
        body {
            font-family: Arial, sans-serif;
            background-color: #f0f0f0;
        }
        .caja-login {
            width: 380px;
            margin: 80px auto;
            background-color: white;
            border: 1px solid #cccccc;
            padding: 20px;
        }
        .caja-login h2 {
            color: #336699;
            margin-top: 0;
        }
        .caja-login table {
            width: 100%;
        }
        .caja-login td {
            padding: 5px;
        }
        .caja-login input[type=text],
        .caja-login input[type=password] {
            width: 200px;
            padding: 4px;
            border: 1px solid #aaaaaa;
        }
        .btn-login {
            background-color: #336699;
            color: white;
            border: none;
            padding: 6px 20px;
            cursor: pointer;
        }
        .btn-registro {
            background-color: #2e7d32;
            color: white;
            border: none;
            padding: 6px 20px;
            cursor: pointer;
        }
        .error { color: red;   font-size: 13px; }
        .ok    { color: green; font-size: 13px; font-weight: bold; }
        .ayuda { color: gray;  font-size: 12px; margin-top: 10px; }
        .link-cambio { font-size: 12px; color: #336699; cursor: pointer;
                       background: none; border: none; padding: 0; text-decoration: underline; }
        hr { border: none; border-top: 1px solid #eee; margin: 12px 0; }
    </style>
</head>
<body>
    <form id="form1" runat="server">
        <div class="caja-login">

            <!-- ===== PANEL LOGIN ===== -->
            <asp:Panel ID="pnlLogin" runat="server">
                <h2>PetShop</h2>
                <hr />
                <p>Por favor ingrese sus datos:</p>

                <table>
                    <tr>
                        <td>Usuario:</td>
                        <td><asp:TextBox ID="txtUsuario" runat="server" MaxLength="50" /></td>
                    </tr>
                    <tr>
                        <td>Contrase&ntilde;a:</td>
                        <td><asp:TextBox ID="txtContrasena" runat="server" TextMode="Password" MaxLength="100" /></td>
                    </tr>
                    <tr>
                        <td>&nbsp;</td>
                        <td>
                            <asp:Button ID="btnIngresar" runat="server" Text="Ingresar"
                                CssClass="btn-login" OnClick="btnIngresar_Click" />
                        </td>
                    </tr>
                </table>

                <asp:Label ID="lblError" runat="server" CssClass="error" Visible="false" />

                <hr />
                <p style="margin:6px 0; font-size:13px;">
                   ¿No tenes cuenta?&nbsp;
                    <asp:LinkButton ID="btnIrRegistro" runat="server"
                        CssClass="link-cambio" OnClick="btnIrRegistro_Click">Registrarse</asp:LinkButton>
                </p>

                <p class="ayuda">
                    Usuarios de prueba:<br />
                    admin / admin123 (Admin)<br />
                    user / user123 (Usuario)<br />
                    webmaster / web123 (WebMaster)
                </p>
            </asp:Panel>

            <!-- ===== PANEL REGISTRO ===== -->
            <asp:Panel ID="pnlRegistro" runat="server" Visible="false">
                <h2>PetShop &mdash; Registro</h2>
                <hr />
                <p>Crea tu cuenta de usuario:</p>

                <table>
                    <tr>
                        <td>Usuario:</td>
                        <td><asp:TextBox ID="txtRegUsuario" runat="server" MaxLength="50" /></td>
                    </tr>
                    <tr>
                        <td>Nombre:</td>
                        <td><asp:TextBox ID="txtRegNombre" runat="server" MaxLength="100" /></td>
                    </tr>
                    <tr>
                        <td>Apellido:</td>
                        <td><asp:TextBox ID="txtRegApellido" runat="server" MaxLength="100" /></td>
                    </tr>
                    <tr>
                        <td>Email:</td>
                        <td><asp:TextBox ID="txtRegEmail" runat="server" MaxLength="200" /></td>
                    </tr>
                    <tr>
                        <td>Telefono:</td>
                        <td><asp:TextBox ID="txtRegTelefono" runat="server" MaxLength="50" /></td>
                    </tr>
                    <tr>
                        <td>Direccion:</td>
                        <td><asp:TextBox ID="txtRegDireccion" runat="server" MaxLength="300" /></td>
                    </tr>
                    <tr>
                        <td>Contrase&ntilde;a:</td>
                        <td><asp:TextBox ID="txtRegPass" runat="server" TextMode="Password" MaxLength="100" /></td>
                    </tr>
                    <tr>
                        <td>Confirmar:</td>
                        <td><asp:TextBox ID="txtRegConfirm" runat="server" TextMode="Password" MaxLength="100" /></td>
                    </tr>
                    <tr>
                        <td>&nbsp;</td>
                        <td>
                            <asp:Button ID="btnRegistrar" runat="server" Text="Crear cuenta"
                                CssClass="btn-registro" OnClick="btnRegistrar_Click" />
                        </td>
                    </tr>
                </table>

                <asp:Label ID="lblRegMensaje" runat="server" Visible="false" />

                <hr />
                <p style="margin:6px 0; font-size:13px;">
                   Ya tenes cuenta?&nbsp;
                    <asp:LinkButton ID="btnIrLogin" runat="server"
                        CssClass="link-cambio" OnClick="btnIrLogin_Click">Iniciar sesion</asp:LinkButton>
                </p>
            </asp:Panel>

        </div>
    </form>
</body>
</html>
