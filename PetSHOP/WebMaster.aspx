<%@ Page Language="C#" AutoEventWireup="true" CodeFile="WebMaster.aspx.cs" Inherits="WebMaster" %>
<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <meta http-equiv="Content-Type" content="text/html; charset=utf-8" />
    <title>PetShop - Panel WebMaster</title>
    <style>
        body { font-family: Arial, sans-serif; background-color: #f0f0f0; margin: 0; padding: 0; }
        #cabecera { background-color: #4a235a; color: white; padding: 10px 15px; }
        #cabecera a { color: white; margin-left: 10px; }
        #contenido { width: 900px; margin: 15px auto; background-color: white; border: 1px solid #ccc; padding: 15px; }
        .tabla { width: 100%; border-collapse: collapse; margin-top: 8px; }
        .tabla th { background-color: #4a235a; color: white; padding: 7px; border: 1px solid #ccc; text-align: left; }
        .tabla td { border: 1px solid #ccc; padding: 7px; }
        .tabla tr:nth-child(even) { background-color: #f5f5f5; }
        .btn         { background-color: #4a235a; color: white; border: none; padding: 6px 16px; cursor: pointer; margin: 3px; }
        .btn-danger  { background-color: #cc0000; color: white; border: none; padding: 6px 16px; cursor: pointer; margin: 3px; }
        .ok       { color: green; font-weight: bold; }
        .alterado { color: red;   font-weight: bold; }
        .msg     { font-weight: bold; margin: 6px 0; }
        .msg-ok  { color: green; }
        .msg-err { color: red; }
        .denegado { color: red; font-size: 18px; font-weight: bold; margin: 30px; }
        .panel-corrupto { background: #ffebee; border: 2px solid #cc0000; padding: 15px; margin: 10px 0; border-radius: 4px; }
        .panel-ok       { background: #e8f5e9; border: 2px solid #2e7d32; padding: 10px;  margin: 10px 0; border-radius: 4px; }
        .panel-error    { background: #fff8e1; border: 2px solid #f57f17; padding: 12px;  margin: 10px 0; border-radius: 4px; }
    </style>
</head>
<body>
    <form id="form1" runat="server">

        <div id="cabecera">
            <strong>PetShop - Panel WebMaster</strong>
            &nbsp;|&nbsp;
            <a href="Menu.aspx">Volver al catalogo</a>
            &nbsp;|&nbsp;
            <a href="Admin.aspx">Panel Admin (solo lectura)</a>
            &nbsp;|&nbsp;
            Usuario: <asp:Label ID="lblWMUser" runat="server" />
        </div>

        <!-- Acceso denegado -->
        <asp:Panel ID="pnlDenegado" runat="server" Visible="false">
            <p class="denegado">No esta disponible el sistema para su usuario.</p>
            <p style="margin-left:30px"><a href="Menu.aspx">Volver al catalogo</a></p>
        </asp:Panel>

        <asp:Panel ID="pnlContenido" runat="server" Visible="false">
            <div id="contenido">
                <h3>Panel WebMaster</h3>

                <!-- Cartel de BD corrupta (aparece automaticamente si hay alteraciones) -->
                <asp:Panel ID="pnlCorrupto" runat="server" Visible="false">
                    <div class="panel-corrupto">
                        <h3 style="color:#cc0000; margin:0 0 8px 0;">&#9888; Base de datos corrupta!</h3>
                        <p style="margin:0 0 10px 0;">Se detectaron alteraciones en los datos de productos.
                           Elija una accion para corregirlo:</p>
                        <asp:Button ID="btnRecalcularHashes" runat="server"
                            Text="Recalcular digitos verificadores"
                            CssClass="btn" OnClick="btnRecalcularHashes_Click" />
                        &nbsp;
                        <asp:Button ID="btnRestaurarBD" runat="server"
                            Text="Restaurar BD desde backup"
                            CssClass="btn-danger" OnClick="btnRestaurarBD_Click"
                            OnClientClick="return confirm('Restaurar la BD desde el ultimo backup disponible?')" />
                    </div>
                </asp:Panel>

                <!-- Cartel de BD integra -->
                <asp:Panel ID="pnlEstadoOK" runat="server" Visible="false">
                    <div class="panel-ok">
                        <strong style="color:#2e7d32;">&#10003; Base de datos integra. Todos los productos estan OK.</strong>
                    </div>
                </asp:Panel>

                <!-- Cartel de error en la verificacion (error SQL, columna faltante, etc.) -->
                <asp:Panel ID="pnlErrorVerificacion" runat="server" Visible="false">
                    <div class="panel-error">
                        <strong style="color:#e65100;">&#9888; No se pudo verificar la integridad de la base de datos.</strong><br />
                        <asp:Label ID="lblErrorVerificacion" runat="server" style="font-size:13px;" /><br /><br />
                        <small>Posibles causas: la columna <em>HashVerificador</em> no existe en la tabla Productos,
                        o hay un problema de conexion. Revise la estructura de la BD o use los botones de abajo
                        para recalcular los digitos verificadores.</small><br /><br />
                        <asp:Button ID="btnRecalcularHashesErr" runat="server"
                            Text="Recalcular digitos verificadores"
                            CssClass="btn" OnClick="btnRecalcularHashes_Click" />
                        &nbsp;
                        <asp:Button ID="btnRestaurarBDErr" runat="server"
                            Text="Restaurar BD desde backup"
                            CssClass="btn-danger" OnClick="btnRestaurarBD_Click"
                            OnClientClick="return confirm('Restaurar la BD desde el ultimo backup disponible?')" />
                    </div>
                </asp:Panel>

                <!-- Mensaje de operaciones -->
                <asp:Label ID="lblMensaje" runat="server" CssClass="msg" Visible="false" />

                <hr />

                <!-- SECCION B: Detalle de verificacion de integridad -->
                <h4>b) Verificacion de integridad (Digitos Verificadores)</h4>
                <p>El sistema verifica automaticamente al ingresar. Resultados:</p>

                <asp:GridView ID="gvIntegridad" runat="server"
                    AutoGenerateColumns="false" CssClass="tabla" Visible="false"
                    OnRowDataBound="gvIntegridad_RowDataBound">
                    <Columns>
                        <asp:BoundField DataField="Id"        HeaderText="ID"        ItemStyle-Width="40px" />
                        <asp:BoundField DataField="Nombre"    HeaderText="Producto" />
                        <asp:BoundField DataField="Categoria" HeaderText="Categoria" ItemStyle-Width="90px" />
                        <asp:BoundField DataField="Precio"    HeaderText="Precio"    DataFormatString="${0:N2}" ItemStyle-Width="80px" />
                        <asp:BoundField DataField="Estado"    HeaderText="Estado"    ItemStyle-Width="90px" />
                    </Columns>
                </asp:GridView>

                <hr />

                <!-- SECCION C: Backup -->
                <h4>c) Backup de datos</h4>
                <p>Guarda todas las tablas en <em>App_Data/backup.json</em>.
                   Hacerlo periodicamente permite restaurar si la BD es alterada externamente.</p>
                <asp:Button ID="btnBackup" runat="server" Text="Hacer Backup ahora"
                    CssClass="btn" OnClick="btnBackup_Click" />
                <br /><br />
                <asp:Label ID="lblBackupMsg" runat="server" CssClass="msg" Visible="false" />

            </div>
        </asp:Panel>

    </form>
</body>
</html>
