using Microsoft.AspNetCore.Mvc;
using MySqlConnector;

namespace MatchPawBackend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MedicalRecordsController : ControllerBase
{
    private readonly IConfiguration _configuration;

    public MedicalRecordsController(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    [HttpGet]
    public async Task<IActionResult> GetMedicalRecords()
    {
        var records = new List<object>();
        await using var connection = new MySqlConnection(_configuration.GetConnectionString("DefaultConnection"));
        await connection.OpenAsync();

        var sql = @"SELECT medical_record_id, animal_id, record_date, treatment_type,
                           description, veterinarian_name, next_appointment, created_at
                    FROM medical_records ORDER BY medical_record_id";

        await using var command = new MySqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            records.Add(MapRecord(reader));
        }

        return Ok(records);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetMedicalRecord(int id)
    {
        await using var connection = new MySqlConnection(_configuration.GetConnectionString("DefaultConnection"));
        await connection.OpenAsync();

        var sql = @"SELECT medical_record_id, animal_id, record_date, treatment_type,
                           description, veterinarian_name, next_appointment, created_at
                    FROM medical_records WHERE medical_record_id = @id";

        await using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@id", id);
        await using var reader = await command.ExecuteReaderAsync();

        if (!await reader.ReadAsync())
            return NotFound();

        return Ok(MapRecord(reader));
    }

    [HttpGet("animal/{animalId}")]
    public async Task<IActionResult> GetByAnimal(int animalId)
    {
        var records = new List<object>();
        await using var connection = new MySqlConnection(_configuration.GetConnectionString("DefaultConnection"));
        await connection.OpenAsync();

        var sql = @"SELECT medical_record_id, animal_id, record_date, treatment_type,
                           description, veterinarian_name, next_appointment, created_at
                    FROM medical_records WHERE animal_id = @animalId ORDER BY record_date DESC";

        await using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@animalId", animalId);
        await using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            records.Add(MapRecord(reader));
        }

        return Ok(records);
    }

    [HttpPost]
    public async Task<IActionResult> CreateMedicalRecord([FromBody] MedicalRecordRequest req)
    {
        await using var connection = new MySqlConnection(_configuration.GetConnectionString("DefaultConnection"));
        await connection.OpenAsync();

        var sql = @"INSERT INTO medical_records (animal_id, record_date, treatment_type, description, veterinarian_name, next_appointment)
                    VALUES (@animalId, @recordDate, @treatmentType, @description, @veterinarianName, @nextAppointment);
                    SELECT LAST_INSERT_ID();";

        await using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@animalId", req.AnimalId);
        command.Parameters.AddWithValue("@recordDate", req.RecordDate);
        command.Parameters.AddWithValue("@treatmentType", (object?)req.TreatmentType ?? DBNull.Value);
        command.Parameters.AddWithValue("@description", (object?)req.Description ?? DBNull.Value);
        command.Parameters.AddWithValue("@veterinarianName", (object?)req.VeterinarianName ?? DBNull.Value);
        command.Parameters.AddWithValue("@nextAppointment", (object?)req.NextAppointment ?? DBNull.Value);

        var newId = Convert.ToInt32(await command.ExecuteScalarAsync());
        return CreatedAtAction(nameof(GetMedicalRecord), new { id = newId }, new { medicalRecordId = newId });
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateMedicalRecord(int id, [FromBody] MedicalRecordRequest req)
    {
        await using var connection = new MySqlConnection(_configuration.GetConnectionString("DefaultConnection"));
        await connection.OpenAsync();

        var sql = @"UPDATE medical_records SET
                        animal_id = @animalId, record_date = @recordDate, treatment_type = @treatmentType,
                        description = @description, veterinarian_name = @veterinarianName, next_appointment = @nextAppointment
                    WHERE medical_record_id = @id";

        await using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@id", id);
        command.Parameters.AddWithValue("@animalId", req.AnimalId);
        command.Parameters.AddWithValue("@recordDate", req.RecordDate);
        command.Parameters.AddWithValue("@treatmentType", (object?)req.TreatmentType ?? DBNull.Value);
        command.Parameters.AddWithValue("@description", (object?)req.Description ?? DBNull.Value);
        command.Parameters.AddWithValue("@veterinarianName", (object?)req.VeterinarianName ?? DBNull.Value);
        command.Parameters.AddWithValue("@nextAppointment", (object?)req.NextAppointment ?? DBNull.Value);

        var rows = await command.ExecuteNonQueryAsync();
        if (rows == 0)
            return NotFound();

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteMedicalRecord(int id)
    {
        await using var connection = new MySqlConnection(_configuration.GetConnectionString("DefaultConnection"));
        await connection.OpenAsync();

        await using var command = new MySqlCommand("DELETE FROM medical_records WHERE medical_record_id = @id", connection);
        command.Parameters.AddWithValue("@id", id);

        var rows = await command.ExecuteNonQueryAsync();
        if (rows == 0)
            return NotFound();

        return NoContent();
    }

    private static object MapRecord(MySqlDataReader reader) => new
    {
        medicalRecordId = reader.GetInt32("medical_record_id"),
        animalId = reader.GetInt32("animal_id"),
        recordDate = reader.GetDateTime("record_date").ToString("yyyy-MM-dd"),
        treatmentType = reader.IsDBNull(reader.GetOrdinal("treatment_type")) ? null : reader.GetString("treatment_type"),
        description = reader.IsDBNull(reader.GetOrdinal("description")) ? null : reader.GetString("description"),
        veterinarianName = reader.IsDBNull(reader.GetOrdinal("veterinarian_name")) ? null : reader.GetString("veterinarian_name"),
        nextAppointment = reader.IsDBNull(reader.GetOrdinal("next_appointment")) ? null : reader.GetDateTime("next_appointment").ToString("yyyy-MM-dd"),
        createdAt = reader.GetDateTime("created_at")
    };
}

public record MedicalRecordRequest(
    int AnimalId,
    string RecordDate,
    string? TreatmentType,
    string? Description,
    string? VeterinarianName,
    string? NextAppointment
);
