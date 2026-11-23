using System.Net.Http.Json;
using System.Text.Json;
using TutorFlow.Api.Contracts;

namespace TutorFlow.Api.Services;

public sealed class MockAcademicApi(HttpClient http)
{
    public async Task<AcademicDirectorySnapshot> GetDirectoryAsync(CancellationToken cancellationToken)
    {
        var programsResponse = await GetAsync("programas", cancellationToken);
        if (programsResponse is null || programsResponse.Value.ValueKind != JsonValueKind.Array)
            return new AcademicDirectorySnapshot(false, [], []);

        var programs = programsResponse.Value;
        var programList = programs.EnumerateArray().Select(program => program.Clone()).ToArray();
        var students = new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase);
        foreach (var program in programList)
        {
            if (!program.TryGetProperty("id", out var idValue) || string.IsNullOrWhiteSpace(idValue.GetString()))
                continue;

            var studentResponse = await GetProgramStudentsAsync(idValue.GetString()!, cancellationToken);
            if (studentResponse is null || studentResponse.Value.ValueKind != JsonValueKind.Array)
                continue;

            var programStudents = studentResponse.Value;
            foreach (var student in programStudents.EnumerateArray())
            {
                if (student.TryGetProperty("id", out var studentId) && !string.IsNullOrWhiteSpace(studentId.GetString()))
                    students[studentId.GetString()!] = student.Clone();
            }
        }

        return new AcademicDirectorySnapshot(true, programList, students.Values.ToArray());
    }

    public Task<JsonElement?> GetStudentAsync(string id, CancellationToken cancellationToken) =>
        GetAsync($"estudiantes/{Uri.EscapeDataString(id)}", cancellationToken);

    public Task<JsonElement?> GetGradesAsync(string id, CancellationToken cancellationToken) =>
        GetAsync($"estudiantes/{Uri.EscapeDataString(id)}/notas", cancellationToken);

    public Task<JsonElement?> GetAttendanceAsync(string id, CancellationToken cancellationToken) =>
        GetAsync($"estudiantes/{Uri.EscapeDataString(id)}/asistencia", cancellationToken);

    public Task<JsonElement?> GetProgramStudentsAsync(string programId, CancellationToken cancellationToken) =>
        GetAsync($"programas/{Uri.EscapeDataString(programId)}/estudiantes", cancellationToken);

    public Task<JsonElement?> GetCoordinatorAsync(string id, CancellationToken cancellationToken) =>
        GetAsync($"coordinadores/{Uri.EscapeDataString(id)}", cancellationToken);

    private async Task<JsonElement?> GetAsync(string path, CancellationToken cancellationToken)
    {
        try
        {
            using var response = await http.GetAsync(path, cancellationToken);
            if (!response.IsSuccessStatusCode) return null;
            return await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: cancellationToken);
        }
        catch (HttpRequestException) { return null; }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested) { return null; }
    }
}

public sealed record AcademicDirectorySnapshot(bool Available, IReadOnlyList<JsonElement> Programs, IReadOnlyList<JsonElement> Students);
