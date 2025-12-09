/*
using UnityEngine;

public class HealthBar : MonoBehaviour
{
    [Header("Health Bar Settings")]
    [SerializeField] private float barWidth = 1.5f;
    [SerializeField] private float barHeight = 0.2f;
    [SerializeField] private float offsetY = 1.5f; // Default offset
    [SerializeField] private Color backgroundColor = new Color(0.2f, 0.2f, 0.2f, 0.8f);
    [SerializeField] private Color healthColor = new Color(0f, 1f, 0f, 1f);
    [SerializeField] private Color lowHealthColor = new Color(1f, 0f, 0f, 1f);
    [SerializeField] private float lowHealthThreshold = 0.3f;
    
    private HealthSystem healthSystem;
    private Camera mainCamera;
    private GameObject healthBarBackground;
    private GameObject healthBarFill;
    private Material bgMaterial;
    private Material fillMaterial;
    private Renderer fillRenderer;
    
    void Start()
    {
        Debug.Log("HealthBar: Starting initialization...");
        
        healthSystem = GetComponentInParent<HealthSystem>();
        if (healthSystem == null)
        {
            Debug.LogError("HealthBar: HealthSystem component not found in parent!");
            enabled = false;
            return;
        }
        Debug.Log("HealthBar: HealthSystem found successfully.");
        
        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            mainCamera = FindObjectOfType<Camera>();
        }
        
        if (mainCamera == null)
        {
            Debug.LogWarning("HealthBar: No camera found! Health bar may not be visible.");
        }
        else
        {
            Debug.Log($"HealthBar: Camera found: {mainCamera.name}");
        }
        
        CreateHealthBarVisuals();
    }
    
    void CreateHealthBarVisuals()
    {
        Debug.Log("HealthBar: Creating health bar visuals...");
        
        // Find appropriate shader for URP
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null)
        {
            shader = Shader.Find("Shader Graphs/Unlit");
        }
        if (shader == null)
        {
            shader = Shader.Find("Unlit/Color");
        }
        if (shader == null)
        {
            shader = Shader.Find("Unlit/Texture");
        }
        if (shader == null)
        {
            shader = Shader.Find("Sprites/Default");
        }
        if (shader == null)
        {
            shader = Shader.Find("Standard");
        }
        
        if (shader == null)
        {
            Debug.LogError("HealthBar: No suitable shader found! Using default.");
            shader = Shader.Find("Diffuse");
        }
        else
        {
            Debug.Log($"HealthBar: Using shader: {shader.name}");
        }
        
        bool isURPShader = shader.name.Contains("Universal Render Pipeline") || shader.name.Contains("Shader Graphs");
        
        // Create background quad
        healthBarBackground = GameObject.CreatePrimitive(PrimitiveType.Quad);
        healthBarBackground.name = "Health Bar Background";
        healthBarBackground.transform.parent = transform;
        healthBarBackground.transform.localPosition = new Vector3(0f, offsetY, 0f);
        healthBarBackground.transform.localScale = new Vector3(barWidth, barHeight, 1f);
        // Rotate quad 180 degrees on Y axis so it faces the camera (quads face -Z by default)
        healthBarBackground.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
        
        bgMaterial = new Material(shader);
        
        // Set color based on shader type
        if (isURPShader && bgMaterial.HasProperty("_BaseColor"))
        {
            bgMaterial.SetColor("_BaseColor", backgroundColor);
            Debug.Log("HealthBar: Set _BaseColor for URP shader");
        }
        else if (bgMaterial.HasProperty("_Color"))
        {
            bgMaterial.SetColor("_Color", backgroundColor);
            Debug.Log("HealthBar: Set _Color for shader");
        }
        else
        {
            bgMaterial.color = backgroundColor;
            Debug.Log("HealthBar: Set color directly");
        }
        
        // Disable culling so quad is visible from both sides
        if (bgMaterial.HasProperty("_Cull"))
        {
            bgMaterial.SetFloat("_Cull", 0f); // 0 = None, 1 = Front, 2 = Back
        }
        
        // Set rendering mode for transparency if using Standard shader
        if (shader.name.Contains("Standard"))
        {
            bgMaterial.SetFloat("_Mode", 3); // Transparent mode
            bgMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            bgMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            bgMaterial.SetInt("_ZWrite", 0);
            bgMaterial.DisableKeyword("_ALPHATEST_ON");
            bgMaterial.EnableKeyword("_ALPHABLEND_ON");
            bgMaterial.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            bgMaterial.renderQueue = 3000;
        }
        
        Renderer bgRenderer = healthBarBackground.GetComponent<Renderer>();
        bgRenderer.material = bgMaterial;
        // Disable shadow casting and receiving
        bgRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        bgRenderer.receiveShadows = false;
        
        // Remove collider from background
        Destroy(healthBarBackground.GetComponent<Collider>());
        Debug.Log("HealthBar: Background quad created successfully.");
        
        // Create fill quad
        healthBarFill = GameObject.CreatePrimitive(PrimitiveType.Quad);
        healthBarFill.name = "Health Bar Fill";
        healthBarFill.transform.parent = transform;
        // Position at right edge of bar (barWidth/2) so it can shrink from right to left
        healthBarFill.transform.localPosition = new Vector3(barWidth / 2f, offsetY, 0.01f); // Right edge, slightly in front
        healthBarFill.transform.localScale = new Vector3(barWidth, barHeight, 1f);
        // Rotate quad 180 degrees on Y axis so it faces the camera
        healthBarFill.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
        
        fillMaterial = new Material(shader);
        
        // Set color based on shader type - ensure it's bright and visible
        Color fillColorToUse = healthColor;
        if (isURPShader && fillMaterial.HasProperty("_BaseColor"))
        {
            fillMaterial.SetColor("_BaseColor", fillColorToUse);
            Debug.Log($"HealthBar: Set _BaseColor to {fillColorToUse} for fill");
        }
        else if (fillMaterial.HasProperty("_Color"))
        {
            fillMaterial.SetColor("_Color", fillColorToUse);
            Debug.Log($"HealthBar: Set _Color to {fillColorToUse} for fill");
        }
        else
        {
            fillMaterial.color = fillColorToUse;
            Debug.Log($"HealthBar: Set color directly to {fillColorToUse} for fill");
        }
        
        // Also try setting main texture color if available
        if (fillMaterial.HasProperty("_MainTex"))
        {
            // Create a simple white texture to ensure color shows
            Texture2D whiteTex = new Texture2D(1, 1);
            whiteTex.SetPixel(0, 0, Color.white);
            whiteTex.Apply();
            fillMaterial.SetTexture("_MainTex", whiteTex);
        }
        
        // Disable culling so quad is visible from both sides
        if (fillMaterial.HasProperty("_Cull"))
        {
            fillMaterial.SetFloat("_Cull", 0f); // 0 = None
        }
        
        // Set rendering mode for transparency if using Standard shader
        if (shader.name.Contains("Standard"))
        {
            fillMaterial.SetFloat("_Mode", 3); // Transparent mode
            fillMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            fillMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            fillMaterial.SetInt("_ZWrite", 0);
            fillMaterial.DisableKeyword("_ALPHATEST_ON");
            fillMaterial.EnableKeyword("_ALPHABLEND_ON");
            fillMaterial.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            fillMaterial.renderQueue = 3000;
        }
        
        fillRenderer = healthBarFill.GetComponent<Renderer>();
        fillRenderer.material = fillMaterial;
        // Disable shadow casting and receiving
        fillRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        fillRenderer.receiveShadows = false;
        
        // Verify the material color was set
        Color actualColor = fillMaterial.HasProperty("_BaseColor") ? fillMaterial.GetColor("_BaseColor") : 
                           (fillMaterial.HasProperty("_Color") ? fillMaterial.GetColor("_Color") : fillMaterial.color);
        Debug.Log($"HealthBar: Fill material color is set to: {actualColor}");
        
        // Remove collider from fill
        Destroy(healthBarFill.GetComponent<Collider>());
        Debug.Log("HealthBar: Fill quad created successfully.");
        Debug.Log($"HealthBar: Health bar created at position {transform.position} with offset Y: {offsetY}");
    }
    
    void LateUpdate()
    {
        if (mainCamera == null || healthSystem == null || healthBarBackground == null || healthBarFill == null) return;
        
        // Make the health bar face the camera (billboard effect)
        Vector3 lookDirection = transform.position - mainCamera.transform.position;
        lookDirection.y = 0f; // Keep it upright
        if (lookDirection != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(-lookDirection);
        }
        
        // Update health bar fill
        float healthPercent = healthSystem.HealthPercentage;
        float currentWidth = barWidth * healthPercent;
        healthBarFill.transform.localScale = new Vector3(currentWidth, barHeight, 1f);
        
        // Keep fill anchored to right edge - adjust position so right edge stays at barWidth/2
        // When scaled, the quad's center moves, so we need to adjust position
        // Right edge of bar is at barWidth/2, so center of fill should be at barWidth/2 - currentWidth/2
        float fillCenterX = (barWidth / 2f) - (currentWidth / 2f);
        healthBarFill.transform.localPosition = new Vector3(fillCenterX, offsetY, 0.01f);
        
        // Change color based on health
        Color currentColor = healthPercent <= lowHealthThreshold ? lowHealthColor : healthColor;
        if (fillMaterial != null && fillRenderer != null)
        {
            // Get the material instance (not shared material) to ensure changes apply
            Material mat = fillRenderer.material;
            
            // Update color based on shader type
            if (mat.HasProperty("_BaseColor"))
            {
                mat.SetColor("_BaseColor", currentColor);
            }
            else if (mat.HasProperty("_Color"))
            {
                mat.SetColor("_Color", currentColor);
            }
            else
            {
                mat.color = currentColor;
            }
        }
    }

    public void SetOffset(float newOffset)
    {
        offsetY = newOffset;
    }
}


*/