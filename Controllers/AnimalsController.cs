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
        await using var connection = new MySqlConnection(_configuration.GetConnectionString("DefaultConnection"));
        await connection.OpenAsync();

        var sql = @"SELECT animal_id, name, species, breed, age, sex, intake_date,
                           adoption_status, health_status, notes, created_at
                    FROM animals ORDER BY animal_id";

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

    [HttpGet("{id}")]
    public async Task<IActionResult> GetAnimal(int id)
    {
        await using var connection = new MySqlConnection(_configuration.GetConnectionString("DefaultConnection"));
        await connection.OpenAsync();

        var sql = @"SELECT animal_id, name, species, breed, age, sex, intake_date,
                           adoption_status, health_status, notes, created_at
                    FROM animals WHERE animal_id = @id";

        await using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@id", id);
        await using var reader = await command.ExecuteReaderAsync();

        if (!await reader.ReadAsync())
            return NotFound();

        return Ok(new
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

    [HttpPost]
    public async Task<IActionResult> CreateAnimal([FromBody] AnimalRequest req)
    {
        await using var connection = new MySqlConnection(_configuration.GetConnectionString("DefaultConnection"));
        await connection.OpenAsync();

        var sql = @"INSERT INTO animals (name, species, breed, age, sex, intake_date, adoption_status, health_status, notes)
                    VALUES (@name, @species, @breed, @age, @sex, @intakeDate, @adoptionStatus, @healthStatus, @notes);
                    SELECT LAST_INSERT_ID();";

        await using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@name", req.Name);
        command.Parameters.AddWithValue("@species", req.Species);
        command.Parameters.AddWithValue("@breed", (object?)req.Breed ?? DBNull.Value);
        command.Parameters.AddWithValue("@age", (object?)req.Age ?? DBNull.Value);
        command.Parameters.AddWithValue("@sex", (object?)req.Sex ?? DBNull.Value);
        command.Parameters.AddWithValue("@intakeDate", (object?)req.IntakeDate ?? DBNull.Value);
        command.Parameters.AddWithValue("@adoptionStatus", req.AdoptionStatus ?? "Available");
        command.Parameters.AddWithValue("@healthStatus", (object?)req.HealthStatus ?? DBNull.Value);
        command.Parameters.AddWithValue("@notes", (object?)req.Notes ?? DBNull.Value);

        var newId = Convert.ToInt32(await command.ExecuteScalarAsync());
        return CreatedAtAction(nameof(GetAnimal), new { id = newId }, new { animalId = newId });
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateAnimal(int id, [FromBody] AnimalRequest req)
    {
        await using var connection = new MySqlConnection(_configuration.GetConnectionString("DefaultConnection"));
        await connection.OpenAsync();

        var sql = @"UPDATE animals SET
                        name = @name, species = @species, breed = @breed, age = @age,
                        sex = @sex, intake_date = @intakeDate, adoption_status = @adoptionStatus,
                        health_status = @healthStatus, notes = @notes
                    WHERE animal_id = @id";

        await using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@id", id);
        command.Parameters.AddWithValue("@name", req.Name);
        command.Parameters.AddWithValue("@species", req.Species);
        command.Parameters.AddWithValue("@breed", (object?)req.Breed ?? DBNull.Value);
        command.Parameters.AddWithValue("@age", (object?)req.Age ?? DBNull.Value);
        command.Parameters.AddWithValue("@sex", (object?)req.Sex ?? DBNull.Value);
        command.Parameters.AddWithValue("@intakeDate", (object?)req.IntakeDate ?? DBNull.Value);
        command.Parameters.AddWithValue("@adoptionStatus", req.AdoptionStatus ?? "Available");
        command.Parameters.AddWithValue("@healthStatus", (object?)req.HealthStatus ?? DBNull.Value);
        command.Parameters.AddWithValue("@notes", (object?)req.Notes ?? DBNull.Value);

        var rows = await command.ExecuteNonQueryAsync();
        if (rows == 0)
            return NotFound();

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteAnimal(int id)
    {
        await using var connection = new MySqlConnection(_configuration.GetConnectionString("DefaultConnection"));
        await connection.OpenAsync();

        await using var command = new MySqlCommand("DELETE FROM animals WHERE animal_id = @id", connection);
        command.Parameters.AddWithValue("@id", id);

        var rows = await command.ExecuteNonQueryAsync();
        if (rows == 0)
            return NotFound();

        return NoContent();
    }
}

public record AnimalRequest(
    string Name,
    string Species,
    string? Breed,
    int? Age,
    string? Sex,
    string? IntakeDate,
    string? AdoptionStatus,
    string? HealthStatus,
    string? Notes
);
