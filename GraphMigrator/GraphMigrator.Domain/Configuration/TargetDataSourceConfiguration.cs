namespace GraphMigrator.Domain.Configuration;

public class TargetDataSourceConfiguration
{
    public Uri Connection { get; set; }

    public string User { get; set; }

    public string Password { get; set; }
}
