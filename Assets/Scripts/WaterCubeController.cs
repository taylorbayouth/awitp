using UnityEngine;

/// <summary>
/// Runtime controller for the water cube visual effect.
/// Creates a material instance and pushes shader properties each frame
/// for live Inspector tuning. Optionally spawns a realtime reflection probe.
/// Attach to the water cube prefab root or visual child.
/// </summary>
public class WaterCubeController : MonoBehaviour
{
    [Header("Colors")]
    [SerializeField] private Color deepColor = new Color(0.02f, 0.08f, 0.25f, 1f);
    [SerializeField] private Color shallowColor = new Color(0.1f, 0.4f, 0.6f, 1f);
    [SerializeField] private Color specularColor = Color.white;
    [SerializeField] private Color sssColor = new Color(0.1f, 0.6f, 0.3f, 1f);

    [Header("Waves Primary")]
    [SerializeField, Range(0f, 0.1f)] private float waveAmplitude = 0.015f;
    [SerializeField, Range(0.5f, 10f)] private float waveFrequency = 3.0f;
    [SerializeField, Range(0f, 5f)] private float waveSpeed = 1.2f;

    [Header("Waves Secondary")]
    [SerializeField, Range(0f, 0.05f)] private float wave2Amplitude = 0.008f;
    [SerializeField, Range(1f, 20f)] private float wave2Frequency = 7.0f;
    [SerializeField, Range(0f, 5f)] private float wave2Speed = 0.8f;

    [Header("Surface Detail")]
    [SerializeField, Range(0f, 2f)] private float normalStrength = 0.8f;
    [SerializeField, Range(1f, 20f)] private float normalScale = 6.0f;
    [SerializeField, Range(0f, 3f)] private float normalSpeed = 0.5f;

    [Header("Fresnel & Reflection")]
    [SerializeField, Range(1f, 10f)] private float fresnelPower = 3.0f;
    [SerializeField, Range(0f, 1f)] private float reflectionStrength = 0.6f;

    [Header("Refraction")]
    [SerializeField, Range(0f, 0.1f)] private float refractionStrength = 0.03f;

    [Header("Specular")]
    [SerializeField, Range(8f, 512f)] private float specularPower = 128f;
    [SerializeField, Range(0f, 3f)] private float specularIntensity = 1.5f;

    [Header("Subsurface Scattering")]
    [SerializeField, Range(1f, 10f)] private float sssPower = 4.0f;
    [SerializeField, Range(0f, 1f)] private float sssIntensity = 0.3f;

    [Header("Transparency")]
    [SerializeField, Range(0.3f, 1f)] private float minAlpha = 0.75f;

    [Header("Reflection Probe (Optional)")]
    [SerializeField] private bool useRealtimeProbe = false;
    [SerializeField, Range(16, 256)] private int probeResolution = 64;
    [SerializeField, Range(0.1f, 10f)] private float probeRefreshInterval = 0.5f;

    private Material materialInstance;
    private Renderer cubeRenderer;
    private ReflectionProbe reflectionProbe;
    private float probeTimer;
    private bool hasBaseColors;
    private Color baseDeepColor;
    private Color baseShallowColor;
    private Color baseSpecularColor;
    private Color baseSssColor;

    // Shader property IDs (cached for performance)
    private static readonly int ID_DeepColor = Shader.PropertyToID("_DeepColor");
    private static readonly int ID_ShallowColor = Shader.PropertyToID("_ShallowColor");
    private static readonly int ID_SpecularColor = Shader.PropertyToID("_SpecularColor");
    private static readonly int ID_SSSColor = Shader.PropertyToID("_SSSColor");
    private static readonly int ID_WaveAmplitude = Shader.PropertyToID("_WaveAmplitude");
    private static readonly int ID_WaveFrequency = Shader.PropertyToID("_WaveFrequency");
    private static readonly int ID_WaveSpeed = Shader.PropertyToID("_WaveSpeed");
    private static readonly int ID_Wave2Amplitude = Shader.PropertyToID("_Wave2Amplitude");
    private static readonly int ID_Wave2Frequency = Shader.PropertyToID("_Wave2Frequency");
    private static readonly int ID_Wave2Speed = Shader.PropertyToID("_Wave2Speed");
    private static readonly int ID_NormalStrength = Shader.PropertyToID("_NormalStrength");
    private static readonly int ID_NormalScale = Shader.PropertyToID("_NormalScale");
    private static readonly int ID_NormalSpeed = Shader.PropertyToID("_NormalSpeed");
    private static readonly int ID_FresnelPower = Shader.PropertyToID("_FresnelPower");
    private static readonly int ID_ReflectionStrength = Shader.PropertyToID("_ReflectionStrength");
    private static readonly int ID_RefractionStrength = Shader.PropertyToID("_RefractionStrength");
    private static readonly int ID_SpecularPower = Shader.PropertyToID("_SpecularPower");
    private static readonly int ID_SpecularIntensity = Shader.PropertyToID("_SpecularIntensity");
    private static readonly int ID_SSSPower = Shader.PropertyToID("_SSSPower");
    private static readonly int ID_SSSIntensity = Shader.PropertyToID("_SSSIntensity");
    private static readonly int ID_MinAlpha = Shader.PropertyToID("_MinAlpha");

