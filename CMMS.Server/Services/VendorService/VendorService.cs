using CMMS.Server.Services.VendorService;
using Microsoft.Data.SqlClient;
using Dapper;
using CMMS.Shared.Dtos.Equipment;
using CMMS.Shared.Dtos.Common;

namespace CMMS.Server.Services.VendorService
{
    public class VendorService : IVendorService
    {
        private readonly IConfiguration _config;
        public VendorService(IConfiguration config)
        {
            _config = config;
        }

        public async Task<List<VendorDto>> GetVendorsAsync()
        {
            var sql = "SELECT * FROM dbo.Tbl_Vendors";
            using var con = new SqlConnection(_config.GetConnectionString("DefaultConnection"));
            var list = await con.QueryAsync<VendorDto>(sql);
            return list.ToList();
        }

        public async Task<VendorDto> GetVendorAsync(int id)
        {
            var sql = "SELECT * FROM dbo.Tbl_Vendors WHERE VendorID = @Id";
            using var con = new SqlConnection(_config.GetConnectionString("DefaultConnection"));
            return await con.QueryFirstOrDefaultAsync<VendorDto>(sql, new { Id = id });
        }

        public async Task<ApiResponse> CreateVendorAsync(VendorDto vendor)
        {
            try
            {
                var sql = @"INSERT INTO dbo.Tbl_Vendors 
                            (VendorName, VendorCode, VendorAddress, VendorEmail, VendorPhone, VendorContact, VendorNote, VendorBankName)
                            VALUES (@VendorName, @VendorCode, @VendorAddress, @VendorEmail, @VendorPhone, @VendorContact, @VendorNote, @VendorBankName)";
                using var con = new SqlConnection(_config.GetConnectionString("DefaultConnection"));
                await con.ExecuteAsync(sql, vendor);
                return new ApiResponse { Success = true, Message = "Vendor created successfully" };
            }
            catch (Exception ex)
            {
                return new ApiResponse { Success = false, Message = ex.Message };
            }
        }

        public async Task<ApiResponse> UpdateVendorAsync(VendorDto vendor)
        {
            try
            {
                var sql = @"UPDATE dbo.Tbl_Vendors 
                            SET VendorName = @VendorName, VendorCode = @VendorCode, VendorAddress = @VendorAddress, 
                                VendorEmail = @VendorEmail, VendorPhone = @VendorPhone, VendorContact = @VendorContact, 
                                VendorNote = @VendorNote, VendorBankName = @VendorBankName
                            WHERE VendorID = @VendorID";
                using var con = new SqlConnection(_config.GetConnectionString("DefaultConnection"));
                await con.ExecuteAsync(sql, vendor);
                return new ApiResponse { Success = true, Message = "Vendor updated successfully" };
            }
            catch (Exception ex)
            {
                return new ApiResponse { Success = false, Message = ex.Message };
            }
        }

        public async Task<ApiResponse> DeleteVendorAsync(int id)
        {
            try
            {
                var sql = "DELETE FROM dbo.Tbl_Vendors WHERE VendorID = @Id";
                using var con = new SqlConnection(_config.GetConnectionString("DefaultConnection"));
                await con.ExecuteAsync(sql, new { Id = id });
                return new ApiResponse { Success = true, Message = "Vendor deleted successfully" };
            }
            catch (Exception ex)
            {
                return new ApiResponse { Success = false, Message = ex.Message };
            }
        }
    }
}
