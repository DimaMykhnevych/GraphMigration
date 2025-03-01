USE GameDB;

CREATE TABLE [User] (
    Id INT PRIMARY KEY,
    Email VARCHAR(100),
    [Password] VARCHAR(100),
    Nickname VARCHAR(50),
    RegistrationDate DATE
);

CREATE TABLE LoginHistory (
    Id INT PRIMARY KEY,
    UserId INT,
    LoginDateTime DATETIME,
    LogoutDateTime DATETIME,
    IpAddress VARCHAR(50),
    DeviceName VARCHAR(100),
    FOREIGN KEY (UserId) REFERENCES [User](Id)
);

CREATE TABLE Attribute (
    Id INT PRIMARY KEY,
    [Name] VARCHAR(50),
    [Description] TEXT
);

CREATE TABLE [Character] (
    Id INT PRIMARY KEY,
    [Name] VARCHAR(100),
    [Level] INT,
    [Xp] INT,
    Currency INT,
    UserId INT,
    FOREIGN KEY (UserId) REFERENCES [User](Id)
);

CREATE TABLE CharacterAttribute (
    Id INT PRIMARY KEY,
    [Value] INT,
	CharacterId INT,
	AttributeId INT,
	FOREIGN KEY (CharacterId) REFERENCES [Character](Id),
	FOREIGN KEY (AttributeId) REFERENCES Attribute(Id),
);

CREATE TABLE CharacterState (
    Id INT PRIMARY KEY,
    Coordinates VARCHAR(100),
    IsAlive BIT,
    Hp INT,
    Mp INT,
	CharacterId INT,
	FOREIGN KEY (CharacterId) REFERENCES [Character](Id),
);

CREATE TABLE Ability (
    Id INT PRIMARY KEY,
    [Name] VARCHAR(100),
    [Description] TEXT,
    MinimalLevel INT,
    AbilityType INT,
    DamageType INT,
    DamageValue INT,
	CharacterId INT,
	FOREIGN KEY (CharacterId) REFERENCES [Character](Id),
);

CREATE TABLE Item (
    Id INT PRIMARY KEY,
    [Name] VARCHAR(100),
    [Type] VARCHAR(50),
    [RequiredLevel] INT,
    [Description] TEXT
);

CREATE TABLE Equipment (
    Id INT PRIMARY KEY,
    CharacterId INT,
	ItemId INT,
    Slot VARCHAR(50),
    FOREIGN KEY (CharacterId) REFERENCES [Character](Id),
    FOREIGN KEY (ItemId) REFERENCES Item(Id)
);

CREATE TABLE InventoryItem (
    Id INT PRIMARY KEY,
    CharacterId INT,
    ItemId INT,
    [Count] INT,
    FOREIGN KEY (CharacterId) REFERENCES [Character](Id),
    FOREIGN KEY (ItemId) REFERENCES [Item](Id)
);

CREATE TABLE ItemAttribute (
    Id INT PRIMARY KEY,
    [Value] INT,
    ItemId INT,
    AttributeId INT,
    FOREIGN KEY (ItemId) REFERENCES Item(Id),
    FOREIGN KEY (AttributeId) REFERENCES Attribute(Id)
);

CREATE TABLE NPC (
    Id INT PRIMARY KEY,
    [Name] VARCHAR(100),
    Coordinates VARCHAR(100)
);

CREATE TABLE Quest (
    Id INT PRIMARY KEY,
    [Name] VARCHAR(100),
    [Description] TEXT,
    Reward INT,
    NpcId INT,
    FOREIGN KEY (NpcId) REFERENCES NPC(Id)
);

-- ACCOMPLISHES
CREATE TABLE QuestProgress (
    Id INT PRIMARY KEY,
    CharacterId INT,
    QuestId INT,
    StartedDate DATE,
    CurrentStage VARCHAR(200),
    FOREIGN KEY (CharacterId) REFERENCES [Character](Id),
    FOREIGN KEY (QuestId) REFERENCES Quest(Id)
);

CREATE TABLE Loot (
    Id INT PRIMARY KEY,
    Xp INT,
    Currency INT,
    DropDate DATETIME,
	CharacterId INT,
	FOREIGN KEY (CharacterId) REFERENCES [Character](Id),
);

CREATE TABLE DroppedItem (
    Id INT PRIMARY KEY,
    ItemId INT,
	LootId INT,
    Quantity INT,
    FOREIGN KEY (ItemId) REFERENCES Item(Id),
	FOREIGN KEY (LootId) REFERENCES Loot(Id)
);

CREATE TABLE QuestReward (
    Id INT PRIMARY KEY,
    QuestId INT,
    ItemId INT,
    [Count] INT,
    FOREIGN KEY (QuestId) REFERENCES Quest(Id),
    FOREIGN KEY (ItemId) REFERENCES Item(Id)
);

CREATE TABLE QuestStage (
    Id INT PRIMARY KEY,
    QuestId INT,
    [Name] VARCHAR(100),
    Objective VARCHAR(255),
    [Description] TEXT,
    FOREIGN KEY (QuestId) REFERENCES Quest(Id)
);

CREATE TABLE Enemy (
    Id INT PRIMARY KEY,
    [Level] INT,
    Reward INT,
    HP INT,
    Coordinates VARCHAR(100),
    SpawnRadius FLOAT,
    SpawnLimit INT
);

CREATE TABLE VictoryReward (
    Id INT PRIMARY KEY,
    [Count] INT,
    ItemId INT,
    EnemyId INT,
    FOREIGN KEY (ItemId) REFERENCES Item(Id),
    FOREIGN KEY (EnemyId) REFERENCES Enemy(Id)
);

CREATE TABLE EnemyAttribute (
    Id INT PRIMARY KEY,
    [Value] INT,
    EnemyId INT,
    AttributeId INT,
    FOREIGN KEY (EnemyId) REFERENCES Enemy(Id),
    FOREIGN KEY (AttributeId) REFERENCES Attribute(Id)
);