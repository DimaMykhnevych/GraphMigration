using Bogus;
using DbSeeder.Models.SocialNetwork;
using DbSeeder.Options;
using Microsoft.Data.SqlClient;

namespace DbSeeder;

public class SocialNetworkDatabaseSeeder
{
    private readonly SocialNetworkOptions _options;
    private readonly Dictionary<string, List<Guid>> _ids = new();
    private readonly Random _random = new();

    public SocialNetworkDatabaseSeeder(SocialNetworkOptions options)
    {
        _options = options;
        InitializeIdCollections();
    }

    private void InitializeIdCollections()
    {
        _ids["Users"] = new();
        _ids["UserProfiles"] = new();
        _ids["Posts"] = new();
        _ids["Comments"] = new();
        _ids["Hashtags"] = new();
        _ids["Skills"] = new();
        _ids["Hobbies"] = new();
        _ids["Chats"] = new();
    }

    public async Task SeedDatabaseAsync()
    {
        using var connection = new SqlConnection(_options.ConnectionString);
        await connection.OpenAsync();

        Console.WriteLine("Connected to database. Starting seeding process...");

        ClearExistingData(connection);

        await SeedUsers(connection);
        await SeedPosts(connection);
        await SeedHashtags(connection);
        await SeedHashtagPost(connection);
        await SeedComments(connection);
        await SeedLikes(connection);
        await SeedSkills(connection);
        await SeedHobbies(connection);
        await SeedUserSkills(connection);
        await SeedUserHobbies(connection);
        await SeedFriendships(connection);
        await SeedChats(connection);
        await SeedUserChats(connection);
        await SeedMessages(connection);
        await SeedBlacklists(connection);

        Console.WriteLine("Seeding completed!");
    }

    private void ClearExistingData(SqlConnection connection)
    {
        Console.WriteLine("Clearing existing data...");

        // Order matters due to foreign key constraints
        List<string> commands = new()
        {
            "DELETE FROM [dbo].[UserSkill]",
            "DELETE FROM [dbo].[UserHobby]",
            "DELETE FROM [dbo].[UserProfile]",
            "DELETE FROM [dbo].[UserChat]",
            "DELETE FROM [dbo].[HashtagPost]",
            "DELETE FROM [dbo].[Comment]",
            "DELETE FROM [dbo].[BlackList]",
            "DELETE FROM [dbo].[Message]",
            "DELETE FROM [dbo].[Friendship]",
            "DELETE FROM [dbo].[Hashtag]",
            "DELETE FROM [dbo].[Hobby]",
            "DELETE FROM [dbo].[Like]",
            "DELETE FROM [dbo].[Post]",
            "DELETE FROM [dbo].[Skill]",
            "DELETE FROM [dbo].[Chat]",
            "DELETE FROM [dbo].[User]"
        };

        foreach (var command in commands) 
        {
            ExecuteNonQuery(connection, command);
        }
    }

    private async Task SeedUsers(SqlConnection connection)
    {
        Console.WriteLine($"Seeding {_options.UserCount} users...");

        var faker = new Faker<UserData>()
            .RuleFor(u => u.UserId, f => Guid.NewGuid())
            .RuleFor(u => u.Username, f => f.Internet.UserName())
            .RuleFor(u => u.Email, (f, u) => f.Internet.Email(u.Username))
            .RuleFor(u => u.PasswordHash, f => f.Internet.Password(12, true))
            .RuleFor(u => u.RegistryDate, f => f.Date.Past(3))
            .RuleFor(u => u.ProfileId, f => Guid.NewGuid())
            .RuleFor(u => u.DateOfBirth, f => f.Date.Past(30, DateTime.Now.AddYears(-18)))
            .RuleFor(u => u.Gender, f => f.Random.Int(0, 2))
            .RuleFor(u => u.PlaceOfStudy, f => f.Company.CompanyName());

        var users = faker.Generate(_options.UserCount);

        using var transaction = connection.BeginTransaction();
        foreach (var user in users)
        {
            _ids["Users"].Add(user.UserId);
            _ids["UserProfiles"].Add(user.ProfileId);

            // Insert User
            string userSql = @"INSERT INTO [User] ([UserId], [Username], [Email], [PasswordHash], [RegistryDate])
                                      VALUES (@UserId, @Username, @Email, @PasswordHash, @RegistryDate)";

            using (var command = new SqlCommand(userSql, connection, transaction))
            {
                command.Parameters.AddWithValue("@UserId", user.UserId);
                command.Parameters.AddWithValue("@Username", user.Username);
                command.Parameters.AddWithValue("@Email", user.Email);
                command.Parameters.AddWithValue("@PasswordHash", user.PasswordHash);
                command.Parameters.AddWithValue("@RegistryDate", user.RegistryDate);
                await command.ExecuteNonQueryAsync();
            }

            // Insert UserProfile
            string profileSql = @"INSERT INTO [UserProfile] ([UserProfileId], [DateOfBirth], [Gender], [PlaceOfStudy], [UserId])
                                         VALUES (@UserProfileId, @DateOfBirth, @Gender, @PlaceOfStudy, @UserId)";

            using (var command = new SqlCommand(profileSql, connection, transaction))
            {
                command.Parameters.AddWithValue("@UserProfileId", user.ProfileId);
                command.Parameters.AddWithValue("@DateOfBirth", user.DateOfBirth);
                command.Parameters.AddWithValue("@Gender", user.Gender);
                command.Parameters.AddWithValue("@PlaceOfStudy", user.PlaceOfStudy);
                command.Parameters.AddWithValue("@UserId", user.UserId);
                await command.ExecuteNonQueryAsync();
            }
        }

        transaction.Commit();
    }

