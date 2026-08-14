using CMMS.Data.Connection;
using Dapper;
using System.Threading.Tasks;

namespace CMMS.Server.Infrastructure
{
    public static class RbacDbSeeder
    {
        public static async Task SeedAsync(ISqlConnectionFactory connectionFactory)
        {
            using var connection = connectionFactory.CreateConnection();
            
            var checkSql = "SELECT COUNT(1) FROM sysobjects WHERE name='Tbl_PermissionPages' AND xtype='U'";
            var hasTable = await connection.ExecuteScalarAsync<int>(checkSql);

            if (hasTable > 0)
            {
                var checkMasterData = "SELECT COUNT(1) FROM Tbl_PermissionPages WHERE ModuleCode = 'MASTERDATA'";
                var countMaster = await connection.ExecuteScalarAsync<int>(checkMasterData);
                if (countMaster == 0)
                {
                    await connection.ExecuteAsync(@"
                        DELETE FROM Tbl_RolePermissions;
                        DELETE FROM Tbl_Permissions;
                        DELETE FROM Tbl_PermissionPages;
                        DBCC CHECKIDENT ('Tbl_Permissions', RESEED, 0);
                        DBCC CHECKIDENT ('Tbl_PermissionPages', RESEED, 0);
                    ");
                }
            }

            var sql = @"
                -- 1. Create Tbl_SystemRoles
                IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Tbl_SystemRoles' AND xtype='U')
                BEGIN
                    CREATE TABLE Tbl_SystemRoles (
                        RoleID INT PRIMARY KEY,
                        RoleCode VARCHAR(50) NOT NULL UNIQUE,
                        RoleName NVARCHAR(100) NOT NULL,
                        Description NVARCHAR(255),
                        IsActive BIT DEFAULT 1
                    );
                    
                    INSERT INTO Tbl_SystemRoles (RoleID, RoleCode, RoleName, Description) VALUES
                    (1, 'MANAGER', N'Manager', N'Quản lý chung'),
                    (2, 'USER', N'User', N'Nhân viên'),
                    (3, 'ADMIN', N'Administrator', N'Quản trị hệ thống'),
                    (4, 'IT', N'IT Support', N'Hỗ trợ kỹ thuật');
                END

                -- 2. Create Tbl_PermissionPages
                IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Tbl_PermissionPages' AND xtype='U')
                BEGIN
                    CREATE TABLE Tbl_PermissionPages (
                        PermissionPageID INT IDENTITY(1,1) PRIMARY KEY,
                        ModuleCode VARCHAR(50) NOT NULL,
                        PageCode VARCHAR(50) NOT NULL,
                        PageName NVARCHAR(100) NOT NULL,
                        DisplayOrder INT DEFAULT 0,
                        IsActive BIT DEFAULT 1,
                        UNIQUE(ModuleCode, PageCode)
                    );
                END

                IF NOT EXISTS (SELECT * FROM Tbl_PermissionPages)
                BEGIN
                    INSERT INTO Tbl_PermissionPages (ModuleCode, PageCode, PageName, DisplayOrder) VALUES
                    -- SPAREPART
                    ('SPAREPART', 'INVENTORY', N'Inventory Stock', 1),
                    ('SPAREPART', 'CODED', N'Coded part', 2),
                    ('SPAREPART', 'NONCODED', N'non Coded part', 3),
                    ('SPAREPART', 'INBOUND', N'Inbound', 4),
                    ('SPAREPART', 'OUTBOUND', N'Outbound', 5),
                    ('SPAREPART', 'ADJUSTMENT', N'Stock Adjustments', 6),
                    ('SPAREPART', 'TRANSACTION', N'Transactions Log', 7),
                    
                    -- EQUIPMENT
                    ('EQUIPMENT', 'EQUIPMENT', N'Equipment', 8),

                    -- MAINTENANCE
                    ('MAINTENANCE', 'MAINTENANCE', N'Maintenance', 9),

                    -- MASTER DATA
                    ('MASTERDATA', 'CATALOG', N'Catalog Spare Part', 10),
                    ('MASTERDATA', 'CATEGORY', N'Category', 11),
                    ('MASTERDATA', 'SUPPLIER', N'Supplier', 12),
                    ('MASTERDATA', 'VENDOR', N'Vendor', 13),
                    ('MASTERDATA', 'LOCATION', N'Location', 14),
                    ('MASTERDATA', 'DEPARTMENT', N'Department', 15),
                    ('MASTERDATA', 'FACTORY', N'Factory', 16),
                    ('MASTERDATA', 'USER', N'User', 17),
                    ('MASTERDATA', 'ROLEPERMISSION', N'Roles & Permission', 18);
                END

                -- 3. Create Tbl_Permissions
                IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Tbl_Permissions' AND xtype='U')
                BEGIN
                    CREATE TABLE Tbl_Permissions (
                        PermissionID INT IDENTITY(1,1) PRIMARY KEY,
                        PermissionPageID INT NOT NULL,
                        ActionCode VARCHAR(50) NOT NULL,
                        ActionName NVARCHAR(100) NOT NULL,
                        DisplayOrder INT DEFAULT 0,
                        IsActive BIT DEFAULT 1,
                        FOREIGN KEY (PermissionPageID) REFERENCES Tbl_PermissionPages(PermissionPageID),
                        UNIQUE(PermissionPageID, ActionCode)
                    );
                END

                IF NOT EXISTS (SELECT * FROM Tbl_Permissions)
                BEGIN
                    -- Insert default permissions
                    DECLARE @PageID INT;

                    -- SPAREPART.INVENTORY
                    SELECT @PageID = PermissionPageID FROM Tbl_PermissionPages WHERE ModuleCode = 'SPAREPART' AND PageCode = 'INVENTORY';
                    INSERT INTO Tbl_Permissions (PermissionPageID, ActionCode, ActionName, DisplayOrder) VALUES
                    (@PageID, 'VIEW', N'View', 1), (@PageID, 'DETAIL', N'detail', 2), (@PageID, 'HISTORY', N'History', 3);

                    -- SPAREPART.CODED
                    SELECT @PageID = PermissionPageID FROM Tbl_PermissionPages WHERE ModuleCode = 'SPAREPART' AND PageCode = 'CODED';
                    INSERT INTO Tbl_Permissions (PermissionPageID, ActionCode, ActionName, DisplayOrder) VALUES
                    (@PageID, 'VIEW', N'View', 1);

                    -- SPAREPART.NONCODED
                    SELECT @PageID = PermissionPageID FROM Tbl_PermissionPages WHERE ModuleCode = 'SPAREPART' AND PageCode = 'NONCODED';
                    INSERT INTO Tbl_Permissions (PermissionPageID, ActionCode, ActionName, DisplayOrder) VALUES
                    (@PageID, 'VIEW', N'View', 1);

                    -- SPAREPART.INBOUND
                    SELECT @PageID = PermissionPageID FROM Tbl_PermissionPages WHERE ModuleCode = 'SPAREPART' AND PageCode = 'INBOUND';
                    INSERT INTO Tbl_Permissions (PermissionPageID, ActionCode, ActionName, DisplayOrder) VALUES
                    (@PageID, 'VIEW', N'View', 1), (@PageID, 'DETAIL', N'detail', 2), (@PageID, 'ADD', N'Add', 3);

                    -- SPAREPART.OUTBOUND
                    SELECT @PageID = PermissionPageID FROM Tbl_PermissionPages WHERE ModuleCode = 'SPAREPART' AND PageCode = 'OUTBOUND';
                    INSERT INTO Tbl_Permissions (PermissionPageID, ActionCode, ActionName, DisplayOrder) VALUES
                    (@PageID, 'VIEW', N'View', 1), (@PageID, 'DETAIL', N'detail', 2), (@PageID, 'ADD', N'Add', 3), (@PageID, 'REVERSE', N'Reverse', 4);

                    -- SPAREPART.ADJUSTMENT
                    SELECT @PageID = PermissionPageID FROM Tbl_PermissionPages WHERE ModuleCode = 'SPAREPART' AND PageCode = 'ADJUSTMENT';
                    INSERT INTO Tbl_Permissions (PermissionPageID, ActionCode, ActionName, DisplayOrder) VALUES
                    (@PageID, 'VIEW', N'View', 1), (@PageID, 'DETAIL', N'detail', 2), (@PageID, 'ADD', N'Add', 3);

                    -- SPAREPART.TRANSACTION
                    SELECT @PageID = PermissionPageID FROM Tbl_PermissionPages WHERE ModuleCode = 'SPAREPART' AND PageCode = 'TRANSACTION';
                    INSERT INTO Tbl_Permissions (PermissionPageID, ActionCode, ActionName, DisplayOrder) VALUES
                    (@PageID, 'VIEW', N'View', 1);

                    -- EQUIPMENT.EQUIPMENT
                    SELECT @PageID = PermissionPageID FROM Tbl_PermissionPages WHERE ModuleCode = 'EQUIPMENT' AND PageCode = 'EQUIPMENT';
                    INSERT INTO Tbl_Permissions (PermissionPageID, ActionCode, ActionName, DisplayOrder) VALUES
                    (@PageID, 'VIEW', N'View', 1), (@PageID, 'ADD', N'Add', 2), (@PageID, 'IMPORT', N'Import', 3), (@PageID, 'EDIT', N'Edit', 4);

                    -- MAINTENANCE.MAINTENANCE
                    SELECT @PageID = PermissionPageID FROM Tbl_PermissionPages WHERE ModuleCode = 'MAINTENANCE' AND PageCode = 'MAINTENANCE';
                    INSERT INTO Tbl_Permissions (PermissionPageID, ActionCode, ActionName, DisplayOrder) VALUES
                    (@PageID, 'VIEW', N'View', 1), (@PageID, 'MAINTENANCE', N'Maintenance', 2), (@PageID, 'DETAIL', N'detail', 3);

                    -- MASTERDATA.CATALOG
                    SELECT @PageID = PermissionPageID FROM Tbl_PermissionPages WHERE ModuleCode = 'MASTERDATA' AND PageCode = 'CATALOG';
                    INSERT INTO Tbl_Permissions (PermissionPageID, ActionCode, ActionName, DisplayOrder) VALUES
                    (@PageID, 'VIEW', N'View', 1), (@PageID, 'ADD', N'Add', 2), (@PageID, 'IMPORT', N'Import', 3), (@PageID, 'DETAIL', N'detail', 4), (@PageID, 'EDIT', N'Edit', 5), (@PageID, 'DELETE', N'Delete', 6);

                    -- MASTERDATA.CATEGORY
                    SELECT @PageID = PermissionPageID FROM Tbl_PermissionPages WHERE ModuleCode = 'MASTERDATA' AND PageCode = 'CATEGORY';
                    INSERT INTO Tbl_Permissions (PermissionPageID, ActionCode, ActionName, DisplayOrder) VALUES
                    (@PageID, 'VIEW', N'view', 1), (@PageID, 'ADD', N'Add', 2), (@PageID, 'EDIT', N'Edit', 3), (@PageID, 'DELETE', N'Delete', 4);

                    -- MASTERDATA.SUPPLIER
                    SELECT @PageID = PermissionPageID FROM Tbl_PermissionPages WHERE ModuleCode = 'MASTERDATA' AND PageCode = 'SUPPLIER';
                    INSERT INTO Tbl_Permissions (PermissionPageID, ActionCode, ActionName, DisplayOrder) VALUES
                    (@PageID, 'VIEW', N'view', 1), (@PageID, 'ADD', N'Add', 2), (@PageID, 'EDIT', N'Edit', 3), (@PageID, 'DELETE', N'Delete', 4);

                    -- MASTERDATA.VENDOR
                    SELECT @PageID = PermissionPageID FROM Tbl_PermissionPages WHERE ModuleCode = 'MASTERDATA' AND PageCode = 'VENDOR';
                    INSERT INTO Tbl_Permissions (PermissionPageID, ActionCode, ActionName, DisplayOrder) VALUES
                    (@PageID, 'VIEW', N'view', 1), (@PageID, 'ADD', N'Add', 2), (@PageID, 'EDIT', N'Edit', 3), (@PageID, 'DELETE', N'Delete', 4);

                    -- MASTERDATA.LOCATION
                    SELECT @PageID = PermissionPageID FROM Tbl_PermissionPages WHERE ModuleCode = 'MASTERDATA' AND PageCode = 'LOCATION';
                    INSERT INTO Tbl_Permissions (PermissionPageID, ActionCode, ActionName, DisplayOrder) VALUES
                    (@PageID, 'VIEW', N'view', 1), (@PageID, 'ADD', N'Add', 2), (@PageID, 'EDIT', N'Edit', 3), (@PageID, 'DELETE', N'Delete', 4);

                    -- MASTERDATA.DEPARTMENT
                    SELECT @PageID = PermissionPageID FROM Tbl_PermissionPages WHERE ModuleCode = 'MASTERDATA' AND PageCode = 'DEPARTMENT';
                    INSERT INTO Tbl_Permissions (PermissionPageID, ActionCode, ActionName, DisplayOrder) VALUES
                    (@PageID, 'VIEW', N'view', 1), (@PageID, 'ADD', N'Add', 2), (@PageID, 'EDIT', N'Edit', 3), (@PageID, 'DELETE', N'Delete', 4);

                    -- MASTERDATA.FACTORY
                    SELECT @PageID = PermissionPageID FROM Tbl_PermissionPages WHERE ModuleCode = 'MASTERDATA' AND PageCode = 'FACTORY';
                    INSERT INTO Tbl_Permissions (PermissionPageID, ActionCode, ActionName, DisplayOrder) VALUES
                    (@PageID, 'VIEW', N'view', 1), (@PageID, 'ADD', N'Add', 2), (@PageID, 'EDIT', N'Edit', 3), (@PageID, 'DELETE', N'Delete', 4);

                    -- MASTERDATA.USER
                    SELECT @PageID = PermissionPageID FROM Tbl_PermissionPages WHERE ModuleCode = 'MASTERDATA' AND PageCode = 'USER';
                    INSERT INTO Tbl_Permissions (PermissionPageID, ActionCode, ActionName, DisplayOrder) VALUES
                    (@PageID, 'VIEW', N'view', 1), (@PageID, 'ADD', N'Add', 2), (@PageID, 'EDIT', N'Edit', 3), (@PageID, 'DELETE', N'Delete', 4);

                    -- MASTERDATA.ROLEPERMISSION
                    SELECT @PageID = PermissionPageID FROM Tbl_PermissionPages WHERE ModuleCode = 'MASTERDATA' AND PageCode = 'ROLEPERMISSION';
                    INSERT INTO Tbl_Permissions (PermissionPageID, ActionCode, ActionName, DisplayOrder) VALUES
                    (@PageID, 'VIEW', N'view', 1), (@PageID, 'EDIT', N'Edit', 2);
                END

                -- 4. Create Tbl_RolePermissions
                IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Tbl_RolePermissions' AND xtype='U')
                BEGIN
                    CREATE TABLE Tbl_RolePermissions (
                        RoleID INT NOT NULL,
                        PermissionID INT NOT NULL,
                        FOREIGN KEY (RoleID) REFERENCES Tbl_SystemRoles(RoleID),
                        FOREIGN KEY (PermissionID) REFERENCES Tbl_Permissions(PermissionID),
                        UNIQUE(RoleID, PermissionID)
                    );
                END

                IF NOT EXISTS (SELECT * FROM Tbl_RolePermissions)
                BEGIN
                    -- Default grant FULL PERMISSIONS to ADMIN (RoleID = 3)
                    INSERT INTO Tbl_RolePermissions (RoleID, PermissionID)
                    SELECT 3, p.PermissionID 
                    FROM Tbl_Permissions p;
                END
            ";

            await connection.ExecuteAsync(sql);
        }
    }
}
