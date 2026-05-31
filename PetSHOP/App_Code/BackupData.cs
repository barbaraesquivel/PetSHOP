using System;

// Estructura para el backup JSON — cada tabla se serializa por separado
// LogBitacora se incluye en el backup pero NO se borra/restaura (es auditoria)
[Serializable]
public class BackupData
{
    public string FechaBackup     { get; set; }
    public string UsuariosJson    { get; set; }
    public string ProductosJson   { get; set; }
    public string PedidosJson     { get; set; }
    public string DetalleJson     { get; set; }
    public string EliminadosJson  { get; set; }
}
