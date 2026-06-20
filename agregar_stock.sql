-- PROCESO 2: Columna Stock en Productos
-- Ejecutar contra la base de datos PetShop

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Productos') AND name = 'Stock')
BEGIN
    ALTER TABLE dbo.Productos ADD Stock INT NOT NULL CONSTRAINT DF_Productos_Stock DEFAULT 0;
    PRINT 'Columna Stock agregada a Productos (valor inicial: 0 en todos los registros).';
END
ELSE
    PRINT 'Columna Stock ya existe en Productos.';

-- Verificacion
SELECT COLUMN_NAME, DATA_TYPE, IS_NULLABLE, COLUMN_DEFAULT
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'Productos'
ORDER BY ORDINAL_POSITION;
