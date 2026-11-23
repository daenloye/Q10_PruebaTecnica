using System.Collections.Concurrent;
using System.Security.Cryptography;
using TutorFlow.Api.Contracts;

namespace TutorFlow.Api.Services;

public sealed class SessionStore
{
    private readonly ConcurrentDictionary<string, DemoUser> _sessions = new();

    public string Create(DemoUser user)
    {
        string token;
        do
        {
            token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
                .TrimEnd('=').Replace('+', '-').Replace('/', '_');
        } while (!_sessions.TryAdd(token, user));
        return token;
    }

    public bool TryGet(string? token, out DemoUser? user)
    {
        if (token is not null && _sessions.TryGetValue(token, out var found))
        {
            user = found;
            return true;
        }
        user = null;
        return false;
    }

    public void Remove(string? token)
    {
        if (token is not null) _sessions.TryRemove(token, out _);
    }
}
