using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MySqlConnector;

namespace MatchPawBackend.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class CareLogsController : ControllerBase
{
    private readonly IConfiguration _configuration;

    public CareLogsController(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    [HttpGet]
    public async Task<IActionResult> GetCareLogs()
    {
        var logs = new List<object>();
        await using var connection = new MySqlConnection(_configuration.GetConnectionString("DefaultConnection"));
        await connection.OpenAsync();

        var sql = @"SELECT care_log_id, animal_id, user_id, log_date,
                           feeding_notes, cleaning_notes, behavior_notes, created_at
                    FROM care_logs ORDER BY care_log_id";

        await using var command = new MySqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            logs.Add(MapLog(reader));
        }

        return Ok(logs);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetCareLog(int id)
    {
        await using var connection = new MySqlConnection(_configuration.GetConnectionString("DefaultConnection"));
        await connection.OpenAsync();

        var sql = @"SELECT care_log_id, animal_id, user_id, log_date,
                           feeding_notes, cleaning_notes, behavior_notes, created_at
                    FROM care_logs WHERE care_log_id = @id";

        await using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@id", id);
        await using var reader = await command.ExecuteReaderAsync();

        if (!await reader.ReadAsync())
            return NotFound();

        return Ok(MapLog(reader));
    }

    [HttpGet("animal/{animalId}")]
    public async Task<IActionResult> GetByAnimal(int animalId)
    {
        var logs = new List<object>();
        await using var connection = new MySqlConnection(_configuration.GetConnectionString("DefaultConnection"));
        await connection.OpenAsync();

        var sql = @"SELECT care_log_id, animal_id, user_id, log_date,
                           feeding_notes, cleaning_notes, behavior_notes, created_at
                    FROM care_logs WHERE animal_id = @animalId ORDER BY log_date DESC";

        await using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@animalId", animalId);
        await using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            logs.Add(MapLog(reader));
        }

        return Ok(logs);
    }

    [HttpPost]
    public async Task<IActionResult> CreateCareLog([FromBody] CareLogRequest req)
    {
        await using var connection = new MySqlConnection(_configuration.GetConnectionString("DefaultConnection"));
        await connection.OpenAsync();

        var sql = @"INSERT INTO care_logs (animal_id, user_id, log_date, feeding_notes, cleaning_notes, behavior_notes)
                    VALUES (@animalId, @userId, @logDate, @feedingNotes, @cleaningNotes, @behaviorNotes);
                    SELECT LAST_INSERT_ID();";

        await using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@animalId", req.AnimalId);
        command.Parameters.AddWithValue("@userId", req.UserId);
        command.Parameters.AddWithValue("@logDate", req.LogDate);
        command.Parameters.AddWithValue("@feedingNotes", (object?)req.FeedingNotes ?? DBNull.Value);
        command.Parameters.AddWithValue("@cleaningNotes", (object?)req.CleaningNotes ?? DBNull.Value);
        command.Parameters.AddWithValue("@behaviorNotes", (object?)req.BehaviorNotes ?? DBNull.Value);

        var newId = Convert.ToInt32(await command.ExecuteScalarAsync());
        return CreatedAtAction(nameof(GetCareLog), new { id = newId }, new { careLogId = newId });
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateCareLog(int id, [FromBody] CareLogRequest req)
    {
        await using var connection = new MySqlConnection(_configuration.GetConnectionString("DefaultConnection"));
        await connection.OpenAsync();

        var sql = @"UPDATE care_logs SET
                        animal_id = @animalId, user_id = @userId, log_date = @logDate,
                        feeding_notes = @feedingNotes, cleaning_notes = @cleaningNotes, behavior_notes = @behaviorNotes
                    WHERE care_log_id = @id";

        await using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@id", id);
        command.Parameters.AddWithValue("@animalId", req.AnimalId);
        command.Parameters.AddWithValue("@userId", req.UserId);
        command.Parameters.AddWithValue("@logDate", req.LogDate);
        command.Parameters.AddWithValue("@feedingNotes", (object?)req.FeedingNotes ?? DBNull.Value);
        command.Parameters.AddWithValue("@cleaningNotes", (object?)req.CleaningNotes ?? DBNull.Value);
        command.Parameters.AddWithValue("@behaviorNotes", (object?)req.BehaviorNotes ?? DBNull.Value);

        var rows = await command.ExecuteNonQueryAsync();
        if (rows == 0)
            return NotFound();

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteCareLog(int id)
    {
        await using var connection = new MySqlConnection(_configuration.GetConnectionString("DefaultConnection"));
        await connection.OpenAsync();

        await using var command = new MySqlCommand("DELETE FROM care_logs WHERE care_log_id = @id", connection);
        command.Parameters.AddWithValue("@id", id);

        var rows = await command.ExecuteNonQueryAsync();
        if (rows == 0)
            return NotFound();

        return NoContent();
    }

    private static object MapLog(MySqlDataReader reader) => new
    {
        careLogId = reader.GetInt32("care_log_id"),
        animalId = reader.GetInt32("animal_id"),
        userId = reader.GetInt32("user_id"),
        logDate = reader.GetDateTime("log_date").ToString("yyyy-MM-dd"),
        feedingNotes = reader.IsDBNull(reader.GetOrdinal("feeding_notes")) ? null : reader.GetString("feeding_notes"),
        cleaningNotes = reader.IsDBNull(reader.GetOrdinal("cleaning_notes")) ? null : reader.GetString("cleaning_notes"),
        behaviorNotes = reader.IsDBNull(reader.GetOrdinal("behavior_notes")) ? null : reader.GetString("behavior_notes"),
        createdAt = reader.GetDateTime("created_at")
    };
}

public record CareLogRequest(
    int AnimalId,
    int UserId,
    string LogDate,
    string? FeedingNotes,
    string? CleaningNotes,
    string? BehaviorNotes
);
