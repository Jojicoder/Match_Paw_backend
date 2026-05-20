using Microsoft.AspNetCore.Mvc;
using MySqlConnector;

namespace MatchPawBackend.Controllers;

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

        var passwordHash = BCrypt.Net.BCrypt.HashPassword(req.Password);

        var sql = @"UPDATE users SET full_name = @fullName, email = @email,
                        password_hash = @passwordHash, role = @role, is_active = @isActive
                    WHERE user_id = @id";

        await using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@id", id);
        command.Parameters.AddWithValue("@fullName", req.FullName);
        command.Parameters.AddWithValue("@email", req.Email);
        command.Parameters.AddWithValue("@passwordHash", passwordHash);
        command.Parameters.AddWithValue("@role", req.Role);
        command.Parameters.AddWithValue("@isActive", req.IsActive ?? true);

        var rows = await command.ExecuteNonQueryAsync();
        if (rows == 0)
            return NotFound();

        return NoContent();
    }

    [HttpPost("verify-password")]
    public async Task<IActionResult> VerifyPassword([FromBody] PasswordVerifyRequest req)
    {
        await using var connection = new MySqlConnection(_configuration.GetConnectionString("DefaultConnection"));
        await connection.OpenAsync();

        var sql = "SELECT password_hash FROM users WHERE email = @email";
        await using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@email", req.Email);
        await using var reader = await command.ExecuteReaderAsync();

        if (!await reader.ReadAsync())
            return Unauthorized(new { message = "Invalid email or password" });

        var hash = reader.GetString("password_hash");
        var isValid = BCrypt.Net.BCrypt.Verify(req.Password, hash);

        if (!isValid)
            return Unauthorized(new { message = "Invalid email or password" });

        return Ok(new { message = "Password verified" });
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
    string Password,
    string Role,
    bool? IsActive
);

public record PasswordVerifyRequest(
    string Email,
    string Password
);
