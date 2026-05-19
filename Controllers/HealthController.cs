using Microsoft.AspNetCore.Mvc;
using MySqlConnector;

namespace MatchPawBackend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    private readonly IConfiguration _configuration;

    public HealthController(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    [HttpGet]
    public IActionResult Get()
    {
        return Ok(new
        {
            status = "MatchPaw backend is running",
            time = DateTime.UtcNow
        });
    }

    [HttpGet("db")]
    public async Task<IActionResult> CheckDatabase()
    {
        try
        {
            var connectionString = _configuration.GetConnectionString("DefaultConnection");

            await using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();

            await using var command = new MySqlCommand("SELECT DATABASE();", connection);
            var databaseName = await command.ExecuteScalarAsync();

            return Ok(new
            {
                status = "Database connection successful",
                database = databaseName,
                time = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                status = "Database connection failed",
                error = ex.Message
            });
        }
    }
}