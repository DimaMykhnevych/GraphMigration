namespace GraphMigrator.Domain.Entities;

public class ColumnSchema
{
    public string Name { get; set; }
    public string DataType { get; set; }
    public int? MaxLength { get; set; }
    public bool IsNullable { get; set; }
    public string DefaultValue { get; set; }
}

