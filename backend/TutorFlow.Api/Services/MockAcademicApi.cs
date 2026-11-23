using System.Net.Http.Json;
using System.Text.Json;
using TutorFlow.Api.Contracts;

namespace TutorFlow.Api.Services;

public sealed class MockAcademicApi(HttpClient http)
{
    public Task<JsonElement?> GetStudentAsync(string id, CancellationToken cancellationToken) =>
        GetAsync($"estudiantes/{Uri.EscapeDataString(id)}", cancellationToken);

    public Task<JsonElement?> GetGradesAsync(string id, CancellationToken cancellationToken) =>
        GetAsync($"estudiantes/{Uri.EscapeDataString(id)}/notas", cancellationToken);

    public Task<JsonElement?> GetAttendanceAsync(string id, CancellationToken cancellationToken) =>
        GetAsync($"estudiantes/{Uri.EscapeDataString(id)}/asistencia", cancellationToken);

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
