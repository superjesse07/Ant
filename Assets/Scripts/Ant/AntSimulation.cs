using System.Linq;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using ComputeShaderUtility;
using UnityEngine.Profiling;
using UnityEngine.Serialization;

public partial class AntSimulation : MonoBehaviour
{
    const int updateKernel = 0;
    const int diffuseMapKernel = 1;
    const int colourKernel = 2;

    public ComputeShader compute;
    public ComputeShader drawAgentsCS;

    public AntSettings settings;

    [Header("Display Settings")] public bool showAgentsOnly;
    public FilterMode filterMode = FilterMode.Point;
    public GraphicsFormat format = ComputeHelper.defaultGraphicsFormat;


    [SerializeField, HideInInspector] protected RenderTexture trailMap;
    [SerializeField, HideInInspector] protected RenderTexture diffusedTrailMap;
    [SerializeField, HideInInspector] protected RenderTexture displayTexture;
    [SerializeField, HideInInspector] protected RenderTexture foodMap;


    public Texture2D antTexture;

    ComputeBuffer antBuffer;
    public Ant[] ants;
    public int respawnAnt;
    public int iterations = 1000;
    public int saveDelay = 0;

    protected virtual void Start()
    {
        Init();
        transform.GetComponentInChildren<MeshRenderer>().material.mainTexture = displayTexture;
    }

    public string csv = "";

    void Init()
    {
        Random.InitState(1000);
        // Create render textures
        ComputeHelper.CreateRenderTexture(ref trailMap, settings.width, settings.height, filterMode, format);
        ComputeHelper.CreateRenderTexture(ref diffusedTrailMap, settings.width, settings.height, filterMode, format);
        ComputeHelper.CreateRenderTexture(ref displayTexture, settings.width, settings.height, filterMode, format);
        ComputeHelper.CreateRenderTexture(ref foodMap, settings.width, settings.height, filterMode, format);
        ComputeHelper.CopyRenderTexture(settings.GetFoodMap(), foodMap);


        // Assign textures
        compute.SetTexture(updateKernel, "TrailMap", trailMap);
        compute.SetTexture(updateKernel, "WallMap", settings.GetWallMap());
        compute.SetTexture(updateKernel, "FoodMap", foodMap);
        compute.SetTexture(diffuseMapKernel, "TrailMap", trailMap);
        compute.SetTexture(diffuseMapKernel, "DiffusedTrailMap", diffusedTrailMap);
        compute.SetTexture(colourKernel, "ColourMap", displayTexture);
        compute.SetTexture(colourKernel, "TrailMap", trailMap);
        compute.SetTexture(colourKernel, "WallMap", settings.GetWallMap());
        compute.SetTexture(colourKernel, "FoodMap", foodMap);

        // Create agents with initial positions and angles
        ants = new Ant[settings.numAnts];
        for (int i = 0; i < ants.Length; i++)
        {
            Vector2 centre = new Vector2(settings.width / 2f, settings.height / 2f);
            Vector2 startPos = settings.colonyLocation;
            float randomAngle = Random.value * Mathf.PI * 2;
            float angle = 0;

            ants[i] = new Ant
            {
                position = startPos, angle = randomAngle, state = 0, liberty_coef = Random.Range(0.001f, 0.01f),
                lifetime = i < ants.Length / 10 ? 0 : settings.maxLifetime + 1,
                markerTime = settings.markerPeriod * Random.Range(0.5f, 1.0f)
            };
        }

        ComputeHelper.CreateAndSetBuffer<Ant>(ref antBuffer, ants, compute, "ants", updateKernel);
        compute.SetInt("numAnts", settings.numAnts);
        drawAgentsCS.SetBuffer(0, "ants", antBuffer);
        drawAgentsCS.SetInt("numAnts", settings.numAnts);
        drawAgentsCS.SetTexture(0, "antTexture", antTexture);


        compute.SetInt("width", settings.width);
        compute.SetInt("height", settings.height);
    }

