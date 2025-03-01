using Bogus;
using DbSeeder.Models.Game;
using DbSeeder.Options;
using Microsoft.Data.SqlClient;

namespace DbSeeder;

public class GameDatabaseSeeder
{
    private readonly SqlConnection _connection;
    private readonly GameOptions _options;
    private readonly Random _random = new();
    private readonly Dictionary<string, List<int>> _entityIds = new();

    public GameDatabaseSeeder(SqlConnection connection, GameOptions options)
    {
        _connection = connection;
        _options = options;
    }

    public void Seed()
    {
        Console.WriteLine("Starting database seeding...");

        ClearExistingData();

        // Seed data in the correct order to maintain referential integrity
        SeedAttributes();
        SeedUsers();
        SeedLoginHistory();
        SeedItems();
        SeedCharacters();
        SeedCharacterAttributes();
        SeedCharacterStates();
        SeedAbilities();
        SeedEquipment();
        SeedInventoryItems();
        SeedItemAttributes();
        SeedNPCs();
        SeedQuests();
        SeedQuestStages();
        SeedQuestRewards();
        SeedQuestProgress();
        SeedEnemies();
        SeedEnemyAttributes();
        SeedVictoryRewards();
        SeedLoots();
        SeedDroppedItems();

        Console.WriteLine("All data seeded successfully!");
    }

    private void ClearExistingData()
    {
        Console.WriteLine("Clearing existing data...");

        // Order matters due to foreign key constraints
        ExecuteNonQuery("DELETE FROM DroppedItem");
        ExecuteNonQuery("DELETE FROM Loot");
        ExecuteNonQuery("DELETE FROM VictoryReward");
        ExecuteNonQuery("DELETE FROM EnemyAttribute");
        ExecuteNonQuery("DELETE FROM Enemy");
        ExecuteNonQuery("DELETE FROM QuestProgress");
        ExecuteNonQuery("DELETE FROM QuestReward");
        ExecuteNonQuery("DELETE FROM QuestStage");
        ExecuteNonQuery("DELETE FROM Quest");
        ExecuteNonQuery("DELETE FROM NPC");
        ExecuteNonQuery("DELETE FROM ItemAttribute");
        ExecuteNonQuery("DELETE FROM InventoryItem");
        ExecuteNonQuery("DELETE FROM Equipment");
        ExecuteNonQuery("DELETE FROM Ability");
        ExecuteNonQuery("DELETE FROM CharacterState");
        ExecuteNonQuery("DELETE FROM CharacterAttribute");
        ExecuteNonQuery("DELETE FROM Character");
        ExecuteNonQuery("DELETE FROM Item");
        ExecuteNonQuery("DELETE FROM LoginHistory");
        ExecuteNonQuery("DELETE FROM [User]");
        ExecuteNonQuery("DELETE FROM Attribute");
    }

    private void SeedAttributes()
    {
        Console.WriteLine($"Seeding {_options.AttributeCount} attributes...");

        var attributeFaker = new Faker<AttributeData>()
            .RuleFor(a => a.Id, f => f.IndexFaker + 1)
            .RuleFor(a => a.Name, f => f.PickRandom(new[] {
                    "Strength", "Dexterity", "Constitution", "Intelligence", "Wisdom", "Charisma",
                    "Agility", "Luck", "Perception", "Endurance", "Willpower", "Focus",
                    "Stealth", "Blocking", "Archery", "One-handed", "Two-handed", "Conjuration",
                    "Destruction", "Restoration", "Alteration", "Enchanting", "Smithing", "Alchemy"
            }))
            .RuleFor(a => a.Description, f => f.Lorem.Sentence());

        var attributes = attributeFaker.Generate(_options.AttributeCount);
        BulkInsert("Attribute", attributes, (attribute, cmd) =>
        {
            cmd.Parameters.AddWithValue("@Id", attribute.Id);
            cmd.Parameters.AddWithValue("@Name", attribute.Name);
            cmd.Parameters.AddWithValue("@Description", attribute.Description);
        });

        _entityIds["Attribute"] = attributes.Select(a => a.Id).ToList();
    }

    private void SeedUsers()
    {
        Console.WriteLine($"Seeding {_options.UserCount} users...");

        var userFaker = new Faker<UserData>()
            .RuleFor(u => u.Id, f => f.IndexFaker + 1)
            .RuleFor(u => u.Email, f => f.Internet.Email())
            .RuleFor(u => u.Password, f => f.Internet.Password())
            .RuleFor(u => u.Nickname, f => f.Internet.UserName())
            .RuleFor(u => u.RegistrationDate, f => f.Date.Past(2));

        var users = userFaker.Generate(_options.UserCount);
        BulkInsert("[User]", users, (user, cmd) =>
        {
            cmd.Parameters.AddWithValue("@Id", user.Id);
            cmd.Parameters.AddWithValue("@Email", user.Email);
            cmd.Parameters.AddWithValue("@Password", user.Password);
            cmd.Parameters.AddWithValue("@Nickname", user.Nickname);
            cmd.Parameters.AddWithValue("@RegistrationDate", user.RegistrationDate);
        });

        _entityIds["User"] = users.Select(u => u.Id).ToList();
    }

