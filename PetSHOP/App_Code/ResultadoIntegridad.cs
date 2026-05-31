// Resultado de verificar la integridad de un producto con digitos verificadores
public class ResultadoIntegridad
{
    public string  Id              { get; set; }
    public string  Nombre          { get; set; }
    public string  Categoria       { get; set; }
    public decimal Precio          { get; set; }
    public string  HashRecalculado { get; set; } // calculado ahora sobre los datos actuales
    public string  HashGuardado    { get; set; } // el que estaba en la columna HashVerificador
    public string  Estado          { get; set; } // "OK" o "ALTERADO"
    public string  Info            { get; set; } // detalle adicional para WebMaster
}
