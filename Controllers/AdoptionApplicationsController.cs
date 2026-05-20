using Microsoft.AspNetCore.Mvc;
using MySqlConnector;

namespace MatchPawBackend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AdoptionApplicationsController : ControllerBase
{
    private readonly IConfiguration _configuration;

    public AdoptionApplicationsController(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    [HttpGet]
    public async Task<IActionResult> GetApplications()
    {
        var applications = new List<object>();
        await using var connection = new MySqlConnection(_configuration.GetConnectionString("DefaultConnection"));
        await connection.OpenAsync();

        var sql = @"SELECT application_id, animal_id, applicant_id, application_date,
                           status, reason, reviewed_by, reviewed_date, created_at
                    FROM adoption_applications ORDER BY application_id";

        await using var command = new MySqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            applications.Add(MapApplication(reader));
        }

        return Ok(applications);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetApplication(int id)
    {
        await using var connection = new MySqlConnection(_configuration.GetConnectionString("DefaultConnection"));
        await connection.OpenAsync();

        var sql = @"SELECT application_id, animal_id, applicant_id, application_date,
                           status, reason, reviewed_by, reviewed_date, created_at
                    FROM adoption_applications WHERE application_id = @id";

        await using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@id", id);
        await using var reader = await command.ExecuteReaderAsync();

        if (!await reader.ReadAsync())
            return NotFound();

        return Ok(MapApplication(reader));
    }

    [HttpGet("animal/{animalId}")]
    public async Task<IActionResult> GetByAnimal(int animalId)
    {
        var applications = new List<object>();
        await using var connection = new MySqlConnection(_configuration.GetConnectionString("DefaultConnection"));
        await connection.OpenAsync();

        var sql = @"SELECT application_id, animal_id, applicant_id, application_date,
                           status, reason, reviewed_by, reviewed_date, created_at
                    FROM adoption_applications WHERE animal_id = @animalId ORDER BY application_date DESC";

        await using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@animalId", animalId);
        await using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            applications.Add(MapApplication(reader));
        }

        return Ok(applications);
    }

    [HttpPost]
    public async Task<IActionResult> CreateApplication([FromBody] AdoptionApplicationRequest req)
    {
        await using var connection = new MySqlConnection(_configuration.GetConnectionString("DefaultConnection"));
        await connection.OpenAsync();

        var sql = @"INSERT INTO adoption_applications (animal_id, applicant_id, application_date, status, reason, reviewed_by, reviewed_date)
                    VALUES (@animalId, @applicantId, @applicationDate, @status, @reason, @reviewedBy, @reviewedDate);
                    SELECT LAST_INSERT_ID();";

        await using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@animalId", req.AnimalId);
        command.Parameters.AddWithValue("@applicantId", req.ApplicantId);
        command.Parameters.AddWithValue("@applicationDate", req.ApplicationDate);
        command.Parameters.AddWithValue("@status", req.Status ?? "Pending");
        command.Parameters.AddWithValue("@reason", (object?)req.Reason ?? DBNull.Value);
        command.Parameters.AddWithValue("@reviewedBy", (object?)req.ReviewedBy ?? DBNull.Value);
        command.Parameters.AddWithValue("@reviewedDate", (object?)req.ReviewedDate ?? DBNull.Value);

        var newId = Convert.ToInt32(await command.ExecuteScalarAsync());
        return CreatedAtAction(nameof(GetApplication), new { id = newId }, new { applicationId = newId });
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateApplication(int id, [FromBody] AdoptionApplicationRequest req)
    {
        await using var connection = new MySqlConnection(_configuration.GetConnectionString("DefaultConnection"));
        await connection.OpenAsync();

        var sql = @"UPDATE adoption_applications SET
                        animal_id = @animalId, applicant_id = @applicantId, application_date = @applicationDate,
                        status = @status, reason = @reason, reviewed_by = @reviewedBy, reviewed_date = @reviewedDate
                    WHERE application_id = @id";

        await using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@id", id);
        command.Parameters.AddWithValue("@animalId", req.AnimalId);
        command.Parameters.AddWithValue("@applicantId", req.ApplicantId);
        command.Parameters.AddWithValue("@applicationDate", req.ApplicationDate);
        command.Parameters.AddWithValue("@status", req.Status ?? "Pending");
        command.Parameters.AddWithValue("@reason", (object?)req.Reason ?? DBNull.Value);
        command.Parameters.AddWithValue("@reviewedBy", (object?)req.ReviewedBy ?? DBNull.Value);
        command.Parameters.AddWithValue("@reviewedDate", (object?)req.ReviewedDate ?? DBNull.Value);

        var rows = await command.ExecuteNonQueryAsync();
        if (rows == 0)
            return NotFound();

        return NoContent();
    }

    [HttpPatch("{id}/status")]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] StatusUpdateRequest req)
    {
        await using var connection = new MySqlConnection(_configuration.GetConnectionString("DefaultConnection"));
        await connection.OpenAsync();

        var sql = @"UPDATE adoption_applications SET status = @status, reviewed_by = @reviewedBy, reviewed_date = @reviewedDate
                    WHERE application_id = @id";

        await using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@id", id);
        command.Parameters.AddWithValue("@status", req.Status);
        command.Parameters.AddWithValue("@reviewedBy", (object?)req.ReviewedBy ?? DBNull.Value);
        command.Parameters.AddWithValue("@reviewedDate", (object?)req.ReviewedDate ?? DBNull.Value);

        var rows = await command.ExecuteNonQueryAsync();
        if (rows == 0)
            return NotFound();

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteApplication(int id)
    {
        await using var connection = new MySqlConnection(_configuration.GetConnectionString("DefaultConnection"));
        await connection.OpenAsync();

        await using var command = new MySqlCommand("DELETE FROM adoption_applications WHERE application_id = @id", connection);
        command.Parameters.AddWithValue("@id", id);

        var rows = await command.ExecuteNonQueryAsync();
        if (rows == 0)
            return NotFound();

        return NoContent();
    }

    private static object MapApplication(MySqlDataReader reader) => new
    {
        applicationId = reader.GetInt32("application_id"),
        animalId = reader.GetInt32("animal_id"),
        applicantId = reader.GetInt32("applicant_id"),
        applicationDate = reader.GetDateTime("application_date").ToString("yyyy-MM-dd"),
        status = reader.IsDBNull(reader.GetOrdinal("status")) ? null : reader.GetString("status"),
        reason = reader.IsDBNull(reader.GetOrdinal("reason")) ? null : reader.GetString("reason"),
        reviewedBy = reader.IsDBNull(reader.GetOrdinal("reviewed_by")) ? (int?)null : reader.GetInt32("reviewed_by"),
        reviewedDate = reader.IsDBNull(reader.GetOrdinal("reviewed_date")) ? null : reader.GetDateTime("reviewed_date").ToString("yyyy-MM-dd"),
        createdAt = reader.GetDateTime("created_at")
    };
}

public record AdoptionApplicationRequest(
    int AnimalId,
    int ApplicantId,
    string ApplicationDate,
    string? Status,
    string? Reason,
    int? ReviewedBy,
    string? ReviewedDate
);

public record StatusUpdateRequest(
    string Status,
    int? ReviewedBy,
    string? ReviewedDate
);
