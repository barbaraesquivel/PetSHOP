using System;

// Registro de cada elemento que fue cancelado o eliminado del sistema
[Serializable]
public class RegistroEliminado
{
    public int      Id           { get; set; }
    public string   Tipo         { get; set; } // "Pedido cancelado", etc.
    public string   Descripcion  { get; set; }
    public string   RealizadoPor { get; set; }
    public string   Fecha        { get; set; }
}