    private async Task SeedPosts(SqlConnection connection)
    {
        Console.WriteLine($"Seeding {_options.PostCount} posts...");

        var faker = new Faker<PostData>()
            .RuleFor(p => p.PostId, f => Guid.NewGuid())
            .RuleFor(p => p.Content, f => f.Lorem.Paragraph(1))
            .RuleFor(p => p.Description, f => f.Lorem.Sentence())
            .RuleFor(p => p.CreationDate, f => f.Date.Past(2))
            .RuleFor(p => p.UserId, f => f.PickRandom(_ids["Users"]));

        var posts = faker.Generate(_options.PostCount);

        using var transaction = connection.BeginTransaction();
        foreach (var post in posts)
        {
            _ids["Posts"].Add(post.PostId);

            string sql = @"INSERT INTO [Post] ([PostId], [Content], [CreationDate], [Description], [UserId])
                                  VALUES (@PostId, @Content, @CreationDate, @Description, @UserId)";

            using var command = new SqlCommand(sql, connection, transaction);
            command.Parameters.AddWithValue("@PostId", post.PostId);
            command.Parameters.AddWithValue("@Content", post.Content);
            command.Parameters.AddWithValue("@CreationDate", post.CreationDate);
            command.Parameters.AddWithValue("@Description", post.Description);
            command.Parameters.AddWithValue("@UserId", post.UserId);
            await command.ExecuteNonQueryAsync();
        }

        transaction.Commit();
    }

    private async Task SeedComments(SqlConnection connection)
    {
        Console.WriteLine($"Seeding {_options.CommentCount} comments...");

        var faker = new Faker<CommentData>()
            .RuleFor(c => c.CommentId, f => Guid.NewGuid())
            .RuleFor(c => c.Content, f => f.Lorem.Sentence())
            .RuleFor(c => c.CreationDate, f => f.Date.Recent(30))
            .RuleFor(c => c.UserId, f => f.PickRandom(_ids["Users"]))
            .RuleFor(c => c.PostId, f => f.PickRandom(_ids["Posts"]));

        var comments = faker.Generate(_options.CommentCount);

        using var transaction = connection.BeginTransaction();
        foreach (var comment in comments)
        {
            _ids["Comments"].Add(comment.CommentId);

            string sql = @"INSERT INTO [Comment] ([CommentId], [Content], [CreationDate], [UserId], [PostId])
                                  VALUES (@CommentId, @Content, @CreationDate, @UserId, @PostId)";

            using var command = new SqlCommand(sql, connection, transaction);
            command.Parameters.AddWithValue("@CommentId", comment.CommentId);
            command.Parameters.AddWithValue("@Content", comment.Content);
            command.Parameters.AddWithValue("@CreationDate", comment.CreationDate);
            command.Parameters.AddWithValue("@UserId", comment.UserId);
            command.Parameters.AddWithValue("@PostId", comment.PostId);
            await command.ExecuteNonQueryAsync();
        }

        transaction.Commit();
    }

