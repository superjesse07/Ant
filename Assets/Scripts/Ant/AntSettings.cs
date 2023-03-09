using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu()]
public class AntSettings : ScriptableObject
{
    [Header("Simulation Settings")] [Min(1)]
    public int stepsPerFrame = 1;

    public int width = 1280;
    public int height = 720;
    [FormerlySerializedAs("numAgents")] public int numAnts = 100;

    [Header("Trail Settings")] public float trailWeight = 1;
    public float decayRate = 1;
    public float diffuseRate = 1;
    public float nestTrailWeight = 1;
    public float foodTrailWeight = 1;
    public float deathTrailWeight = -1;

    [Header("Movement Settings")] public float moveSpeed;
    public float turnSpeed;
    public float jitterSpeed;

    [Header("Sensor Settings")] public float sensorAngleSpacing;
    public float sensorOffsetDst;
    [Min(1)] public int sensorSize;
}