namespace TutorFlow.Api.Services;

public interface IGeminiTextGenerator
{
    Task<string?> GenerateAsync(string model, string systemInstruction, string prompt, CancellationToken cancellationToken);
}
