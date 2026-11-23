using System.Text.RegularExpressions;
using TutorFlow.Api.Contracts;

namespace TutorFlow.Api.Services;

/// <summary>Local deterministic stub. Replace this adapter with the cloud provider without changing the pipeline.</summary>
public sealed class DemoAgentClient : IAgentClient
{
    public Task<AgentReply> SendAsync(AgentRequest request, CancellationToken cancellationToken)
    {
        var terms = Regex.Matches(request.Question.ToLowerInvariant(), @"[\p{L}\p{N}]{3,}")
            .Select(match => match.Value).Distinct().ToArray();
        var candidates = request.Context.Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(line => line.Trim())
            .Where(line => line.Length > 30 && !line.StartsWith("#", StringComparison.Ordinal))
            .Select(line => new { Line = line, Score = terms.Count(term => line.Contains(term, StringComparison.OrdinalIgnoreCase)) })
            .Where(item => item.Score > 0)
            .OrderByDescending(item => item.Score)
            .Take(4)
            .Select(item => item.Line.TrimStart('-', ' ', '*'))
            .ToArray();

        var answer = candidates.Length == 0
            ? "El agente demo recibió tu consulta y preparó el contexto autorizado. La integración con el agente en la nube está pendiente; configura un proveedor que implemente IAgentClient para generar respuestas completas."
            : "Respuesta del agente demo basada en el contexto autorizado:\n\n" +
              string.Join("\n", candidates.Select(line => $"• {line}")) +
              "\n\nLa integración con el agente en la nube está pendiente; este adaptador solo recupera fragmentos relevantes."
              ;
        string[] sources = ["rules.md", "api.md"];
        return Task.FromResult(new AgentReply(answer, sources));
    }
}
