using UnityEngine;
using UnityEngine.Rendering;

public class ParticleSystemGPU : MonoBehaviour
{
    [Header("Particle Count")]
    [Range(100000, 4000000)]
    public int particleCount = 500000;

    [Header("References")]
    public ComputeShader computeShader;
    public Material particleMaterial;
    public PhaseController phaseController;
    public AttractorField attractorField;

    [Header("Rendering")]
    public float basePointSize = 1.8f;
    public float glowIntensity = 1.8f;

    [Header("Explosion")]
    public float explosionStrength = 600f;
    public float explosionRadius = 400f;

    private ComputeBuffer particleBuffer;
    private bool buffersReady = false;
    private bool attractorReady = false;

    private int kernelMain;
    private int kernelInit;
    private int kernelExplode;
    private ComputeBuffer densityBuffer;
    private int kernelDensity;
    private float densityCheckInterval = 0.5f;
    private float densityCheckTimer = 0f;

    private Vector2 prevMousePos;
    private float mouseSpeed;
    private Vector2 clickWorldPos;

    private float simTime = 0f;

    void Start()
    {
        clickWorldPos = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);

        InitBuffers();
        GetKernels();
        InitParticles();

        phaseController.OnExplodeStarted += OnExplodeStarted;
        phaseController.OnConvergingStarted += OnConvergingStarted;
        phaseController.OnCollapseStarted += OnCollapseStarted;
        phaseController.OnCollapseExplode += OnCollapseExplode;
    }

    void InitBuffers()
    {
        densityBuffer = new ComputeBuffer(1, sizeof(int));
        int stride = sizeof(float) * 12;
        particleBuffer = new ComputeBuffer(particleCount, stride);
        buffersReady = true;
        Debug.Log($"[ParticleSystemGPU] Buffer: {particleCount} particulas ({(particleCount * stride / 1024f / 1024f):F1} MB)");
    }

    void GetKernels()
    {
        kernelDensity = computeShader.FindKernel("CSCountDensity");
        kernelMain = computeShader.FindKernel("CSMain");
        kernelInit = computeShader.FindKernel("CSInit");
        kernelExplode = computeShader.FindKernel("CSExplode");
    }

    void InitParticles()
    {
        if (!buffersReady) return;
        computeShader.SetBuffer(kernelInit, "particles", particleBuffer);
        computeShader.SetVector("screenSize", new Vector2(Screen.width, Screen.height));
        computeShader.SetInt("particleCount", particleCount);
        int threadGroups = Mathf.CeilToInt(particleCount / 64f);
        computeShader.Dispatch(kernelInit, threadGroups, 1, 1);
        Debug.Log("[ParticleSystemGPU] Particulas inicializadas");
    }

    public void OnAttractorTexturesReady()
    {
        attractorReady = true;
        Debug.Log("[ParticleSystemGPU] Texturas del atractor listas");
    }

    void Update()
    {

        densityCheckTimer += Time.deltaTime;
        if (phaseController.CurrentPhase == SimPhase.Converging && densityCheckTimer >= densityCheckInterval)
        {
            densityCheckTimer = 0f;
            CheckWordDensity();
        }

        if (!buffersReady) return;

        Vector2 mousePos = new Vector2(Input.mousePosition.x, Input.mousePosition.y);
        mouseSpeed = (mousePos - prevMousePos).magnitude / Time.deltaTime;
        prevMousePos = mousePos;

        if (mouseSpeed > 30f)
            phaseController.InjectChaos(mouseSpeed / 10f);

        if (Input.GetMouseButtonDown(0))
        {
            clickWorldPos = mousePos;
            phaseController.ForceConverge();
        }

        SimulateStep(mousePos);
    }

    void SimulateStep(Vector2 mousePos)
    {
        simTime += Time.deltaTime;

        float chaosTemp = phaseController.ChaosTemperature;
        float attractorW = phaseController.AttractorWeight;

        computeShader.SetBuffer(kernelMain, "particles", particleBuffer);

        if (attractorReady && attractorField.CurrentTexture != null)
            computeShader.SetTexture(kernelMain, "attractorTex", attractorField.CurrentTexture);
        else
            return;

        computeShader.SetFloat("deltaTime", Time.deltaTime);
        computeShader.SetFloat("time", simTime);
        computeShader.SetVector("screenSize", new Vector2(Screen.width, Screen.height));
        computeShader.SetVector("mousePos", mousePos);
        computeShader.SetFloat("mouseSpeed", Mathf.Clamp01(mouseSpeed / 100f));
        computeShader.SetFloat("attractorWeight", attractorW);
        computeShader.SetFloat("chaosTemperature", chaosTemp);
        computeShader.SetInt("particleCount", particleCount);
        computeShader.SetVector("clickPos", clickWorldPos);  

        int threadGroups = Mathf.CeilToInt(particleCount / 64f);
        computeShader.Dispatch(kernelMain, threadGroups, 1, 1);

        particleMaterial.SetFloat("_AttractorWeight", attractorW);
        particleMaterial.SetFloat("_ChaosTemperature", chaosTemp);
        particleMaterial.SetFloat("_PointSize", basePointSize);
        particleMaterial.SetFloat("_GlowIntensity", glowIntensity);
        particleMaterial.SetFloat("_GlowMultiplier", phaseController.GlowMultiplier);
        particleMaterial.SetVector("_CustomScreenSize", new Vector2(Screen.width, Screen.height));
        particleMaterial.SetBuffer("_Particles", particleBuffer);
    }

    void CheckWordDensity()
    {
        densityBuffer.SetData(new int[] { 0 });
        computeShader.SetBuffer(kernelDensity, "particles", particleBuffer);
        computeShader.SetBuffer(kernelDensity, "densityCounter", densityBuffer);
        computeShader.SetTexture(kernelDensity, "densityTex", attractorField.CurrentTexture);
        computeShader.SetVector("screenSize", new Vector2(Screen.width, Screen.height));
        int threadGroups = Mathf.CeilToInt(particleCount / 64f);
        computeShader.Dispatch(kernelDensity, threadGroups, 1, 1);

        AsyncGPUReadback.Request(densityBuffer, (request) =>
        {
            if (request.hasError || !buffersReady) return;
            int count = request.GetData<int>()[0];
            phaseController.WordDensity = Mathf.Clamp01(count / (particleCount * 0.08f));
            Debug.Log($"[Density] {count} particulas en letra = {phaseController.WordDensity:F2}");
        });
    }

    void LateUpdate()
    {
        if (!buffersReady || particleMaterial == null) return;
        Graphics.DrawProcedural(
            particleMaterial,
            new Bounds(Vector3.zero, Vector3.one * 99999f),
            MeshTopology.Points,
            particleCount,
            1
        );
    }


    void OnExplodeStarted()
    {
        computeShader.SetBuffer(kernelExplode, "particles", particleBuffer);
        computeShader.SetFloat("clickX", clickWorldPos.x);
        computeShader.SetFloat("clickY", clickWorldPos.y);
        computeShader.SetFloat("clickStrength", explosionStrength);
        computeShader.SetFloat("explosionRadius", explosionRadius);
        int threadGroups = Mathf.CeilToInt(particleCount / 64f);
        computeShader.Dispatch(kernelExplode, threadGroups, 1, 1);
        Debug.Log($"[ParticleSystemGPU] Explosion en {clickWorldPos}");
    }

    void OnConvergingStarted(bool isAmine)
    {
        if (attractorField != null)
            attractorField.SetTarget(isAmine);
        Debug.Log($"[ParticleSystemGPU] Convergiendo -> {(isAmine ? "Amine" : "48")}");
    }

    void OnCollapseStarted()
    {
        Debug.Log("[ParticleSystemGPU] Colapso");
    }

    void OnCollapseExplode()
    {
        computeShader.SetBuffer(kernelExplode, "particles", particleBuffer);
        computeShader.SetFloat("clickX", clickWorldPos.x);
        computeShader.SetFloat("clickY", clickWorldPos.y);
        computeShader.SetFloat("clickStrength", explosionStrength * 10000.5f);
        computeShader.SetFloat("explosionRadius", explosionRadius * 4000.2f);
        int threadGroups = Mathf.CeilToInt(particleCount / 64f);
        computeShader.Dispatch(kernelExplode, threadGroups, 1, 1);
        Debug.Log("[ParticleSystemGPU] BOOM real post-colapso");
    }

    void OnDestroy()
    {
        if (densityBuffer != null) { densityBuffer.Release(); densityBuffer = null; }

        if (particleBuffer != null) { particleBuffer.Release(); particleBuffer = null; }
    }


}