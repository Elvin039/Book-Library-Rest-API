﻿using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace Project.API.Controllers
{
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [Route("api/[controller]")]
    public class FileController : Controller
    {
        private readonly IWebHostEnvironment _environment;

        public FileController(IWebHostEnvironment environment)
        {
            _environment = environment;
        }

        [SwaggerOperation(Summary = "upload file")]
        [SwaggerResponse(StatusCodes.Status200OK, type: typeof(string))]
        [HttpPost("upload")]
        public async Task<IActionResult> Upload([FromForm] IFormFile file)
        {
            string originalFileName = file.FileName;
            string fileExtension = file.FileName.Substring(file.FileName.LastIndexOf('.') + 1);
            Guid guid = Guid.NewGuid();
            var uploads = Path.Combine(_environment.WebRootPath, "files");
            string fileName = guid.ToString() + "-fileName-" + file.FileName;
            string filePath = "/" + fileName;

            if (file.Length > 0)
            {
                using (var fileStream = new FileStream(Path.Combine(uploads, fileName), FileMode.Create))
                {
                    await file.CopyToAsync(fileStream);
                }
            }

            return Ok(fileName);
        }

        [SwaggerOperation(Summary = "download file")]
        [SwaggerResponse(StatusCodes.Status200OK, type: typeof(void))]
        [HttpGet("download/{fileName}")]
        public IActionResult Download([FromRoute] string fileName)
        {
            var uploads = Path.Combine(_environment.WebRootPath, "files");
            string filePath = uploads + "/" + fileName;
            return PhysicalFile(filePath, "APPLICATION/octet-stream", Path.GetFileName(fileName));
        }
    }
}