using AntDesign;
using CMMS.Server.Services.DepartmentService;
using CMMS.Server.Services.EquipmentService;

namespace CMMS.Server
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddAppServices(this IServiceCollection services)
        {
            services.AddHttpContextAccessor();

            services.AddScoped<IEquipmentService, EquipmentService>();
            services.AddScoped<IDepartmentService, DepartmentService>();

            return services;
        }
    }
}