    private async Task SeedLikes(SqlConnection connection)
    {
        Console.WriteLine($"Seeding {_options.LikeCount} likes...");

        var faker = new Faker<LikeData>()
            .RuleFor(l => l.LikeId, f => Guid.NewGuid())
            .RuleFor(l => l.CreationDate, f => f.Date.Recent(90))
            .RuleFor(l => l.UserId, f => f.PickRandom(_ids["Users"]))
            .RuleFor(l => l.PostId, f => f.PickRandom(_ids["Posts"]));

        var likes = faker.Generate(_options.LikeCount);

        // Create a hashset to track unique user-post pairs for likes
        var uniqueLikes = new HashSet<string>();

        using var transaction = connection.BeginTransaction();
        foreach (var like in likes)
        {
            // Ensure we don't create duplicate likes from the same user on the same post
            string userPostPair = $"{like.UserId}-{like.PostId}";
            if (uniqueLikes.Contains(userPostPair))
                continue;

            uniqueLikes.Add(userPostPair);

            string sql = @"INSERT INTO [Like] ([LikeId], [CreationDate], [UserId], [PostId])
                                  VALUES (@LikeId, @CreationDate, @UserId, @PostId)";

            using var command = new SqlCommand(sql, connection, transaction);
            command.Parameters.AddWithValue("@LikeId", like.LikeId);
            command.Parameters.AddWithValue("@CreationDate", like.CreationDate);
            command.Parameters.AddWithValue("@UserId", like.UserId);
            command.Parameters.AddWithValue("@PostId", like.PostId);
            await command.ExecuteNonQueryAsync();
        }

        transaction.Commit();
    }

    private async Task SeedHashtags(SqlConnection connection)
    {
        Console.WriteLine($"Seeding {_options.HashtagCount} hashtags...");

        var topics = new[] { "tech", "travel", "food", "fitness", "fashion", "music", "art", "science", "gaming", "sports", "nature", "business", "education", "politics", "health" };
        var subtopics = new[] { "news", "inspiration", "tips", "review", "facts", "trends", "humor", "diy", "tutorial", "challenge" };

        var hashtags = new List<HashtagData>();

        for (int i = 0; i < _options.HashtagCount; i++)
        {
            var topic = topics[_random.Next(topics.Length)];
            var subtopic = subtopics[_random.Next(subtopics.Length)];
            var name = $"#{topic}{subtopic}";

            var hashtag = new HashtagData
            {
                HashtagId = Guid.NewGuid(),
                Name = name
            };

            hashtags.Add(hashtag);
        }

        using var transaction = connection.BeginTransaction();
        foreach (var hashtag in hashtags)
        {
            _ids["Hashtags"].Add(hashtag.HashtagId);

            string sql = @"INSERT INTO [Hashtag] ([HashtagId], [Name])
                                  VALUES (@HashtagId, @Name)";

            using var command = new SqlCommand(sql, connection, transaction);
            command.Parameters.AddWithValue("@HashtagId", hashtag.HashtagId);
            command.Parameters.AddWithValue("@Name", hashtag.Name);
            await command.ExecuteNonQueryAsync();
        }

        transaction.Commit();
    }

    private async Task SeedHashtagPost(SqlConnection connection)
    {
        Console.WriteLine("Seeding hashtag-post relationships...");

        var hashtagPostCount = Math.Min(_options.PostCount * 2, _options.PostCount * _options.HashtagCount);
        var uniquePairs = new HashSet<string>();

        using var transaction = connection.BeginTransaction();
        for (int i = 0; i < hashtagPostCount; i++)
        {
            var postId = _ids["Posts"][_random.Next(_ids["Posts"].Count)];
            var hashtagId = _ids["Hashtags"][_random.Next(_ids["Hashtags"].Count)];

            // Ensure we don't create duplicate hashtag-post relationships
            string pair = $"{hashtagId}-{postId}";
            if (uniquePairs.Contains(pair))
                continue;

            uniquePairs.Add(pair);

            string sql = @"INSERT INTO [HashtagPost] ([HashtagPostId], [Relevance], [HashtagId], [PostId])
                                  VALUES (@HashtagPostId, @Relevance, @HashtagId, @PostId)";

            using var command = new SqlCommand(sql, connection, transaction);
            command.Parameters.AddWithValue("@HashtagPostId", Guid.NewGuid());
            command.Parameters.AddWithValue("@Relevance", _random.NextDouble());
            command.Parameters.AddWithValue("@HashtagId", hashtagId);
            command.Parameters.AddWithValue("@PostId", postId);
            await command.ExecuteNonQueryAsync();
        }

        transaction.Commit();
    }

