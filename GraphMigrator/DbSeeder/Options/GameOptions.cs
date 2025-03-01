using CommandLine;

namespace DbSeeder.Options;

public class GameOptions
{
    [Option('c', "connection", Required = true, HelpText = "SQL Server connection string")]
    public string ConnectionString { get; set; }

    [Option('u', "users", Default = 100, HelpText = "Number of users to generate")]
    public int UserCount { get; set; }

    [Option('h', "character-per-user", Default = 2, HelpText = "Number of characters per user")]
    public int CharactersPerUser { get; set; }

    [Option('l', "logins-per-user", Default = 10, HelpText = "Number of login history entries per user")]
    public int LoginsPerUser { get; set; }

    [Option('a', "attributes", Default = 10, HelpText = "Number of attributes")]
    public int AttributeCount { get; set; }

    [Option('i', "items", Default = 100, HelpText = "Number of items")]
    public int ItemCount { get; set; }

    [Option('n', "npcs", Default = 50, HelpText = "Number of NPCs")]
    public int NpcCount { get; set; }

    [Option('q', "quests", Default = 30, HelpText = "Number of quests")]
    public int QuestCount { get; set; }

    [Option('e', "enemies", Default = 50, HelpText = "Number of enemies")]
    public int EnemyCount { get; set; }
}