    private void SeedLoginHistory()
    {
        Console.WriteLine($"Seeding login history ({_options.LoginsPerUser} entries per user)...");

        var loginFaker = new Faker<LoginHistoryData>();
        var loginHistories = new List<LoginHistoryData>();
        var loginId = 1;

        foreach (var userId in _entityIds["User"])
        {
            var userLogins = new Faker<LoginHistoryData>()
                .RuleFor(l => l.Id, f => loginId++)
                .RuleFor(l => l.UserId, f => userId)
                .RuleFor(l => l.LoginDateTime, f => f.Date.Past(1))
                .RuleFor(l => l.LogoutDateTime, (f, l) => f.Date.Between(l.LoginDateTime, l.LoginDateTime.AddHours(f.Random.Number(1, 6))))
                .RuleFor(l => l.IpAddress, f => f.Internet.Ip())
                .RuleFor(l => l.DeviceName, f => f.PickRandom(new[] {
                        "Windows PC", "MacBook Pro", "iPhone", "Android", "iPad", "Linux Desktop" }))
                .Generate(_options.LoginsPerUser);

            loginHistories.AddRange(userLogins);
        }

        BulkInsert("LoginHistory", loginHistories, (login, cmd) =>
        {
            cmd.Parameters.AddWithValue("@Id", login.Id);
            cmd.Parameters.AddWithValue("@UserId", login.UserId);
            cmd.Parameters.AddWithValue("@LoginDateTime", login.LoginDateTime);
            cmd.Parameters.AddWithValue("@LogoutDateTime", login.LogoutDateTime);
            cmd.Parameters.AddWithValue("@IpAddress", login.IpAddress);
            cmd.Parameters.AddWithValue("@DeviceName", login.DeviceName);
        });

        _entityIds["LoginHistory"] = loginHistories.Select(l => l.Id).ToList();
    }

    private void SeedItems()
    {
        Console.WriteLine($"Seeding {_options.ItemCount} items...");

        var itemTypes = new[] { "Weapon", "Armor", "Potion", "Scroll", "Material", "Quest", "Food", "Jewelry" };

        var itemFaker = new Faker<ItemData>()
            .RuleFor(i => i.Id, f => f.IndexFaker + 1)
            .RuleFor(i => i.Name, f => $"{f.Commerce.ProductAdjective()} {f.Commerce.ProductName()}")
            .RuleFor(i => i.Type, f => f.PickRandom(itemTypes))
            .RuleFor(i => i.RequiredLevel, f => f.Random.Number(1, 50))
            .RuleFor(i => i.Description, f => f.Commerce.ProductDescription());

        var items = itemFaker.Generate(_options.ItemCount);
        BulkInsert("Item", items, (item, cmd) =>
        {
            cmd.Parameters.AddWithValue("@Id", item.Id);
            cmd.Parameters.AddWithValue("@Name", item.Name);
            cmd.Parameters.AddWithValue("@Type", item.Type);
            cmd.Parameters.AddWithValue("@RequiredLevel", item.RequiredLevel);
            cmd.Parameters.AddWithValue("@Description", item.Description);
        });

        _entityIds["Item"] = items.Select(i => i.Id).ToList();
    }

    private void SeedCharacters()
    {
        Console.WriteLine($"Seeding characters ({_options.CharactersPerUser} per user)...");

        var characterNames = new[] {
                "Aragorn", "Gandalf", "Legolas", "Gimli", "Frodo", "Samwise", "Boromir", "Meriadoc", "Peregrin",
                "Arwen", "Galadriel", "Eowyn", "Elrond", "Thranduil", "Denethor", "Théoden", "Eomer", "Celeborn",
                "Arthur", "Lancelot", "Guinevere", "Mordred", "Galahad", "Percival", "Tristan", "Isolde", "Gawain",
                "Odysseus", "Achilles", "Hector", "Agamemnon", "Menelaus", "Ajax", "Aeneas", "Helen", "Paris",
                "Beowulf", "Siegfried", "Brunhild", "Thor", "Odin", "Loki", "Freya", "Frigg", "Baldr",
                "Merlin", "Morgana", "Nimue", "Gawain", "Elaine", "Igraine", "Uther", "Ygraine", "Gorlois",
                "Roland", "Oliver", "Charlemagne", "William", "Robin", "Marian", "Richard", "Joan", "Lancelot"
            };

        var characterFaker = new Faker<CharacterData>();
        var characters = new List<CharacterData>();
        var characterId = 1;

        foreach (var userId in _entityIds["User"])
        {
            var userCharacters = new Faker<CharacterData>()
                .RuleFor(c => c.Id, f => characterId++)
                .RuleFor(c => c.Name, f => f.PickRandom(characterNames) + "_" + f.Random.AlphaNumeric(4))
                .RuleFor(c => c.Level, f => f.Random.Number(1, 50))
                .RuleFor(c => c.Xp, (f, c) => c.Level * f.Random.Number(1000, 5000))
                .RuleFor(c => c.Currency, f => f.Random.Number(0, 100000))
                .RuleFor(c => c.UserId, f => userId)
                .Generate(_options.CharactersPerUser);

            characters.AddRange(userCharacters);
        }

        BulkInsert("[Character]", characters, (character, cmd) =>
        {
            cmd.Parameters.AddWithValue("@Id", character.Id);
            cmd.Parameters.AddWithValue("@Name", character.Name);
            cmd.Parameters.AddWithValue("@Level", character.Level);
            cmd.Parameters.AddWithValue("@Xp", character.Xp);
            cmd.Parameters.AddWithValue("@Currency", character.Currency);
            cmd.Parameters.AddWithValue("@UserId", character.UserId);
        });

        _entityIds["Character"] = characters.Select(c => c.Id).ToList();
    }

