using Microsoft.AspNetCore.Mvc;
using MySqlConnector;

namespace MatchPawBackend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ApplicantsController : ControllerBase
{
    private readonly IConfiguration _configuration;

    public ApplicantsController(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    [HttpGet]
    public async Task<IActionResult> GetApplicants()
    {
        var applicants = new List<object>();
        await using var connection = new MySqlConnection(_configuration.GetConnectionString("DefaultConnection"));
        await connection.OpenAsync();

        var sql = "SELECT applicant_id, full_name, email, phone, address, housing_type, has_pets, has_children, experience_with_pets, preferred_contact_method, created_at FROM applicants ORDER BY applicant_id";
        await using var command = new MySqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            applicants.Add(MapApplicant(reader));
        }

        return Ok(applicants);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetApplicant(int id)
    {
        await using var connection = new MySqlConnection(_configuration.GetConnectionString("DefaultConnection"));
        await connection.OpenAsync();

        var sql = "SELECT applicant_id, full_name, email, phone, address, housing_type, has_pets, has_children, experience_with_pets, preferred_contact_method, created_at FROM applicants WHERE applicant_id = @id";
        await using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@id", id);
        await using var reader = await command.ExecuteReaderAsync();

        if (!await reader.ReadAsync())
            return NotFound();

        return Ok(MapApplicant(reader));
    }

    [HttpPost]
    public async Task<IActionResult> CreateApplicant([FromBody] ApplicantRequest req)
    {
        await using var connection = new MySqlConnection(_configuration.GetConnectionString("DefaultConnection"));
        await connection.OpenAsync();

        var sql = @"INSERT INTO applicants (full_name, email, phone, address, housing_type, has_pets, has_children, experience_with_pets, preferred_contact_method)
                    VALUES (@fullName, @email, @phone, @address, @housingType, @hasPets, @hasChildren, @experienceWithPets, @preferredContactMethod);
                    SELECT LAST_INSERT_ID();";

        await using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@fullName", req.FullName);
        command.Parameters.AddWithValue("@email", req.Email);
        command.Parameters.AddWithValue("@phone", (object?)req.Phone ?? DBNull.Value);
        command.Parameters.AddWithValue("@address", (object?)req.Address ?? DBNull.Value);
        command.Parameters.AddWithValue("@housingType", (object?)req.HousingType ?? DBNull.Value);
        command.Parameters.AddWithValue("@hasPets", req.HasPets);
        command.Parameters.AddWithValue("@hasChildren", req.HasChildren);
        command.Parameters.AddWithValue("@experienceWithPets", (object?)req.ExperienceWithPets ?? DBNull.Value);
        command.Parameters.AddWithValue("@preferredContactMethod", (object?)req.PreferredContactMethod ?? DBNull.Value);

        var newId = Convert.ToInt32(await command.ExecuteScalarAsync());
        return CreatedAtAction(nameof(GetApplicant), new { id = newId }, new { applicantId = newId });
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateApplicant(int id, [FromBody] ApplicantRequest req)
    {
        await using var connection = new MySqlConnection(_configuration.GetConnectionString("DefaultConnection"));
        await connection.OpenAsync();

        var sql = @"UPDATE applicants SET full_name = @fullName, email = @email, phone = @phone, address = @address,
                        housing_type = @housingType, has_pets = @hasPets, has_children = @hasChildren,
                        experience_with_pets = @experienceWithPets, preferred_contact_method = @preferredContactMethod
                    WHERE applicant_id = @id";

        await using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@id", id);
        command.Parameters.AddWithValue("@fullName", req.FullName);
        command.Parameters.AddWithValue("@email", req.Email);
        command.Parameters.AddWithValue("@phone", (object?)req.Phone ?? DBNull.Value);
        command.Parameters.AddWithValue("@address", (object?)req.Address ?? DBNull.Value);
        command.Parameters.AddWithValue("@housingType", (object?)req.HousingType ?? DBNull.Value);
        command.Parameters.AddWithValue("@hasPets", req.HasPets);
        command.Parameters.AddWithValue("@hasChildren", req.HasChildren);
        command.Parameters.AddWithValue("@experienceWithPets", (object?)req.ExperienceWithPets ?? DBNull.Value);
        command.Parameters.AddWithValue("@preferredContactMethod", (object?)req.PreferredContactMethod ?? DBNull.Value);

        var rows = await command.ExecuteNonQueryAsync();
        if (rows == 0)
            return NotFound();

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteApplicant(int id)
    {
        await using var connection = new MySqlConnection(_configuration.GetConnectionString("DefaultConnection"));
        await connection.OpenAsync();

        await using var command = new MySqlCommand("DELETE FROM applicants WHERE applicant_id = @id", connection);
        command.Parameters.AddWithValue("@id", id);

        var rows = await command.ExecuteNonQueryAsync();
        if (rows == 0)
            return NotFound();

        return NoContent();
    }

    private static object MapApplicant(MySqlDataReader reader) => new
    {
        applicantId = reader.GetInt32("applicant_id"),
        fullName = reader.GetString("full_name"),
        email = reader.GetString("email"),
        phone = reader.IsDBNull(reader.GetOrdinal("phone")) ? null : reader.GetString("phone"),
        address = reader.IsDBNull(reader.GetOrdinal("address")) ? null : reader.GetString("address"),
        housingType = reader.IsDBNull(reader.GetOrdinal("housing_type")) ? null : reader.GetString("housing_type"),
        hasPets = reader.GetBoolean("has_pets"),
        hasChildren = reader.GetBoolean("has_children"),
        experienceWithPets = reader.IsDBNull(reader.GetOrdinal("experience_with_pets")) ? null : reader.GetString("experience_with_pets"),
        preferredContactMethod = reader.IsDBNull(reader.GetOrdinal("preferred_contact_method")) ? null : reader.GetString("preferred_contact_method"),
        createdAt = reader.GetDateTime("created_at")
    };
}

public record ApplicantRequest(
    string FullName,
    string Email,
    string? Phone,
    string? Address,
    string? HousingType,
    bool HasPets,
    bool HasChildren,
    string? ExperienceWithPets,
    string? PreferredContactMethod
);
