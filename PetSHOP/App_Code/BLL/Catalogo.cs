// Capa de logica de negocio para el catalogo de productos
public static class Catalogo
{
    // Calcula el hash verificador de un producto a partir de sus datos clave
    public static string CalcularHash(Producto p)
    {
        return Encriptacion.HashSHA256(p.Nombre + p.Precio.ToString("F2", System.Globalization.CultureInfo.InvariantCulture) + p.Categoria);
    }
}
