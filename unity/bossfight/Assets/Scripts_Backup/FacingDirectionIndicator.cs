/*
using UnityEngine;

public class FacingDirectionIndicator : MonoBehaviour
{
    [Header("Indicator Settings")]
    [SerializeField] private float arrowLength = 2f; // Longer for laser effect
    [SerializeField] private float arrowWidth = 0.05f; // Much thinner, like a laser
    [SerializeField] private Color arrowColor = Color.yellow;
    [SerializeField] private float heightOffset = 1.2f; // Position at "face" level
    
    private GameObject arrowObject;
    private Material arrowMaterial;
    
    void Start()
    {
        CreateArrowIndicator();
    }
    
    void CreateArrowIndicator()
    {
        // Create arrow using a cylinder (pointing forward) - thin like a laser
        arrowObject = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        arrowObject.name = "Facing Direction Arrow";
        arrowObject.transform.parent = transform;
        // Position at face level (higher up) and extend forward
        arrowObject.transform.localPosition = new Vector3(0f, heightOffset, arrowLength / 2f); // Position at face level, in front
        arrowObject.transform.localRotation = Quaternion.Euler(90f, 0f, 0f); // Rotate to point forward
        arrowObject.transform.localScale = new Vector3(arrowWidth, arrowLength / 2f, arrowWidth); // Thin laser-like cylinder
        
        // Create material for arrow
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null)
        {
            shader = Shader.Find("Unlit/Color");
        }
        if (shader == null)
        {
            shader = Shader.Find("Standard");
        }
        
        arrowMaterial = new Material(shader);
        bool isURPShader = shader != null && (shader.name.Contains("Universal Render Pipeline") || shader.name.Contains("Shader Graphs"));
        
        if (isURPShader && arrowMaterial.HasProperty("_BaseColor"))
        {
            arrowMaterial.SetColor("_BaseColor", arrowColor);
        }
        else if (arrowMaterial.HasProperty("_Color"))
        {
            arrowMaterial.SetColor("_Color", arrowColor);
        }
        else
        {
            arrowMaterial.color = arrowColor;
        }
        
        arrowObject.GetComponent<Renderer>().material = arrowMaterial;
        
        // Remove collider from arrow (it's just visual)
        Destroy(arrowObject.GetComponent<Collider>());
        
        // Disable shadows
        Renderer renderer = arrowObject.GetComponent<Renderer>();
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
    }
    
    void LateUpdate()
    {
        // Keep arrow pointing in the forward direction (it rotates with the parent)
        if (arrowObject != null)
        {
            // Arrow is already positioned and rotated correctly as a child
            // It will automatically rotate with the parent transform
        }
    }
}


*/