using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using MySqlConnector;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace MatchPawBackend.Controllers;

[Authorize]
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

        var sql = "SELECT applicant_id, full_name, email, is_active, phone, address, housing_type, has_pets, has_children, experience_with_pets, preferred_contact_method, created_at FROM applicants ORDER BY applicant_id";
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

        var sql = "SELECT applicant_id, full_name, email, is_active, phone, address, housing_type, has_pets, has_children, experience_with_pets, preferred_contact_method, created_at FROM applicants WHERE applicant_id = @id";
        await using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@id", id);
        await using var reader = await command.ExecuteReaderAsync();

        if (!await reader.ReadAsync())
            return NotFound();

        return Ok(MapApplicant(reader));
    }

    [AllowAnonymous]
    [HttpPost]
    public async Task<IActionResult> CreateApplicant([FromBody] ApplicantRequest req)
    {
        await using var connection = new MySqlConnection(_configuration.GetConnectionString("DefaultConnection"));
        await connection.OpenAsync();

        var passwordHash = BCrypt.Net.BCrypt.HashPassword(req.Password);

        var sql = @"INSERT INTO applicants (full_name, email, password_hash, is_active, phone, address, housing_type, has_pets, has_children, experience_with_pets, preferred_contact_method)
                    VALUES (@fullName, @email, @passwordHash, @isActive, @phone, @address, @housingType, @hasPets, @hasChildren, @experienceWithPets, @preferredContactMethod);
                    SELECT LAST_INSERT_ID();";

        await using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@fullName", req.FullName);
        command.Parameters.AddWithValue("@email", req.Email);
        command.Parameters.AddWithValue("@passwordHash", passwordHash);
        command.Parameters.AddWithValue("@isActive", req.IsActive ?? true);
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

        string sql;
        MySqlCommand command;

        if (!string.IsNullOrEmpty(req.Password))
        {
            var passwordHash = BCrypt.Net.BCrypt.HashPassword(req.Password);
            sql = @"UPDATE applicants SET full_name = @fullName, email = @email,
                        password_hash = @passwordHash, is_active = @isActive,
                        phone = @phone, address = @address,
                        housing_type = @housingType, has_pets = @hasPets, has_children = @hasChildren,
                        experience_with_pets = @experienceWithPets, preferred_contact_method = @preferredContactMethod
                    WHERE applicant_id = @id";
            command = new MySqlCommand(sql, connection);
            command.Parameters.AddWithValue("@passwordHash", passwordHash);
        }
        else
        {
            sql = @"UPDATE applicants SET full_name = @fullName, email = @email,
                        is_active = @isActive,
                        phone = @phone, address = @address,
                        housing_type = @housingType, has_pets = @hasPets, has_children = @hasChildren,
                        experience_with_pets = @experienceWithPets, preferred_contact_method = @preferredContactMethod
                    WHERE applicant_id = @id";
            command = new MySqlCommand(sql, connection);
        }

        command.Parameters.AddWithValue("@id", id);
        command.Parameters.AddWithValue("@fullName", req.FullName);
        command.Parameters.AddWithValue("@email", req.Email);
        command.Parameters.AddWithValue("@isActive", req.IsActive ?? true);
        command.Parameters.AddWithValue("@phone", (object?)req.Phone ?? DBNull.Value);
        command.Parameters.AddWithValue("@address", (object?)req.Address ?? DBNull.Value);
        command.Parameters.AddWithValue("@housingType", (object?)req.HousingType ?? DBNull.Value);
        command.Parameters.AddWithValue("@hasPets", req.HasPets);
        command.Parameters.AddWithValue("@hasChildren", req.HasChildren);
        command.Parameters.AddWithValue("@experienceWithPets", (object?)req.ExperienceWithPets ?? DBNull.Value);
        command.Parameters.AddWithValue("@preferredContactMethod", (object?)req.PreferredContactMethod ?? DBNull.Value);

        var rows = await command.ExecuteNonQueryAsync();
        await command.DisposeAsync();

        if (rows == 0)
            return NotFound();

        return NoContent();
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest req)
    {
        await using var connection = new MySqlConnection(_configuration.GetConnectionString("DefaultConnection"));
        await connection.OpenAsync();

        var sql = "SELECT applicant_id, password_hash, is_active FROM applicants WHERE email = @email";
        await using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@email", req.Email);
        await using var reader = await command.ExecuteReaderAsync();

        if (!await reader.ReadAsync())
            return Unauthorized(new { message = "Invalid email or password" });

        if (!reader.GetBoolean("is_active"))
            return Unauthorized(new { message = "Account is inactive" });

        var hash = reader.GetString("password_hash");
        var applicantId = reader.GetInt32("applicant_id");

        if (!BCrypt.Net.BCrypt.Verify(req.Password, hash))
            return Unauthorized(new { message = "Invalid email or password" });

        var token = GenerateJwtToken(applicantId.ToString(), req.Email);
        return Ok(new { token, applicantId });
    }

    [HttpPost("{id}/change-password")]
    public async Task<IActionResult> ChangePassword(int id, [FromBody] ChangePasswordRequest req)
    {
        await using var connection = new MySqlConnection(_configuration.GetConnectionString("DefaultConnection"));
        await connection.OpenAsync();

        await using var selectCommand = new MySqlCommand(
            "SELECT password_hash FROM applicants WHERE applicant_id = @id", connection);
        selectCommand.Parameters.AddWithValue("@id", id);
        await using var reader = await selectCommand.ExecuteReaderAsync();

        if (!await reader.ReadAsync())
            return NotFound();

        var currentHash = reader.GetString("password_hash");
        await reader.CloseAsync();

        if (!BCrypt.Net.BCrypt.Verify(req.CurrentPassword, currentHash))
            return BadRequest(new { message = "Current password is incorrect" });

        var newHash = BCrypt.Net.BCrypt.HashPassword(req.NewPassword);
        await using var updateCommand = new MySqlCommand(
            "UPDATE applicants SET password_hash = @hash WHERE applicant_id = @id", connection);
        updateCommand.Parameters.AddWithValue("@hash", newHash);
        updateCommand.Parameters.AddWithValue("@id", id);
        await updateCommand.ExecuteNonQueryAsync();

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

    private string GenerateJwtToken(string applicantId, string email)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, applicantId),
            new Claim(JwtRegisteredClaimNames.Email, email),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };
        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddDays(Convert.ToInt32(_configuration["Jwt:ExpiryDays"])),
            signingCredentials: creds
        );
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static object MapApplicant(MySqlDataReader reader) => new
    {
        applicantId = reader.GetInt32("applicant_id"),
        fullName = reader.GetString("full_name"),
        email = reader.GetString("email"),
        isActive = reader.GetBoolean("is_active"),
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
    string? Password,
    string? Phone,
    string? Address,
    string? HousingType,
    bool HasPets,
    bool HasChildren,
    string? ExperienceWithPets,
    string? PreferredContactMethod,
    bool? IsActive
);

public record ApplicantPasswordVerifyRequest(
    string Email,
    string Password
);
