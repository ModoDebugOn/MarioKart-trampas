namespace KartRace.Domain.Items
{
    /// <summary>
    /// What an item box can give you. Our own set, not Nintendo's: each one does the job of an item
    /// of Mario Kart 8 Deluxe (in brackets), so the roulette can use the real odds tables.
    /// </summary>
    public enum Item
    {
        Banana,          // trap you drop behind (banana)
        TripleBanana,    // (triple bananas)
        GreenPuck,       // thrown straight, bounces off walls (green shell)
        TripleGreenPuck, // (triple green shells)
        RedPuck,         // homes in on the racer ahead (red shell)
        TripleRedPuck,   // (triple red shells)
        BlueBomb,        // flies to the leader, whoever you are (spiny / blue shell)
        Turbo,           // one boost (mushroom)
        TripleTurbo,     // (triple mushrooms)
        GoldenTurbo,     // boosts for a while (golden mushroom)
        Star,            // invincible and faster (super star)
        Bullet,          // drives itself to the front (bullet bill)
        Lightning,       // shrinks every rival (lightning)
        Bomb,            // thrown, explodes (bob-omb)
        Ink,             // blinds the racers ahead (blooper)
        Coin,            // a little speed (coin)
        Horn,            // blast around you, even stops the blue bomb (super horn)
    }
}
