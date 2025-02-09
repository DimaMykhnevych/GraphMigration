using GraphMigrator.Domain.Entities;

namespace GraphMigrator.Algorithms.RelationalSchemaExtractors;

public interface IRelationalSchemaExtractor
{
    Task<RelationalDatabaseSchema> GetSchema();
}
