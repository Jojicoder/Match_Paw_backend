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
public class UsersController : ControllerBase
{
    private readonly IConfiguration _configuration;

    public UsersController(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    [HttpGet]
    public async Task<IActionResult> GetUsers()
    {
        var users = new List<object>();
        await using var connection = new MySqlConnection(_configuration.GetConnectionString("DefaultConnection"));
        await connection.OpenAsync();

        var sql = "SELECT user_id, full_name, email, role, is_active, created_at FROM users ORDER BY user_id";
        await using var command = new MySqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            users.Add(MapUser(reader));
        }

        return Ok(users);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetUser(int id)
    {
        await using var connection = new MySqlConnection(_configuration.GetConnectionString("DefaultConnection"));
        await connection.OpenAsync();

        var sql = "SELECT user_id, full_name, email, role, is_active, created_at FROM users WHERE user_id = @id";
        await using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@id", id);
        await using var reader = await command.ExecuteReaderAsync();

        if (!await reader.ReadAsync())
            return NotFound();

        return Ok(MapUser(reader));
    }

    [HttpPost]
    public async Task<IActionResult> CreateUser([FromBody] UserRequest req)
    {
        await using var connection = new MySqlConnection(_configuration.GetConnectionString("DefaultConnection"));
        await connection.OpenAsync();

        var passwordHash = BCrypt.Net.BCrypt.HashPassword(req.Password);

        var sql = @"INSERT INTO users (full_name, email, password_hash, role, is_active)
                    VALUES (@fullName, @email, @passwordHash, @role, @isActive);
                    SELECT LAST_INSERT_ID();";

        await using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@fullName", req.FullName);
        command.Parameters.AddWithValue("@email", req.Email);
        command.Parameters.AddWithValue("@passwordHash", passwordHash);
        command.Parameters.AddWithValue("@role", req.Role);
        command.Parameters.AddWithValue("@isActive", req.IsActive ?? true);

        var newId = Convert.ToInt32(await command.ExecuteScalarAsync());
        return CreatedAtAction(nameof(GetUser), new { id = newId }, new { userId = newId });
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateUser(int id, [FromBody] UserRequest req)
    {
        await using var connection = new MySqlConnection(_configuration.GetConnectionString("DefaultConnection"));
        await connection.OpenAsync();

        string sql;
        MySqlCommand command;

        if (!string.IsNullOrEmpty(req.Password))
        {
            var passwordHash = BCrypt.Net.BCrypt.HashPassword(req.Password);
            sql = @"UPDATE users SET full_name = @fullName, email = @email,
                        password_hash = @passwordHash, role = @role, is_active = @isActive
                    WHERE user_id = @id";
            command = new MySqlCommand(sql, connection);
            command.Parameters.AddWithValue("@passwordHash", passwordHash);
        }
        else
        {
            sql = @"UPDATE users SET full_name = @fullName, email = @email,
                        role = @role, is_active = @isActive
                    WHERE user_id = @id";
            command = new MySqlCommand(sql, connection);
        }

        command.Parameters.AddWithValue("@id", id);
        command.Parameters.AddWithValue("@fullName", req.FullName);
        command.Parameters.AddWithValue("@email", req.Email);
        command.Parameters.AddWithValue("@role", req.Role);
        command.Parameters.AddWithValue("@isActive", req.IsActive ?? true);

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

        var sql = "SELECT user_id, password_hash, role, is_active FROM users WHERE email = @email";
        await using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@email", req.Email);
        await using var reader = await command.ExecuteReaderAsync();

        if (!await reader.ReadAsync())
            return Unauthorized(new { message = "Invalid email or password" });

        if (!reader.GetBoolean("is_active"))
            return Unauthorized(new { message = "Account is inactive" });

        var hash = reader.GetString("password_hash");
        var userId = reader.GetInt32("user_id");
        var role = reader.GetString("role");

        if (!BCrypt.Net.BCrypt.Verify(req.Password, hash))
            return Unauthorized(new { message = "Invalid email or password" });

        var token = GenerateJwtToken(userId.ToString(), req.Email, role);
        return Ok(new { token, userId, role });
    }

    [HttpPost("{id}/change-password")]
    public async Task<IActionResult> ChangePassword(int id, [FromBody] ChangePasswordRequest req)
    {
        await using var connection = new MySqlConnection(_configuration.GetConnectionString("DefaultConnection"));
        await connection.OpenAsync();

        await using var selectCommand = new MySqlCommand(
            "SELECT password_hash FROM users WHERE user_id = @id", connection);
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
            "UPDATE users SET password_hash = @hash WHERE user_id = @id", connection);
        updateCommand.Parameters.AddWithValue("@hash", newHash);
        updateCommand.Parameters.AddWithValue("@id", id);
        await updateCommand.ExecuteNonQueryAsync();

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteUser(int id)
    {
        await using var connection = new MySqlConnection(_configuration.GetConnectionString("DefaultConnection"));
        await connection.OpenAsync();

        await using var command = new MySqlCommand("DELETE FROM users WHERE user_id = @id", connection);
        command.Parameters.AddWithValue("@id", id);

        var rows = await command.ExecuteNonQueryAsync();
        if (rows == 0)
            return NotFound();

        return NoContent();
    }

    private string GenerateJwtToken(string userId, string email, string role)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, userId),
            new Claim(JwtRegisteredClaimNames.Email, email),
            new Claim(ClaimTypes.Role, role),
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

    private static object MapUser(MySqlDataReader reader) => new
    {
        userId = reader.GetInt32("user_id"),
        fullName = reader.GetString("full_name"),
        email = reader.GetString("email"),
        role = reader.GetString("role"),
        isActive = reader.GetBoolean("is_active"),
        createdAt = reader.GetDateTime("created_at")
    };
}

public record UserRequest(
    string FullName,
    string Email,
    string? Password,
    string Role,
    bool? IsActive
);

public record LoginRequest(string Email, string Password);

public record ChangePasswordRequest(string CurrentPassword, string NewPassword);
