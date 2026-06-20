using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using DAL;

namespace BLL
{
    public static class BitacoraService
    {
        public static DataTable ObtenerRegistros(
            DateTime? desde, DateTime? hasta, string usuario, string accion)
        {
            const string sql = @"
                SELECT IdLog, FechaHora, NombreUsuario, Accion, Detalle
                FROM   LogBitacora
                WHERE  (@desde   IS NULL OR FechaHora       >= @desde)
                  AND  (@hasta   IS NULL OR FechaHora        < DATEADD(day, 1, @hasta))
                  AND  (@usuario IS NULL OR NombreUsuario LIKE '%' + @usuario + '%')
                  AND  (@accion  IS NULL OR Accion            = @accion)
                ORDER BY FechaHora DESC";

            using (SqlConnection con = ConexionBD.ObtenerConexion())
            {
                con.Open();
                using (SqlCommand cmd = new SqlCommand(sql, con))
                {
                    AgregarParametros(cmd, desde, hasta, usuario, accion);
                    var dt = new DataTable();
                    using (var da = new SqlDataAdapter(cmd))
                        da.Fill(dt);
                    return dt;
                }
            }
        }

        public static List<string> ObtenerAccionesDistintas()
        {
            var lista = new List<string>();
            using (SqlConnection con = ConexionBD.ObtenerConexion())
            {
                con.Open();
                SqlCommand    cmd    = new SqlCommand("SELECT DISTINCT Accion FROM LogBitacora ORDER BY Accion", con);
                SqlDataReader reader = cmd.ExecuteReader();
                while (reader.Read())
                    lista.Add(reader["Accion"].ToString());
                reader.Close();
            }
            return lista;
        }

        private static void AgregarParametros(SqlCommand cmd,
            DateTime? desde, DateTime? hasta, string usuario, string accion)
        {
            cmd.Parameters.Add("@desde",   SqlDbType.DateTime).Value =
                desde.HasValue  ? (object)desde.Value                : DBNull.Value;
            cmd.Parameters.Add("@hasta",   SqlDbType.DateTime).Value =
                hasta.HasValue  ? (object)hasta.Value                : DBNull.Value;
            cmd.Parameters.Add("@usuario", SqlDbType.NVarChar, 100).Value =
                string.IsNullOrEmpty(usuario) ? (object)DBNull.Value : usuario;
            cmd.Parameters.Add("@accion",  SqlDbType.NVarChar, 100).Value =
                string.IsNullOrEmpty(accion)  ? (object)DBNull.Value : accion;
        }
    }
}
