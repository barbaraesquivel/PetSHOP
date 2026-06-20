using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using DAL;

namespace BLL
{
    public static class PedidoBLL
    {
        // ----------------------------------------------------------------
        // Maquina de estados
        // ----------------------------------------------------------------

        public static string GetSiguienteEstado(string estado)
        {
            switch (estado)
            {
                case "Pendiente":        return "Confirmado";
                case "Confirmado":       return "EnPreparacion";
                case "EnPreparacion":    return "ListoParaRetirar";
                case "ListoParaRetirar": return "Retirado";
                default:                 return null;
            }
        }

        public static string GetEtiquetaAvance(string estado)
        {
            switch (estado)
            {
                case "Pendiente":        return "Confirmar";
                case "Confirmado":       return "Iniciar preparacion";
                case "EnPreparacion":    return "Listo para retirar";
                case "ListoParaRetirar": return "Marcar retirado";
                default:                 return "";
            }
        }

        public static bool PuedeCancelarAdmin(string estado)
        {
            return estado != "ListoParaRetirar" && estado != "Retirado" && estado != "Cancelado";
        }

        public static bool PuedeCancelarCliente(string estado)
        {
            return estado == "Pendiente";
        }

        // ----------------------------------------------------------------
        // Consultas
        // ----------------------------------------------------------------

        public static DataTable GetByCliente(int idCliente)
        {
            using (SqlConnection con = ConexionBD.ObtenerConexion())
            {
                con.Open();
                SqlDataAdapter da = new SqlDataAdapter(
                    @"SELECT IdPedido, FechaPedido, Total, Estado,
                             ISNULL(ModificadoPor, '') AS ModificadoPor
                      FROM Pedidos
                      WHERE IdCliente = @id
                      ORDER BY FechaPedido DESC", con);
                da.SelectCommand.Parameters.AddWithValue("@id", idCliente);
                DataTable dt = new DataTable();
                da.Fill(dt);
                return dt;
            }
        }

        public static DataTable GetDetalleByPedido(int idPedido)
        {
            using (SqlConnection con = ConexionBD.ObtenerConexion())
            {
                con.Open();
                SqlDataAdapter da = new SqlDataAdapter(
                    @"SELECT NombreProducto, PrecioUnitario, Cantidad, Subtotal
                      FROM DetallePedido
                      WHERE IdPedido = @id
                      ORDER BY IdDetalle", con);
                da.SelectCommand.Parameters.AddWithValue("@id", idPedido);
                DataTable dt = new DataTable();
                da.Fill(dt);
                return dt;
            }
        }

        public static DataTable GetAllAdmin()
        {
            using (SqlConnection con = ConexionBD.ObtenerConexion())
            {
                con.Open();
                SqlDataAdapter da = new SqlDataAdapter(
                    @"SELECT p.IdPedido,
                             ISNULL(c.Nombre + ' ' + c.Apellido, 'Sin cliente') AS NombreCliente,
                             p.FechaPedido, p.Total, p.Estado,
                             ISNULL(p.ModificadoPor, '') AS ModificadoPor
                      FROM Pedidos p
                      LEFT JOIN Clientes c ON p.IdCliente = c.IdCliente
                      ORDER BY p.FechaPedido DESC", con);
                DataTable dt = new DataTable();
                da.Fill(dt);
                return dt;
            }
        }

        // ----------------------------------------------------------------
        // Cambios de estado
        // ----------------------------------------------------------------

        public static string AvanzarEstado(int idPedido, string usuario)
        {
            string estadoActual = GetEstadoActual(idPedido);
            string nuevoEstado  = GetSiguienteEstado(estadoActual);

            if (nuevoEstado == null)
                throw new Exception("El pedido #" + idPedido + " no puede avanzar desde: " + estadoActual);

            using (SqlConnection con = ConexionBD.ObtenerConexion())
            {
                con.Open();
                SqlCommand cmd = new SqlCommand(
                    @"UPDATE Pedidos
                      SET Estado=@estado, ModificadoPor=@usuario, FechaModif=GETDATE()
                      WHERE IdPedido=@id", con);
                cmd.Parameters.AddWithValue("@estado",  nuevoEstado);
                cmd.Parameters.AddWithValue("@usuario", usuario);
                cmd.Parameters.AddWithValue("@id",      idPedido);
                cmd.ExecuteNonQuery();
            }

            return nuevoEstado;
        }

        public static string Cancelar(int idPedido, string usuario, bool esAdmin)
        {
            string estadoActual = GetEstadoActual(idPedido);

            if (esAdmin)
            {
                if (!PuedeCancelarAdmin(estadoActual))
                    throw new Exception("No se puede cancelar un pedido en estado: " + estadoActual);
            }
            else
            {
                if (!PuedeCancelarCliente(estadoActual))
                    throw new Exception("Solo puede cancelar pedidos en estado Pendiente. Estado actual: " + estadoActual);
            }

            using (SqlConnection con = ConexionBD.ObtenerConexion())
            {
                con.Open();
                SqlTransaction tx = con.BeginTransaction();
                try
                {
                    // Leer items para restaurar stock
                    SqlCommand cmdItems = new SqlCommand(
                        "SELECT IdProducto, Cantidad FROM DetallePedido WHERE IdPedido=@id AND IdProducto IS NOT NULL",
                        con, tx);
                    cmdItems.Parameters.AddWithValue("@id", idPedido);
                    SqlDataReader r = cmdItems.ExecuteReader();
                    var items = new List<int[]>();
                    while (r.Read())
                        items.Add(new int[] { (int)r["IdProducto"], (int)r["Cantidad"] });
                    r.Close();

                    // Restaurar stock
                    foreach (int[] item in items)
                    {
                        SqlCommand cmdStock = new SqlCommand(
                            "UPDATE Productos SET Stock = Stock + @cant WHERE IdProducto = @id",
                            con, tx);
                        cmdStock.Parameters.AddWithValue("@cant", item[1]);
                        cmdStock.Parameters.AddWithValue("@id",   item[0]);
                        cmdStock.ExecuteNonQuery();
                    }

                    // Cancelar pedido
                    SqlCommand cmdCancel = new SqlCommand(
                        @"UPDATE Pedidos
                          SET Estado='Cancelado', ModificadoPor=@usuario, FechaModif=GETDATE()
                          WHERE IdPedido=@id",
                        con, tx);
                    cmdCancel.Parameters.AddWithValue("@usuario", usuario);
                    cmdCancel.Parameters.AddWithValue("@id",      idPedido);
                    cmdCancel.ExecuteNonQuery();

                    tx.Commit();
                }
                catch
                {
                    tx.Rollback();
                    throw;
                }
            }

            return estadoActual;
        }

        // ----------------------------------------------------------------
        // Helper privado
        // ----------------------------------------------------------------

        private static string GetEstadoActual(int idPedido)
        {
            using (SqlConnection con = ConexionBD.ObtenerConexion())
            {
                con.Open();
                SqlCommand cmd = new SqlCommand(
                    "SELECT Estado FROM Pedidos WHERE IdPedido=@id", con);
                cmd.Parameters.AddWithValue("@id", idPedido);
                object result = cmd.ExecuteScalar();
                if (result == null)
                    throw new Exception("Pedido #" + idPedido + " no encontrado.");
                return result.ToString();
            }
        }
    }
}