    private void SeedCharacterAttributes()
    {
        Console.WriteLine("Seeding character attributes...");

        var characterAttributeFaker = new Faker<CharacterAttributeData>();
        var characterAttributes = new List<CharacterAttributeData>();
        var characterAttributeId = 1;

        foreach (var characterId in _entityIds["Character"])
        {
            // Each character gets 5 random attributes
            var characterAttributeIds = _entityIds["Attribute"]
                .OrderBy(x => Guid.NewGuid())
                .Take(5)
                .ToList();

            foreach (var attributeId in characterAttributeIds)
            {
                characterAttributes.Add(new CharacterAttributeData
                {
                    Id = characterAttributeId++,
                    Value = _random.Next(1, 100),
                    CharacterId = characterId,
                    AttributeId = attributeId
                });
            }
        }

        BulkInsert("CharacterAttribute", characterAttributes, (characterAttribute, cmd) =>
        {
            cmd.Parameters.AddWithValue("@Id", characterAttribute.Id);
            cmd.Parameters.AddWithValue("@Value", characterAttribute.Value);
            cmd.Parameters.AddWithValue("@CharacterId", characterAttribute.CharacterId);
            cmd.Parameters.AddWithValue("@AttributeId", characterAttribute.AttributeId);
        });

        _entityIds["CharacterAttribute"] = characterAttributes.Select(ca => ca.Id).ToList();
    }

    private void SeedCharacterStates()
    {
        Console.WriteLine("Seeding character states...");

        var characterStateFaker = new Faker<CharacterStateData>();
        var characterStates = new List<CharacterStateData>();
        var characterStateId = 1;

        foreach (var characterId in _entityIds["Character"])
        {
            characterStates.Add(new CharacterStateData
            {
                Id = characterStateId++,
                Coordinates = $"{_random.Next(-1000, 1000)},{_random.Next(-1000, 1000)},{_random.Next(0, 100)}",
                IsAlive = _random.Next(0, 10) > 1, // 90% chance of being alive
                Hp = _random.Next(50, 1000),
                Mp = _random.Next(50, 1000),
                CharacterId = characterId
            });
        }

        BulkInsert("CharacterState", characterStates, (characterState, cmd) =>
        {
            cmd.Parameters.AddWithValue("@Id", characterState.Id);
            cmd.Parameters.AddWithValue("@Coordinates", characterState.Coordinates);
            cmd.Parameters.AddWithValue("@IsAlive", characterState.IsAlive);
            cmd.Parameters.AddWithValue("@Hp", characterState.Hp);
            cmd.Parameters.AddWithValue("@Mp", characterState.Mp);
            cmd.Parameters.AddWithValue("@CharacterId", characterState.CharacterId);
        });

        _entityIds["CharacterState"] = characterStates.Select(cs => cs.Id).ToList();
    }

    private void SeedAbilities()
    {
        Console.WriteLine("Seeding character abilities...");

        var abilityNames = new[] {
                "Fireball", "Ice Spike", "Healing Wave", "Thunder Strike", "Shadow Step",
                "Divine Smite", "Backstab", "Whirlwind", "Shield Bash", "Snipe",
                "Arcane Missiles", "Charge", "Blink", "Mind Control", "Summon Elemental",
                "Death Grip", "Rejuvenation", "Sunfire", "Moonbeam", "Earthquake"
            };

        var abilitiesFaker = new Faker<AbilityData>();
        var abilities = new List<AbilityData>();
        var abilityId = 1;

        foreach (var characterId in _entityIds["Character"])
        {
            // Each character gets 1-4 abilities based on level
            var character = ExecuteScalar<int>($"SELECT [Level] FROM [Character] WHERE Id = {characterId}");
            var numAbilities = Math.Min(4, Math.Max(1, character / 10));

            for (int i = 0; i < numAbilities; i++)
            {
                abilities.Add(new AbilityData
                {
                    Id = abilityId++,
                    Name = abilityNames[_random.Next(abilityNames.Length)],
                    Description = new Faker().Lorem.Sentence(),
                    MinimalLevel = _random.Next(1, character + 1),
                    AbilityType = _random.Next(1, 5), // 1: Attack, 2: Defense, 3: Utility, 4: Healing
                    DamageType = _random.Next(0, 6),  // 0: None, 1: Physical, 2: Fire, 3: Ice, 4: Lightning, 5: Shadow
                    DamageValue = _random.Next(10, 100),
                    CharacterId = characterId
                });
            }
        }

        BulkInsert("Ability", abilities, (ability, cmd) =>
        {
            cmd.Parameters.AddWithValue("@Id", ability.Id);
            cmd.Parameters.AddWithValue("@Name", ability.Name);
            cmd.Parameters.AddWithValue("@Description", ability.Description);
            cmd.Parameters.AddWithValue("@MinimalLevel", ability.MinimalLevel);
            cmd.Parameters.AddWithValue("@AbilityType", ability.AbilityType);
            cmd.Parameters.AddWithValue("@DamageType", ability.DamageType);
            cmd.Parameters.AddWithValue("@DamageValue", ability.DamageValue);
            cmd.Parameters.AddWithValue("@CharacterId", ability.CharacterId);
        });

        _entityIds["Ability"] = abilities.Select(a => a.Id).ToList();
    }