    private async Task SeedSkills(SqlConnection connection)
    {
        Console.WriteLine($"Seeding {_options.SkillCount} skills...");

        var skillsList = new[] {
                new { Name = "Python", Category = "Programming" },
                new { Name = "JavaScript", Category = "Programming" },
                new { Name = "Java", Category = "Programming" },
                new { Name = "C#", Category = "Programming" },
                new { Name = "SQL", Category = "Database" },
                new { Name = "MongoDB", Category = "Database" },
                new { Name = "React", Category = "Frontend" },
                new { Name = "Angular", Category = "Frontend" },
                new { Name = "Node.js", Category = "Backend" },
                new { Name = "ASP.NET", Category = "Backend" },
                new { Name = "Machine Learning", Category = "Data Science" },
                new { Name = "Data Analysis", Category = "Data Science" },
                new { Name = "Photoshop", Category = "Design" },
                new { Name = "Illustrator", Category = "Design" },
                new { Name = "Video Editing", Category = "Media" },
                new { Name = "Photography", Category = "Media" },
                new { Name = "Project Management", Category = "Business" },
                new { Name = "Marketing", Category = "Business" },
                new { Name = "Spanish", Category = "Languages" },
                new { Name = "French", Category = "Languages" },
                new { Name = "German", Category = "Languages" },
                new { Name = "Chinese", Category = "Languages" },
                new { Name = "Public Speaking", Category = "Communication" },
                new { Name = "Writing", Category = "Communication" },
                new { Name = "Leadership", Category = "Soft Skills" },
                new { Name = "Teamwork", Category = "Soft Skills" },
                new { Name = "Problem Solving", Category = "Soft Skills" },
                new { Name = "Critical Thinking", Category = "Soft Skills" },
                new { Name = "Time Management", Category = "Productivity" },
                new { Name = "Organization", Category = "Productivity" },
                new { Name = "Piano", Category = "Music" },
                new { Name = "Guitar", Category = "Music" },
                new { Name = "Singing", Category = "Music" },
                new { Name = "Drawing", Category = "Art" },
                new { Name = "Painting", Category = "Art" }
            };

        // Get a random subset of skills
        var selectedSkills = new List<SkillData>();
        var usedIndexes = new HashSet<int>();

        for (int i = 0; i < Math.Min(_options.SkillCount, skillsList.Length); i++)
        {
            int index;
            do
            {
                index = _random.Next(skillsList.Length);
            } while (usedIndexes.Contains(index));

            usedIndexes.Add(index);
            var skill = skillsList[index];

            selectedSkills.Add(new SkillData
            {
                SkillId = Guid.NewGuid(),
                Name = skill.Name,
                Category = skill.Category
            });
        }

        using var transaction = connection.BeginTransaction();
        foreach (var skill in selectedSkills)
        {
            _ids["Skills"].Add(skill.SkillId);

            string sql = @"INSERT INTO [Skill] ([SkillId], [Name], [Category])
                                  VALUES (@SkillId, @Name, @Category)";

            using var command = new SqlCommand(sql, connection, transaction);
            command.Parameters.AddWithValue("@SkillId", skill.SkillId);
            command.Parameters.AddWithValue("@Name", skill.Name);
            command.Parameters.AddWithValue("@Category", skill.Category);
            await command.ExecuteNonQueryAsync();
        }

        transaction.Commit();
    }

