using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.IO;
using System.Text;
using DAL;

namespace BLL
{
    public class InfoBackup
    {
        public string   NombreArchivo { get; set; }
        public DateTime FechaHora     { get; set; }
        public long     TamanioBytes  { get; set; }

        public string TamanioTexto
        {
            get
            {
                if (TamanioBytes < 1024)         return TamanioBytes + " B";
                if (TamanioBytes < 1024L * 1024) return (TamanioBytes / 1024) + " KB";
                return (TamanioBytes / (1024L * 1024)) + " MB";
            }
        }
    }

    public static class BackupService
    {
        private const int MAX_BACKUPS = 7;

        private static readonly string[] OrdenDelete =
            { "LogBitacora", "DetallePedido", "Pedidos", "Clientes", "Productos", "Usuarios" };

        private static readonly string[] OrdenInsert =
            { "Usuarios", "Clientes", "Productos", "Pedidos", "DetallePedido", "LogBitacora" };

        // ----------------------------------------------------------------
        // Generacion
        // ----------------------------------------------------------------

        public static string GenerarContenidoBackup()
        {
            var sb = new StringBuilder();
            sb.AppendLine("-- Backup PetShop generado: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            sb.AppendLine("-- Para restaurar: ejecutar este archivo contra la BD PetShop");
            sb.AppendLine();

            using (SqlConnection con = ConexionBD.ObtenerConexion())
            {
                con.Open();

                sb.AppendLine("-- === LIMPIEZA (hijo a padre) ===");
                foreach (string tabla in OrdenDelete)
                    sb.AppendLine("DELETE FROM [" + tabla + "];");
                sb.AppendLine();

                foreach (string tabla in OrdenInsert)
                {
                    sb.AppendLine("-- === Tabla: " + tabla + " ===");

                    bool tieneId = TieneIdentidad(tabla, con);
                    if (tieneId)
                        sb.AppendLine("SET IDENTITY_INSERT [" + tabla + "] ON;");

                    var dt = new DataTable();
                    using (var da = new SqlDataAdapter("SELECT * FROM [" + tabla + "]", con))
                        da.Fill(dt);

                    foreach (DataRow row in dt.Rows)
                        sb.AppendLine(GenerarInsert(tabla, dt.Columns, row));

                    if (tieneId)
                        sb.AppendLine("SET IDENTITY_INSERT [" + tabla + "] OFF;");

                    sb.AppendLine();
                }
            }

            return sb.ToString();
        }

        public static void GuardarBackupEnArchivo(string rutaCompleta)
        {
            string contenido = GenerarContenidoBackup();
            File.WriteAllText(rutaCompleta, contenido, Encoding.UTF8);
        }

        // ----------------------------------------------------------------
        // Retencion
        // ----------------------------------------------------------------

        public static void AplicarRetencion(string carpeta)
        {
            if (!Directory.Exists(carpeta)) return;

            string[] archivos = Directory.GetFiles(carpeta, "*.sql");
            if (archivos.Length <= MAX_BACKUPS) return;

            Array.Sort(archivos);                            // mas viejo primero (nombre es fecha)
            int aEliminar = archivos.Length - MAX_BACKUPS;
            for (int i = 0; i < aEliminar; i++)
                File.Delete(archivos[i]);
        }

        // ----------------------------------------------------------------
        // Consulta de backups disponibles
        // ----------------------------------------------------------------

        public static List<InfoBackup> ObtenerInfoBackups(string carpeta)
        {
            var lista = new List<InfoBackup>();
            if (!Directory.Exists(carpeta)) return lista;

            string[] archivos = Directory.GetFiles(carpeta, "*.sql");
            Array.Sort(archivos);
            Array.Reverse(archivos);                        // mas reciente primero

            foreach (string arch in archivos)
            {
                var fi = new FileInfo(arch);
                lista.Add(new InfoBackup
                {
                    NombreArchivo = fi.Name,
                    FechaHora     = ParseFechaDeNombre(fi.Name, fi.LastWriteTime),
                    TamanioBytes  = fi.Length
                });
            }
            return lista;
        }

        private static DateTime ParseFechaDeNombre(string nombre, DateTime fallback)
        {
            try
            {
                // backup_YYYYMMDD_HHmm.sql
                string sinExt = Path.GetFileNameWithoutExtension(nombre);
                string parte  = sinExt.Substring("backup_".Length);
                return DateTime.ParseExact(parte, "yyyyMMdd_HHmm", CultureInfo.InvariantCulture);
            }
            catch
            {
                return fallback;
            }
        }

        // ----------------------------------------------------------------
        // Restauracion
        // ----------------------------------------------------------------

        public static void RestaurarDesdeContenido(string contenido)
        {
            string[] lineas = contenido.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            using (SqlConnection con = ConexionBD.ObtenerConexion())
            {
                con.Open();
                SqlTransaction tx = con.BeginTransaction();
                try
                {
                    foreach (string linea in lineas)
                    {
                        string sql = linea.Trim();
                        if (string.IsNullOrEmpty(sql) || sql.StartsWith("--")) continue;
                        if (sql.EndsWith(";")) sql = sql.Substring(0, sql.Length - 1);

                        using (SqlCommand cmd = new SqlCommand(sql, con, tx))
                        {
                            cmd.CommandTimeout = 60;
                            cmd.ExecuteNonQuery();
                        }
                    }
                    tx.Commit();
                }
                catch
                {
                    tx.Rollback();
                    throw;
                }
            }
        }

        // ----------------------------------------------------------------
        // Helpers privados
        // ----------------------------------------------------------------

        private static bool TieneIdentidad(string tabla, SqlConnection con)
        {
            const string sql = @"SELECT COUNT(*) FROM sys.columns c
                                 JOIN sys.tables  t ON c.object_id = t.object_id
                                 WHERE t.name = @t AND c.is_identity = 1";
            using (SqlCommand cmd = new SqlCommand(sql, con))
            {
                cmd.Parameters.AddWithValue("@t", tabla);
                return (int)cmd.ExecuteScalar() > 0;
            }
        }

        private static string GenerarInsert(string tabla, DataColumnCollection cols, DataRow row)
        {
            var nombres = new List<string>();
            var valores = new List<string>();

            foreach (DataColumn col in cols)
            {
                nombres.Add("[" + col.ColumnName + "]");
                valores.Add(EscaparValor(row[col]));
            }

            return string.Format("INSERT INTO [{0}] ({1}) VALUES ({2});",
                tabla,
                string.Join(", ", nombres.ToArray()),
                string.Join(", ", valores.ToArray()));
        }

        private static string EscaparValor(object val)
        {
            if (val == null || val == DBNull.Value) return "NULL";
            if (val is bool)     return (bool)val ? "1" : "0";
            if (val is DateTime) return "'" + ((DateTime)val).ToString("yyyy-MM-dd HH:mm:ss.fff") + "'";
            if (val is decimal)  return ((decimal)val).ToString(CultureInfo.InvariantCulture);
            if (val is double)   return ((double)val).ToString(CultureInfo.InvariantCulture);
            if (val is float)    return ((float)val).ToString(CultureInfo.InvariantCulture);
            if (val is int || val is long || val is short || val is byte)
                return val.ToString();
            return "'" + val.ToString().Replace("'", "''") + "'";
        }
    }
}
