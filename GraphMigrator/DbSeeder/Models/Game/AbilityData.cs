namespace DbSeeder.Models.Game;

public class AbilityData
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public int MinimalLevel { get; set; }
    public int AbilityType { get; set; }
    public int DamageType { get; set; }
    public int DamageValue { get; set; }
    public int CharacterId { get; set; }
}
