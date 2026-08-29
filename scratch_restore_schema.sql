SET QUOTED_IDENTIFIER ON;
GO

IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Tbl_BarcodeSequence' and xtype='U')
BEGIN
    CREATE TABLE Tbl_BarcodeSequence (
        Department VARCHAR(10) NOT NULL,
        ItemType VARCHAR(10) NOT NULL,
        LastNumber INT NOT NULL DEFAULT 0,
        PRIMARY KEY (Department, ItemType)
    );
END
GO

IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Tbl_SparePart' AND COLUMN_NAME = 'SparePartBarcode')
BEGIN
    ALTER TABLE Tbl_SparePart ADD SparePartBarcode VARCHAR(50) NULL;
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'UQ_SparePartBarcode_Filtered')
BEGIN
    CREATE UNIQUE NONCLUSTERED INDEX UQ_SparePartBarcode_Filtered ON Tbl_SparePart(SparePartBarcode) WHERE SparePartBarcode IS NOT NULL;
END
GO

IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Tbl_EquipmentInfo' AND COLUMN_NAME = 'EquipmentBarcode')
BEGIN
    ALTER TABLE Tbl_EquipmentInfo ADD EquipmentBarcode VARCHAR(50) NULL;
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'UQ_EquipmentBarcode_Filtered')
BEGIN
    CREATE UNIQUE NONCLUSTERED INDEX UQ_EquipmentBarcode_Filtered ON Tbl_EquipmentInfo(EquipmentBarcode) WHERE EquipmentBarcode IS NOT NULL;
END
GO
