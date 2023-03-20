using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu()]
public class AntSettings : ScriptableObject
{
    [Header("Simulation Settings")] [Min(1)]
    public int stepsPerFrame = 1;

    public Texture2D map;

    public Texture2D GetWallMap()
    {
        Texture2D texture2D = new(width, height);
        texture2D.SetPixels(map.GetPixels().Select(x => new Color(x.r * x.a,0,0)).ToArray());
        texture2D.Apply();
        return texture2D;
    }

    public Texture2D GetFoodMap()
    {
        Texture2D texture2D = new(width, height);
        texture2D.SetPixels(map.GetPixels().Select(x => new Color(x.g * x.a,0,0)).ToArray());
        texture2D.Apply();
        return texture2D;
    }
    
    public Vector2Int colonyLocation;
    public float colonySize;
    public int width => map.width;
    public int height => map.height;
    public int numAnts = 100;
    public float maxLifetime;

    public float markerPeriod = 0.25f;

    [Header("Trail Settings")] public float trailWeight = 1;
    public float decayRate = 1;
    public float diffuseRate = 1;
    public float nestTrailWeight = 1;
    public float foodTrailWeight = 1;
    public float deathTrailWeight = -1;

    [Header("Color Settings")] public Color nestTrailColor;
    public Color foodTrailColor;
    public Color deathTrailColor;
    public Color antColor;
    public Color foodColor;
    public Color wallColor;
    

    [Header("Movement Settings")] public float moveSpeed; 
    public float directionNoise;

    [Header("Sensor Settings")] public float sampleMaxDistance;
    public float sampleAngleRange;
    [Min(1)] public int sampleCount;
}