    private void SeedEquipment()
    {
        Console.WriteLine("Seeding character equipment...");

        var slots = new[] { "Head", "Chest", "Legs", "Feet", "Hands", "MainHand", "OffHand", "Ring1", "Ring2", "Necklace" };
        var equipmentFaker = new Faker<EquipmentData>();
        var equipments = new List<EquipmentData>();
        var equipmentId = 1;

        // Get weapon and armor items
        var weaponArmorItems = ExecuteReader<ItemData>("SELECT Id, Type, RequiredLevel FROM Item WHERE Type IN ('Weapon', 'Armor', 'Jewelry')");

        foreach (var characterId in _entityIds["Character"])
        {
            // Character level determines what equipment they can use
            var characterLevel = ExecuteScalar<int>($"SELECT [Level] FROM [Character] WHERE Id = {characterId}");
            var eligibleItems = weaponArmorItems.Where(i => i.RequiredLevel <= characterLevel).ToList();

            if (eligibleItems.Any())
            {
                // Randomly equip 3-8 slots
                var slotsToEquip = slots.OrderBy(x => Guid.NewGuid()).Take(_random.Next(3, 9)).ToList();

                foreach (var slot in slotsToEquip)
                {
                    // For each slot, find appropriate item type
                    string itemType = "Armor";
                    if (slot == "MainHand" || slot == "OffHand")
                        itemType = "Weapon";
                    else if (slot == "Ring1" || slot == "Ring2" || slot == "Necklace")
                        itemType = "Jewelry";

                    var filteredItems = eligibleItems.Where(i => i.Type == itemType).ToList();
                    if (filteredItems.Any())
                    {
                        var randomItem = filteredItems[_random.Next(filteredItems.Count)];
                        equipments.Add(new EquipmentData
                        {
                            Id = equipmentId++,
                            CharacterId = characterId,
                            ItemId = randomItem.Id,
                            Slot = slot
                        });
                    }
                }
            }
        }

        BulkInsert("Equipment", equipments, (equipment, cmd) =>
        {
            cmd.Parameters.AddWithValue("@Id", equipment.Id);
            cmd.Parameters.AddWithValue("@CharacterId", equipment.CharacterId);
            cmd.Parameters.AddWithValue("@ItemId", equipment.ItemId);
            cmd.Parameters.AddWithValue("@Slot", equipment.Slot);
        });

        _entityIds["Equipment"] = equipments.Select(e => e.Id).ToList();
    }

    private void SeedInventoryItems()
    {
        Console.WriteLine("Seeding character inventory items...");

        var inventoryItemFaker = new Faker<InventoryItemData>();
        var inventoryItems = new List<InventoryItemData>();
        var inventoryItemId = 1;

        foreach (var characterId in _entityIds["Character"])
        {
            // Each character gets 5-15 items
            var itemCount = _random.Next(5, 16);
            var characterInventoryItems = new List<int>();

            for (int i = 0; i < itemCount; i++)
            {
                var itemId = _entityIds["Item"][_random.Next(_entityIds["Item"].Count)];

                // Check if item is already in inventory
                if (characterInventoryItems.Contains(itemId))
                {
                    // Find the existing item and update its count
                    var existingItem = inventoryItems.FirstOrDefault(ii => ii.CharacterId == characterId && ii.ItemId == itemId);
                    if (existingItem != null)
                    {
                        existingItem.Count += _random.Next(1, 10);
                    }
                }
                else
                {
                    // Add new item to inventory
                    characterInventoryItems.Add(itemId);
                    inventoryItems.Add(new InventoryItemData
                    {
                        Id = inventoryItemId++,
                        CharacterId = characterId,
                        ItemId = itemId,
                        Count = _random.Next(1, 10)
                    });
                }
            }
        }

        BulkInsert("InventoryItem", inventoryItems, (inventoryItem, cmd) =>
        {
            cmd.Parameters.AddWithValue("@Id", inventoryItem.Id);
            cmd.Parameters.AddWithValue("@CharacterId", inventoryItem.CharacterId);
            cmd.Parameters.AddWithValue("@ItemId", inventoryItem.ItemId);
            cmd.Parameters.AddWithValue("@Count", inventoryItem.Count);
        });

        _entityIds["InventoryItem"] = inventoryItems.Select(ii => ii.Id).ToList();
    }

