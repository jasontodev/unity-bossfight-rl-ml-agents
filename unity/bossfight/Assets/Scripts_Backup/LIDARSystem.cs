/*
using UnityEngine;
using System.Collections.Generic;

public class LIDARSystem : MonoBehaviour
{
    [Header("LIDAR Settings")]
    [SerializeField] private int rayCount = 30;
    [SerializeField] private float rayRange = 10f;
    [SerializeField] private float attackRange = 2f; // Attack range for orange ray
    [SerializeField] private float healRange = 5f; // Healing range for green ray
    [SerializeField] private Color forwardRayColor = new Color(1f, 0.5f, 0f, 1f); // Orange
    [SerializeField] private Color healRayColor = Color.green; // Green for healing
    [SerializeField] private Color otherRayColor = Color.yellow;
    [SerializeField] private bool showRays = true;
    
    private RaycastHit[] rayHits;
    private bool[] rayHitsTarget;
    private GameObject[] rayVisualizers;
    private GameObject yellowForwardRayVisualizer; // Extra yellow ray at forward position with full range
    private GameObject greenHealRayVisualizer; // Green ray for healing detection
    
    void Start()
    {
        rayHits = new RaycastHit[rayCount];
        rayHitsTarget = new bool[rayCount];
        rayVisualizers = new GameObject[rayCount];
        
        // Create visual line renderers for each ray
        if (showRays)
        {
            CreateRayVisualizers();
        }
    }
    
    void CreateRayVisualizers()
    {
        for (int i = 0; i < rayCount; i++)
        {
            GameObject rayVisualizer = new GameObject($"LIDAR Ray {i}");
            rayVisualizer.transform.parent = transform;
            rayVisualizer.transform.localPosition = Vector3.zero;
            
            LineRenderer lineRenderer = rayVisualizer.AddComponent<LineRenderer>();
            lineRenderer.material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            lineRenderer.startWidth = 0.05f;
            lineRenderer.endWidth = 0.05f;
            lineRenderer.positionCount = 2;
            lineRenderer.useWorldSpace = true;
            lineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lineRenderer.receiveShadows = false;
            
            // Set color: forward ray (index 0) is green, others are yellow
            Color rayColor = (i == 0) ? forwardRayColor : otherRayColor;
            lineRenderer.startColor = rayColor;
            lineRenderer.endColor = rayColor;
            
            // For URP shader, set _BaseColor
            if (lineRenderer.material.HasProperty("_BaseColor"))
            {
                lineRenderer.material.SetColor("_BaseColor", rayColor);
            }
            else if (lineRenderer.material.HasProperty("_Color"))
            {
                lineRenderer.material.SetColor("_Color", rayColor);
            }
            
            rayVisualizers[i] = rayVisualizer;
        }
        
        // Create extra yellow ray at forward position with full range
        GameObject yellowForwardRay = new GameObject("LIDAR Yellow Forward Ray");
        yellowForwardRay.transform.parent = transform;
        yellowForwardRay.transform.localPosition = Vector3.zero;
        
        LineRenderer yellowLineRenderer = yellowForwardRay.AddComponent<LineRenderer>();
        yellowLineRenderer.material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        yellowLineRenderer.startWidth = 0.05f;
        yellowLineRenderer.endWidth = 0.05f;
        yellowLineRenderer.positionCount = 2;
        yellowLineRenderer.useWorldSpace = true;
        yellowLineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        yellowLineRenderer.receiveShadows = false;
        yellowLineRenderer.startColor = otherRayColor; // Yellow
        yellowLineRenderer.endColor = otherRayColor;
        
        if (yellowLineRenderer.material.HasProperty("_BaseColor"))
        {
            yellowLineRenderer.material.SetColor("_BaseColor", otherRayColor);
        }
        else if (yellowLineRenderer.material.HasProperty("_Color"))
        {
            yellowLineRenderer.material.SetColor("_Color", otherRayColor);
        }
        
        yellowForwardRayVisualizer = yellowForwardRay;
        
        // Create green healing ray at forward position (for healers)
        GameObject greenHealRay = new GameObject("LIDAR Green Heal Ray");
        greenHealRay.transform.parent = transform;
        greenHealRay.transform.localPosition = Vector3.zero;
        
        LineRenderer greenLineRenderer = greenHealRay.AddComponent<LineRenderer>();
        greenLineRenderer.material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        greenLineRenderer.startWidth = 0.05f;
        greenLineRenderer.endWidth = 0.05f;
        greenLineRenderer.positionCount = 2;
        greenLineRenderer.useWorldSpace = true;
        greenLineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        greenLineRenderer.receiveShadows = false;
        greenLineRenderer.startColor = healRayColor; // Green
        greenLineRenderer.endColor = healRayColor;
        
        if (greenLineRenderer.material.HasProperty("_BaseColor"))
        {
            greenLineRenderer.material.SetColor("_BaseColor", healRayColor);
        }
        else if (greenLineRenderer.material.HasProperty("_Color"))
        {
            greenLineRenderer.material.SetColor("_Color", healRayColor);
        }
        
        greenHealRayVisualizer = greenHealRay;
    }
    
    void Update()
    {
        // Get character center position (account for CharacterController if present)
        CharacterController charController = GetComponent<CharacterController>();
        Vector3 rayOrigin = transform.position;
        if (charController != null)
        {
            rayOrigin = transform.position + charController.center;
        }
        
        // Cast rays in a circle around the character
        float angleStep = 360f / rayCount;
        
        for (int i = 0; i < rayCount; i++)
        {
            // Calculate ray direction
            // Ray 0 is forward (0 degrees), then rotate around
            float angle = i * angleStep;
            Vector3 direction = Quaternion.Euler(0, angle, 0) * transform.forward;
            
            // Cast ray from character center
            // For forward ray (index 0), use attack range for detection, others use full range
            float detectionRange = (i == 0) ? attackRange : rayRange;
            RaycastHit hit;
            bool hitSomething = Physics.Raycast(rayOrigin, direction, out hit, detectionRange);
            
            rayHits[i] = hit;
            rayHitsTarget[i] = false;
            
            // Check if we hit a valid target (boss or player)
            if (hitSomething)
            {
                // Check if it's a boss or player
                BossController boss = hit.collider.GetComponent<BossController>();
                StationaryBoss stationaryBoss = hit.collider.GetComponent<StationaryBoss>();
                PlayerController player = hit.collider.GetComponent<PlayerController>();
                StationaryPlayer stationaryPlayer = hit.collider.GetComponent<StationaryPlayer>();
                
                // Don't detect self - check if the hit object is this character or a child
                Transform hitTransform = hit.collider.transform;
                bool isSelf = (hitTransform == transform || hitTransform.IsChildOf(transform));
                
                if (!isSelf && (boss != null || stationaryBoss != null || player != null || stationaryPlayer != null))
                {
                    rayHitsTarget[i] = true;
                }
            }
            
            // Update visualizer
            if (showRays && rayVisualizers[i] != null)
            {
                LineRenderer lineRenderer = rayVisualizers[i].GetComponent<LineRenderer>();
                if (lineRenderer != null)
                {
                    // For forward ray (index 0), limit to attack range
                    float displayRange = (i == 0) ? attackRange : rayRange;
                    Vector3 endPoint;
                    
                    if (hitSomething)
                    {
                        // If hit is within display range, show hit point, otherwise show max range
                        float hitDistance = Vector3.Distance(rayOrigin, hit.point);
                        endPoint = (hitDistance <= displayRange) ? hit.point : (rayOrigin + direction * displayRange);
                    }
                    else
                    {
                        endPoint = rayOrigin + direction * displayRange;
                    }
                    
                    lineRenderer.SetPosition(0, rayOrigin);
                    lineRenderer.SetPosition(1, endPoint);
                    
                    // Make the ray more visible when it hits a target
                    Color rayColor = (i == 0) ? forwardRayColor : otherRayColor;
                    if (rayHitsTarget[i])
                    {
                        rayColor = Color.red; // Red when hitting a target
                    }
                    
                    lineRenderer.startColor = rayColor;
                    lineRenderer.endColor = rayColor;
                }
            }
        }
        
        // Update yellow forward ray (full range)
        if (showRays && yellowForwardRayVisualizer != null)
        {
            LineRenderer yellowLineRenderer = yellowForwardRayVisualizer.GetComponent<LineRenderer>();
            if (yellowLineRenderer != null)
            {
                Vector3 forwardDirection = transform.forward;
                RaycastHit forwardHit;
                bool forwardHitSomething = Physics.Raycast(rayOrigin, forwardDirection, out forwardHit, rayRange);
                
                Vector3 yellowEndPoint = forwardHitSomething ? forwardHit.point : (rayOrigin + forwardDirection * rayRange);
                yellowLineRenderer.SetPosition(0, rayOrigin);
                yellowLineRenderer.SetPosition(1, yellowEndPoint);
            }
        }
        
        // Update green healing ray (heal range) - only show for healers
        PlayerClassSystem classSystem = GetComponent<PlayerClassSystem>();
        bool isHealer = classSystem != null && classSystem.CurrentClass == PlayerClass.Healer;
        
        if (showRays && greenHealRayVisualizer != null)
        {
            LineRenderer greenLineRenderer = greenHealRayVisualizer.GetComponent<LineRenderer>();
            if (greenLineRenderer != null)
            {
                // Only show green ray for healers
                greenLineRenderer.enabled = isHealer;
                
                if (isHealer)
                {
                    Vector3 forwardDirection = transform.forward;
                    RaycastHit healHit;
                    bool healHitSomething = Physics.Raycast(rayOrigin, forwardDirection, out healHit, healRange);
                    
                    Vector3 greenEndPoint = healHitSomething ? healHit.point : (rayOrigin + forwardDirection * healRange);
                    greenLineRenderer.SetPosition(0, rayOrigin);
                    greenLineRenderer.SetPosition(1, greenEndPoint);
                }
            }
        }
    }
    
    /// <summary>
    /// Check if the forward ray (ray 0) is hitting a valid target
    /// </summary>
    public bool IsForwardRayHittingTarget()
    {
        return rayHitsTarget[0];
    }
    
    /// <summary>
    /// Get the target hit by the forward ray
    /// </summary>
    public GameObject GetForwardRayTarget()
    {
        if (rayHitsTarget[0] && rayHits[0].collider != null)
        {
            return rayHits[0].collider.gameObject;
        }
        return null;
    }
    
    /// <summary>
    /// Get all ray hit information
    /// </summary>
    public RaycastHit[] GetAllRayHits()
    {
        return rayHits;
    }
    
    /// <summary>
    /// Get which rays are hitting targets
    /// </summary>
    public bool[] GetRayHitsTarget()
    {
        return rayHitsTarget;
    }
    
    /// <summary>
    /// Set the attack range (for class-based range modifications)
    /// </summary>
    public void SetAttackRange(float newAttackRange)
    {
        attackRange = newAttackRange;
    }
    
    /// <summary>
    /// Check if a target is within healing line of sight
    /// </summary>
    public bool IsTargetInHealRange(GameObject target)
    {
        if (target == null) return false;
        
        CharacterController charController = GetComponent<CharacterController>();
        Vector3 rayOrigin = transform.position;
        if (charController != null)
        {
            rayOrigin = transform.position + charController.center;
        }
        
        Vector3 direction = (target.transform.position - rayOrigin).normalized;
        float distance = Vector3.Distance(rayOrigin, target.transform.position);
        
        // Check if target is within heal range
        if (distance > healRange) return false;
        
        // Check line of sight
        RaycastHit hit;
        if (Physics.Raycast(rayOrigin, direction, out hit, healRange))
        {
            // Check if we hit the target or a child of the target
            Transform hitTransform = hit.collider.transform;
            if (hitTransform == target.transform || hitTransform.IsChildOf(target.transform))
            {
                return true;
            }
        }
        
        return false;
    }
    
    /// <summary>
    /// Get all targets within healing line of sight (forward direction only, like green ray)
    /// </summary>
    public List<GameObject> GetTargetsInHealRange()
    {
        List<GameObject> validTargets = new List<GameObject>();
        
        CharacterController charController = GetComponent<CharacterController>();
        Vector3 rayOrigin = transform.position;
        if (charController != null)
        {
            rayOrigin = transform.position + charController.center;
        }
        
        // Cast ray forward (same as green healing ray)
        Vector3 forwardDirection = transform.forward;
        RaycastHit[] hits = Physics.RaycastAll(rayOrigin, forwardDirection, healRange);
        
        foreach (RaycastHit hit in hits)
        {
            GameObject hitObject = hit.collider.gameObject;
            
            // Don't target self - check if the hit is this character or a child
            Transform hitTransform = hit.collider.transform;
            bool isSelf = (hitTransform == transform || hitTransform.IsChildOf(transform));
            if (isSelf) continue;
            
            // Check if it's a valid target (player or boss) - check on the GameObject, not the collider
            PlayerController player = hitObject.GetComponent<PlayerController>();
            BossController boss = hitObject.GetComponent<BossController>();
            StationaryPlayer stationaryPlayer = hitObject.GetComponent<StationaryPlayer>();
            StationaryBoss stationaryBoss = hitObject.GetComponent<StationaryBoss>();
            
            // Also check parent in case collider is on a child object
            if (player == null && boss == null && stationaryPlayer == null && stationaryBoss == null && hitObject.transform.parent != null)
            {
                GameObject parent = hitObject.transform.parent.gameObject;
                player = parent.GetComponent<PlayerController>();
                boss = parent.GetComponent<BossController>();
                stationaryPlayer = parent.GetComponent<StationaryPlayer>();
                stationaryBoss = parent.GetComponent<StationaryBoss>();
                if (player != null || boss != null || stationaryPlayer != null || stationaryBoss != null)
                {
                    hitObject = parent;
                }
            }
            
            if (player != null || boss != null || stationaryPlayer != null || stationaryBoss != null)
            {
                // Only add if not already in list
                if (!validTargets.Contains(hitObject))
                {
                    validTargets.Add(hitObject);
                }
            }
        }
        
        return validTargets;
    }
}


*/