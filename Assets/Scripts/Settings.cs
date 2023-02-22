using System;
using UnityEngine;

public class Settings : MonoBehaviour
{
    public static Settings Instance { private set; get; }

    [Header("Pheromone colors")] public Color foodPheromone, nestPheromone, deathPheromone;


    private void Awake()
    {
        Instance = this;
    }
}