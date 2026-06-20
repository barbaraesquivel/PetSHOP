using System;

namespace BE
{
    [Serializable]
    public class Cliente
    {
        public int      IdCliente { get; set; }
        public int      IdUsuario { get; set; }
        public string   Nombre    { get; set; }
        public string   Apellido  { get; set; }
        public string   Email     { get; set; }
        public string   Telefono  { get; set; }
        public string   Direccion { get; set; }
        public DateTime FechaAlta { get; set; }
    }
}
