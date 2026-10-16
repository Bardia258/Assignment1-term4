namespace MunicipalElections.Web.Authorization;

public static class AdminRoles
{
    public const string SuperAdmin = "Super Admin";
    public const string PittMeadowsAdmin = "Pitt Meadows Admin";
    public const string MapleRidgeAdmin = "Maple Ridge Admin";
    public const string BurnabyAdmin = "Burnaby Admin";

    // Development bootstrap names only. Runtime authorization uses persisted
    // MunicipalityAdminGroups, including groups for dynamically created municipalities.
    public static IReadOnlyDictionary<string, string> MunicipalityGroups { get; } =
        new Dictionary<string, string>
        {
            [PittMeadowsAdmin] = "demo:pitt-meadows",
            [MapleRidgeAdmin] = "demo:maple-ridge",
            [BurnabyAdmin] = "demo:burnaby"
        }.AsReadOnly();
}
