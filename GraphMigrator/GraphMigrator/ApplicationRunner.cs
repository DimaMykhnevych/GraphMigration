namespace GraphMigrator;

internal class ApplicationRunner : IApplicationRunner
{
    public void Run()
    {
        Console.Clear();
        Console.Title = "GraphMigrator";
        Console.WriteLine($"Hello from {nameof(ApplicationRunner)}");
    }
}
