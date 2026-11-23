using TutorFlow.Api.Contracts;

namespace TutorFlow.Api.Services;

public static class UserDirectory
{
    private static readonly DemoUser[] Users =
    [
        new("EST-001", "Laura Gómez", "Student", "ING-SIS", []),
        new("EST-002", "Andrés Martínez", "Student", "ING-SIS", []),
        new("EST-003", "Sofía Herrera", "Student", "ING-SIS", []),
        new("EST-004", "Camilo Rojas", "Student", "ADM", []),
        new("EST-005", "Valentina Castro", "Student", "ADM", []),
        new("EST-006", "Mateo Vargas", "Student", "ADM", []),
        new("COORD-01", "Marcela Ríos", "Coordinator", null, ["ING-SIS"]),
        new("COORD-02", "Julián Pardo", "Coordinator", null, ["ADM"])
    ];

    public static IReadOnlyList<DemoUser> All => Users;
    public static DemoUser? Find(string? id) => Users.FirstOrDefault(user =>
        string.Equals(user.Id, id?.Trim(), StringComparison.OrdinalIgnoreCase));
    public static string[] StudentIdsForPrograms(IEnumerable<string> programIds) => Users
        .Where(user => user.Role == "Student" && user.ProgramId is not null && programIds.Contains(user.ProgramId))
        .Select(user => user.Id)
        .ToArray();
}
