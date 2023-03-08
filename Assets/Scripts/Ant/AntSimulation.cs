using UnityEngine;
using UnityEngine.Experimental.Rendering;
using ComputeShaderUtility;

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
	[SerializeField, HideInInspector] protected RenderTexture map;

	ComputeBuffer antBuffer;

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
		ComputeHelper.CreateRenderTexture(ref map, settings.width,settings.height,filterMode,format);

		// Assign textures
		compute.SetTexture(updateKernel, "TrailMap", trailMap);
		compute.SetTexture(diffuseMapKernel, "TrailMap", trailMap);
		compute.SetTexture(diffuseMapKernel, "DiffusedTrailMap", diffusedTrailMap);
		compute.SetTexture(colourKernel, "ColourMap", displayTexture);
		compute.SetTexture(colourKernel, "TrailMap", trailMap);

		// Create agents with initial positions and angles
		Ant[] ants = new Ant[settings.numAnts];
		for (int i = 0; i < ants.Length; i++)
		{
			Vector2 centre = new Vector2(settings.width / 2f, settings.height / 2f);
			Vector2 startPos = centre;
			float randomAngle = Random.value * Mathf.PI * 2;
			float angle = 0;
			
			ants[i] = new Ant() { position = startPos, angle = randomAngle, state =0 };
		}

		ComputeHelper.CreateAndSetBuffer<Ant>(ref antBuffer, ants, compute, "ants", updateKernel);
		compute.SetInt("numAnts", settings.numAnts);
		drawAgentsCS.SetBuffer(0, "ants", antBuffer);
		drawAgentsCS.SetInt("numAnts", settings.numAnts);


		compute.SetInt("width", settings.width);
		compute.SetInt("height", settings.height);
	}

	void FixedUpdate()
	{
		for (int i = 0; i < settings.stepsPerFrame; i++)
		{
			RunSimulation();
		}
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
			ComputeHelper.CopyRenderTexture(trailMap, displayTexture);
		}
	}

	void RunSimulation()
	{

		// Assign settings
		compute.SetFloat("deltaTime", Time.fixedDeltaTime);
		compute.SetFloat("time", Time.fixedTime);

		compute.SetFloat("trailWeight", settings.trailWeight);
		compute.SetFloat("decayRate", settings.decayRate);
		compute.SetFloat("diffuseRate", settings.diffuseRate);
		
		compute.SetFloat("nestTrailWeight",settings.nestTrailWeight);
		compute.SetFloat("foodTrailWeight",settings.foodTrailWeight);
		compute.SetFloat("deathTrailWeight",settings.deathTrailWeight);
		
		compute.SetFloat("moveSpeed",settings.moveSpeed);
		compute.SetFloat("turnSpeed",settings.turnSpeed);


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
	}


}