    void Start()
    {
        cubeRenderer = GetComponentInChildren<Renderer>();
        if (cubeRenderer == null)
        {
            Debug.LogWarning("[WaterCubeController] No Renderer found on " + gameObject.name);
            return;
        }

        // Create material instance to avoid shared material mutation
        Shader waterShader = Shader.Find("Custom/WaterCube");
        if (waterShader == null)
        {
            Debug.LogWarning("[WaterCubeController] Custom/WaterCube shader not found, using existing material");
            materialInstance = cubeRenderer.material; // Creates instance automatically
        }
        else
        {
            materialInstance = new Material(waterShader);
            cubeRenderer.material = materialInstance;
        }

        UpdateMaterial();
        SetupReflectionProbe();
    }

    void Update()
    {
        if (materialInstance == null) return;

        UpdateMaterial();
        UpdateReflectionProbe();
    }

    void OnValidate()
    {
        if (materialInstance != null)
            UpdateMaterial();
    }

    void OnDestroy()
    {
        if (materialInstance != null)
        {
            if (Application.isPlaying)
                Destroy(materialInstance);
            else
                DestroyImmediate(materialInstance);
        }

        if (reflectionProbe != null)
        {
            if (Application.isPlaying)
                Destroy(reflectionProbe.gameObject);
            else
                DestroyImmediate(reflectionProbe.gameObject);
        }
    }

    private void UpdateMaterial()
    {
        materialInstance.SetColor(ID_DeepColor, deepColor);
        materialInstance.SetColor(ID_ShallowColor, shallowColor);
        materialInstance.SetColor(ID_SpecularColor, specularColor);
        materialInstance.SetColor(ID_SSSColor, sssColor);

        materialInstance.SetFloat(ID_WaveAmplitude, waveAmplitude);
        materialInstance.SetFloat(ID_WaveFrequency, waveFrequency);
        materialInstance.SetFloat(ID_WaveSpeed, waveSpeed);
        materialInstance.SetFloat(ID_Wave2Amplitude, wave2Amplitude);
        materialInstance.SetFloat(ID_Wave2Frequency, wave2Frequency);
        materialInstance.SetFloat(ID_Wave2Speed, wave2Speed);

        materialInstance.SetFloat(ID_NormalStrength, normalStrength);
        materialInstance.SetFloat(ID_NormalScale, normalScale);
        materialInstance.SetFloat(ID_NormalSpeed, normalSpeed);

        materialInstance.SetFloat(ID_FresnelPower, fresnelPower);
        materialInstance.SetFloat(ID_ReflectionStrength, reflectionStrength);
        materialInstance.SetFloat(ID_RefractionStrength, refractionStrength);

        materialInstance.SetFloat(ID_SpecularPower, specularPower);
        materialInstance.SetFloat(ID_SpecularIntensity, specularIntensity);

        materialInstance.SetFloat(ID_SSSPower, sssPower);
        materialInstance.SetFloat(ID_SSSIntensity, sssIntensity);

        materialInstance.SetFloat(ID_MinAlpha, minAlpha);
    }

    public void ApplyFlavorPalette(Color deep, Color shallow, Color sss)
    {
        CacheBaseColors();

        deepColor = deep;
        shallowColor = shallow;
        sssColor = sss;
        specularColor = baseSpecularColor;

        if (materialInstance != null)
        {
            UpdateMaterial();
        }
    }

    public void ResetFlavorPalette()
    {
        CacheBaseColors();

        deepColor = baseDeepColor;
        shallowColor = baseShallowColor;
        sssColor = baseSssColor;
        specularColor = baseSpecularColor;

        if (materialInstance != null)
        {
            UpdateMaterial();
        }
    }

    private void CacheBaseColors()
    {
        if (hasBaseColors) return;
        hasBaseColors = true;
        baseDeepColor = deepColor;
        baseShallowColor = shallowColor;
        baseSpecularColor = specularColor;
        baseSssColor = sssColor;
    }

    private void SetupReflectionProbe()
    {
        if (!useRealtimeProbe) return;

        GameObject probeObj = new GameObject("WaterReflectionProbe");
        probeObj.transform.SetParent(transform);
        probeObj.transform.localPosition = Vector3.zero;

        reflectionProbe = probeObj.AddComponent<ReflectionProbe>();
        reflectionProbe.mode = UnityEngine.Rendering.ReflectionProbeMode.Realtime;
        reflectionProbe.refreshMode = UnityEngine.Rendering.ReflectionProbeRefreshMode.ViaScripting;
        reflectionProbe.resolution = probeResolution;
        reflectionProbe.size = Vector3.one * 5f;
        reflectionProbe.nearClipPlane = 0.1f;
        reflectionProbe.farClipPlane = 100f;
        reflectionProbe.RenderProbe();
    }

    private void UpdateReflectionProbe()
    {
        if (reflectionProbe == null) return;

        probeTimer += Time.deltaTime;
        if (probeTimer >= probeRefreshInterval)
        {
            probeTimer = 0f;
            reflectionProbe.RenderProbe();
        }
    }
}
