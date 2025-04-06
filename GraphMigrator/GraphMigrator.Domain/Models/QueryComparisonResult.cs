namespace GraphMigrator.Domain.Models;

public class QueryComparisonResult
{
    public bool AreIdentical { get; set; } = true;
    public List<string> Differences { get; set; } = [];

    public List<Dictionary<string, object>> SqlResults { get; set; } = [];
    public List<Dictionary<string, object>> CypherResults { get; set; } = [];
}
