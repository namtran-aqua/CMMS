using CMMS.Shared.Dtos.SpareParts;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.AspNetCore.Authorization;
using CMMS.Shared.Authorization;
using CMMS.Server.Infrastructure.Authorization;

namespace CMMS.Server.Controllers.SparePart
{
    public partial class SparePartController : ControllerBase
    {
        [HttpGet("categories")]
        public async Task<List<SparePartCategoryDto>> GetCategories() => await _service.GetCategoriesAsync();

        [HttpGet("suppliers")]
        public async Task<List<SparePartSupplierDto>> GetSuppliers() => await _service.GetSuppliersAsync();

        [HttpPost("create")]
        [RequirePermission(Permissions.MasterDataCatalogAdd)]
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
        [RequirePermission(Permissions.MasterDataCatalogEdit)]
        public async Task<IActionResult> Update(SparePartDto dto)
        {
            try
            {
                var success = await _service.UpdateAsync(dto, await GetCurrentUserAsync());
                return success ? Ok(new { message = "Cập nhật thành công" }) : NotFound();
            }
            catch (InvalidOperationException ex) { return Conflict(ex.Message); }
        }

        [HttpPost("category/create")]
        [RequirePermission(Permissions.MasterDataCategoryAdd)]
        public async Task<IActionResult> CreateCategory(SparePartCategoryDto dto)
        {
            try
            {
                var result = await _service.CreateCategory(dto, await GetCurrentUserAsync());
                return Ok(result);
            }
            catch (InvalidOperationException ex) { return Conflict(ex.Message); }
        }

        [HttpPut("category/update")]
        [RequirePermission(Permissions.MasterDataCategoryEdit)]
        public async Task<IActionResult> UpdateCategory(SparePartCategoryDto dto)
        {
            try
            {
                var success = await _service.UpdateCategory(dto, await GetCurrentUserAsync());
                return success ? Ok(new { message = "Cập nhật thành công" }) : NotFound();
            }
            catch (InvalidOperationException ex) { return Conflict(ex.Message); }
        }

        [HttpDelete("category/delete/{categoryid}")]
        [RequirePermission(Permissions.MasterDataCategoryDelete)]
        public async Task<IActionResult> DeleteCategory(int categoryid)
        {
            var success = await _service.DeleteCategory(categoryid, await GetCurrentUserAsync());
            return success ? Ok(new { message = "Xóa thành công" }) : NotFound();
        }

        [HttpPost("supplier/create")]
        [RequirePermission(Permissions.MasterDataSupplierAdd)]
        public async Task<IActionResult> CreateSupplier(SparePartSupplierDto dto)
        {
            try
            {
                var result = await _service.CreateSupplier(dto, await GetCurrentUserAsync());
                return Ok(result);
            }
            catch (InvalidOperationException ex) { return Conflict(ex.Message); }
        }

        [HttpPut("supplier/update")]
        [RequirePermission(Permissions.MasterDataSupplierEdit)]
        public async Task<IActionResult> UpdateSupplier(SparePartSupplierDto dto)
        {
            try
            {
                var success = await _service.UpdateSupplier(dto, await GetCurrentUserAsync());
                return success ? Ok(new { message = "Cập nhật thành công" }) : NotFound();
            }
            catch (InvalidOperationException ex) { return Conflict(ex.Message); }
        }

        [HttpDelete("supplier/delete/{spid}")]
        [RequirePermission(Permissions.MasterDataSupplierDelete)]
        public async Task<IActionResult> DeleteSupplier(int spid)
        {
            var success = await _service.DeleteSupplier(spid, await GetCurrentUserAsync());
            return success ? Ok(new { message = "Xóa thành công" }) : NotFound();
        }
    }
}
