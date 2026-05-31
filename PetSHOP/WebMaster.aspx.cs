// REQUIERE: Newtonsoft.Json instalado via NuGet (ya esta en Bin/)
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using Newtonsoft.Json;

public partial class WebMaster : System.Web.UI.Page
{
    protected void Page_Load(object sender, EventArgs e)
    {
        if (!SesionHelper.VerificarSesion(this)) return;

        if (!SesionHelper.VerificarRol(this, "WebMaster"))
        {
            pnlDenegado.Visible  = true;
            pnlContenido.Visible = false;
            return;
        }

        if (!SesionHelper.VerificarDB(this))
        {
            pnlDenegado.Visible  = true;
            pnlContenido.Visible = false;
            return;
        }

        pnlContenido.Visible = true;
        pnlDenegado.Visible  = false;
        lblWMUser.Text       = Session["Usuario"].ToString();

        // Al entrar, el sistema verifica automaticamente la integridad de la BD
        ActualizarEstadoIntegridad();

        if (!IsPostBack)
            Bitacora.Registrar(Session["Usuario"].ToString(), "ACCESO", "WebMaster.aspx");
    }

    // Verifica todos los hashes, actualiza la tabla y muestra el panel correspondiente.
    // Se llama en Page_Load Y al final de cada operacion de reparacion,
    // para que el estado mostrado siempre refleje la situacion actual.
    private void ActualizarEstadoIntegridad()
    {
        try
        {
            List<ResultadoIntegridad> resultados = new List<ResultadoIntegridad>();
            bool hayCorrupcion = false;

            using (SqlConnection con = ConexionDB.ObtenerConexion())
            {
                con.Open();
                SqlCommand cmd = new SqlCommand(
                    "SELECT IdProducto, Nombre, Precio, Categoria, HashVerificador FROM Productos WHERE Activo=1", con);
                SqlDataReader reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    string  nombre       = reader["Nombre"].ToString();
                    decimal precio       = (decimal)reader["Precio"];
                    string  categoria    = reader["Categoria"].ToString();
                    string  hashGuardado = reader["HashVerificador"] == DBNull.Value ? "" : reader["HashVerificador"].ToString();
                    string  hashActual   = Seguridad.HashSHA256(nombre + precio.ToString("N2") + categoria);
                    bool    ok           = (hashActual == hashGuardado);

                    if (!ok) hayCorrupcion = true;

                    ResultadoIntegridad res = new ResultadoIntegridad();
                    res.Id             = reader["IdProducto"].ToString();
                    res.Nombre         = nombre;
                    res.Categoria      = categoria;
                    res.Precio         = precio;
                    res.HashRecalculado = hashActual;
                    res.HashGuardado   = hashGuardado;
                    res.Estado         = ok ? "OK" : "ALTERADO";
                    res.Info           = ok ? "Sin cambios" : "Modificacion externa detectada";
                    resultados.Add(res);
                }
                reader.Close();
            }

            gvIntegridad.DataSource = resultados;
            gvIntegridad.DataBind();
            gvIntegridad.Visible = (resultados.Count > 0);

            // Mostramos el panel segun el resultado
            pnlCorrupto.Visible  = hayCorrupcion;
            pnlEstadoOK.Visible  = !hayCorrupcion;
        }
        catch (Exception ex)
        {
            MostrarMsg("Error al verificar integridad: " + ex.Message, true);
        }
    }

    // Boton 1: recalcula los hashes a partir de los datos actuales y los guarda.
    // Esto "acepta" los datos actuales como validos y corrige el HashVerificador.
    protected void btnRecalcularHashes_Click(object sender, EventArgs e)
    {
        try
        {
            using (SqlConnection con = ConexionDB.ObtenerConexion())
            {
                con.Open();
                RecalcularHashes(con);
            }

            Bitacora.Registrar(Session["Usuario"].ToString(), "RECALCULAR_HASHES",
                "HashVerificador recalculado para todos los productos");
            MostrarMsg("Digitos verificadores recalculados y guardados correctamente.", false);
        }
        catch (Exception ex)
        {
            MostrarMsg("Error al recalcular hashes: " + ex.Message, true);
        }

        // Actualizamos el estado para reflejar los nuevos hashes
        ActualizarEstadoIntegridad();
    }

    // Boton 2: restaura la BD automaticamente desde App_Data/backup.json
    // El sistema lo hace solo, sin intervencion manual adicional.
    protected void btnRestaurarBD_Click(object sender, EventArgs e)
    {
        try
        {
            string archivo = Server.MapPath("~/App_Data/backup.json");
            if (!File.Exists(archivo))
            {
                MostrarMsg("No se encontro el archivo de backup (App_Data/backup.json). Haga un backup primero.", true);
                return;
            }

            string     json  = File.ReadAllText(archivo);
            BackupData datos = JsonConvert.DeserializeObject<BackupData>(json);

            DataTable dtUsuarios   = Deserializar(datos.UsuariosJson);
            DataTable dtProductos  = Deserializar(datos.ProductosJson);
            DataTable dtPedidos    = Deserializar(datos.PedidosJson);
            DataTable dtDetalle    = Deserializar(datos.DetalleJson);
            DataTable dtEliminados = Deserializar(datos.EliminadosJson);

            using (SqlConnection con = ConexionDB.ObtenerConexion())
            {
                con.Open();

                // Borrar en orden respetando FK
                EjecutarSQL("DELETE FROM DetallePedido", con);
                EjecutarSQL("DELETE FROM Pedidos",       con);
                EjecutarSQL("DELETE FROM Eliminados",    con);
                EjecutarSQL("DELETE FROM Productos",     con);
                EjecutarSQL("DELETE FROM Usuarios",      con);

                // Reinsertar en orden inverso
                if (dtUsuarios.Rows.Count   > 0) InsertarUsuarios(dtUsuarios, con);
                if (dtProductos.Rows.Count  > 0) InsertarProductos(dtProductos, con);
                if (dtPedidos.Rows.Count    > 0) InsertarPedidos(dtPedidos, con);
                if (dtDetalle.Rows.Count    > 0) InsertarDetalles(dtDetalle, con);
                if (dtEliminados.Rows.Count > 0) InsertarEliminados(dtEliminados, con);

                RecalcularHashes(con);
            }

            string msg = "Restauracion exitosa desde backup del " + datos.FechaBackup;
            Bitacora.Registrar(Session["Usuario"].ToString(), "RESTORE", msg);
            MostrarMsg(msg, false);
        }
        catch (Exception ex)
        {
            MostrarMsg("Error en la restauracion: " + ex.Message, true);
        }

        // Actualizamos el estado para confirmar que la BD quedo integra
        ActualizarEstadoIntegridad();
    }

    // Seccion c: backup de todas las tablas
    protected void btnBackup_Click(object sender, EventArgs e)
    {
        try
        {
            using (SqlConnection con = ConexionDB.ObtenerConexion())
            {
                con.Open();

                BackupData datos = new BackupData();
                datos.FechaBackup    = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                datos.UsuariosJson   = JsonConvert.SerializeObject(LeerTabla("SELECT * FROM Usuarios",      con));
                datos.ProductosJson  = JsonConvert.SerializeObject(LeerTabla("SELECT * FROM Productos",     con));
                datos.PedidosJson    = JsonConvert.SerializeObject(LeerTabla("SELECT * FROM Pedidos",       con));
                datos.DetalleJson    = JsonConvert.SerializeObject(LeerTabla("SELECT * FROM DetallePedido", con));
                datos.EliminadosJson = JsonConvert.SerializeObject(LeerTabla("SELECT * FROM Eliminados",    con));

                string json    = JsonConvert.SerializeObject(datos, Formatting.Indented);
                string carpeta = Server.MapPath("~/App_Data");
                if (!Directory.Exists(carpeta)) Directory.CreateDirectory(carpeta);
                File.WriteAllText(Path.Combine(carpeta, "backup.json"), json);
            }

            string msg = "Backup realizado: " + DateTime.Now.ToString("dd/MM/yyyy HH:mm");
            MostrarMsg(msg, false);
            Bitacora.Registrar(Session["Usuario"].ToString(), "BACKUP", msg);
        }
        catch (Exception ex)
        {
            MostrarMsg("Error en backup: " + ex.Message, true);
        }
    }

    // ===== HELPERS =====

    private DataTable LeerTabla(string sql, SqlConnection con)
    {
        SqlDataAdapter da = new SqlDataAdapter(sql, con);
        DataTable dt = new DataTable();
        da.Fill(dt);
        return dt;
    }

    private DataTable Deserializar(string json)
    {
        if (string.IsNullOrEmpty(json)) return new DataTable();
        return JsonConvert.DeserializeObject<DataTable>(json);
    }

    private void EjecutarSQL(string sql, SqlConnection con)
    {
        new SqlCommand(sql, con).ExecuteNonQuery();
    }

    // Columnas reales de Usuarios: IdUsuario, NombreUsuario, PasswordHash, Rol
    private void InsertarUsuarios(DataTable dt, SqlConnection con)
    {
        try
        {
            EjecutarSQL("SET IDENTITY_INSERT Usuarios ON", con);
            foreach (DataRow row in dt.Rows)
            {
                SqlCommand cmd = new SqlCommand(
                    @"INSERT INTO Usuarios (IdUsuario, NombreUsuario, PasswordHash, Rol)
                      VALUES (@id, @nombre, @hash, @rol)", con);
                cmd.Parameters.AddWithValue("@id",     row["IdUsuario"]);
                cmd.Parameters.AddWithValue("@nombre", row["NombreUsuario"]);
                cmd.Parameters.AddWithValue("@hash",   row["PasswordHash"]);
                cmd.Parameters.AddWithValue("@rol",    row["Rol"]);
                cmd.ExecuteNonQuery();
            }
        }
        finally { try { EjecutarSQL("SET IDENTITY_INSERT Usuarios OFF", con); } catch { } }
    }

    // Columnas reales: IdProducto, Nombre, Descripcion, Precio, Categoria, Activo, HashVerificador
    private void InsertarProductos(DataTable dt, SqlConnection con)
    {
        try
        {
            EjecutarSQL("SET IDENTITY_INSERT Productos ON", con);
            foreach (DataRow row in dt.Rows)
            {
                SqlCommand cmd = new SqlCommand(
                    @"INSERT INTO Productos (IdProducto, Nombre, Descripcion, Precio, Categoria, Activo, HashVerificador)
                      VALUES (@id, @nombre, @desc, @precio, @cat, @activo, @hash)", con);
                cmd.Parameters.AddWithValue("@id",     row["IdProducto"]);
                cmd.Parameters.AddWithValue("@nombre", row["Nombre"]);
                cmd.Parameters.AddWithValue("@desc",   row["Descripcion"] == DBNull.Value ? (object)DBNull.Value : row["Descripcion"]);
                cmd.Parameters.AddWithValue("@precio", row["Precio"]);
                cmd.Parameters.AddWithValue("@cat",    row["Categoria"]);
                cmd.Parameters.AddWithValue("@activo", row["Activo"]);
                cmd.Parameters.AddWithValue("@hash",   row["HashVerificador"] == DBNull.Value ? (object)DBNull.Value : row["HashVerificador"]);
                cmd.ExecuteNonQuery();
            }
        }
        finally { try { EjecutarSQL("SET IDENTITY_INSERT Productos OFF", con); } catch { } }
    }

    // Columnas reales: IdPedido, IdUsuario, FechaPedido, Total, Estado, ModificadoPor, FechaModif
    private void InsertarPedidos(DataTable dt, SqlConnection con)
    {
        try
        {
            EjecutarSQL("SET IDENTITY_INSERT Pedidos ON", con);
            foreach (DataRow row in dt.Rows)
            {
                SqlCommand cmd = new SqlCommand(
                    @"INSERT INTO Pedidos (IdPedido, IdUsuario, FechaPedido, Total, Estado, ModificadoPor, FechaModif)
                      VALUES (@id, @idUsuario, @fecha, @total, @estado, @modPor, @fechaModif)", con);
                cmd.Parameters.AddWithValue("@id",         row["IdPedido"]);
                cmd.Parameters.AddWithValue("@idUsuario",  row["IdUsuario"]);
                cmd.Parameters.AddWithValue("@fecha",      row["FechaPedido"]);
                cmd.Parameters.AddWithValue("@total",      row["Total"]);
                cmd.Parameters.AddWithValue("@estado",     row["Estado"]);
                cmd.Parameters.AddWithValue("@modPor",     row["ModificadoPor"] == DBNull.Value ? (object)DBNull.Value : row["ModificadoPor"]);
                cmd.Parameters.AddWithValue("@fechaModif", row["FechaModif"]    == DBNull.Value ? (object)DBNull.Value : row["FechaModif"]);
                cmd.ExecuteNonQuery();
            }
        }
        finally { try { EjecutarSQL("SET IDENTITY_INSERT Pedidos OFF", con); } catch { } }
    }

    // Columnas reales: IdDetalle, IdPedido, NombreProducto, PrecioUnitario, Cantidad, Subtotal
    private void InsertarDetalles(DataTable dt, SqlConnection con)
    {
        try
        {
            EjecutarSQL("SET IDENTITY_INSERT DetallePedido ON", con);
            foreach (DataRow row in dt.Rows)
            {
                SqlCommand cmd = new SqlCommand(
                    @"INSERT INTO DetallePedido (IdDetalle, IdPedido, NombreProducto, PrecioUnitario, Cantidad, Subtotal)
                      VALUES (@id, @idPedido, @nombre, @precio, @cantidad, @subtotal)", con);
                cmd.Parameters.AddWithValue("@id",       row["IdDetalle"]);
                cmd.Parameters.AddWithValue("@idPedido", row["IdPedido"]);
                cmd.Parameters.AddWithValue("@nombre",   row["NombreProducto"]);
                cmd.Parameters.AddWithValue("@precio",   row["PrecioUnitario"]);
                cmd.Parameters.AddWithValue("@cantidad", row["Cantidad"]);
                cmd.Parameters.AddWithValue("@subtotal", row["Subtotal"]);
                cmd.ExecuteNonQuery();
            }
        }
        finally { try { EjecutarSQL("SET IDENTITY_INSERT DetallePedido OFF", con); } catch { } }
    }

    // Columnas reales: IdEliminado, Tipo, Descripcion, RealizadoPor, FechaHora, EsExterno
    private void InsertarEliminados(DataTable dt, SqlConnection con)
    {
        try
        {
            EjecutarSQL("SET IDENTITY_INSERT Eliminados ON", con);
            foreach (DataRow row in dt.Rows)
            {
                SqlCommand cmd = new SqlCommand(
                    @"INSERT INTO Eliminados (IdEliminado, Tipo, Descripcion, RealizadoPor, FechaHora, EsExterno)
                      VALUES (@id, @tipo, @desc, @por, @fecha, @esExt)", con);
                cmd.Parameters.AddWithValue("@id",    row["IdEliminado"]);
                cmd.Parameters.AddWithValue("@tipo",  row["Tipo"]);
                cmd.Parameters.AddWithValue("@desc",  row["Descripcion"]);
                cmd.Parameters.AddWithValue("@por",   row["RealizadoPor"]);
                cmd.Parameters.AddWithValue("@fecha", row["FechaHora"]);
                cmd.Parameters.AddWithValue("@esExt", row["EsExterno"]);
                cmd.ExecuteNonQuery();
            }
        }
        finally { try { EjecutarSQL("SET IDENTITY_INSERT Eliminados OFF", con); } catch { } }
    }

    // Recalcula HashVerificador para todos los productos y los actualiza en BD
    private void RecalcularHashes(SqlConnection con)
    {
        List<int>    ids    = new List<int>();
        List<string> hashes = new List<string>();

        SqlCommand cmd = new SqlCommand("SELECT IdProducto, Nombre, Precio, Categoria FROM Productos", con);
        SqlDataReader reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            ids.Add((int)reader["IdProducto"]);
            hashes.Add(Seguridad.HashSHA256(
                reader["Nombre"].ToString() +
                ((decimal)reader["Precio"]).ToString("N2") +
                reader["Categoria"].ToString()));
        }
        reader.Close();

        for (int i = 0; i < ids.Count; i++)
        {
            SqlCommand upd = new SqlCommand(
                "UPDATE Productos SET HashVerificador=@hash WHERE IdProducto=@id", con);
            upd.Parameters.AddWithValue("@hash", hashes[i]);
            upd.Parameters.AddWithValue("@id",   ids[i]);
            upd.ExecuteNonQuery();
        }
    }

    private void MostrarMsg(string texto, bool esError)
    {
        lblMensaje.Text     = texto;
        lblMensaje.CssClass = esError ? "msg msg-err" : "msg msg-ok";
        lblMensaje.Visible  = true;
    }
}
