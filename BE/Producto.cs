using System;

namespace BE
{
    [Serializable]
    public class Producto
    {
        public int     IdProducto      { get; set; }
        public string  Nombre          { get; set; }
        public string  Descripcion     { get; set; }
        public decimal Precio          { get; set; }
        public string  Categoria       { get; set; }
        public bool    Activo          { get; set; }
        public string  HashVerificador { get; set; }
        public bool    Eliminado       { get; set; }
        public int     Stock           { get; set; }
    }
}
