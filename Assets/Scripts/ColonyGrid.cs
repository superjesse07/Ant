using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ColonyGrid : MonoBehaviour
{
    public CellData[,] grid;
    private Color[] flattenedGrid;

    public Vector2Int gridSize;

    public static ColonyGrid Instance { get; private set; }

    private Texture2D gridTexture;


    private void Awake()
    {
        grid = new CellData[gridSize.x, gridSize.y];
        Instance = this;
        gridTexture = new Texture2D(gridSize.x, gridSize.y);
        flattenedGrid = new Color[grid.Length];
    }

    public void WriteToTexture()
    {
        for (int i = 0; i < flattenedGrid.Length; i++)
        {
            flattenedGrid[i] = grid[i % grid.GetLength(0), i / grid.GetLength(0)].ToColor(Settings.Instance.foodPheromone,
                Settings.Instance.nestPheromone, Settings.Instance.deathPheromone);
        }

        gridTexture.SetPixels(flattenedGrid);
        gridTexture.Apply();
    }

    public Texture2D GetTexture()
    {
        return gridTexture;
    }
}