    private void SeedItemAttributes()
    {
        Console.WriteLine("Seeding item attributes...");

        var itemAttributeFaker = new Faker<ItemAttributeData>();
        var itemAttributes = new List<ItemAttributeData>();
        var itemAttributeId = 1;

        // Get weapon, armor, and jewelry items which typically have attributes
        var itemsWithAttributes = ExecuteReader<ItemData>("SELECT Id FROM Item WHERE Type IN ('Weapon', 'Armor', 'Jewelry')");

        foreach (var item in itemsWithAttributes)
        {
            // Each item gets 1-3 random attributes
            var attributeCount = _random.Next(1, 4);
            var attributeIds = _entityIds["Attribute"]
                .OrderBy(x => Guid.NewGuid())
                .Take(attributeCount)
                .ToList();

            foreach (var attributeId in attributeIds)
            {
                itemAttributes.Add(new ItemAttributeData
                {
                    Id = itemAttributeId++,
                    Value = _random.Next(1, 50),
                    ItemId = item.Id,
                    AttributeId = attributeId
                });
            }
        }

        BulkInsert("ItemAttribute", itemAttributes, (itemAttribute, cmd) =>
        {
            cmd.Parameters.AddWithValue("@Id", itemAttribute.Id);
            cmd.Parameters.AddWithValue("@Value", itemAttribute.Value);
            cmd.Parameters.AddWithValue("@ItemId", itemAttribute.ItemId);
            cmd.Parameters.AddWithValue("@AttributeId", itemAttribute.AttributeId);
        });

        _entityIds["ItemAttribute"] = itemAttributes.Select(ia => ia.Id).ToList();
    }

    private void SeedNPCs()
    {
        Console.WriteLine($"Seeding {_options.NpcCount} NPCs...");

        var npcNames = new Faker<string>()
            .CustomInstantiator(f => f.Name.FirstName());

        var npcFaker = new Faker<NPCData>()
            .RuleFor(n => n.Id, f => f.IndexFaker + 1)
            .RuleFor(n => n.Name, f => npcNames.Generate())
            .RuleFor(n => n.Coordinates, f => $"{f.Random.Number(-1000, 1000)},{f.Random.Number(-1000, 1000)},{f.Random.Number(0, 100)}");

        var npcs = npcFaker.Generate(_options.NpcCount);
        BulkInsert("NPC", npcs, (npc, cmd) =>
        {
            cmd.Parameters.AddWithValue("@Id", npc.Id);
            cmd.Parameters.AddWithValue("@Name", npc.Name);
            cmd.Parameters.AddWithValue("@Coordinates", npc.Coordinates);
        });

        _entityIds["NPC"] = npcs.Select(n => n.Id).ToList();
    }

    private void SeedQuests()
    {
        Console.WriteLine($"Seeding {_options.QuestCount} quests...");

        var questFaker = new Faker<QuestData>()
            .RuleFor(q => q.Id, f => f.IndexFaker + 1)
            .RuleFor(q => q.Name, f => $"{f.Commerce.ProductAdjective()} {f.PickRandom(new[] { "Quest", "Adventure", "Journey", "Mission", "Task" })}")
            .RuleFor(q => q.Description, f => f.Lorem.Paragraph())
            .RuleFor(q => q.Reward, f => f.Random.Number(100, 5000))
            .RuleFor(q => q.NpcId, f => f.PickRandom(_entityIds["NPC"]));

        var quests = questFaker.Generate(_options.QuestCount);
        BulkInsert("Quest", quests, (quest, cmd) =>
        {
            cmd.Parameters.AddWithValue("@Id", quest.Id);
            cmd.Parameters.AddWithValue("@Name", quest.Name);
            cmd.Parameters.AddWithValue("@Description", quest.Description);
            cmd.Parameters.AddWithValue("@Reward", quest.Reward);
            cmd.Parameters.AddWithValue("@NpcId", quest.NpcId);
        });

        _entityIds["Quest"] = quests.Select(q => q.Id).ToList();
    }

    private void SeedQuestStages()
    {
        Console.WriteLine("Seeding quest stages...");

        var questStageFaker = new Faker<QuestStageData>();
        var questStages = new List<QuestStageData>();
        var questStageId = 1;

        foreach (var questId in _entityIds["Quest"])
        {
            // Each quest gets 2-5 stages
            var stageCount = _random.Next(2, 6);

            for (int i = 0; i < stageCount; i++)
            {
                var stage = new QuestStageData
                {
                    Id = questStageId++,
                    QuestId = questId,
                    Name = $"Stage {i + 1}: {new Faker().PickRandom(new[] { "Find", "Defeat", "Collect", "Escort", "Scout", "Investigate" })}",
                    Objective = new Faker().Lorem.Sentence(),
                    Description = new Faker().Lorem.Paragraph()
                };
                questStages.Add(stage);
            }
        }

        BulkInsert("QuestStage", questStages, (questStage, cmd) =>
        {
            cmd.Parameters.AddWithValue("@Id", questStage.Id);
            cmd.Parameters.AddWithValue("@QuestId", questStage.QuestId);
            cmd.Parameters.AddWithValue("@Name", questStage.Name);
            cmd.Parameters.AddWithValue("@Objective", questStage.Objective);
            cmd.Parameters.AddWithValue("@Description", questStage.Description);
        });

        _entityIds["QuestStage"] = questStages.Select(qs => qs.Id).ToList();
    }

