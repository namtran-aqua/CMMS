using CMMS.Data.Connection;
using CMMS.Server.Services.UserService;
using CMMS.Shared.Dtos.AuthModels;
using CMMS.Shared.Dtos.User;
using CMMS.Shared.PasswordHelpers;
using CMMS.Server.Services.EmailService;
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
        private readonly IEmailService _emailService;
        public UserService(IConfiguration config, ISqlConnectionFactory connectionFactory, IHttpContextAccessor httpContextAccessor, IEmailService emailService)
        {
            _config = config;
            _connectionFactory = connectionFactory;
            _httpContextAccessor = httpContextAccessor;
            _emailService = emailService;
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
            using var connection = _connectionFactory.CreateSolutionConnection();

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
            using var connection = _connectionFactory.CreateSolutionConnection();

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

        public async Task<bool> SendOtpAsync(ForgotPasswordRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.WorkDayId) || string.IsNullOrWhiteSpace(request.Email))
                throw new Exception("WorkDay ID and Email are required.");

            using var connection = _connectionFactory.CreateSolutionConnection();
            connection.Open();

            // Step 1: Validate User
            const string getUserSql = @"
                SELECT Id, WorkDayId, Email, IsDeleted, IsActive
                FROM Admin.tbl_Users
                WHERE WorkDayId = @WorkDayId AND IsDeleted = 0 AND IsActive = 1";

            var user = await connection.QueryFirstOrDefaultAsync<UserDto>(
                getUserSql,
                new { WorkDayId = request.WorkDayId }
            );

            if (user == null || !string.Equals(user.Email?.Trim(), request.Email?.Trim(), StringComparison.OrdinalIgnoreCase))
                throw new Exception("Invalid WorkDay ID or Email.");

            // Step 2: Rate limit check (max 5 OTPs per User in 15 minutes)
            const string checkRateLimitSql = @"
                SELECT COUNT(*)
                FROM Admin.Tbl_PasswordReset
                WHERE UserID = @UserId
                  AND CreatedDate > DATEADD(minute, -15, GETDATE())";

            var requestCount = await connection.ExecuteScalarAsync<int>(
                checkRateLimitSql,
                new { UserId = user.Id }
            );

            if (requestCount >= 5)
                throw new Exception("Too many password reset requests. Please try again later.");

            // Step 3: Generate 6-digit OTP
            var random = new Random();
            var otp = random.Next(100000, 999999).ToString();
            var otpHash = PasswordHelper.ComputeSha256Hash(otp);

            // Step 4: Send Email first
            var subject = "CMMS - Password Reset Verification Code";
            var bodyEmail = new StringBuilder();
            bodyEmail.AppendLine("<div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px; border: 1px solid #e0e0e0; border-radius: 8px;'>");
            bodyEmail.AppendLine("<h2 style='color: #1677ff; margin-bottom: 20px;'>CMMS Account Verification</h2>");
            bodyEmail.AppendLine("<p>We received a request to reset the password for your CMMS account.</p>");
            bodyEmail.AppendLine("<p>Please use the following 6-digit verification code (OTP) to complete your request:</p>");
            bodyEmail.AppendLine($"<div style='background-color: #f5f5f5; padding: 15px; text-align: center; font-size: 24px; font-weight: bold; letter-spacing: 5px; color: #1677ff; border-radius: 4px; margin: 20px 0;'>{otp}</div>");
            bodyEmail.AppendLine("<p style='color: #ff4d4f; font-weight: bold;'>Note: This verification code is only valid for 5 minutes and can be used only once.</p>");
            bodyEmail.AppendLine("<p>If you did not request this, please ignore this email or contact support if you have security concerns.</p>");
            bodyEmail.AppendLine("<hr style='border: none; border-top: 1px solid #e0e0e0; margin: 20px 0;' />");
            bodyEmail.AppendLine("<p style='font-size: 12px; color: #8c8c8c;'>This is an automated message from the CMMS System. Please do not reply directly to this email.</p>");
            bodyEmail.AppendLine("</div>");

            var isSent = await _emailService.SendEmailAsync(user.Email, subject, bodyEmail.ToString());
            if (!isSent)
            {
                throw new Exception("Failed to send verification email. Please contact support or try again later.");
            }

            // Step 5: Save OTP to database (transaction / database save after email succeeds)
            using var transaction = connection.BeginTransaction();
            try
            {
                // Invalidate any previous active OTPs
                const string invalidateSql = @"
                    UPDATE Admin.Tbl_PasswordReset
                    SET IsUsed = 1
                    WHERE UserID = @UserId AND IsUsed = 0";

                await connection.ExecuteAsync(invalidateSql, new { UserId = user.Id }, transaction);

                // Insert new OTP
                const string insertSql = @"
                    INSERT INTO Admin.Tbl_PasswordReset (UserID, OtpHash, OtpExpiredAt, IsUsed, CreatedDate)
                    VALUES (@UserId, @OtpHash, DATEADD(minute, 5, GETDATE()), 0, GETDATE())";

                await connection.ExecuteAsync(insertSql, new { UserId = user.Id, OtpHash = otpHash }, transaction);

                transaction.Commit();
                return true;
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        public async Task<string> VerifyOtpAsync(VerifyOtpRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.WorkDayId) || string.IsNullOrWhiteSpace(request.Otp))
                throw new Exception("WorkDay ID and OTP are required.");

            using var connection = _connectionFactory.CreateSolutionConnection();
            connection.Open();

            // Step 1: Validate User
            const string getUserSql = @"
                SELECT Id FROM Admin.tbl_Users
                WHERE WorkDayId = @WorkDayId AND IsDeleted = 0 AND IsActive = 1";

            var user = await connection.QueryFirstOrDefaultAsync<UserDto>(
                getUserSql,
                new { WorkDayId = request.WorkDayId }
            );

            if (user == null)
                throw new Exception("Invalid or expired verification code.");

            // Step 2: Get active OTP record
            const string getOtpSql = @"
                SELECT TOP 1 ID, UserID, OtpHash, Attempts, OtpExpiredAt, IsUsed
                FROM Admin.Tbl_PasswordReset
                WHERE UserID = @UserId AND IsUsed = 0 AND OtpExpiredAt > GETDATE()
                ORDER BY CreatedDate DESC";

            var resetRecord = await connection.QueryFirstOrDefaultAsync<dynamic>(
                getOtpSql,
                new { UserId = user.Id }
            );

            if (resetRecord == null)
                throw new Exception("Invalid or expired verification code.");

            int attempts = resetRecord.Attempts;
            int resetRecordId = resetRecord.ID;

            if (attempts >= 5)
            {
                // Invalidate this OTP if attempts >= 5
                const string lockOtpSql = "UPDATE Admin.Tbl_PasswordReset SET IsUsed = 1 WHERE ID = @Id";
                await connection.ExecuteAsync(lockOtpSql, new { Id = resetRecordId });
                throw new Exception("Invalid or expired verification code.");
            }

            var inputHash = PasswordHelper.ComputeSha256Hash(request.Otp);
            string dbHash = resetRecord.OtpHash;

            if (!string.Equals(dbHash, inputHash, StringComparison.OrdinalIgnoreCase))
            {
                // Increment attempts
                attempts++;
                if (attempts >= 5)
                {
                    const string lockOtpSql = "UPDATE Admin.Tbl_PasswordReset SET IsUsed = 1, Attempts = @Attempts WHERE ID = @Id";
                    await connection.ExecuteAsync(lockOtpSql, new { Id = resetRecordId, Attempts = attempts });
                }
                else
                {
                    const string incAttemptsSql = "UPDATE Admin.Tbl_PasswordReset SET Attempts = @Attempts WHERE ID = @Id";
                    await connection.ExecuteAsync(incAttemptsSql, new { Id = resetRecordId, Attempts = attempts });
                }
                throw new Exception("Invalid or expired verification code.");
            }

            // Correct OTP: Generate ResetToken and update record
            var resetToken = Guid.NewGuid().ToString();

            const string updateTokenSql = @"
                UPDATE Admin.Tbl_PasswordReset
                SET ResetToken = @ResetToken,
                    ResetTokenExpiredAt = DATEADD(minute, 15, GETDATE())
                WHERE ID = @Id";

            await connection.ExecuteAsync(updateTokenSql, new { ResetToken = resetToken, Id = resetRecordId });

            return resetToken;
        }

        public async Task<bool> ResetPasswordWithTokenAsync(ResetPasswordRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.ResetToken))
                throw new Exception("Reset token is required.");

            if (string.IsNullOrWhiteSpace(request.NewPassword))
                throw new Exception("New password cannot be blank.");

            if (request.NewPassword != request.ConfirmPassword)
                throw new Exception("Confirm password does not match the new password.");

            using var connection = _connectionFactory.CreateSolutionConnection();
            connection.Open();

            // Step 1: Validate active ResetToken
            const string getRecordSql = @"
                SELECT TOP 1 ID, UserID
                FROM Admin.Tbl_PasswordReset
                WHERE ResetToken = @ResetToken
                  AND IsUsed = 0
                  AND ResetTokenExpiredAt > GETDATE()";

            var resetRecord = await connection.QueryFirstOrDefaultAsync<dynamic>(
                getRecordSql,
                new { ResetToken = request.ResetToken }
            );

            if (resetRecord == null)
                throw new Exception("Invalid or expired reset token.");

            Guid userId = resetRecord.UserID;
            int recordId = resetRecord.ID;

            // Step 2: Validate User
            const string getUserSql = @"
                SELECT Id, IsDeleted, IsActive
                FROM Admin.tbl_Users
                WHERE Id = @UserId";

            var user = await connection.QueryFirstOrDefaultAsync<UserDto>(
                getUserSql,
                new { UserId = userId }
            );

            if (user == null || user.IsDeleted || !user.IsActive)
                throw new Exception("User does not exist or has been deactivated.");

            // Step 3: Hash password and update inside transaction
            var newHashedPassword = PasswordHelper.HashPassword(request.NewPassword);

            using var transaction = connection.BeginTransaction();
            try
            {
                const string updatePassSql = @"
                    UPDATE Admin.tbl_Users
                    SET PasswordHash = @PasswordHash,
                        UpdatedTime = GETDATE()
                    WHERE Id = @UserId";

                await connection.ExecuteAsync(updatePassSql, new { PasswordHash = newHashedPassword, UserId = userId }, transaction);

                const string markUsedSql = @"
                    UPDATE Admin.Tbl_PasswordReset
                    SET IsUsed = 1
                    WHERE ID = @Id";

                await connection.ExecuteAsync(markUsedSql, new { Id = recordId }, transaction);

                transaction.Commit();
                return true;
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
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
