namespace DbSeeder.Models.Game;

public class CharacterAttributeData
{
    public int Id { get; set; }
    public int Value { get; set; }
    public int CharacterId { get; set; }
    public int AttributeId { get; set; }
}
