using CMMS.Shared.Dtos.AuthModels;

namespace CMMS.Server.Services.Auth
{
    public interface IDataPermissionService
    {
        DataPermission GetPermission();
    }

    public class DataPermissionService : IDataPermissionService
    {
        private readonly ICurrentUser _currentUser;

        public DataPermissionService(ICurrentUser currentUser)
        {
            _currentUser = currentUser;
        }

        public DataPermission GetPermission()
        {
            if (_currentUser.HasGlobalView)
            {
                return new DataPermission
                {
                    IsGlobal = true
                };
            }

            return new DataPermission
            {
                IsGlobal = false,
                FacId = _currentUser.FacId,
                DeptId = _currentUser.DeptId
            };
        }
    }
}
