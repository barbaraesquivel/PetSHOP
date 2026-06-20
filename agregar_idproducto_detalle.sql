-- PROCESO 3: IdProducto en DetallePedido (necesario para restaurar stock al cancelar)
-- Ejecutar contra la base de datos PetShop

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('DetallePedido') AND name = 'IdProducto')
BEGIN
    ALTER TABLE dbo.DetallePedido
        ADD IdProducto INT NULL
            CONSTRAINT FK_DetallePedido_Productos FOREIGN KEY REFERENCES dbo.Productos(IdProducto);
    PRINT 'Columna IdProducto agregada a DetallePedido.';
END
ELSE
    PRINT 'Columna IdProducto ya existe en DetallePedido.';

-- Verificacion
SELECT COLUMN_NAME, DATA_TYPE, IS_NULLABLE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'DetallePedido'
ORDER BY ORDINAL_POSITION;
