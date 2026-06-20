<%@ Page Language="C#" AutoEventWireup="true" CodeFile="WebMaster.aspx.cs" Inherits="WebMaster" %>
<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <meta http-equiv="Content-Type" content="text/html; charset=utf-8" />
    <title>PetShop - Panel WebMaster</title>
    <style>
        body  { font-family: Arial, sans-serif; background-color: #f0f0f0; margin: 0; padding: 0; }
        #cabecera { background-color: #4a235a; color: white; padding: 10px 15px; }
        #contenido { width: 940px; margin: 15px auto; background-color: white; border: 1px solid #ccc; padding: 15px; }
        .tabla    { width: 100%; border-collapse: collapse; margin-top: 8px; }
        .tabla th { background-color: #4a235a; color: white; padding: 7px; border: 1px solid #ccc; text-align: left; }
        .tabla td { border: 1px solid #ccc; padding: 7px; vertical-align: middle; }
        .tabla tr:nth-child(even) { background-color: #f5f5f5; }
        .btn        { background-color: #4a235a; color: white; border: none; padding: 6px 16px; cursor: pointer; margin: 3px; }
        .btn-danger { background-color: #cc0000; color: white; border: none; padding: 6px 14px; cursor: pointer; margin: 2px; }
        .btn-sm     { background-color: #4a235a; color: white; border: none; padding: 3px 10px; cursor: pointer; font-size: 12px; }
        .ok       { color: green; font-weight: bold; }
        .alterado { color: red;   font-weight: bold; }
        .msg      { font-weight: bold; margin: 6px 0; display: inline-block; }
        .msg-ok   { color: green; }
        .msg-err  { color: red; }
        .denegado { color: red; font-size: 18px; font-weight: bold; margin: 30px; }
        .panel-corrupto  { background: #ffebee; border: 2px solid #cc0000; padding: 15px; margin: 10px 0; border-radius: 4px; }
        .panel-bloqueado { background: #fff3e0; border: 2px solid #e65100; padding: 15px; margin: 10px 0; border-radius: 4px; }
        .panel-ok        { background: #e8f5e9; border: 2px solid #2e7d32; padding: 10px;  margin: 10px 0; border-radius: 4px; }
        .panel-error     { background: #fff8e1; border: 2px solid #f57f17; padding: 12px;  margin: 10px 0; border-radius: 4px; }
        .panel-info      { background: #f5f5f5; border: 1px solid #ccc;    padding: 10px;  margin-bottom: 12px; border-radius: 3px; font-size: 13px; }
        .filtros  { background: #fafafa; border: 1px solid #ddd; padding: 10px 12px; border-radius: 3px; margin-bottom: 10px; }
        .filtros label { font-size: 13px; margin-right: 16px; white-space: nowrap; }
        .filtros input[type=text], .filtros input[type=date], .filtros select { padding: 3px 5px; border: 1px solid #bbb; }
        select { padding: 3px 5px; }
        .pager  { margin-top: 8px; }
        .pager table { margin: 0 auto; }
        .pager td a, .pager td span { display: inline-block; padding: 3px 8px; border: 1px solid #bbb; margin: 1px; font-size: 13px; color: #4a235a; text-decoration: none; }
        .pager td span { background: #4a235a; color: white; font-weight: bold; border-color: #4a235a; }
    </style>
</head>
<body>
    <form id="form1" runat="server">

        <div id="cabecera">
            <strong>PetShop - Panel WebMaster</strong>
            &nbsp;|&nbsp;
            Usuario: <asp:Label ID="lblWMUser" runat="server" />
        </div>

        <!-- Acceso denegado -->
        <asp:Panel ID="pnlDenegado" runat="server" Visible="false">
            <p class="denegado">No esta disponible el sistema para su usuario.</p>
            <p style="margin-left:30px"><a href="Menu.aspx">Volver al catalogo</a></p>
        </asp:Panel>

        <!-- ============================================================ -->
        <!-- PANEL BLOQUEADO                                               -->
        <!-- ============================================================ -->
        <asp:Panel ID="pnlBloqueado" runat="server" Visible="false">
            <div id="contenido">

                <div class="panel-bloqueado">
                    <h3 style="color:#e65100; margin:0 0 6px 0;">&#9888; Sistema bloqueado por integridad comprometida</h3>
                    <p style="margin:0;">Se detectaron alteraciones en los datos del catalogo.
                       Revisa el estado y usa una de las acciones disponibles.</p>
                </div>

                <asp:Panel ID="pnlCorruptoBloqueado" runat="server" Visible="false">
                    <div class="panel-corrupto">
                        <strong style="color:#cc0000;">&#9888; Hay productos con datos alterados (ver tabla).</strong>
                    </div>
                </asp:Panel>
                <asp:Panel ID="pnlEstadoOKBloqueado" runat="server" Visible="false">
                    <div class="panel-ok">
                        <strong style="color:#2e7d32;">&#10003; Todos los productos estan OK. Puedes desbloquear el sistema.</strong>
                    </div>
                </asp:Panel>

                <asp:GridView ID="gvIntBloqueado" runat="server"
                    AutoGenerateColumns="false" CssClass="tabla" Visible="false"
                    OnRowDataBound="gvIntBloqueado_RowDataBound">
                    <Columns>
                        <asp:BoundField DataField="Id"        HeaderText="ID"        ItemStyle-Width="40px" />
                        <asp:BoundField DataField="Nombre"    HeaderText="Producto" />
                        <asp:BoundField DataField="Categoria" HeaderText="Categoria" ItemStyle-Width="90px" />
                        <asp:BoundField DataField="Precio"    HeaderText="Precio"    DataFormatString="${0:N2}" ItemStyle-Width="80px" />
                        <asp:BoundField DataField="Estado"    HeaderText="Estado"    ItemStyle-Width="90px" />
                    </Columns>
                </asp:GridView>

                <hr />

                <h4>a) Recalcular digitos verificadores</h4>
                <p style="font-size:13px; margin:0 0 10px 0;">
                    Si todos quedan OK, se genera un backup automatico en <em>App_Data/backups/</em> y el sistema se desbloquea.
                </p>
                <asp:Button ID="btnRecalcularBloqueado" runat="server"
                    Text="Recalcular digitos verificadores"
                    CssClass="btn" OnClick="btnRecalcularBloqueado_Click" />
                <br /><br />
                <asp:Label ID="lblRecalcMsg" runat="server" CssClass="msg" Visible="false" />

                <hr />

                <h4>b) Restaurar desde backup guardado</h4>
                <p style="font-size:13px; margin:0 0 6px 0;">
                    Selecciona un archivo de backup guardado en <em>App_Data/backups/</em>.
                </p>
                <p style="margin:0 0 10px 0; font-size:12px; color:#cc0000; font-weight:bold;">
                    &#9888; Reemplaza TODOS los datos actuales. No se puede deshacer.
                </p>
                <asp:DropDownList ID="ddlBackups" runat="server" />&nbsp;
                <asp:Button ID="btnActualizarLista" runat="server"
                    Text="Actualizar lista" CssClass="btn" OnClick="btnActualizarLista_Click" />
                <br /><br />
                <asp:Button ID="btnRestaurarDesdeArchivo" runat="server"
                    Text="Restaurar backup seleccionado" CssClass="btn-danger"
                    OnClick="btnRestaurarDesdeArchivo_Click"
                    OnClientClick="return confirm('Esto reemplazara TODOS los datos actuales con el backup seleccionado. Confirmar?');" />
                <br /><br />
                <asp:Label ID="lblRestaurarMsg" runat="server" CssClass="msg" Visible="false" />

            </div>
        </asp:Panel>

        <!-- ============================================================ -->
        <!-- PANEL NORMAL                                                  -->
        <!-- ============================================================ -->
        <asp:Panel ID="pnlContenido" runat="server" Visible="false">
            <div id="contenido">
                <h3>Panel WebMaster</h3>

                <asp:Panel ID="pnlCorrupto" runat="server" Visible="false">
                    <div class="panel-corrupto">
                        <h3 style="color:#cc0000; margin:0 0 8px 0;">&#9888; Base de datos corrupta!</h3>
                        <p style="margin:0 0 10px 0;">Se detectaron alteraciones en los datos de productos.</p>
                        <asp:Button ID="btnRecalcularHashes" runat="server"
                            Text="Recalcular digitos verificadores"
                            CssClass="btn" OnClick="btnRecalcularHashes_Click" />
                    </div>
                </asp:Panel>

                <asp:Panel ID="pnlEstadoOK" runat="server" Visible="false">
                    <div class="panel-ok">
                        <strong style="color:#2e7d32;">&#10003; Base de datos integra. Todos los productos estan OK.</strong>
                    </div>
                </asp:Panel>

                <asp:Panel ID="pnlErrorVerificacion" runat="server" Visible="false">
                    <div class="panel-error">
                        <strong style="color:#e65100;">&#9888; No se pudo verificar la integridad de la base de datos.</strong><br />
                        <asp:Label ID="lblErrorVerificacion" runat="server" style="font-size:13px;" /><br /><br />
                        <asp:Button ID="btnRecalcularHashesErr" runat="server"
                            Text="Recalcular digitos verificadores"
                            CssClass="btn" OnClick="btnRecalcularHashes_Click" />
                    </div>
                </asp:Panel>

                <asp:Label ID="lblMensaje" runat="server" CssClass="msg" Visible="false" />

                <hr />
                <p>
                    <a href="Menu.aspx">Volver al catalogo</a>
                    &nbsp;|&nbsp;
                    <a href="Admin.aspx">Panel Admin (solo lectura)</a>
                </p>
                <hr />

                <!-- -------------------------------------------------- -->
                <!-- b) Verificacion de integridad                        -->
                <!-- -------------------------------------------------- -->
                <h4>b) Verificacion de integridad (Digitos Verificadores)</h4>
                <p style="font-size:13px;">El sistema verifica automaticamente al ingresar. Resultados:</p>

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

                <!-- -------------------------------------------------- -->
                <!-- c) Gestion de Base de Datos                          -->
                <!-- -------------------------------------------------- -->
                <h4>c) Gestion de Base de Datos</h4>

                <div class="panel-info">
                    <asp:Label ID="lblInfoBackup" runat="server" />
                </div>

                <asp:Button ID="btnGenerarBackup" runat="server"
                    Text="Generar backup ahora"
                    CssClass="btn" OnClick="btnGenerarBackup_Click" />
                &nbsp;<asp:Label ID="lblGenBackupMsg" runat="server" CssClass="msg" Visible="false" />
                <br /><br />

                <h5 style="margin:0 0 6px 0;">Backups disponibles</h5>
                <asp:GridView ID="gvBackups" runat="server"
                    AutoGenerateColumns="false" CssClass="tabla" Visible="false"
                    OnRowCommand="gvBackups_RowCommand">
                    <Columns>
                        <asp:BoundField DataField="NombreArchivo" HeaderText="Archivo" />
                        <asp:BoundField DataField="FechaHora"     HeaderText="Fecha"
                            DataFormatString="{0:dd/MM/yyyy HH:mm}" ItemStyle-Width="130px" />
                        <asp:BoundField DataField="TamanioTexto"  HeaderText="Tamano" ItemStyle-Width="70px" />
                        <asp:TemplateField HeaderText="Accion" ItemStyle-Width="90px" ItemStyle-HorizontalAlign="Center">
                            <ItemTemplate>
                                <asp:Button runat="server" CommandName="Restaurar"
                                    CommandArgument='<%# Eval("NombreArchivo") %>'
                                    Text="Restaurar" CssClass="btn-danger"
                                    OnClientClick="return confirm('Esto reemplazara TODOS los datos actuales. Confirmar?');" />
                            </ItemTemplate>
                        </asp:TemplateField>
                    </Columns>
                </asp:GridView>
                <asp:Label ID="lblNoBackups" runat="server" Visible="false"
                    Text="No hay backups disponibles." style="color:#888; font-size:13px;" />

                <hr />

                <h5 style="margin:0 0 6px 0;">Subir backup externo</h5>
                <p style="margin:0 0 6px 0; font-size:13px;">
                    Selecciona un archivo <em>.sql</em> generado por este sistema para restaurar desde fuera del servidor.
                </p>
                <p style="margin:0 0 10px 0; font-size:12px; color:#cc0000; font-weight:bold;">
                    &#9888; Reemplaza TODOS los datos actuales. No se puede deshacer.
                </p>
                <asp:FileUpload ID="fuRestore" runat="server" /><br /><br />
                <asp:Button ID="btnRestaurar" runat="server"
                    Text="Restaurar desde .sql externo" CssClass="btn-danger"
                    OnClick="btnRestaurar_Click"
                    OnClientClick="return confirm('Esto reemplazara TODOS los datos actuales con el archivo seleccionado. Confirmar?');" />
                <br /><br />
                <asp:Label ID="lblRestoreMsg" runat="server" CssClass="msg" Visible="false" />

                <hr />

                <!-- -------------------------------------------------- -->
                <!-- d) Registros de Bitacora                             -->
                <!-- -------------------------------------------------- -->
                <h4>d) Registros de Bitacora</h4>

                <div class="filtros">
                    <label>Desde:&nbsp;<asp:TextBox ID="txtDesde" runat="server"
                        TextMode="Date" style="padding:3px 5px; border:1px solid #bbb;" /></label>
                    <label>Hasta:&nbsp;<asp:TextBox ID="txtHasta" runat="server"
                        TextMode="Date" style="padding:3px 5px; border:1px solid #bbb;" /></label>
                    <label>Usuario:&nbsp;<asp:TextBox ID="txtFiltroUsuario" runat="server"
                        style="padding:3px 5px; border:1px solid #bbb; width:120px;" /></label>
                    <label>Accion:&nbsp;<asp:DropDownList ID="ddlAccion" runat="server" /></label>
                </div>
                <asp:Button ID="btnFiltrar" runat="server" Text="Filtrar"
                    CssClass="btn" OnClick="btnFiltrar_Click" />
                <asp:Button ID="btnLimpiarFiltros" runat="server" Text="Limpiar filtros"
                    CssClass="btn" OnClick="btnLimpiarFiltros_Click" />
                <br /><br />
                <asp:Label ID="lblConteoLog" runat="server" Visible="false"
                    style="font-size:13px; font-weight:bold;" />
                <asp:GridView ID="gvBitacora" runat="server"
                    AllowPaging="true" PageSize="20"
                    AutoGenerateColumns="false" CssClass="tabla" Visible="false"
                    OnPageIndexChanging="gvBitacora_PageIndexChanging">
                    <Columns>
                        <asp:BoundField DataField="FechaHora"     HeaderText="Fecha/Hora"
                            DataFormatString="{0:dd/MM/yyyy HH:mm:ss}" ItemStyle-Width="145px" />
                        <asp:BoundField DataField="NombreUsuario" HeaderText="Usuario"  ItemStyle-Width="100px" />
                        <asp:BoundField DataField="Accion"        HeaderText="Accion"   ItemStyle-Width="130px" />
                        <asp:BoundField DataField="Detalle"       HeaderText="Detalle" />
                    </Columns>
                    <PagerStyle CssClass="pager" HorizontalAlign="Center" />
                </asp:GridView>

            </div>
        </asp:Panel>

    </form>
</body>
</html>
