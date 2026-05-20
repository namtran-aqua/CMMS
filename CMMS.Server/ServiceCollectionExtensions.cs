using AntDesign;
using CMMS.Data.Connection;
using CMMS.Server.Services.DashBoardService;
using CMMS.Server.Services.DepartmentService;
using CMMS.Server.Services.EquipmentService;

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


            return services;
        }
    }
}
