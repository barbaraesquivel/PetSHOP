using System;

namespace BE
{
    [Serializable]
    public class Pedido
    {
        public int      Id            { get; set; }
        public int      IdCliente     { get; set; }
        public string   Detalle       { get; set; }
        public decimal  Total         { get; set; }
        public string   Estado        { get; set; }
        public DateTime Fecha         { get; set; }
        public string   ModificadoPor { get; set; }
        public string   FechaModif    { get; set; }
    }
}
