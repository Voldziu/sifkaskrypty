using UnityEngine;

[System.Serializable]
public struct Yields
{
    public int food;
    public int production;
    public int gold;
    public int science;
    public int culture;
    public int faith;

    public Yields(int food = 0, int production = 0, int gold = 0, int science = 0, int culture = 0, int faith = 0)
    {
        this.food = food;
        this.production = production;
        this.gold = gold;
        this.science = science;
        this.culture = culture;
        this.faith = faith;
    }

    public static Yields operator +(Yields a, Yields b)
    {
        return new Yields(
            a.food + b.food,
            a.production + b.production,
            a.gold + b.gold,
            a.science + b.science,
            a.culture + b.culture,
            a.faith + b.faith
        );
    }

    public static Yields operator -(Yields a, Yields b)
    {
        return new Yields(
            a.food - b.food,
            a.production - b.production,
            a.gold - b.gold,
            a.science - b.science,
            a.culture - b.culture,
            a.faith - b.faith
        );
    }

    public static Yields operator *(Yields a, int multiplier)
    {
        return new Yields(
            a.food * multiplier,
            a.production * multiplier,
            a.gold * multiplier,
            a.science * multiplier,
            a.culture * multiplier,
            a.faith * multiplier
        );
    }

    public override string ToString()
    {
        return $"Food: {food}, Production: {production}, Gold: {gold}, Science: {science}, Culture: {culture}, Faith: {faith}";
    }
}