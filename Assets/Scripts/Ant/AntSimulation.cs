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

	[Header("Display Settings")]
	public bool showAgentsOnly;
	public FilterMode filterMode = FilterMode.Point;
	public GraphicsFormat format = ComputeHelper.defaultGraphicsFormat;


	[SerializeField, HideInInspector] protected RenderTexture trailMap;
	[SerializeField, HideInInspector] protected RenderTexture diffusedTrailMap;
	[SerializeField, HideInInspector] protected RenderTexture displayTexture;
	[SerializeField, HideInInspector] protected RenderTexture foodMap;


	public Texture2D antTexture;

	ComputeBuffer antBuffer;
	public Ant[] ants;

	protected virtual void Start()
	{
		Init();
		transform.GetComponentInChildren<MeshRenderer>().material.mainTexture = displayTexture;
	}


	void Init()
	{
		// Create render textures
		ComputeHelper.CreateRenderTexture(ref trailMap, settings.width, settings.height, filterMode, format);
		ComputeHelper.CreateRenderTexture(ref diffusedTrailMap, settings.width, settings.height, filterMode, format);
		ComputeHelper.CreateRenderTexture(ref displayTexture, settings.width, settings.height, filterMode, format);
		ComputeHelper.CreateRenderTexture(ref foodMap, settings.width,settings.height,filterMode,format);
		ComputeHelper.CopyRenderTexture(settings.GetFoodMap(),foodMap);
		
		
		// Assign textures
		compute.SetTexture(updateKernel, "TrailMap", trailMap);
		compute.SetTexture(updateKernel,"WallMap",settings.GetWallMap());
		compute.SetTexture(updateKernel,"FoodMap",foodMap);
		compute.SetTexture(diffuseMapKernel, "TrailMap", trailMap);
		compute.SetTexture(diffuseMapKernel, "DiffusedTrailMap", diffusedTrailMap);
		compute.SetTexture(colourKernel, "ColourMap", displayTexture);
		compute.SetTexture(colourKernel, "TrailMap", trailMap);
		compute.SetTexture(colourKernel,"WallMap",settings.GetWallMap());
		compute.SetTexture(colourKernel,"FoodMap",foodMap);

		// Create agents with initial positions and angles
		ants = new Ant[settings.numAnts];
		for (int i = 0; i < ants.Length; i++)
		{
			Vector2 centre = new Vector2(settings.width / 2f, settings.height / 2f);
			Vector2 startPos = centre;
			float randomAngle = Random.value * Mathf.PI * 2;
			float angle = 0;
			
			ants[i] = new Ant { position = startPos, angle = randomAngle, state =0, liberty_coef = Random.Range(0.001f,0.01f),lifetime =  0, markerTime = settings.markerPeriod * Random.Range(0.5f,1.0f)};
		}

		ComputeHelper.CreateAndSetBuffer<Ant>(ref antBuffer, ants, compute, "ants", updateKernel);
		compute.SetInt("numAnts", settings.numAnts);
		drawAgentsCS.SetBuffer(0, "ants", antBuffer);
		drawAgentsCS.SetInt("numAnts", settings.numAnts);
		drawAgentsCS.SetTexture(0,"antTexture",antTexture);


		compute.SetInt("width", settings.width);
		compute.SetInt("height", settings.height);
	}

	void FixedUpdate()
	{
		for (int i = 0; i < settings.stepsPerFrame; i++)
		{
			RunSimulation(i);
		}
		//antBuffer.GetData(ants);
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
			ComputeHelper.Dispatch(compute, settings.width, settings.height, 1, kernelIndex : colourKernel);
			//ComputeHelper.CopyRenderTexture(trailMap, displayTexture);
		}
	}

	void RunSimulation(int i)
	{

		// Assign settings
		compute.SetFloat("deltaTime", Time.fixedDeltaTime);
		compute.SetFloat("time", Time.fixedTime);
		compute.SetFloat("seed", Random.Range(0, 1));

		compute.SetFloat("trailWeight", settings.trailWeight);
		compute.SetFloat("decayRate", settings.decayRate);
		compute.SetFloat("diffuseRate", settings.diffuseRate);
		
		compute.SetFloat("nestTrailWeight",settings.nestTrailWeight);
		compute.SetFloat("foodTrailWeight",settings.foodTrailWeight);
		compute.SetFloat("deathTrailWeight",settings.deathTrailWeight);
		
		compute.SetFloat("moveSpeed",settings.moveSpeed);

		//compute.SetFloat("sensorAngleDegrees",settings.sensorAngleSpacing);
		//compute.SetFloat("sensorOffsetDst",settings.sensorOffsetDst);
		//compute.SetInt("sensorSize",settings.sensorSize);
		
		compute.SetVector("nestTrailColor",settings.nestTrailColor);
		compute.SetVector("foodTrailColor",settings.foodTrailColor);
		compute.SetVector("deathTrailColor",settings.deathTrailColor);
		compute.SetVector("antColor",settings.antColor);
		compute.SetVector("foodColor",settings.foodColor);
		compute.SetVector("wallColor",settings.wallColor);
		
		
		compute.SetFloat("directionNoise", settings.directionNoise);
		compute.SetFloat("sampleMaxDistance",settings.sampleMaxDistance);
		compute.SetFloat("sampleAngleRange",settings.sampleAngleRange);
		compute.SetInt("sampleCount",settings.sampleCount);
		compute.SetFloat("maxLifetime",settings.maxLifetime);
		compute.SetInts("colonyLocation", settings.colonyLocation.x, settings.colonyLocation.y);
		compute.SetFloat("colonySize",settings.colonySize);
		compute.SetFloat("markerPeriod",settings.markerPeriod);


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
	}


}