    private async Task SeedHobbies(SqlConnection connection)
    {
        Console.WriteLine($"Seeding {_options.HobbyCount} hobbies...");

        var hobbyList = new[] {
                "Reading", "Traveling", "Hiking", "Gaming", "Cooking", "Photography", "Gardening", "Painting",
                "Dancing", "Running", "Yoga", "Swimming", "Cycling", "Fishing", "Camping", "Chess",
                "Bird Watching", "Knitting", "Model Building", "Collecting", "Singing", "Playing Music",
                "Writing", "Meditation", "Skiing", "Surfing", "Skateboarding", "Pottery", "Woodworking", "Astronomy"
            };

        // Get a random subset of hobbies
        var selectedHobbies = new List<HobbyData>();
        var usedIndexes = new HashSet<int>();

        for (int i = 0; i < Math.Min(_options.HobbyCount, hobbyList.Length); i++)
        {
            int index;
            do
            {
                index = _random.Next(hobbyList.Length);
            } while (usedIndexes.Contains(index));

            usedIndexes.Add(index);
            var hobby = hobbyList[index];

            selectedHobbies.Add(new HobbyData
            {
                HobbyId = Guid.NewGuid(),
                Name = hobby
            });
        }

        using var transaction = connection.BeginTransaction();
        foreach (var hobby in selectedHobbies)
        {
            _ids["Hobbies"].Add(hobby.HobbyId);

            string sql = @"INSERT INTO [Hobby] ([HobbyId], [Name])
                                  VALUES (@HobbyId, @Name)";

            using var command = new SqlCommand(sql, connection, transaction);
            command.Parameters.AddWithValue("@HobbyId", hobby.HobbyId);
            command.Parameters.AddWithValue("@Name", hobby.Name);
            await command.ExecuteNonQueryAsync();
        }

        transaction.Commit();
    }

    private async Task SeedUserSkills(SqlConnection connection)
    {
        Console.WriteLine("Seeding user skills...");

        var uniquePairs = new HashSet<string>();
        var userSkillCount = Math.Min(_options.UserCount * 3, _options.UserCount * _options.SkillCount);

        using var transaction = connection.BeginTransaction();
        for (int i = 0; i < userSkillCount; i++)
        {
            var profileId = _ids["UserProfiles"][_random.Next(_ids["UserProfiles"].Count)];
            var skillId = _ids["Skills"][_random.Next(_ids["Skills"].Count)];

            // Ensure we don't create duplicate user-skill relationships
            string pair = $"{profileId}-{skillId}";
            if (uniquePairs.Contains(pair))
                continue;

            uniquePairs.Add(pair);

            string sql = @"INSERT INTO [UserSkill] ([UserSkillId], [Level], [UserProfileId], [SkillId])
                                  VALUES (@UserSkillId, @Level, @UserProfileId, @SkillId)";

            using var command = new SqlCommand(sql, connection, transaction);
            command.Parameters.AddWithValue("@UserSkillId", Guid.NewGuid());
            command.Parameters.AddWithValue("@Level", _random.Next(1, 6)); // Skill level from 1-5
            command.Parameters.AddWithValue("@UserProfileId", profileId);
            command.Parameters.AddWithValue("@SkillId", skillId);
            await command.ExecuteNonQueryAsync();
        }

        transaction.Commit();
    }

    private async Task SeedUserHobbies(SqlConnection connection)
    {
        Console.WriteLine("Seeding user hobbies...");

        var uniquePairs = new HashSet<string>();
        var userHobbyCount = Math.Min(_options.UserCount * 2, _options.UserCount * _options.HobbyCount);

        using var transaction = connection.BeginTransaction();
        for (int i = 0; i < userHobbyCount; i++)
        {
            var profileId = _ids["UserProfiles"][_random.Next(_ids["UserProfiles"].Count)];
            var hobbyId = _ids["Hobbies"][_random.Next(_ids["Hobbies"].Count)];

            // Ensure we don't create duplicate user-hobby relationships
            string pair = $"{profileId}-{hobbyId}";
            if (uniquePairs.Contains(pair))
                continue;

            uniquePairs.Add(pair);

            string sql = @"INSERT INTO [UserHobby] ([UserHobbyId], [UserProfileId], [HobbyId])
                                  VALUES (@UserHobbyId, @UserProfileId, @HobbyId)";

            using var command = new SqlCommand(sql, connection, transaction);
            command.Parameters.AddWithValue("@UserHobbyId", Guid.NewGuid());
            command.Parameters.AddWithValue("@UserProfileId", profileId);
            command.Parameters.AddWithValue("@HobbyId", hobbyId);
            await command.ExecuteNonQueryAsync();
        }

        transaction.Commit();
    }

