namespace TutorFlow.Api.Contracts;

public sealed record DemoUser(string Id, string Name, string Role, string? ProgramId, string[] ProgramIds);
public sealed record LoginRequest(string UserId);
public sealed record LoginResponse(string Token, DemoUser User);
public sealed record ChatRequest(string? Message);
public sealed record ChatResponse(string Answer, string[] Sources, string Provider);
public sealed record AgentRequest(DemoUser User, string Question, string Context, string Rules);
public sealed record AgentReply(string Answer, string[] Sources);
public sealed record AcademicContext(string Text, HashSet<string> AllowedStudentIds);
