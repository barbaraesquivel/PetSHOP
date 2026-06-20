-- =============================================================
-- Migracion PetShop: eliminacion del sistema de backup en tablas
-- Ejecutar UNA sola vez contra la base de datos PetShop
-- antes de desplegar la nueva version del sistema.
-- =============================================================

BEGIN TRANSACTION;

-- 1. Verificacion y DROP de tablas de backup
--    Orden FK-safe: DetallePedido_Backup antes que Pedidos_Backup antes que Usuarios_Backup

IF OBJECT_ID('dbo.DetallePedido_Backup', 'U') IS NOT NULL
    DROP TABLE [dbo].[DetallePedido_Backup];

IF OBJECT_ID('dbo.Pedidos_Backup', 'U') IS NOT NULL
    DROP TABLE [dbo].[Pedidos_Backup];

IF OBJECT_ID('dbo.Productos_Backup', 'U') IS NOT NULL
    DROP TABLE [dbo].[Productos_Backup];

IF OBJECT_ID('dbo.Usuarios_Backup', 'U') IS NOT NULL
    DROP TABLE [dbo].[Usuarios_Backup];

IF OBJECT_ID('dbo.Backup_Info', 'U') IS NOT NULL
    DROP TABLE [dbo].[Backup_Info];

-- 2. Stored procedures del sistema anterior

IF OBJECT_ID('dbo.SP_HacerBackup', 'P') IS NOT NULL
    DROP PROCEDURE [dbo].[SP_HacerBackup];

IF OBJECT_ID('dbo.SP_RestaurarBD', 'P') IS NOT NULL
    DROP PROCEDURE [dbo].[SP_RestaurarBD];

COMMIT TRANSACTION;

-- 3. Confirmar tablas restantes
SELECT TABLE_NAME
FROM INFORMATION_SCHEMA.TABLES
WHERE TABLE_TYPE = 'BASE TABLE'
ORDER BY TABLE_NAME;