    private void SeedQuestRewards()
    {
        Console.WriteLine("Seeding quest rewards...");

        var questRewardFaker = new Faker<QuestRewardData>();
        var questRewards = new List<QuestRewardData>();
        var questRewardId = 1;

        foreach (var questId in _entityIds["Quest"])
        {
            // Each quest gets 1-3 item rewards
            var rewardCount = _random.Next(1, 4);

            for (int i = 0; i < rewardCount; i++)
            {
                var itemId = _entityIds["Item"][_random.Next(_entityIds["Item"].Count)];

                questRewards.Add(new QuestRewardData
                {
                    Id = questRewardId++,
                    QuestId = questId,
                    ItemId = itemId,
                    Count = _random.Next(1, 5)
                });
            }
        }

        BulkInsert("QuestReward", questRewards, (reward, cmd) =>
        {
            cmd.Parameters.AddWithValue("@Id", reward.Id);
            cmd.Parameters.AddWithValue("@QuestId", reward.QuestId);
            cmd.Parameters.AddWithValue("@ItemId", reward.ItemId);
            cmd.Parameters.AddWithValue("@Count", reward.Count);
        });

        _entityIds["QuestReward"] = questRewards.Select(qr => qr.Id).ToList();
    }

    private void SeedQuestProgress()
    {
        Console.WriteLine("Seeding quest progress...");

        var questProgressFaker = new Faker<QuestProgressData>();
        var questProgresses = new List<QuestProgressData>();
        var questProgressId = 1;
        var stageNames = new[] { "Started", "InProgress", "NearlyComplete", "ReadyToTurnIn" };

        // Get all quest stages for reference
        var questStages = ExecuteReader<QuestStageData>("SELECT Id, QuestId, Name FROM QuestStage");
        var questIdToStages = questStages.GroupBy(qs => qs.QuestId)
                                        .ToDictionary(g => g.Key, g => g.Select(s => s.Name).ToList());

        // Only some characters will have active quests
        var characterSample = _entityIds["Character"]
                               .OrderBy(x => Guid.NewGuid())
                               .Take(_entityIds["Character"].Count / 3)
                               .ToList();

        foreach (var characterId in characterSample)
        {
            // Each character gets 1-3 quests in progress
            var questCount = _random.Next(1, 4);
            var questSample = _entityIds["Quest"]
                               .OrderBy(x => Guid.NewGuid())
                               .Take(questCount)
                               .ToList();

            foreach (var questId in questSample)
            {
                string currentStage;
                if (questIdToStages.ContainsKey(questId) && questIdToStages[questId].Any())
                {
                    // Use a real stage name if available
                    currentStage = questIdToStages[questId][_random.Next(questIdToStages[questId].Count)];
                }
                else
                {
                    // Use a generic stage name
                    currentStage = stageNames[_random.Next(stageNames.Length)];
                }

                questProgresses.Add(new QuestProgressData
                {
                    Id = questProgressId++,
                    CharacterId = characterId,
                    QuestId = questId,
                    StartedDate = new Faker().Date.Past(1),
                    CurrentStage = currentStage
                });
            }
        }

        BulkInsert("QuestProgress", questProgresses, (progress, cmd) =>
        {
            cmd.Parameters.AddWithValue("@Id", progress.Id);
            cmd.Parameters.AddWithValue("@CharacterId", progress.CharacterId);
            cmd.Parameters.AddWithValue("@QuestId", progress.QuestId);
            cmd.Parameters.AddWithValue("@StartedDate", progress.StartedDate);
            cmd.Parameters.AddWithValue("@CurrentStage", progress.CurrentStage);
        });

        _entityIds["QuestProgress"] = questProgresses.Select(qp => qp.Id).ToList();
    }

