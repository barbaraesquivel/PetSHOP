using System;

// Representa un pedido confirmado por un usuario
[Serializable]
public class Pedido
{
    public int      Id              { get; set; }
    public string   Usuario         { get; set; } // quien hizo el pedido
    public string   Detalle         { get; set; } // descripcion del pedido
    public decimal  Total           { get; set; }
    public string   Estado          { get; set; } // "Activo" o "Cancelado"
    public DateTime Fecha           { get; set; } // cuando se realizo
    public string   ModificadoPor   { get; set; } // quien cambio el estado
    public string   FechaModif      { get; set; } // cuando se cambio el estado
}
