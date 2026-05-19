using Microsoft.AspNetCore.Mvc;
using MySqlConnector;

namespace MatchPawBackend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AnimalsController : ControllerBase
{
    private readonly IConfiguration _configuration;

    public AnimalsController(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    [HttpGet]
    public async Task<IActionResult> GetAnimals()
    {
        var animals = new List<object>();

        var connectionString = _configuration.GetConnectionString("DefaultConnection");

        await using var connection = new MySqlConnection(connectionString);
        await connection.OpenAsync();

        var sql = @"
            SELECT
                animal_id,
                name,
                species,
                breed,
                age,
                sex,
                intake_date,
                adoption_status,
                health_status,
                notes,
                created_at
            FROM animals
            ORDER BY animal_id;
        ";

        await using var command = new MySqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            animals.Add(new
            {
                animalId = reader.GetInt32("animal_id"),
                name = reader.GetString("name"),
                species = reader.GetString("species"),
                breed = reader.IsDBNull(reader.GetOrdinal("breed")) ? null : reader.GetString("breed"),
                age = reader.IsDBNull(reader.GetOrdinal("age")) ? (int?)null : reader.GetInt32("age"),
                sex = reader.IsDBNull(reader.GetOrdinal("sex")) ? null : reader.GetString("sex"),
                intakeDate = reader.IsDBNull(reader.GetOrdinal("intake_date")) ? null : reader.GetDateTime("intake_date").ToString("yyyy-MM-dd"),
                adoptionStatus = reader.IsDBNull(reader.GetOrdinal("adoption_status")) ? null : reader.GetString("adoption_status"),
                healthStatus = reader.IsDBNull(reader.GetOrdinal("health_status")) ? null : reader.GetString("health_status"),
                notes = reader.IsDBNull(reader.GetOrdinal("notes")) ? null : reader.GetString("notes"),
                createdAt = reader.GetDateTime("created_at")
            });
        }

        return Ok(animals);
    }
}