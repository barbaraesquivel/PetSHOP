using System;

namespace BE
{
    // producto dentro del carrito de compras
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
}
