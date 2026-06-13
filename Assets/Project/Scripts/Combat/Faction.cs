namespace SpaceShooter.Combat
{
    /// <summary>Who a bullet or ship belongs to. Bullets only damage opposing factions.</summary>
    public enum Faction
    {
        Player = 0,
        Enemy = 1,
        Neutral = 2
    }
}
