CREATE TABLE [User] (
  [UserId] UNIQUEIDENTIFIER PRIMARY KEY,
  [Username] NVARCHAR(300),
  [Email] NVARCHAR(300),
  [PasswordHash] NVARCHAR(300),
  [RegistryDate] DateTime
)
GO

CREATE TABLE [Comment] (
  [CommentId] UNIQUEIDENTIFIER PRIMARY KEY,
  [Content] NVARCHAR(300),
  [CreationDate] DateTime,
  [UserId] UNIQUEIDENTIFIER,
  [PostId] UNIQUEIDENTIFIER
)
GO

CREATE TABLE [Post] (
  [PostId] UNIQUEIDENTIFIER PRIMARY KEY,
  [Content] NVARCHAR(300),
  [CreationDate] DateTime,
  [Description] NVARCHAR(300),
  [UserId] UNIQUEIDENTIFIER
)
GO

CREATE TABLE [Like] (
  [LikeId] UNIQUEIDENTIFIER PRIMARY KEY,
  [CreationDate] DateTime,
  [UserId] UNIQUEIDENTIFIER,
  [PostId] UNIQUEIDENTIFIER
)
GO

CREATE TABLE [Hashtag] (
  [HashtagId] UNIQUEIDENTIFIER PRIMARY KEY,
  [Name] NVARCHAR(300)
)
GO

CREATE TABLE [HashtagPost] (
  [HashtagPostId] UNIQUEIDENTIFIER PRIMARY KEY,
  [Relevance] float,
  [HashtagId] UNIQUEIDENTIFIER,
  [PostId] UNIQUEIDENTIFIER
)
GO

CREATE TABLE [Chat] (
  [ChatId] UNIQUEIDENTIFIER PRIMARY KEY,
  [Title] NVARCHAR(300),
  [CreationDate] DateTime
)
GO

CREATE TABLE [UserChat] (
  [UserChatId] UNIQUEIDENTIFIER PRIMARY KEY,
  [UserRole] int,
  [UserId] UNIQUEIDENTIFIER,
  [ChatId] UNIQUEIDENTIFIER
)
GO

CREATE TABLE [Message] (
  [MessageId] UNIQUEIDENTIFIER PRIMARY KEY,
  [Content] NVARCHAR(300),
  [SendingDate] DateTime,
  [IsRead] bit,
  [ChatId] UNIQUEIDENTIFIER,
  [UserId] UNIQUEIDENTIFIER
)
GO

CREATE TABLE [BlackList] (
  [BlackListId] UNIQUEIDENTIFIER PRIMARY KEY,
  [Reason] NVARCHAR(300),
  [UserId] UNIQUEIDENTIFIER,
  [BlockedUserId] UNIQUEIDENTIFIER
)
GO

CREATE TABLE [Friendship] (
  [FriendshipId] UNIQUEIDENTIFIER PRIMARY KEY,
  [Status] int,
  [InitiatorId] UNIQUEIDENTIFIER,
  [RequestedFriendId] UNIQUEIDENTIFIER
)
GO

CREATE TABLE [UserProfile] (
  [UserProfileId] UNIQUEIDENTIFIER PRIMARY KEY,
  [DateOfBirth] DateTime,
  [Gender] int,
  [PlaceOfStudy] NVARCHAR(300),
  [UserId] UNIQUEIDENTIFIER
)
GO

CREATE TABLE [Skill] (
  [SkillId] UNIQUEIDENTIFIER PRIMARY KEY,
  [Name] NVARCHAR(300),
  [Category] NVARCHAR(300)
)
GO

CREATE TABLE [UserSkill] (
  [UserSkillId] UNIQUEIDENTIFIER PRIMARY KEY,
  [Level] int,
  [UserProfileId] UNIQUEIDENTIFIER,
  [SkillId] UNIQUEIDENTIFIER
)
GO

CREATE TABLE [Hobby] (
  [HobbyId] UNIQUEIDENTIFIER PRIMARY KEY,
  [Name] NVARCHAR(300)
)
GO

CREATE TABLE [UserHobby] (
  [UserHobbyId] UNIQUEIDENTIFIER PRIMARY KEY,
  [UserProfileId] UNIQUEIDENTIFIER,
  [HobbyId] UNIQUEIDENTIFIER
)
GO

ALTER TABLE [Comment] ADD FOREIGN KEY ([UserId]) REFERENCES [User] ([UserId])
GO

ALTER TABLE [Comment] ADD FOREIGN KEY ([PostId]) REFERENCES [Post] ([PostId])
GO

ALTER TABLE [Post] ADD FOREIGN KEY ([UserId]) REFERENCES [User] ([UserId])
GO

ALTER TABLE [Like] ADD FOREIGN KEY ([UserId]) REFERENCES [User] ([UserId])
GO

ALTER TABLE [Like] ADD FOREIGN KEY ([PostId]) REFERENCES [Post] ([PostId])
GO

ALTER TABLE [HashtagPost] ADD FOREIGN KEY ([HashtagId]) REFERENCES [Hashtag] ([HashtagId])
GO

ALTER TABLE [HashtagPost] ADD FOREIGN KEY ([PostId]) REFERENCES [Post] ([PostId])
GO

ALTER TABLE [UserChat] ADD FOREIGN KEY ([UserId]) REFERENCES [User] ([UserId])
GO

ALTER TABLE [UserChat] ADD FOREIGN KEY ([ChatId]) REFERENCES [Chat] ([ChatId])
GO

ALTER TABLE [Message] ADD FOREIGN KEY ([UserId]) REFERENCES [User] ([UserId])
GO

ALTER TABLE [Message] ADD FOREIGN KEY ([ChatId]) REFERENCES [Chat] ([ChatId])
GO

ALTER TABLE [BlackList] ADD FOREIGN KEY ([UserId]) REFERENCES [User] ([UserId])
GO

ALTER TABLE [BlackList] ADD FOREIGN KEY ([BlockedUserId]) REFERENCES [User] ([UserId])
GO

ALTER TABLE [Friendship] ADD FOREIGN KEY ([InitiatorId]) REFERENCES [User] ([UserId])
GO

ALTER TABLE [Friendship] ADD FOREIGN KEY ([RequestedFriendId]) REFERENCES [User] ([UserId])
GO

ALTER TABLE [UserProfile] ADD FOREIGN KEY ([UserId]) REFERENCES [User] ([UserId])
GO

ALTER TABLE [UserSkill] ADD FOREIGN KEY ([UserProfileId]) REFERENCES [UserProfile] ([UserProfileId])
GO

ALTER TABLE [UserSkill] ADD FOREIGN KEY ([SkillId]) REFERENCES [Skill] ([SkillId])
GO

ALTER TABLE [UserHobby] ADD FOREIGN KEY ([UserProfileId]) REFERENCES [UserProfile] ([UserProfileId])
GO

ALTER TABLE [UserHobby] ADD FOREIGN KEY ([HobbyId]) REFERENCES [Hobby] ([HobbyId])
GO
