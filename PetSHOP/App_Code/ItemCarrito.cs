using System;

// Representa un producto dentro del carrito de compras
// El carrito se guarda en Session como Dictionary<int, ItemCarrito>
// donde la clave (int) es el IdProducto
[Serializable]
public class ItemCarrito
{
    public string  Nombre   { get; set; }
    public decimal Precio   { get; set; }
    public int     Cantidad { get; set; }

    public decimal Subtotal
    {
        get { return Precio * Cantidad; }
    }
}
