namespace MunicipalElections.Web.Authorization;

public static class AdminRoles
{
    public const string SuperAdmin = "Super Admin";
    public const string PittMeadowsAdmin = "Pitt Meadows Admin";
    public const string MapleRidgeAdmin = "Maple Ridge Admin";
    public const string BurnabyAdmin = "Burnaby Admin";

    // Explicit mappings to immutable sample identifiers, never parsed display names or PKs.
    public static IReadOnlyDictionary<string, string> MunicipalityGroups { get; } =
        new Dictionary<string, string>
        {
            [PittMeadowsAdmin] = "demo:pitt-meadows",
            [MapleRidgeAdmin] = "demo:maple-ridge",
            [BurnabyAdmin] = "demo:burnaby"
        }.AsReadOnly();
}
