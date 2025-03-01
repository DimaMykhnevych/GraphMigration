namespace DbSeeder.Models.Game;

public class LootData
{
    public int Id { get; set; }
    public int Xp { get; set; }
    public int Currency { get; set; }
    public DateTime DropDate { get; set; }
    public int CharacterId { get; set; }
}