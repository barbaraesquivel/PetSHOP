using System.Globalization;
using SEGURIDAD;

namespace BLL
{
    public static class Catalogo
    {
        // Unico punto de calculo del hash de integridad de un producto.
        // Incluye IdProducto para detectar intercambio de filas y Descripcion
        // para detectar modificaciones en ese campo. Activo y Eliminado se
        // excluyen porque cambian legitimamente desde el panel admin.
        // El separador | evita colisiones por concatenacion de campos.
        public static string CalcularHash(int id, string nombre, string descripcion,
                                          decimal precio, string categoria)
        {
            string entrada = id   + "|"
                           + nombre + "|"
                           + (descripcion ?? "") + "|"
                           + precio.ToString("F2", CultureInfo.InvariantCulture) + "|"
                           + categoria;
            return Encriptacion.HashSHA256(entrada);
        }
    }
}