    private void SeedEnemies()
    {
        Console.WriteLine($"Seeding {_options.EnemyCount} enemies...");

        var enemyFaker = new Faker<EnemyData>()
            .RuleFor(e => e.Id, f => f.IndexFaker + 1)
            .RuleFor(e => e.Level, f => f.Random.Number(1, 50))
            .RuleFor(e => e.Reward, f => f.Random.Number(50, 1000))
            .RuleFor(e => e.HP, f => f.Random.Number(100, 5000))
            .RuleFor(e => e.Coordinates, f => $"{f.Random.Number(-1000, 1000)},{f.Random.Number(-1000, 1000)},{f.Random.Number(0, 100)}")
            .RuleFor(e => e.SpawnRadius, f => (float)f.Random.Double(10, 100))
            .RuleFor(e => e.SpawnLimit, f => f.Random.Number(1, 10));

        var enemies = enemyFaker.Generate(_options.EnemyCount);
        BulkInsert("Enemy", enemies, (enemy, cmd) =>
        {
            cmd.Parameters.AddWithValue("@Id", enemy.Id);
            cmd.Parameters.AddWithValue("@Level", enemy.Level);
            cmd.Parameters.AddWithValue("@Reward", enemy.Reward);
            cmd.Parameters.AddWithValue("@HP", enemy.HP);
            cmd.Parameters.AddWithValue("@Coordinates", enemy.Coordinates);
            cmd.Parameters.AddWithValue("@SpawnRadius", enemy.SpawnRadius);
            cmd.Parameters.AddWithValue("@SpawnLimit", enemy.SpawnLimit);
        });

        _entityIds["Enemy"] = enemies.Select(e => e.Id).ToList();
    }

    private void SeedEnemyAttributes()
    {
        Console.WriteLine("Seeding enemy attributes...");

        var enemyAttributeFaker = new Faker<EnemyAttributeData>();
        var enemyAttributes = new List<EnemyAttributeData>();
        var enemyAttributeId = 1;

        foreach (var enemyId in _entityIds["Enemy"])
        {
            // Each enemy gets 3-6 random attributes
            var attributeCount = _random.Next(3, 7);
            var attributeIds = _entityIds["Attribute"]
                .OrderBy(x => Guid.NewGuid())
                .Take(attributeCount)
                .ToList();

            foreach (var attributeId in attributeIds)
            {
                enemyAttributes.Add(new EnemyAttributeData
                {
                    Id = enemyAttributeId++,
                    Value = _random.Next(1, 100),
                    EnemyId = enemyId,
                    AttributeId = attributeId
                });
            }
        }

        BulkInsert("EnemyAttribute", enemyAttributes, (enemyAttribute, cmd) =>
        {
            cmd.Parameters.AddWithValue("@Id", enemyAttribute.Id);
            cmd.Parameters.AddWithValue("@Value", enemyAttribute.Value);
            cmd.Parameters.AddWithValue("@EnemyId", enemyAttribute.EnemyId);
            cmd.Parameters.AddWithValue("@AttributeId", enemyAttribute.AttributeId);
        });

        _entityIds["EnemyAttribute"] = enemyAttributes.Select(ea => ea.Id).ToList();
    }

    private void SeedVictoryRewards()
    {
        Console.WriteLine("Seeding victory rewards...");

        var victoryRewardFaker = new Faker<VictoryRewardData>();
        var victoryRewards = new List<VictoryRewardData>();
        var victoryRewardId = 1;

        foreach (var enemyId in _entityIds["Enemy"])
        {
            // Each enemy drops 1-3 different items
            var rewardCount = _random.Next(1, 4);
            var rewardItems = _entityIds["Item"]
                .OrderBy(x => Guid.NewGuid())
                .Take(rewardCount)
                .ToList();

            foreach (var itemId in rewardItems)
            {
                victoryRewards.Add(new VictoryRewardData
                {
                    Id = victoryRewardId++,
                    Count = _random.Next(1, 5),
                    ItemId = itemId,
                    EnemyId = enemyId
                });
            }
        }

        BulkInsert("VictoryReward", victoryRewards, (reward, cmd) =>
        {
            cmd.Parameters.AddWithValue("@Id", reward.Id);
            cmd.Parameters.AddWithValue("@Count", reward.Count);
            cmd.Parameters.AddWithValue("@ItemId", reward.ItemId);
            cmd.Parameters.AddWithValue("@EnemyId", reward.EnemyId);
        });

        _entityIds["VictoryReward"] = victoryRewards.Select(vr => vr.Id).ToList();
    }

    private void SeedLoots()
    {
        Console.WriteLine("Seeding loots...");

        var lootFaker = new Faker<LootData>();
        var loots = new List<LootData>();
        var lootId = 1;

        // Only some characters will have loot history
        var characterSample = _entityIds["Character"]
                               .OrderBy(x => Guid.NewGuid())
                               .Take(_entityIds["Character"].Count / 2)
                               .ToList();

        foreach (var characterId in characterSample)
        {
            // Each character gets 1-5 loot entries
            var lootCount = _random.Next(1, 6);

            for (int i = 0; i < lootCount; i++)
            {
                loots.Add(new LootData
                {
                    Id = lootId++,
                    Xp = _random.Next(50, 1000),
                    Currency = _random.Next(10, 500),
                    DropDate = new Faker().Date.Past(1),
                    CharacterId = characterId
                });
            }
        }

        BulkInsert("Loot", loots, (loot, cmd) =>
        {
            cmd.Parameters.AddWithValue("@Id", loot.Id);
            cmd.Parameters.AddWithValue("@Xp", loot.Xp);
            cmd.Parameters.AddWithValue("@Currency", loot.Currency);
            cmd.Parameters.AddWithValue("@DropDate", loot.DropDate);
            cmd.Parameters.AddWithValue("@CharacterId", loot.CharacterId);
        });

        _entityIds["Loot"] = loots.Select(l => l.Id).ToList();
    }

