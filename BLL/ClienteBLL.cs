using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using BE;
using DAL;

namespace BLL
{
    public static class ClienteBLL
    {
        public static Cliente GetByIdUsuario(int idUsuario)
        {
            using (SqlConnection con = ConexionBD.ObtenerConexion())
            {
                con.Open();
                SqlCommand cmd = new SqlCommand(
                    "SELECT IdCliente, IdUsuario, Nombre, Apellido, Email, Telefono, Direccion, FechaAlta " +
                    "FROM Clientes WHERE IdUsuario = @idUsuario", con);
                cmd.Parameters.AddWithValue("@idUsuario", idUsuario);
                SqlDataReader r = cmd.ExecuteReader();
                if (r.Read())
                {
                    var c = MapearCliente(r);
                    r.Close();
                    return c;
                }
                r.Close();
                return null;
            }
        }

        public static int Crear(SqlConnection con, SqlTransaction tx,
            int idUsuario, string nombre, string apellido, string email,
            string telefono, string direccion)
        {
            SqlCommand cmd = new SqlCommand(
                @"INSERT INTO Clientes (IdUsuario, Nombre, Apellido, Email, Telefono, Direccion, FechaAlta)
                  OUTPUT INSERTED.IdCliente
                  VALUES (@idUsuario, @nombre, @apellido, @email, @telefono, @direccion, GETDATE())",
                con, tx);
            cmd.Parameters.AddWithValue("@idUsuario", idUsuario);
            cmd.Parameters.AddWithValue("@nombre",    nombre);
            cmd.Parameters.AddWithValue("@apellido",  apellido);
            cmd.Parameters.AddWithValue("@email",     email);
            cmd.Parameters.AddWithValue("@telefono",  string.IsNullOrEmpty(telefono)  ? (object)DBNull.Value : telefono);
            cmd.Parameters.AddWithValue("@direccion", string.IsNullOrEmpty(direccion) ? (object)DBNull.Value : direccion);
            return (int)cmd.ExecuteScalar();
        }

        public static List<Cliente> GetAll()
        {
            var lista = new List<Cliente>();
            using (SqlConnection con = ConexionBD.ObtenerConexion())
            {
                con.Open();
                SqlCommand cmd = new SqlCommand(
                    "SELECT IdCliente, IdUsuario, Nombre, Apellido, Email, Telefono, Direccion, FechaAlta " +
                    "FROM Clientes ORDER BY FechaAlta DESC", con);
                SqlDataReader r = cmd.ExecuteReader();
                while (r.Read())
                    lista.Add(MapearCliente(r));
                r.Close();
            }
            return lista;
        }

        private static Cliente MapearCliente(SqlDataReader r)
        {
            return new Cliente
            {
                IdCliente = (int)r["IdCliente"],
                IdUsuario = (int)r["IdUsuario"],
                Nombre    = r["Nombre"].ToString(),
                Apellido  = r["Apellido"].ToString(),
                Email     = r["Email"].ToString(),
                Telefono  = r["Telefono"]  == DBNull.Value ? "" : r["Telefono"].ToString(),
                Direccion = r["Direccion"] == DBNull.Value ? "" : r["Direccion"].ToString(),
                FechaAlta = (DateTime)r["FechaAlta"]
            };
        }
    }
}
