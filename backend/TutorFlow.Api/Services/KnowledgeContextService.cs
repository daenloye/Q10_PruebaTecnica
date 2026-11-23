using System.Text;
using System.Text.Json;
using TutorFlow.Api.Contracts;

namespace TutorFlow.Api.Services;

public sealed class KnowledgeContextService(MockAcademicApi mockApi, IWebHostEnvironment environment)
{
    public async Task<AcademicContext> BuildAsync(DemoUser user, CancellationToken cancellationToken)
    {
        var rulesPath = Path.Combine(AppContext.BaseDirectory, "Knowledge", "rules.md");
        var apiPath = Path.Combine(AppContext.BaseDirectory, "Knowledge", "api.md");
        var apiKnowledge = await ReadAsync(apiPath);
        // api.md contains real-shaped examples from the shared mock fixture. Keep its schema
        // useful while preventing another student's sample identity from entering this session.
        foreach (var student in UserDirectory.All.Where(candidate => candidate.Role == "Student"))
        {
            apiKnowledge = apiKnowledge.Replace(student.Id, "[ID de estudiante de ejemplo]", StringComparison.OrdinalIgnoreCase)
                .Replace(student.Name, "[nombre de estudiante de ejemplo]", StringComparison.OrdinalIgnoreCase);
        }
        var builder = new StringBuilder();
        builder.AppendLine("## Reglamento académico y reglas de negocio\n").AppendLine(await ReadAsync(rulesPath));
        builder.AppendLine("\n## Especificación de API (identidades de los ejemplos anonimizadas)\n").AppendLine(apiKnowledge);

        var allowedIds = user.Role == "Student"
            ? new HashSet<string>([user.Id], StringComparer.OrdinalIgnoreCase)
            : new HashSet<string>(UserDirectory.StudentIdsForPrograms(user.ProgramIds), StringComparer.OrdinalIgnoreCase);

        builder.AppendLine("\n## Contexto académico permitido para esta sesión\n");
        if (user.Role == "Student")
        {
            await AppendStudentAsync(builder, user.Id, cancellationToken);
        }
        else
        {
            builder.AppendLine($"Coordinador: {user.Id}; programas autorizados: {string.Join(", ", user.ProgramIds)}.");
            builder.AppendLine("Estudiantes autorizados en esos programas: " + string.Join(", ", allowedIds));
            foreach (var studentId in allowedIds)
                await AppendStudentAsync(builder, studentId, cancellationToken);
        }

        return new AcademicContext(builder.ToString(), allowedIds);
    }

    private async Task AppendStudentAsync(StringBuilder builder, string id, CancellationToken cancellationToken)
    {
        builder.AppendLine($"\n### Datos de {id}");
        AppendJson(builder, "Datos básicos", await mockApi.GetStudentAsync(id, cancellationToken));
        AppendJson(builder, "Notas por corte", await mockApi.GetGradesAsync(id, cancellationToken));
        AppendJson(builder, "Asistencia", await mockApi.GetAttendanceAsync(id, cancellationToken));
    }

    private static void AppendJson(StringBuilder builder, string label, JsonElement? value)
    {
        if (value is not null)
            builder.AppendLine($"{label}: {value.Value.GetRawText()}");
    }

    private async Task<string> ReadAsync(string path)
    {
        if (File.Exists(path)) return await File.ReadAllTextAsync(path);
        // Helpful fallback for `dotnet run` from the repository root during development.
        var fileName = Path.GetFileName(path);
        var rootCopy = Path.GetFullPath(Path.Combine(environment.ContentRootPath, "..", "..", fileName));
        return File.Exists(rootCopy) ? await File.ReadAllTextAsync(rootCopy) : "Knowledge document unavailable.";
    }
}
