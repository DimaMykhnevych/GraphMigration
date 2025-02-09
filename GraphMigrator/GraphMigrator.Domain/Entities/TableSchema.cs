namespace GraphMigrator.Domain.Entities;

public class TableSchema
{
    public string Name { get; set; }
    public List<ColumnSchema> Columns { get; set; } = [];
    public List<string> PrimaryKeys { get; set; } = [];
    public List<ForeignKeySchema> ForeignKeys { get; set; } = [];
}
