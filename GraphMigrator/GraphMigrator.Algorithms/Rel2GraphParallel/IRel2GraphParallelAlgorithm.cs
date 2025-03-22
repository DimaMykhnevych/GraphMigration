namespace GraphMigrator.Algorithms.Rel2GraphParallel;

public interface IRel2GraphParallelAlgorithm : IDisposable
{
    Task MigrateToGraphDatabaseAsync(CancellationToken cancellationToken);
}
