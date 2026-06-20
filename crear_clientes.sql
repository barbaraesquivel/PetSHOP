-- PROCESO 1: Tabla Clientes y ajuste de Pedidos
-- Ejecutar contra la base de datos PetShop

-- =====================================================================
-- Paso 1: Columnas de perfil en Usuarios (guardadas al registrarse)
-- =====================================================================
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Usuarios') AND name = 'Nombre')
    ALTER TABLE Usuarios ADD Nombre    NVARCHAR(100) NULL;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Usuarios') AND name = 'Apellido')
    ALTER TABLE Usuarios ADD Apellido  NVARCHAR(100) NULL;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Usuarios') AND name = 'Email')
    ALTER TABLE Usuarios ADD Email     NVARCHAR(200) NULL;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Usuarios') AND name = 'Telefono')
    ALTER TABLE Usuarios ADD Telefono  NVARCHAR(50)  NULL;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Usuarios') AND name = 'Direccion')
    ALTER TABLE Usuarios ADD Direccion NVARCHAR(300) NULL;

PRINT 'Paso 1 OK: columnas de perfil en Usuarios.';

-- =====================================================================
-- Paso 2: Tabla Clientes
-- =====================================================================
IF NOT EXISTS (SELECT 1 FROM sys.objects WHERE name = 'Clientes' AND type = 'U')
BEGIN
    CREATE TABLE dbo.Clientes (
        IdCliente  INT           IDENTITY(1,1) NOT NULL,
        IdUsuario  INT           NOT NULL,
        Nombre     NVARCHAR(100) NOT NULL,
        Apellido   NVARCHAR(100) NOT NULL,
        Email      NVARCHAR(200) NOT NULL,
        Telefono   NVARCHAR(50)  NULL,
        Direccion  NVARCHAR(300) NULL,
        FechaAlta  DATETIME      NOT NULL CONSTRAINT DF_Clientes_FechaAlta DEFAULT GETDATE(),
        CONSTRAINT PK_Clientes           PRIMARY KEY (IdCliente),
        CONSTRAINT UQ_Clientes_IdUsuario UNIQUE      (IdUsuario),
        CONSTRAINT FK_Clientes_Usuarios  FOREIGN KEY (IdUsuario) REFERENCES dbo.Usuarios(IdUsuario)
    );
    PRINT 'Paso 2 OK: tabla Clientes creada.';
END
ELSE
    PRINT 'Paso 2: tabla Clientes ya existe.';

-- =====================================================================
-- Paso 3: Agregar IdCliente a Pedidos
-- =====================================================================
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Pedidos') AND name = 'IdCliente')
BEGIN
    ALTER TABLE dbo.Pedidos ADD IdCliente INT NULL;
    ALTER TABLE dbo.Pedidos ADD CONSTRAINT FK_Pedidos_Clientes
        FOREIGN KEY (IdCliente) REFERENCES dbo.Clientes(IdCliente);
    PRINT 'Paso 3 OK: columna IdCliente agregada a Pedidos.';
END
ELSE
    PRINT 'Paso 3: columna IdCliente ya existe en Pedidos.';

-- =====================================================================
-- Paso 4: Eliminar columna IdUsuario de Pedidos (quitar FK si existe)
-- =====================================================================
IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Pedidos') AND name = 'IdUsuario')
BEGIN
    DECLARE @fkName NVARCHAR(200);
    SELECT @fkName = fk.name
    FROM sys.foreign_keys fk
    INNER JOIN sys.foreign_key_columns fkc ON fk.object_id = fkc.constraint_object_id
    INNER JOIN sys.columns c ON fkc.parent_column_id = c.column_id
                             AND fkc.parent_object_id = c.object_id
    WHERE fk.parent_object_id = OBJECT_ID('Pedidos') AND c.name = 'IdUsuario';

    IF @fkName IS NOT NULL
        EXEC('ALTER TABLE dbo.Pedidos DROP CONSTRAINT ' + @fkName);

    ALTER TABLE dbo.Pedidos DROP COLUMN IdUsuario;
    PRINT 'Paso 4 OK: columna IdUsuario eliminada de Pedidos.';
END
ELSE
    PRINT 'Paso 4: columna IdUsuario ya no existe en Pedidos.';

-- =====================================================================
-- Verificacion final
-- =====================================================================
SELECT 'Usuarios' AS Tabla, COLUMN_NAME, DATA_TYPE, IS_NULLABLE
FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Usuarios' ORDER BY ORDINAL_POSITION;

SELECT 'Pedidos' AS Tabla, COLUMN_NAME, DATA_TYPE, IS_NULLABLE
FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Pedidos' ORDER BY ORDINAL_POSITION;

SELECT 'Clientes' AS Tabla, COLUMN_NAME, DATA_TYPE, IS_NULLABLE
FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Clientes' ORDER BY ORDINAL_POSITION;