    private void SeedDroppedItems()
    {
        Console.WriteLine("Seeding dropped items...");

        var droppedItemFaker = new Faker<DroppedItemData>();
        var droppedItems = new List<DroppedItemData>();
        var droppedItemId = 1;

        foreach (var lootId in _entityIds["Loot"])
        {
            // Each loot gets 1-4 items
            var itemCount = _random.Next(1, 5);
            var lootItems = _entityIds["Item"]
                .OrderBy(x => Guid.NewGuid())
                .Take(itemCount)
                .ToList();

            foreach (var itemId in lootItems)
            {
                droppedItems.Add(new DroppedItemData
                {
                    Id = droppedItemId++,
                    ItemId = itemId,
                    LootId = lootId,
                    Quantity = _random.Next(1, 5)
                });
            }
        }

        BulkInsert("DroppedItem", droppedItems, (item, cmd) =>
        {
            cmd.Parameters.AddWithValue("@Id", item.Id);
            cmd.Parameters.AddWithValue("@ItemId", item.ItemId);
            cmd.Parameters.AddWithValue("@LootId", item.LootId);
            cmd.Parameters.AddWithValue("@Quantity", item.Quantity);
        });

        _entityIds["DroppedItem"] = droppedItems.Select(di => di.Id).ToList();
    }

    private int ExecuteNonQuery(string commandText, Dictionary<string, object> parameters = null)
    {
        using var command = new SqlCommand(commandText, _connection);
        if (parameters != null)
        {
            foreach (var param in parameters)
            {
                command.Parameters.AddWithValue(param.Key, param.Value ?? DBNull.Value);
            }
        }

        return command.ExecuteNonQuery();
    }

    // BulkInsert - Efficiently inserts multiple records of the same type
    private void BulkInsert<T>(string tableName, IEnumerable<T> entities, Action<T, SqlCommand> parameterMapper)
    {
        if (!entities.Any()) return;

        // Create a transaction for better performance with bulk operations
        using var transaction = _connection.BeginTransaction();
        try
        {
            // Build the insert query based on the first entity
            var firstEntity = entities.First();
            var sqlCommand = new SqlCommand();
            parameterMapper(firstEntity, sqlCommand);

            var paramNames = new List<string>();
            var valueNames = new List<string>();

            foreach (SqlParameter param in sqlCommand.Parameters)
            {
                paramNames.Add(param.ParameterName.Substring(1)); // Remove @ prefix
                valueNames.Add(param.ParameterName);
            }

            var insertSql = $"INSERT INTO {tableName} ({string.Join(", ", paramNames)}) VALUES ({string.Join(", ", valueNames)})";

            // Execute insert for each entity
            int count = 0;
            foreach (var entity in entities)
            {
                using (var command = new SqlCommand(insertSql, _connection, transaction))
                {
                    parameterMapper(entity, command);
                    command.ExecuteNonQuery();
                }

                count++;
                if (count % 1000 == 0)
                {
                    Console.WriteLine($"Inserted {count} records into {tableName}...");
                }
            }

            transaction.Commit();
            Console.WriteLine($"Successfully inserted {entities.Count()} records into {tableName}");
        }
        catch (Exception ex)
        {
            transaction.Rollback();
            Console.WriteLine($"Error during bulk insert to {tableName}: {ex.Message}");
            throw;
        }
    }

    // ExecuteScalar - Executes a SQL command and returns a single value
    private T ExecuteScalar<T>(string commandText, Dictionary<string, object> parameters = null)
    {
        using var command = new SqlCommand(commandText, _connection);
        if (parameters != null)
        {
            foreach (var param in parameters)
            {
                command.Parameters.AddWithValue(param.Key, param.Value ?? DBNull.Value);
            }
        }

        var result = command.ExecuteScalar();
        if (result == DBNull.Value || result == null)
        {
            return default;
        }
        return (T)Convert.ChangeType(result, typeof(T));
    }

    // ExecuteReader - Executes a SQL command and returns multiple records
    private List<T> ExecuteReader<T>(string commandText, Dictionary<string, object> parameters = null) where T : new()
    {
        var results = new List<T>();
        using var command = new SqlCommand(commandText, _connection);

        if (parameters != null)
        {
            foreach (var param in parameters)
            {
                command.Parameters.AddWithValue(param.Key, param.Value ?? DBNull.Value);
            }
        }

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            T item = new();

            // Get all properties of the type
            var properties = typeof(T).GetProperties();

            foreach (var property in properties)
            {
                try
                {
                    // Try to get the column value by property name
                    var ordinal = reader.GetOrdinal(property.Name);
                    if (!reader.IsDBNull(ordinal))
                    {
                        var value = reader.GetValue(ordinal);
                        property.SetValue(item, Convert.ChangeType(value, property.PropertyType));
                    }
                }
                catch (IndexOutOfRangeException)
                {
                    // Column doesn't exist, just continue
                    continue;
                }
            }

            results.Add(item);
        }


        return results;
    }
}