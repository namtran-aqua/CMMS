namespace CMMS.Shared.Authorization
{
    public static class Permissions
    {
        // MODULE.PAGE.ACTION format
        
        // --- SPARE PART MODULE ---
        // Inventory Stock
        public const string SparePartInventoryView = "SPAREPART.INVENTORY.VIEW";
        public const string SparePartInventoryDetail = "SPAREPART.INVENTORY.DETAIL";
        public const string SparePartInventoryHistory = "SPAREPART.INVENTORY.HISTORY";

        // Coded part
        public const string SparePartCodedView = "SPAREPART.CODED.VIEW";

        // non Coded part
        public const string SparePartNonCodedView = "SPAREPART.NONCODED.VIEW";

        // Inbound
        public const string SparePartInboundView = "SPAREPART.INBOUND.VIEW";
        public const string SparePartInboundDetail = "SPAREPART.INBOUND.DETAIL";
        public const string SparePartInboundAdd = "SPAREPART.INBOUND.ADD";

        // Outbound
        public const string SparePartOutboundView = "SPAREPART.OUTBOUND.VIEW";
        public const string SparePartOutboundDetail = "SPAREPART.OUTBOUND.DETAIL";
        public const string SparePartOutboundAdd = "SPAREPART.OUTBOUND.ADD";
        public const string SparePartOutboundReverse = "SPAREPART.OUTBOUND.REVERSE";

        // Stock Adjustments
        public const string SparePartAdjustmentView = "SPAREPART.ADJUSTMENT.VIEW";
        public const string SparePartAdjustmentDetail = "SPAREPART.ADJUSTMENT.DETAIL";
        public const string SparePartAdjustmentAdd = "SPAREPART.ADJUSTMENT.ADD";

        // Transactions Log
        public const string SparePartTransactionsView = "SPAREPART.TRANSACTION.VIEW";

        // --- EQUIPMENT MODULE ---
        public const string EquipmentView = "EQUIPMENT.EQUIPMENT.VIEW";
        public const string EquipmentAdd = "EQUIPMENT.EQUIPMENT.ADD";
        public const string EquipmentImport = "EQUIPMENT.EQUIPMENT.IMPORT";
        public const string EquipmentEdit = "EQUIPMENT.EQUIPMENT.EDIT";

        // --- MAINTENANCE MODULE ---
        public const string MaintenanceView = "MAINTENANCE.MAINTENANCE.VIEW";
        public const string MaintenanceAction = "MAINTENANCE.MAINTENANCE.MAINTENANCE";
        public const string MaintenanceDetail = "MAINTENANCE.MAINTENANCE.DETAIL";

        // --- MASTER DATA MODULE ---
        // Catalog Spare Part
        public const string MasterDataCatalogView = "MASTERDATA.CATALOG.VIEW";
        public const string MasterDataCatalogAdd = "MASTERDATA.CATALOG.ADD";
        public const string MasterDataCatalogImport = "MASTERDATA.CATALOG.IMPORT";
        public const string MasterDataCatalogDetail = "MASTERDATA.CATALOG.DETAIL";
        public const string MasterDataCatalogEdit = "MASTERDATA.CATALOG.EDIT";
        public const string MasterDataCatalogDelete = "MASTERDATA.CATALOG.DELETE";

        // Category
        public const string MasterDataCategoryView = "MASTERDATA.CATEGORY.VIEW";
        public const string MasterDataCategoryAdd = "MASTERDATA.CATEGORY.ADD";
        public const string MasterDataCategoryEdit = "MASTERDATA.CATEGORY.EDIT";
        public const string MasterDataCategoryDelete = "MASTERDATA.CATEGORY.DELETE";

        // Supplier
        public const string MasterDataSupplierView = "MASTERDATA.SUPPLIER.VIEW";
        public const string MasterDataSupplierAdd = "MASTERDATA.SUPPLIER.ADD";
        public const string MasterDataSupplierEdit = "MASTERDATA.SUPPLIER.EDIT";
        public const string MasterDataSupplierDelete = "MASTERDATA.SUPPLIER.DELETE";

        // Vendor
        public const string MasterDataVendorView = "MASTERDATA.VENDOR.VIEW";
        public const string MasterDataVendorAdd = "MASTERDATA.VENDOR.ADD";
        public const string MasterDataVendorEdit = "MASTERDATA.VENDOR.EDIT";
        public const string MasterDataVendorDelete = "MASTERDATA.VENDOR.DELETE";

        // Location
        public const string MasterDataLocationView = "MASTERDATA.LOCATION.VIEW";
        public const string MasterDataLocationAdd = "MASTERDATA.LOCATION.ADD";
        public const string MasterDataLocationEdit = "MASTERDATA.LOCATION.EDIT";
        public const string MasterDataLocationDelete = "MASTERDATA.LOCATION.DELETE";

        // Department
        public const string MasterDataDepartmentView = "MASTERDATA.DEPARTMENT.VIEW";
        public const string MasterDataDepartmentAdd = "MASTERDATA.DEPARTMENT.ADD";
        public const string MasterDataDepartmentEdit = "MASTERDATA.DEPARTMENT.EDIT";
        public const string MasterDataDepartmentDelete = "MASTERDATA.DEPARTMENT.DELETE";

        // Factory
        public const string MasterDataFactoryView = "MASTERDATA.FACTORY.VIEW";
        public const string MasterDataFactoryAdd = "MASTERDATA.FACTORY.ADD";
        public const string MasterDataFactoryEdit = "MASTERDATA.FACTORY.EDIT";
        public const string MasterDataFactoryDelete = "MASTERDATA.FACTORY.DELETE";

        // User
        public const string MasterDataUserView = "MASTERDATA.USER.VIEW";
        public const string MasterDataUserAdd = "MASTERDATA.USER.ADD";
        public const string MasterDataUserEdit = "MASTERDATA.USER.EDIT";
        public const string MasterDataUserDelete = "MASTERDATA.USER.DELETE";

        // Roles & Permission
        public const string MasterDataRolePermissionView = "MASTERDATA.ROLEPERMISSION.VIEW";
        public const string MasterDataRolePermissionEdit = "MASTERDATA.ROLEPERMISSION.EDIT";
    }
}
