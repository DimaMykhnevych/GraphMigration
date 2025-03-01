using CommandLine;
using DbSeeder.Options;
using Microsoft.Data.SqlClient;

namespace DbSeeder;

public class Program
{
    static async Task Main(string[] args)
    {
        // SocialNetworkDB seeding

        await Parser.Default.ParseArguments<SocialNetworkOptions>(args)
            .WithParsedAsync(async options =>
            {
                Console.WriteLine("Starting Social Network Database Seeder...");
                Console.WriteLine($"Configured to seed: {options.UserCount} users, {options.PostCount} posts, {options.CommentCount} comments");
                Console.WriteLine($"{options.LikeCount} likes, {options.HashtagCount} hashtags, {options.FriendshipCount} friendships");
                Console.WriteLine($"{options.SkillCount} skills, {options.HobbyCount} hobbies, {options.ChatCount} chats, {options.MessageCount} messages");

                var seeder = new SocialNetworkDatabaseSeeder(options);
                await seeder.SeedDatabaseAsync();

                Console.WriteLine("Database seeding completed successfully!");
            });

        // GameDB seeding

        //Parser.Default.ParseArguments<GameOptions>(args).WithParsed(RunGameDbSeeder);
    }


    static void RunGameDbSeeder(GameOptions options)
    {
        Console.WriteLine("Game Database Seeder");
        Console.WriteLine($"Connecting to: {options.ConnectionString}");
        Console.WriteLine();

        using (var connection = new SqlConnection(options.ConnectionString))
        {
            try
            {
                connection.Open();
                Console.WriteLine("Connected to database successfully!");

                var seeder = new GameDatabaseSeeder(connection, options);
                seeder.Seed();

                Console.WriteLine("Database seeding completed successfully!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
            }
        }
    }
}
