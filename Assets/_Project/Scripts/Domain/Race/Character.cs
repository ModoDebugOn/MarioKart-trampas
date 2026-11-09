namespace KartRace.Domain.Race
{
    /// <summary>
    /// Who sits in the kart. Each name is also the kit's outfit and the racer's prefab
    /// (Prefabs/Characters/Mario.prefab, Prefabs/Racers/Mario.prefab), so do not rename them lightly.
    /// </summary>
    public enum Character
    {
        Mario, Luigi, Peach, Daisy, Rosalina,
        Yoshi, Birdo, Toad, Toadette, KoopaTroopa,
        Lakitu, DryBones, ShyGuy, Bowser, BowserJr,
        KingBoo, Wario, Waluigi, DonkeyKong, DiddyKong,
    }
}
