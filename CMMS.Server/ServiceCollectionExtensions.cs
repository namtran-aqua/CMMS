using AntDesign;
using CMMS.Data.Connection;
using CMMS.Server.Services.DashBoardService;
using CMMS.Data.Connection;
using CMMS.Server.Services.DepartmentService;
using CMMS.Server.Services.EquipmentService;
using CMMS.Server.Services.VendorService;
using CMMS.Server.Services.StatusUsingService;
using CMMS.Server.Services.UserService;
using CMMS.Server.Services.MaintenanceService;

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
            return services;
        }
    }
}
