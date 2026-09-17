using AntDesign;
using CMMS.Data.Connection;
using CMMS.Server.Services.DailyJobService;
using CMMS.Server.Services.DashBoardService;
using CMMS.Server.Services.DepartmentService;
using CMMS.Server.Services.EquipmentService;
using CMMS.Server.Services.LocationService;
using CMMS.Server.Services.MaintenanceService;
using CMMS.Server.Services.StatusUsingService;
using CMMS.Server.Services.UserService;
using CMMS.Server.Services.VendorService;
using CMMS.Server.Services.SparePartService;
using CMMS.Server.Services.EmailService;
using CMMS.Server.Services.FactoryService;
using CMMS.Server.Services.Auth;
using CMMS.Server.Services.Barcode;

namespace CMMS.Server
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddAppServices(this IServiceCollection services)
        {
            services.AddHttpContextAccessor();
            #region DB
            services.AddScoped<ISqlConnectionFactory, SqlConnectionFactory>();
            #endregion
            services.AddScoped<IEquipmentService, EquipmentService>();
            services.AddScoped<IDepartmentService, DepartmentService>();
            services.AddScoped<IDashBoardService, DashBoardService>();
            services.AddScoped<ISqlConnectionFactory, SqlConnectionFactory>();
            services.AddScoped<IVendorService, VendorService>();
            services.AddScoped<IStatusUsingService, StatusUsingService>();
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<IMaintenanceService, MaintenanceService>();
            services.AddScoped<IDailyJobService, DailyJobService>();
            services.AddScoped<ILocationService, LocationService>();
            services.AddScoped<ISparePartService, SparePartService>();
            services.AddScoped<IEmailService, EmailService>();
            services.AddScoped<IFactoryService, FactoryService>();
            
            services.AddScoped<ICurrentUser, CurrentUser>();
            services.AddScoped<IDataPermissionService, DataPermissionService>();
            
            services.AddScoped<IBarcodeIdService, BarcodeIdService>();
            services.AddScoped<IQRCodeService, QRCodeService>();
            
            // Spare Part Dashboard Services
            services.AddScoped<CMMS.Server.Services.SparePartDashboardService.ISparePartDashboardService, CMMS.Server.Services.SparePartDashboardService.SparePartDashboardService>();
            services.AddScoped<CMMS.Server.Services.SparePartDashboardService.IInventoryAnalyticsService, CMMS.Server.Services.SparePartDashboardService.InventoryAnalyticsService>();
            services.AddScoped<CMMS.Server.Services.SparePartDashboardService.IInventoryKpiService, CMMS.Server.Services.SparePartDashboardService.InventoryKpiService>();
            services.AddScoped<CMMS.Server.Services.SparePartDashboardService.IInventoryAlertService, CMMS.Server.Services.SparePartDashboardService.InventoryAlertService>();
            
            // RBAC Permission Engine
            services.AddScoped<CMMS.Server.Services.PermissionService.IPermissionService, CMMS.Server.Services.PermissionService.PermissionService>();
            services.AddSingleton<Microsoft.AspNetCore.Authorization.IAuthorizationPolicyProvider, CMMS.Server.Infrastructure.Authorization.PermissionPolicyProvider>();
            services.AddScoped<Microsoft.AspNetCore.Authorization.IAuthorizationHandler, CMMS.Server.Infrastructure.Authorization.PermissionAuthorizationHandler>();
            
            return services;
        }
    }
}
