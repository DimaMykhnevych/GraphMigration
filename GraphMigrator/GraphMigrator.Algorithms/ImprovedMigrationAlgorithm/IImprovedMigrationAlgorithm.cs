namespace GraphMigrator.Algorithms.ImprovedMigrationAlgorithmN;

public interface IImprovedMigrationAlgorithm : IDisposable
{
    Task MigrateToGraphDatabaseAsync(CancellationToken cancellationToken);
}