    void FixedUpdate()
    {
        if (iterations > 0)
        {
            for (int i = 0; i < settings.stepsPerFrame; i++)
            {
                RunSimulation(i);
            }
        
            antBuffer.GetData(ants);
            for (int i = 0; i < ants.Length; i++)
            {
                if (ants[i].lifetime > settings.maxLifetime)
                {
                    respawnAnt = i;
                    break;
                }
            }

            iterations = ants.Count(x => x.lifetime < settings.maxLifetime);

            csv += $"{iterations}\n";
        }
        //Debug.Log(ants.Count(x => x.lifetime < settings.maxLifetime));
    }

    void LateUpdate()
    {
        if (showAgentsOnly)
        {
            ComputeHelper.ClearRenderTexture(displayTexture);

            drawAgentsCS.SetTexture(0, "TargetTexture", displayTexture);
            ComputeHelper.Dispatch(drawAgentsCS, settings.numAnts, 1, 1, 0);
        }
        else
        {
            ComputeHelper.Dispatch(compute, settings.width, settings.height, 1, kernelIndex: colourKernel);
            //ComputeHelper.CopyRenderTexture(trailMap, displayTexture);
        }
    }

    void RunSimulation(int i)
    {
        // Assign settings
        compute.SetFloat("deltaTime", Time.fixedDeltaTime);
        compute.SetFloat("time", Time.fixedTime);
        compute.SetFloat("seed", Random.Range(0.0f, 1000.0f));

        compute.SetFloat("trailWeight", settings.trailWeight);
        compute.SetFloat("decayRate", settings.decayRate);
        compute.SetFloat("diffuseRate", settings.diffuseRate);

        compute.SetFloat("nestTrailWeight", settings.nestPheromone ? settings.nestTrailWeight : 0);
        compute.SetFloat("foodTrailWeight", settings.foodPheromone ? settings.foodTrailWeight : 0);
        compute.SetFloat("deathTrailWeight", settings.deathPheromone ? settings.deathTrailWeight : 0);

        compute.SetFloat("moveSpeed", settings.moveSpeed);
        compute.SetInt("respawnAnt", respawnAnt);

        //compute.SetFloat("sensorAngleDegrees",settings.sensorAngleSpacing);
        //compute.SetFloat("sensorOffsetDst",settings.sensorOffsetDst);
        //compute.SetInt("sensorSize",settings.sensorSize);

        compute.SetVector("nestTrailColor", settings.nestPheromone ? settings.nestTrailColor : Color.black);
        compute.SetVector("foodTrailColor", settings.foodPheromone ? settings.foodTrailColor : Color.black);
        compute.SetVector("deathTrailColor", settings.deathPheromone ? settings.deathTrailColor : Color.black);
        compute.SetVector("antColor", settings.antColor);
        compute.SetVector("foodColor", settings.foodColor);
        compute.SetVector("wallColor", settings.wallColor);


        compute.SetFloat("directionNoise", settings.directionNoise);
        compute.SetFloat("sampleMaxDistance", settings.sampleMaxDistance);
        compute.SetFloat("sampleAngleRange", settings.sampleAngleRange);
        compute.SetInt("sampleCount", settings.sampleCount);
        compute.SetFloat("maxLifetime", settings.maxLifetime);
        compute.SetInts("colonyLocation", settings.colonyLocation.x, settings.colonyLocation.y);
        compute.SetFloat("colonySize", settings.colonySize);
        compute.SetFloat("markerPeriod", settings.markerPeriod);
        compute.SetFloat("deathRadius", settings.deathRadius);


        ComputeHelper.Dispatch(compute, settings.numAnts, 1, 1, kernelIndex: updateKernel);
        ComputeHelper.Dispatch(compute, settings.width, settings.height, 1, kernelIndex: diffuseMapKernel);


        ComputeHelper.CopyRenderTexture(diffusedTrailMap, trailMap);
    }

    void OnDestroy()
    {
        ComputeHelper.Release(antBuffer);
    }

    public struct Ant
    {
        public Vector2 position;
        public float angle;
        public int state;
        public int hits;
        public float lifetime;
        public float liberty_coef;
        public float markerTime;
        public float marker;
        public int hasFood;
    }
}