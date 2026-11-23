using System.Globalization;
using System.Text.Json;

namespace TutorFlow.Api.Services;

public sealed record CalculatedCourseGrade(string CourseName, int Credits, decimal? Definitive);

/// <summary>Applies the weighted-cut formula and rounding rule defined in rules.md.</summary>
public static class AcademicGradeCalculator
{
    public static IReadOnlyList<CalculatedCourseGrade> CalculateCourses(JsonElement? grades)
    {
        if (grades is null || grades.Value.ValueKind != JsonValueKind.Array)
            return [];

        var values = grades.Value;
        var result = new List<CalculatedCourseGrade>();
        foreach (var grade in values.EnumerateArray())
        {
            var name = grade.TryGetProperty("asignatura", out var nameValue) ? nameValue.GetString() ?? "Asignatura" : "Asignatura";
            var credits = grade.TryGetProperty("creditos", out var creditValue) && creditValue.TryGetInt32(out var parsedCredits)
                ? parsedCredits
                : 0;
            var cuts = new[] { ReadDecimal(grade, "corte1"), ReadDecimal(grade, "corte2"), ReadDecimal(grade, "corte3") };
            decimal? definitive = cuts.Any(cut => cut is null)
                ? null
                : decimal.Round(cuts[0]!.Value * 0.30m + cuts[1]!.Value * 0.30m + cuts[2]!.Value * 0.40m,
                    1, MidpointRounding.AwayFromZero);
            result.Add(new CalculatedCourseGrade(name, credits, definitive));
        }

        return result;
    }

    public static decimal? CalculateWeightedAverage(IEnumerable<CalculatedCourseGrade> grades)
    {
        var gradedCourses = grades.Where(grade => grade.Definitive is not null && grade.Credits > 0).ToArray();
        var totalCredits = gradedCourses.Sum(grade => grade.Credits);
        if (totalCredits == 0) return null;

        var weightedSum = gradedCourses.Sum(grade => grade.Definitive!.Value * grade.Credits);
        return decimal.Round(weightedSum / totalCredits, 2, MidpointRounding.AwayFromZero);
    }

    private static decimal? ReadDecimal(JsonElement element, string propertyName) =>
        element.TryGetProperty(propertyName, out var value) && value.ValueKind == JsonValueKind.Number && value.TryGetDecimal(out var number)
            ? number
            : null;
}