    private async Task SeedFriendships(SqlConnection connection)
    {
        Console.WriteLine($"Seeding {_options.FriendshipCount} friendships...");

        var uniquePairs = new HashSet<string>();

        using var transaction = connection.BeginTransaction();
        for (int i = 0; i < _options.FriendshipCount; i++)
        {
            var initiatorId = _ids["Users"][_random.Next(_ids["Users"].Count)];
            var requestedId = _ids["Users"][_random.Next(_ids["Users"].Count)];

            // Skip if same user or if friendship already exists
            if (initiatorId == requestedId)
                continue;

            // Check both directions to avoid duplicate friendships
            string pair1 = $"{initiatorId}-{requestedId}";
            string pair2 = $"{requestedId}-{initiatorId}";

            if (uniquePairs.Contains(pair1) || uniquePairs.Contains(pair2))
                continue;

            uniquePairs.Add(pair1);

            // Status: 0 = Pending, 1 = Accepted, 2 = Rejected
            int status = _random.Next(3);

            string sql = @"INSERT INTO [Friendship] ([FriendshipId], [Status], [CreationDate], [InitiatorId], [RequestedFriendId])
                                  VALUES (@FriendshipId, @Status, @CreationDate, @InitiatorId, @RequestedFriendId)";

            using var command = new SqlCommand(sql, connection, transaction);
            command.Parameters.AddWithValue("@FriendshipId", Guid.NewGuid());
            command.Parameters.AddWithValue("@Status", status);
            command.Parameters.AddWithValue("@CreationDate", DateTime.Now.AddDays(-_random.Next(365)));
            command.Parameters.AddWithValue("@InitiatorId", initiatorId);
            command.Parameters.AddWithValue("@RequestedFriendId", requestedId);
            await command.ExecuteNonQueryAsync();
        }

        transaction.Commit();
    }

    private async Task SeedChats(SqlConnection connection)
    {
        Console.WriteLine($"Seeding {_options.ChatCount} chats...");

        var chatFaker = new Faker<ChatData>()
            .RuleFor(c => c.ChatId, f => Guid.NewGuid())
            .RuleFor(c => c.Title, f => f.Random.Bool(0.3f) ? f.Lorem.Sentence(2, 4) : null) // 30% chance of having a title
            .RuleFor(c => c.CreationDate, f => f.Date.Past(1));

        var chats = chatFaker.Generate(_options.ChatCount);

        using var transaction = connection.BeginTransaction();
        foreach (var chat in chats)
        {
            _ids["Chats"].Add(chat.ChatId);

            string sql = @"INSERT INTO [Chat] ([ChatId], [Title], [CreationDate])
                                  VALUES (@ChatId, @Title, @CreationDate)";

            using var command = new SqlCommand(sql, connection, transaction);
            command.Parameters.AddWithValue("@ChatId", chat.ChatId);
            command.Parameters.AddWithValue("@Title", chat.Title ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@CreationDate", chat.CreationDate);
            await command.ExecuteNonQueryAsync();
        }

        transaction.Commit();
    }

    private async Task SeedUserChats(SqlConnection connection)
    {
        Console.WriteLine("Seeding user-chat relationships...");

        var uniquePairs = new HashSet<string>();

        using var transaction = connection.BeginTransaction();
        foreach (var chatId in _ids["Chats"])
        {
            // Each chat has 2-5 participants
            int participantCount = _random.Next(2, 6);
            var participants = new List<Guid>();

            // Add random participants
            for (int i = 0; i < participantCount; i++)
            {
                var userId = _ids["Users"][_random.Next(_ids["Users"].Count)];

                // Ensure unique users in each chat
                if (participants.Contains(userId))
                    continue;

                participants.Add(userId);

                // UserRole: 0 = Regular, 1 = Admin
                int userRole = (i == 0) ? 1 : 0; // First user is admin

                string sql = @"INSERT INTO [UserChat] ([UserChatId], [UserRole], [UserId], [ChatId])
                              VALUES (@UserChatId, @UserRole, @UserId, @ChatId)";

                using var command = new SqlCommand(sql, connection, transaction);
                command.Parameters.AddWithValue("@UserChatId", Guid.NewGuid());
                command.Parameters.AddWithValue("@UserRole", userRole);
                command.Parameters.AddWithValue("@UserId", userId);
                command.Parameters.AddWithValue("@ChatId", chatId);
                await command.ExecuteNonQueryAsync();
            }
        }

        transaction.Commit();
    }

    private async Task SeedMessages(SqlConnection connection)
    {
        Console.WriteLine($"Seeding {_options.MessageCount} messages...");

        var messageFaker = new Faker<MessageData>()
            .RuleFor(m => m.MessageId, f => Guid.NewGuid())
            .RuleFor(m => m.Content, f => f.Lorem.Sentence())
            .RuleFor(m => m.SendingDate, f => f.Date.Recent(60))
            .RuleFor(m => m.IsRead, f => f.Random.Bool(0.7f)); // 70% of messages are read

        // Get eligible chat participants
        var chatParticipants = new Dictionary<Guid, List<Guid>>();

        using (var command = new SqlCommand("SELECT ChatId, UserId FROM [UserChat]", connection))
        {
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var chatId = reader.GetGuid(0);
                var userId = reader.GetGuid(1);

                if (!chatParticipants.ContainsKey(chatId))
                    chatParticipants[chatId] = new List<Guid>();

                chatParticipants[chatId].Add(userId);
            }
        }

        using var transaction = connection.BeginTransaction();
        // Only proceed if we have chats with participants
        if (chatParticipants.Count > 0)
        {
            for (int i = 0; i < _options.MessageCount; i++)
            {
                // Select a random chat
                var chatIndex = _random.Next(chatParticipants.Count);
                var chatId = chatParticipants.Keys.ElementAt(chatIndex);
                var participants = chatParticipants[chatId];

                // Skip if chat has no participants
                if (participants.Count == 0)
                    continue;

                // Select a random sender
                var senderId = participants[_random.Next(participants.Count)];

                var message = messageFaker.Generate();

                string sql = @"INSERT INTO [Message] ([MessageId], [Content], [SendingDate], [IsRead], [ChatId], [UserId])
                              VALUES (@MessageId, @Content, @SendingDate, @IsRead, @ChatId, @UserId)";

                using var command = new SqlCommand(sql, connection, transaction);
                command.Parameters.AddWithValue("@MessageId", message.MessageId);
                command.Parameters.AddWithValue("@Content", message.Content);
                command.Parameters.AddWithValue("@SendingDate", message.SendingDate);
                command.Parameters.AddWithValue("@IsRead", message.IsRead);
                command.Parameters.AddWithValue("@ChatId", chatId);
                command.Parameters.AddWithValue("@UserId", senderId);
                await command.ExecuteNonQueryAsync();
            }
        }

        transaction.Commit();
    }

