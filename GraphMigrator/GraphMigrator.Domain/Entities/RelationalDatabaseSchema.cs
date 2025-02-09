namespace GraphMigrator.Domain.Entities;

public class RelationalDatabaseSchema
{
    public List<TableSchema> Tables { get; set; } = [];
}
