using TutorFlow.Api.Contracts;

namespace TutorFlow.Api.Services;

public interface IAgentClient
{
    Task<AgentReply> SendAsync(AgentRequest request, CancellationToken cancellationToken);
}
