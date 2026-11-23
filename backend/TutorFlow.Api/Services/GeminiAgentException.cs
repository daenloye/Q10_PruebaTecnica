namespace TutorFlow.Api.Services;

public sealed class GeminiAgentException(
    int statusCode,
    string code,
    string publicMessage,
    Exception? innerException = null) : Exception(publicMessage, innerException)
{
    public int StatusCode { get; } = statusCode;
    public string Code { get; } = code;
    public string PublicMessage { get; } = publicMessage;
}
