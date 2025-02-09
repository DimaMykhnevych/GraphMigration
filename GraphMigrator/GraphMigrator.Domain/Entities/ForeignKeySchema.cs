namespace GraphMigrator.Domain.Entities;

public class ForeignKeySchema
{
    public string Name { get; set; }
    public string ColumnName { get; set; }
    public string ReferencedTableSchema { get; set; }
    public string ReferencedTableName { get; set; }
    public string ReferencedColumnName { get; set; }
}
