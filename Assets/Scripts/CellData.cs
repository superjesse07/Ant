using UnityEngine;

public struct CellData
{
    public float foodPheromone;
    public float nestPheromone;
    public float deathPheromone;

    public CellData(float foodPheromone, float nestPheromone, float deathPheromone)
    {
        this.foodPheromone = foodPheromone;
        this.nestPheromone = nestPheromone;
        this.deathPheromone = deathPheromone;
    }

    public Color ToColor(Color food, Color nest, Color death)
    {
        return (food * foodPheromone + nest * nestPheromone + death * deathPheromone) /3f;
    }
}