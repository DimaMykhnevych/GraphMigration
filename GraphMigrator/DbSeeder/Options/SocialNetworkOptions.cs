using CommandLine;

namespace DbSeeder.Options;

public class SocialNetworkOptions
{
    [Option('c', "connection", Required = true, HelpText = "SQL Server connection string")]
    public string ConnectionString { get; set; }

    [Option('u', "users", Default = 100, HelpText = "Number of users to generate")]
    public int UserCount { get; set; }

    [Option('p', "posts", Default = 500, HelpText = "Number of posts to generate")]
    public int PostCount { get; set; }

    [Option('m', "comments", Default = 1000, HelpText = "Number of comments to generate")]
    public int CommentCount { get; set; }

    [Option('l', "likes", Default = 2000, HelpText = "Number of likes to generate")]
    public int LikeCount { get; set; }

    [Option('h', "hashtags", Default = 50, HelpText = "Number of hashtags to generate")]
    public int HashtagCount { get; set; }

    [Option('f', "friendships", Default = 300, HelpText = "Number of friendships to generate")]
    public int FriendshipCount { get; set; }

    [Option('s', "skills", Default = 30, HelpText = "Number of skills to generate")]
    public int SkillCount { get; set; }

    [Option('b', "hobbies", Default = 20, HelpText = "Number of hobbies to generate")]
    public int HobbyCount { get; set; }

    [Option('g', "chats", Default = 50, HelpText = "Number of chats to generate")]
    public int ChatCount { get; set; }

    [Option('n', "messages", Default = 1000, HelpText = "Number of messages to generate")]
    public int MessageCount { get; set; }
}

