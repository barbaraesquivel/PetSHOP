using System.Collections.Generic;

// Clase con la lista de productos del catalogo y metodos de integridad
public static class Catalogo
{
    // Devuelve todos los productos hardcodeados del catalogo
    public static List<Producto> ObtenerProductos()
    {
        List<Producto> lista = new List<Producto>();

        lista.Add(new Producto { Id = "P01", Nombre = "Royal Canin Adulto 3kg",    Descripcion = "Alimento completo para perros adultos",         Precio = 2850, Categoria = "Perros",     Emoji = "" });
        lista.Add(new Producto { Id = "P02", Nombre = "Pedigree Cachorro 2kg",     Descripcion = "Alimento para cachorros en crecimiento",        Precio = 1950, Categoria = "Perros",     Emoji = "" });
        lista.Add(new Producto { Id = "P03", Nombre = "Purina Cat Chow 1.5kg",     Descripcion = "Croquetas para gatos adultos",                  Precio = 1650, Categoria = "Gatos",      Emoji = "" });
        lista.Add(new Producto { Id = "P04", Nombre = "Whiskas Gatito x12 latas",  Descripcion = "Alimento humedo para gatitos",                  Precio = 2200, Categoria = "Gatos",      Emoji = "" });
        lista.Add(new Producto { Id = "P05", Nombre = "Rascador con plataforma",   Descripcion = "Torre rascadora de sisal con colgante",         Precio = 3200, Categoria = "Gatos",      Emoji = "" });
        lista.Add(new Producto { Id = "P06", Nombre = "Pelota de goma",            Descripcion = "Pelota resistente para perros activos",         Precio = 850,  Categoria = "Juguetes",   Emoji = "" });
        lista.Add(new Producto { Id = "P07", Nombre = "Cuerda de juego",           Descripcion = "Cuerda de algodon para tirar y morder",         Precio = 650,  Categoria = "Juguetes",   Emoji = "" });
        lista.Add(new Producto { Id = "P08", Nombre = "Collar anti-pulgas",        Descripcion = "Proteccion contra pulgas por 8 meses",          Precio = 1200, Categoria = "Salud",      Emoji = "" });
        lista.Add(new Producto { Id = "P09", Nombre = "Vitaminas para gato x30",   Descripcion = "Suplemento vitaminico sabor pollo",             Precio = 950,  Categoria = "Salud",      Emoji = "" });
        lista.Add(new Producto { Id = "P10", Nombre = "Shampoo hipoalergenico",    Descripcion = "Shampoo suave para pieles sensibles 500ml",     Precio = 780,  Categoria = "Accesorios", Emoji = "" });
        lista.Add(new Producto { Id = "P11", Nombre = "Cama circular 60cm",        Descripcion = "Cama acolchada y lavable para mascotas",        Precio = 2100, Categoria = "Accesorios", Emoji = "" });

        return lista;
    }

    // Calcula el hash SHA-256 de un producto basandose en nombre+precio+categoria
    // Se usa para verificar que los datos no fueron alterados
    public static string CalcularHash(Producto p)
    {
        string datos = p.Nombre + p.Precio.ToString("N2") + p.Categoria;
        return Seguridad.HashSHA256(datos);
    }
}
