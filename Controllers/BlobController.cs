using Azure.Storage.Blobs;
using Azure.Storage.Sas;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MatchPawBackend.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class BlobController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly IWebHostEnvironment _environment;

    public BlobController(IConfiguration configuration, IWebHostEnvironment environment)
    {
        _configuration = configuration;
        _environment = environment;
    }

    [HttpGet("sas-url")]
    public IActionResult GetSasUrl([FromQuery] string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return BadRequest("fileName is required.");

        var connectionString = _configuration["AzureBlob:ConnectionString"];
        var containerName = _configuration["AzureBlob:ContainerName"] ?? "animal-photos";

        if (string.IsNullOrEmpty(connectionString))
            return StatusCode(500, "Azure Blob Storage is not configured.");

        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        if (!IsAllowedImageExtension(extension))
            return BadRequest("Only image files are allowed (.jpg, .jpeg, .png, .webp, .gif).");

        var blobName = $"{Guid.NewGuid()}{extension}";
        var serviceClient = new BlobServiceClient(connectionString);
        var containerClient = serviceClient.GetBlobContainerClient(containerName);
        var blobClient = containerClient.GetBlobClient(blobName);

        var sasBuilder = new BlobSasBuilder
        {
            BlobContainerName = containerName,
            BlobName = blobName,
            Resource = "b",
            ExpiresOn = DateTimeOffset.UtcNow.AddMinutes(15)
        };
        sasBuilder.SetPermissions(BlobSasPermissions.Write | BlobSasPermissions.Create);

        var sasUrl = blobClient.GenerateSasUri(sasBuilder).ToString();
        var blobUrl = blobClient.Uri.ToString();

        return Ok(new { sasUrl, blobUrl });
    }

    [HttpPost("upload")]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<IActionResult> Upload([FromForm] IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest("file is required.");

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!IsAllowedImageExtension(extension))
            return BadRequest("Only image files are allowed (.jpg, .jpeg, .png, .webp, .gif).");

        var webRoot = _environment.WebRootPath;
        if (string.IsNullOrWhiteSpace(webRoot))
        {
            webRoot = Path.Combine(_environment.ContentRootPath, "wwwroot");
        }

        var uploadDirectory = Path.Combine(webRoot, "uploads", "animal-photos");
        Directory.CreateDirectory(uploadDirectory);

        var savedFileName = $"{Guid.NewGuid()}{extension}";
        var path = Path.Combine(uploadDirectory, savedFileName);
        await using (var stream = System.IO.File.Create(path))
        {
            await file.CopyToAsync(stream);
        }

        var photoPath = $"/uploads/animal-photos/{savedFileName}";
        var photoUrl = $"{Request.Scheme}://{Request.Host}{photoPath}";
        return Ok(new { blobUrl = photoUrl });
    }

    private static bool IsAllowedImageExtension(string extension)
    {
        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp", ".gif" };
        return allowedExtensions.Contains(extension);
    }
}
