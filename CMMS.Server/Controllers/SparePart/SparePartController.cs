using CMMS.Server.Services.SparePartService;
using CMMS.Server.Services.UserService;
using CMMS.Shared.Dtos.SpareParts;
using CMMS.Shared.Dtos.Common;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using System.IO;
using System.Linq;

[ApiController]
[Route("api/[controller]")]
public class SparePartController : ControllerBase
{
    private readonly ISparePartService _service;
    private readonly IUserService _userService;

    public SparePartController(ISparePartService service, IUserService userService)
    {
        _service = service;
        _userService = userService;
    }

    private async Task<CMMS.Shared.Dtos.User.UserDto?> GetCurrentUserAsync()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userIdClaim, out var userId)) return null;
        return await _userService.GetCurrentUserAsync(userId);
    }

    [HttpGet("get-all")]
    public async Task<List<SparePartDto>> GetAll() => await _service.GetAllAsync();

    [HttpGet("get-paged")]
    public async Task<ActionResult<SparePartPagedResultDto>> GetPaged(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? searchText = null,
        [FromQuery] int? categoryId = null,
        [FromQuery] string? stockStatus = null,
        [FromQuery] string? sortBy = null,
        [FromQuery] int? factoryId = null)
    {
        return Ok(await _service.GetPagedAsync(page, pageSize, searchText, categoryId, stockStatus, sortBy, factoryId));
    }

    [HttpGet("history-paged")]
    public async Task<ActionResult<PagedResultDto<SparePartTransactionDto>>> GetHistoryPaged(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? searchText = null,
        [FromQuery] string? typeFilter = null,
        [FromQuery] int? factoryId = null)
    {
        return Ok(await _service.GetTransactionHistoryPagedAsync(page, pageSize, searchText, typeFilter, factoryId));
    }

    [HttpPost("import")]
    public async Task<IActionResult> Import(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest("File không hợp lệ hoặc rỗng.");

        try
        {
            var currentUser = await GetCurrentUserAsync();
            if (currentUser == null) return Unauthorized("Không tìm thấy thông tin người dùng.");

            using var stream = file.OpenReadStream();
            var result = await _service.ImportSparePartsAsync(stream, file.FileName, currentUser);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Lỗi import: {ex.Message}");
        }
    }

    [HttpGet("import-template")]
    public IActionResult DownloadTemplate()
    {
        var csv = "PartCode,PartName,Unit,Price,Inventory,MinStock,CategoryName,SupplierName,LocName,DeptCode,Note\n" +
                  "SP-001,Cảm biến tiệm cận M18,Cái,150000,20,5,Cảm biến,Nhà cung cấp Omron,Khu vực A,Maint-Dept,Ghi chú mẫu\n" +
                  "SP-002,Băng tải cao su B500,Mét,450000,5,2,Băng tải,Nhà cung cấp Habasit,Khu vực B,Prod-Dept,Ghi chú mẫu 2";
        
        var bytes = System.Text.Encoding.UTF8.GetPreamble().Concat(System.Text.Encoding.UTF8.GetBytes(csv)).ToArray();
        return File(bytes, "text/csv; charset=utf-8", "SparePart_Import_Template.csv");
    }

    [HttpGet("categories")]
    public async Task<List<SparePartCategoryDto>> GetCategories() => await _service.GetCategoriesAsync();

    [HttpGet("suppliers")]
    public async Task<List<SparePartSupplierDto>> GetSuppliers() => await _service.GetSuppliersAsync();

    [HttpGet("history")]
    public async Task<List<SparePartTransactionDto>> GetHistory() => await _service.GetTransactionHistoryAsync();

    [HttpPost("create")]
    public async Task<IActionResult> Create(SparePartDto dto)
    {
        try
        {
            var result = await _service.CreateAsync(dto, await GetCurrentUserAsync());
            return Ok(result);
        }
        catch (InvalidOperationException ex) { return Conflict(ex.Message); }
    }

    [HttpPut("update")]
    public async Task<IActionResult> Update(SparePartDto dto)
    {
        try
        {
            var success = await _service.UpdateAsync(dto, await GetCurrentUserAsync());
            return success ? Ok(new { message = "Cập nhật thành công" }) : NotFound();
        }
        catch (InvalidOperationException ex) { return Conflict(ex.Message); }
    }

    [HttpDelete("delete/{spid}")]
    public async Task<IActionResult> Delete(int spid)
    {
        var success = await _service.DeleteAsync(spid, await GetCurrentUserAsync());
        return success ? Ok(new { message = "Xóa thành công" }) : NotFound();
    }

    [HttpPost("adjust-stock")]
    public async Task<IActionResult> AdjustStock(AdjustStockRequestDto request)
    {
        try
        {
            var success = await _service.AdjustStockAsync(request, await GetCurrentUserAsync());
            return success ? Ok(new { message = "Cập nhật tồn kho thành công" }) : StatusCode(500, "Thất bại");
        }
        catch (KeyNotFoundException ex) { return NotFound(ex.Message); }
        catch (InvalidOperationException ex) { return Conflict(ex.Message); }
        catch (ArgumentException ex) { return BadRequest(ex.Message); }
    }

    [HttpPost("export-maintenance")]
    public async Task<IActionResult> ExportForMaintenance(MaintenanceExportRequestDto request)
    {
        try
        {
            var refCode = await _service.ExportForMaintenanceAsync(request, await GetCurrentUserAsync());
            return Ok(new { message = "Xuất bảo trì thành công", refCode });
        }
        catch (KeyNotFoundException ex) { return NotFound(ex.Message); }
        catch (InvalidOperationException ex) { return Conflict(ex.Message); }
        catch (ArgumentException ex) { return BadRequest(ex.Message); }
    }
    [HttpPost("category/create")]
    public async Task<IActionResult> CreateCategory(SparePartCategoryDto dto)
    {
        try
        {
            var result = await _service.CreateCategory(dto, await GetCurrentUserAsync());
            return Ok(result);
        }
        catch (InvalidOperationException ex) { return Conflict(ex.Message); }
    }
    [HttpDelete("category/delete/{categoryid}")]
    public async Task<IActionResult> DeleteCategory(int categoryid)
    {
        var success = await _service.DeleteCategory(categoryid, await GetCurrentUserAsync());
        return success ? Ok(new { message = "Xóa thành công" }) : NotFound();
    }
    [HttpPost("supplier/create")]
    public async Task<IActionResult> CreateSupplier(SparePartSupplierDto dto)
    {
        try
        {
            var result = await _service.CreateSupplier(dto, await GetCurrentUserAsync());
            return Ok(result);
        }
        catch (InvalidOperationException ex) { return Conflict(ex.Message); }
    }
    [HttpDelete("supplier/delete/{spid}")]
    public async Task<IActionResult> DeleteSupplier(int spid)
    {
        var success = await _service.DeleteSupplier(spid, await GetCurrentUserAsync());
        return success ? Ok(new { message = "Xóa thành công" }) : NotFound();
    }
}