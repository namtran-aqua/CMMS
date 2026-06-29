using Microsoft.AspNetCore.Mvc;

namespace CMMS.Server.Controllers.Common
{
    [ApiController]
    [Route("api/[controller]")]
    public class CommonController : ControllerBase
    {
        private readonly IWebHostEnvironment _env;
        public CommonController(IWebHostEnvironment env)
        {
            _env = env;
        }
        [HttpPost("upload-file-suport")]
        public async Task<IActionResult> UploadFileSuport(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("File is empty.");

            //var fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
            var originalName = Path.GetFileNameWithoutExtension(file.FileName);
            var extension = Path.GetExtension(file.FileName);
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmssfff");
            var fileName = $"{originalName}_{timestamp}{extension}";
            var uploadDir = Path.Combine("wwwroot", "uploads", "support-data");
            if (!Directory.Exists(uploadDir))
            {
                Directory.CreateDirectory(uploadDir);
            }
            var filePath = Path.Combine(uploadDir, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var baseUrl = $"{Request.Scheme}://{Request.Host}";
            var url = $"{baseUrl}/uploads/support-data/{fileName}";
            return Ok(url);
        }
        [HttpDelete("delete-file-suport")]
        public IActionResult DeleteAvatar([FromQuery] string avatarUrl)
        {
            if (string.IsNullOrWhiteSpace(avatarUrl))
                return BadRequest("Avatar URL is required.");

            avatarUrl = Uri.UnescapeDataString(avatarUrl);

            // Lấy base URL (https://localhost:7195/)
            var baseUrl = $"{Request.Scheme}://{Request.Host}{Request.PathBase}/";

            // Cắt bỏ base URL, chỉ lấy relative path
            if (avatarUrl.StartsWith(baseUrl, StringComparison.OrdinalIgnoreCase))
            {
                avatarUrl = avatarUrl.Substring(baseUrl.Length);
            }

            // Thay dấu / thành separator theo hệ điều hành
            var relativePath = avatarUrl.Replace("/", Path.DirectorySeparatorChar.ToString());

            var physicalPath = Path.Combine(_env.WebRootPath, relativePath);

            if (System.IO.File.Exists(physicalPath))
            {
                System.IO.File.Delete(physicalPath);
                return Ok(new { message = "Avatar deleted successfully." });
            }

            return NotFound("Avatar file not found.");
        }
    }
}
