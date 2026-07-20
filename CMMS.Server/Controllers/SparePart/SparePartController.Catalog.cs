using CMMS.Shared.Dtos.SpareParts;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace CMMS.Server.Controllers.SparePart
{
    public partial class SparePartController : ControllerBase
    {
        [HttpGet("categories")]
        public async Task<List<SparePartCategoryDto>> GetCategories() => await _service.GetCategoriesAsync();

        [HttpGet("suppliers")]
        public async Task<List<SparePartSupplierDto>> GetSuppliers() => await _service.GetSuppliersAsync();

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
}
