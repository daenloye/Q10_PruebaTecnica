using System.Text.RegularExpressions;

namespace TutorFlow.Api.Services;

public static partial class PromptGuard
{
    [GeneratedRegex("(ignora|ignore|disregard|forget)\\s+(all\\s+|todas?\\s+)?(las?\\s+)?(instrucciones|reglas|previous instructions)|revela(r)?\\s+(el\\s+)?(prompt|instrucciones)\\s+(del\\s+)?sistema|system\\s+prompt|act[uú]a\\s+como\\s+(admin|administrador)|bypass\\s+(the\\s+)?(rules|permissions|security)|muestra(r)?\\s+(la\\s+)?lista\\s+completa\\s+de\\s+estudiantes", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex InjectionPattern();

    [GeneratedRegex("\\bEST-\\d{3}\\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex StudentIdPattern();

    public static bool HasObviousInjection(string message) => InjectionPattern().IsMatch(message);
    public static IEnumerable<string> MentionedStudentIds(string message) =>
        StudentIdPattern().Matches(message).Select(match => match.Value.ToUpperInvariant()).Distinct();
}
