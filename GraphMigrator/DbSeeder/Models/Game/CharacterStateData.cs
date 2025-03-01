namespace DbSeeder.Models.Game;

public class CharacterStateData
{
    public int Id { get; set; }
    public string Coordinates { get; set; }
    public bool IsAlive { get; set; }
    public int Hp { get; set; }
    public int Mp { get; set; }
    public int CharacterId { get; set; }
}
