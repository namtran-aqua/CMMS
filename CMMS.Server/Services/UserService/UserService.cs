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
            var list = new List<UserDto>();
            using var con = new SqlConnection(_config.GetConnectionString("SolutionConnection"));
            var sql = "SELECT * FROM Admin.tbl_Users";
            using var cmd = new SqlCommand(sql, con);
            await con.OpenAsync();
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(new UserDto
                {
                    Id = (Guid)reader["Id"],
                    WorkDayId = reader["WorkDayId"].ToString(),
                    FullName = reader["FullName"].ToString()
                });
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

            // Validate password
            if (!PasswordHelper.VerifyPassword(
                    user.PasswordHash,
                    loginRequest.Password))
            {
                return null;
            }

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

            var claims = new[]
            {
        new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
        new Claim(ClaimTypes.SerialNumber, user.WorkDayId),
        new Claim(ClaimTypes.Name, user.FullName)

    };

            var token = new JwtSecurityToken(
                claims: claims,
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

                const string sql = @"
            SELECT Id,
                   WorkDayId,
                   FullName
            FROM Admin.tbl_Users
            WHERE Id = @Id";

                using var cmd = new SqlCommand(sql, con);

                cmd.Parameters.Add("@Id", SqlDbType.UniqueIdentifier)
                    .Value = userId;

                await con.OpenAsync();

                using var reader = await cmd.ExecuteReaderAsync();

                if (!await reader.ReadAsync())
                    return null;

                return new UserDto
                {
                    Id = (Guid)reader["Id"],
                    WorkDayId = reader["WorkDayId"]?.ToString(),
                    FullName = reader["FullName"]?.ToString()
                };
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}
