using CMMS.Data.Connection;
using CMMS.Server.Services.UserService;
using CMMS.Shared.Dtos.AuthModels;
using CMMS.Shared.Dtos.User;
using CMMS.Shared.PasswordHelpers;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.IdentityModel.Tokens;
using NPOI.SS.Formula.Functions;
using System.Data;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace CMMS.Server.Services.UserService
{
    public class UserService : IUserService
    {
        private readonly IConfiguration _config;
        private readonly ISqlConnectionFactory _connectionFactory;
        private readonly IHttpContextAccessor _httpContextAccessor;
        public UserService(IConfiguration config, ISqlConnectionFactory connectionFactory , IHttpContextAccessor httpContextAccessor)
        {
            _config = config;
            _connectionFactory = connectionFactory;
            _httpContextAccessor = httpContextAccessor;
        }
        public async Task<List<UserDto>> GetUsersAsync()
        {
            using var con = new SqlConnection(_config.GetConnectionString("SolutionConnection"));
            // JOIN cross-database với Tbl_User trong CMMS để lấy FACID, DeptID, LocID, RoleID
            const string sql = @"
                SELECT
                    u.Id,
                    u.WorkDayId,
                    u.FullName,
                    cu.FACID,
                    cu.DeptID,
                    cu.LocID,
                    cu.RoleID
                FROM Admin.tbl_Users u
                LEFT JOIN CMMS.dbo.Tbl_User cu ON cu.Id = u.Id
                WHERE u.IsActive = 1";

            var result = await con.QueryAsync<UserDto>(sql);
            var list = result.AsList();
            foreach (var user in list)
            {
                user.Roles = new List<string>();
                if (user.RoleID == 1) user.Roles.Add("Manager");
                else if (user.RoleID == 2) user.Roles.Add("User");
            }
            return list;
        }
        public async Task<LoginResponse?> LoginAsync(LoginRequest loginRequest)
        {
            using var con = new SqlConnection(_config.GetConnectionString("SolutionConnection"));

            const string sql = @"
        SELECT Id,
               WorkDayId,
               FullName,
               PasswordHash,
               IsDeleted,
               IsActive
        FROM Admin.tbl_Users
        WHERE WorkDayId = @WorkDayId
            AND IsDeleted = 0
            AND IsActive = 1";

            using var cmd = new SqlCommand(sql, con);

            cmd.Parameters.AddWithValue("@WorkDayId", loginRequest.UserName);

            await con.OpenAsync();

            using var reader = await cmd.ExecuteReaderAsync();

            if (!await reader.ReadAsync())
                return null;

            var user = new UserDto
            {
                Id = (Guid)reader["Id"],
                WorkDayId = reader["WorkDayId"].ToString(),
                FullName = reader["FullName"].ToString(),
                PasswordHash = reader["PasswordHash"].ToString()
            };
            await reader.CloseAsync();


            // Validate password
            if (!PasswordHelper.VerifyPassword(
                    user.PasswordHash,
                    loginRequest.Password))
            {
                return null;
            }
            var userId = user.Id;
            using var cmmsCon = new SqlConnection(
                _config.GetConnectionString("DefaultConnection"));

            const string checkSql = @"
            SELECT
                Id,
                FACID,
                DeptID,
                LocID,
                RoleID
            FROM Tbl_User
            WHERE Id = @Id";

            using var checkCmd = new SqlCommand(checkSql, cmmsCon);

            checkCmd.Parameters.AddWithValue("@Id", userId);

            await cmmsCon.OpenAsync();

            using var cmmsReader = await checkCmd.ExecuteReaderAsync();

            if (!await cmmsReader.ReadAsync())
            {
                return null;
            }

            var facId = cmmsReader["FACID"]?.ToString();
            var deptId = cmmsReader["DeptID"]?.ToString();
            var locId = cmmsReader["LocID"]?.ToString();
            var roleId = cmmsReader["RoleID"]?.ToString();

            // Generate JWT
            var jwtKey = _config["Jwt:Key"];

            if (string.IsNullOrEmpty(jwtKey))
                throw new InvalidOperationException("JWT key not configured");

            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtKey));

            var creds = new SigningCredentials(
                key,
                SecurityAlgorithms.HmacSha256);

            var expires = DateTime.UtcNow.AddMinutes(60);

            var claimsList = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.SerialNumber, user.WorkDayId),
                new Claim(ClaimTypes.Name, user.FullName),

                new Claim("FACID", facId ?? ""),
                new Claim("DeptID", deptId ?? ""),
                new Claim("LocID", locId ?? ""),
                new Claim("RoleID", roleId ?? "")
            };

            if (roleId == "1")
            {
                claimsList.Add(new Claim(ClaimTypes.Role, "Manager"));
            }
            else if (roleId == "2")
            {
                claimsList.Add(new Claim(ClaimTypes.Role, "User"));
            }

            var token = new JwtSecurityToken(
                claims: claimsList,
                expires: expires,
                signingCredentials: creds
            );

            return new LoginResponse
            {
                Token = new JwtSecurityTokenHandler().WriteToken(token),
                Expiration = expires
            };
        }
        public async Task<bool> ChangePasswordAsync(ChangePassRequest changePassRequest)
        {
            using var connection = _connectionFactory.CreateConnection();

            // ✅ Step 1: Get user by Id
            const string getUserSql = @"
        SELECT Id, WorkDayId, PasswordHash, IsDeleted, IsActive
        FROM Admin.tbl_Users
        WHERE Id = @UserId";

            var user = await connection.QueryFirstOrDefaultAsync<UserDto>(
                getUserSql,
                new { UserId = changePassRequest.UserId }
            );

            if (user == null || user.IsDeleted || !user.IsActive)
                throw new Exception("User does not exist or has been deactivated.");

            // ✅ Step 2: Check old password
            var isOldPasswordCorrect = PasswordHelper.VerifyPassword(
                user.PasswordHash,
                changePassRequest.OldPassword
            );

            if (!isOldPasswordCorrect)
                throw new Exception("Old password is incorrect.");

            // ✅ Step 3: Validate confirm password
            if (changePassRequest.NewPassword != changePassRequest.ConfirmPassword)
                throw new Exception("Confirm password does not match the new password.");

            // ✅ Step 4: Hash new password
            var newHashedPassword = PasswordHelper.HashPassword(changePassRequest.NewPassword);

            // ✅ Step 5: Update DB
            const string updateSql = @"
        UPDATE Admin.tbl_Users
        SET PasswordHash = @PasswordHash,
            UpdatedTime = GETDATE()
        WHERE Id = @UserId";

            var rowsAffected = await connection.ExecuteAsync(updateSql, new
            {
                PasswordHash = newHashedPassword,
                UserId = changePassRequest.UserId
            });

            return rowsAffected > 0;
        }
        public async Task<bool> ResetPasswordAsync(ResetPassword request)
        {
            using var connection = _connectionFactory.CreateConnection();

            // ✅ Step 1: Get user
            const string getUserSql = @"
        SELECT Id, PasswordHash, IsDeleted, IsActive
        FROM Admin.tbl_Users
        WHERE Id = @UserId";

            var user = await connection.QueryFirstOrDefaultAsync<UserDto>(
                getUserSql,
                new { UserId = request.UserId }
            );

            // ✅ Validate user
            if (user == null || user.IsDeleted || !user.IsActive)
                throw new Exception("User does not exist or has been deactivated.");

            // ✅ Step 2: Check confirm password
            if (request.NewPassword != request.ConfirmPassword)
                throw new Exception("Confirm password does not match the new password.");

            // ✅ Step 3: Hash password
            var newHashedPassword = PasswordHelper.HashPassword(request.NewPassword);

            // ✅ Step 4: Update DB
            const string updateSql = @"
        UPDATE Admin.tbl_Users
        SET PasswordHash = @PasswordHash,
            UpdatedTime = GETDATE()
        WHERE Id = @UserId";

            var rowsAffected = await connection.ExecuteAsync(updateSql, new
            {
                PasswordHash = newHashedPassword,
                UserId = request.UserId
            });

            return rowsAffected > 0;
        }
        public async Task<UserDto?> GetCurrentUserAsync(Guid userId)
        {
            try
            {
                using var con = new SqlConnection(
                    _config.GetConnectionString("SolutionConnection"));

                // JOIN cross-database với Tbl_User trong CMMS để lấy FACID, DeptID, LocID, RoleID
                const string sql = @"
                    SELECT
                        u.Id,
                        u.WorkDayId,
                        u.FullName,
                        cu.FACID,
                        cu.DeptID,
                        cu.LocID,
                        cu.RoleID
                    FROM Admin.tbl_Users u
                    LEFT JOIN CMMS.dbo.Tbl_User cu ON cu.Id = u.Id
                    WHERE u.Id = @Id";

                var user = await con.QueryFirstOrDefaultAsync<UserDto>(
                    sql,
                    new { Id = userId }
                );

                if (user != null)
                {
                    user.Roles = new List<string>();
                    if (user.RoleID == 1) user.Roles.Add("Manager");
                    else if (user.RoleID == 2) user.Roles.Add("User");
                }

                return user;
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}