    private async Task SeedBlacklists(SqlConnection connection)
    {
        Console.WriteLine("Seeding blacklists...");

        // Generate approximately 5-10% of user count as blacklist entries
        int blacklistCount = (int)(_options.UserCount * (_random.NextDouble() * 0.05 + 0.05));
        Console.WriteLine($"Creating {blacklistCount} blacklist entries...");

        var faker = new Faker<BlacklistData>()
            .RuleFor(b => b.BlackListId, f => Guid.NewGuid())
            .RuleFor(b => b.Reason, f => f.PickRandom(new[] {
            "Inappropriate behavior",
            "Spam",
            "Harassment",
            "Unwanted contact",
            "Privacy concerns",
            "Personal reasons",
            "Offensive content",
            "Fake account"
            }));

        var uniquePairs = new HashSet<string>();

        using var transaction = connection.BeginTransaction();
        for (int i = 0; i < blacklistCount; i++)
        {
            var userId = _ids["Users"][_random.Next(_ids["Users"].Count)];
            var blockedUserId = _ids["Users"][_random.Next(_ids["Users"].Count)];

            // Skip if same user or if blacklist already exists
            if (userId == blockedUserId)
                continue;

            string pair = $"{userId}-{blockedUserId}";
            if (uniquePairs.Contains(pair))
                continue;

            uniquePairs.Add(pair);

            var blacklist = faker.Generate();

            string sql = @"INSERT INTO [BlackList] ([BlackListId], [Reason], [UserId], [BlockedUserId])
                          VALUES (@BlackListId, @Reason, @UserId, @BlockedUserId)";

            using var command = new SqlCommand(sql, connection, transaction);
            command.Parameters.AddWithValue("@BlackListId", blacklist.BlackListId);
            command.Parameters.AddWithValue("@Reason", blacklist.Reason);
            command.Parameters.AddWithValue("@UserId", userId);
            command.Parameters.AddWithValue("@BlockedUserId", blockedUserId);
            await command.ExecuteNonQueryAsync();
        }

        transaction.Commit();
    }

    private static int ExecuteNonQuery(SqlConnection connection, string commandText, Dictionary<string, object> parameters = null)
    {
        using var command = new SqlCommand(commandText, connection);
        if (parameters != null)
        {
            foreach (var param in parameters)
            {
                command.Parameters.AddWithValue(param.Key, param.Value ?? DBNull.Value);
            }
        }

        return command.ExecuteNonQuery();
    }
}
