using System.Globalization;
using System.Text;
using System.Text.Json;
using TutorFlow.Api.Contracts;

namespace TutorFlow.Api.Services;

public sealed class KnowledgeContextService(MockAcademicApi mockApi, IWebHostEnvironment environment)
{
    public async Task<AcademicContext> BuildAsync(DemoUser user, CancellationToken cancellationToken)
    {
        var directory = await mockApi.GetDirectoryAsync(cancellationToken);
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

        var knownStudentNames = UserDirectory.All
            .Where(candidate => candidate.Role == "Student")
            .ToDictionary(candidate => candidate.Id, candidate => candidate.Name, StringComparer.OrdinalIgnoreCase);
        foreach (var student in directory.Students)
        {
            if (student.TryGetProperty("id", out var id) && student.TryGetProperty("nombre", out var name)
                && !string.IsNullOrWhiteSpace(id.GetString()) && !string.IsNullOrWhiteSpace(name.GetString()))
                knownStudentNames[id.GetString()!] = name.GetString()!;
        }

        var allowedIds = user.Role == "Student"
            ? new HashSet<string>([user.Id], StringComparer.OrdinalIgnoreCase)
            : directory.Available
                ? directory.Students
                    .Where(student => student.TryGetProperty("programaId", out var programId)
                        && user.ProgramIds.Contains(programId.GetString() ?? string.Empty, StringComparer.OrdinalIgnoreCase))
                    .Where(student => student.TryGetProperty("id", out var id) && !string.IsNullOrWhiteSpace(id.GetString()))
                    .Select(student => student.GetProperty("id").GetString()!)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase)
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

        return new AcademicContext(builder.ToString(), allowedIds, knownStudentNames);
    }

    private async Task AppendStudentAsync(StringBuilder builder, string id, CancellationToken cancellationToken)
    {
        builder.AppendLine($"\n### Datos de {id}");
        AppendJson(builder, "Datos básicos", await mockApi.GetStudentAsync(id, cancellationToken));
        var grades = await mockApi.GetGradesAsync(id, cancellationToken);
        AppendJson(builder, "Notas por corte", grades);
        AppendCalculatedGrades(builder, grades);
        AppendJson(builder, "Asistencia", await mockApi.GetAttendanceAsync(id, cancellationToken));
    }

    private static void AppendJson(StringBuilder builder, string label, JsonElement? value)
    {
        if (value is not null)
            builder.AppendLine($"{label}: {value.Value.GetRawText()}");
    }

    private static void AppendCalculatedGrades(StringBuilder builder, JsonElement? grades)
    {
        var calculated = AcademicGradeCalculator.CalculateCourses(grades);
        if (calculated.Count == 0) return;

        builder.AppendLine("Definitivas calculadas por TutorFlow con la ponderación de rules.md:");
        foreach (var course in calculated)
        {
            var definitive = course.Definitive?.ToString("0.0", CultureInfo.InvariantCulture) ?? "pendiente (falta calificar un corte)";
            builder.AppendLine($"- {course.CourseName} ({course.Credits} créditos): {definitive}.");
        }

        var average = AcademicGradeCalculator.CalculateWeightedAverage(calculated);
        if (average is not null)
            builder.AppendLine($"Promedio ponderado calculado por TutorFlow: {average.Value.ToString("0.00", CultureInfo.InvariantCulture)}.");
        builder.AppendLine("Los efectos de inasistencia se verifican por separado con el registro de asistencia y rules.md; los datos no indican si cada inasistencia tiene excusa aprobada.");
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
