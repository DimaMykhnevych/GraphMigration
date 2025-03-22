using GraphMigrator.Domain.Entities;

namespace GraphMigrator.Domain.Models;

public class ForeignKeyPair
{
    public TableSchema Table { get; set; }
    public ForeignKeySchema ForeignKey { get; set; }
}

