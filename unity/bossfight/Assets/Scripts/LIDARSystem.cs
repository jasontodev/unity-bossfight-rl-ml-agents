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
    [SerializeField] private bool showYellowRays = false; // Yellow LIDAR rays, toggle with \ key
    [SerializeField] private bool showOrangeRay = true; // Orange attack range ray, toggle with [ key
    [SerializeField] private bool showGreenRay = true; // Green heal range ray, toggle with ] key
    
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
        
        // Create visual line renderers for each ray (always create, but disable by default)
        CreateRayVisualizers();
        
        // Set initial visibility state
        SetYellowRaysEnabled(showYellowRays);
        SetOrangeRayEnabled(showOrangeRay);
        SetGreenRayEnabled(showGreenRay);
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
        // Toggle LIDAR visualization with [, ], \ keys (to avoid conflict with class selection U/I/O/P)
        // \ key for yellow rays
        if (Input.GetKeyDown(KeyCode.Backslash) || Input.GetKeyDown(KeyCode.BackQuote))
        {
            showYellowRays = !showYellowRays;
            SetYellowRaysEnabled(showYellowRays);
            Debug.Log($"Yellow LIDAR rays: {(showYellowRays ? "ON" : "OFF")}");
        }
        
        // [ key for orange attack range ray
        if (Input.GetKeyDown(KeyCode.LeftBracket))
        {
            showOrangeRay = !showOrangeRay;
            SetOrangeRayEnabled(showOrangeRay);
            Debug.Log($"Orange attack range ray: {(showOrangeRay ? "ON" : "OFF")}");
        }
        
        // ] key for green heal range ray
        if (Input.GetKeyDown(KeyCode.RightBracket))
        {
            showGreenRay = !showGreenRay;
            SetGreenRayEnabled(showGreenRay);
            Debug.Log($"Green heal range ray: {(showGreenRay ? "ON" : "OFF")}");
        }
        
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
                
                // Also check for walls, lava, and void
                Wall wall = hit.collider.GetComponent<Wall>();
                bool isLava = hit.collider.CompareTag("Lava");
                bool isVoid = hit.collider.CompareTag("Void");
                
                // Don't detect self - check if the hit object is this character or a child
                Transform hitTransform = hit.collider.transform;
                bool isSelf = (hitTransform == transform || hitTransform.IsChildOf(transform));
                
                if (!isSelf && (boss != null || stationaryBoss != null || player != null || stationaryPlayer != null || wall != null || isLava || isVoid))
                {
                    // Only mark as target if it's an agent (for attack purposes)
                    if (boss != null || stationaryBoss != null || player != null || stationaryPlayer != null)
                    {
                        rayHitsTarget[i] = true;
                    }
                }
            }
            
            // Update visualizer (skip ray 0 as it's handled separately for orange attack range)
            // Always keep yellow ray positions in sync with the agent, even if hidden.
            // This way, when we toggle them back on after an episode reset, they appear
            // at the correct location instead of where they were last drawn.
            if (i > 0 && rayVisualizers[i] != null)
            {
                LineRenderer lineRenderer = rayVisualizers[i].GetComponent<LineRenderer>();
                if (lineRenderer != null)
                {
                    float displayRange = rayRange;
                    Vector3 endPoint;
                    
                    if (hitSomething)
                    {
                        // If hit is within display range, show hit point, otherwise show max range
                        float hitDistance = Vector3.Distance(rayOrigin, hit.point);
                        endPoint = (hitDistance <= displayRange) ? hit.point : (rayOrigin + direction * displayRange);
                        
                        // Don't show rays that hit the ground/arena (only show if hitting an agent, wall, or hazard)
                        // Check if it's hitting something we care about
                        bool isImportantHit = rayHitsTarget[i] || 
                                             hit.collider.GetComponent<Wall>() != null ||
                                             hit.collider.CompareTag("Lava") || 
                                             hit.collider.CompareTag("Void");
                        
                        // If it's not an important hit, it's probably the ground/arena
                        if (!isImportantHit)
                        {
                            // Hide the ray if it's just hitting ground/arena.
                            lineRenderer.enabled = false;
                            continue; // Skip rendering this ray
                        }
                    }
                    else
                    {
                        endPoint = rayOrigin + direction * displayRange;
                    }

                    // Enable or disable purely based on the toggle, but always keep
                    // the positions updated so they don't "lag behind" the agent.
                    lineRenderer.enabled = showYellowRays;
                    lineRenderer.SetPosition(0, rayOrigin);
                    lineRenderer.SetPosition(1, endPoint);
                    
                    // Make the ray more visible when it hits a target
                    Color rayColor = otherRayColor; // Yellow
                    if (rayHitsTarget[i])
                    {
                        rayColor = Color.red; // Red when hitting a target
                    }
                    
                    lineRenderer.startColor = rayColor;
                    lineRenderer.endColor = rayColor;
                }
            }
        }
        
        // Update orange attack range ray (ray 0) - toggleable with O key
        if (rayVisualizers != null && rayVisualizers[0] != null)
        {
            LineRenderer orangeLineRenderer = rayVisualizers[0].GetComponent<LineRenderer>();
            if (orangeLineRenderer != null)
            {
                orangeLineRenderer.enabled = showOrangeRay;
                
                if (showOrangeRay)
                {
                    Vector3 forwardDirection = transform.forward;
                    RaycastHit forwardHit;
                    bool forwardHitSomething = Physics.Raycast(rayOrigin, forwardDirection, out forwardHit, attackRange);
                    
                    Vector3 orangeEndPoint = forwardHitSomething ? forwardHit.point : (rayOrigin + forwardDirection * attackRange);
                    orangeLineRenderer.SetPosition(0, rayOrigin);
                    orangeLineRenderer.SetPosition(1, orangeEndPoint);
                    
                    // Make the ray more visible when it hits a target
                    Color rayColor = forwardRayColor; // Orange
                    if (rayHitsTarget[0])
                    {
                        rayColor = Color.red; // Red when hitting a target
                    }
                    
                    orangeLineRenderer.startColor = rayColor;
                    orangeLineRenderer.endColor = rayColor;
                }
            }
        }
        
        // Update yellow forward ray (full range) - toggleable with Y key
        if (showYellowRays && yellowForwardRayVisualizer != null)
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
        
        // Update green healing ray (heal range) - toggleable with G key, only for healers
        PlayerClassSystem classSystem = GetComponent<PlayerClassSystem>();
        bool isHealer = classSystem != null && classSystem.CurrentClass == PlayerClass.Healer;
        
        if (greenHealRayVisualizer != null)
        {
            LineRenderer greenLineRenderer = greenHealRayVisualizer.GetComponent<LineRenderer>();
            if (greenLineRenderer != null)
            {
                // Always update position, but only enable if toggle is on and is healer
                bool shouldShow = showGreenRay && isHealer;
                greenLineRenderer.enabled = shouldShow;
                
                if (shouldShow)
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
    
    void SetYellowRaysEnabled(bool enabled)
    {
        // Enable/disable all yellow LIDAR ray visualizers (rays 1-29 and yellow forward ray)
        if (rayVisualizers != null)
        {
            for (int i = 1; i < rayVisualizers.Length; i++) // Skip ray 0 (orange attack range)
            {
                if (rayVisualizers[i] != null)
                {
                    LineRenderer lineRenderer = rayVisualizers[i].GetComponent<LineRenderer>();
                    if (lineRenderer != null)
                    {
                        lineRenderer.enabled = enabled;
                    }
                }
            }
        }
        
        // Enable/disable yellow forward ray
        if (yellowForwardRayVisualizer != null)
        {
            LineRenderer lineRenderer = yellowForwardRayVisualizer.GetComponent<LineRenderer>();
            if (lineRenderer != null)
            {
                lineRenderer.enabled = enabled;
            }
        }
    }
    
    void SetOrangeRayEnabled(bool enabled)
    {
        // Enable/disable orange attack range ray (ray 0)
        if (rayVisualizers != null && rayVisualizers[0] != null)
        {
            LineRenderer lineRenderer = rayVisualizers[0].GetComponent<LineRenderer>();
            if (lineRenderer != null)
            {
                lineRenderer.enabled = enabled;
            }
        }
    }
    
    void SetGreenRayEnabled(bool enabled)
    {
        // Enable/disable green heal range ray (only visible for healers)
        if (greenHealRayVisualizer != null)
        {
            LineRenderer lineRenderer = greenHealRayVisualizer.GetComponent<LineRenderer>();
            if (lineRenderer != null)
            {
                PlayerClassSystem classSystem = GetComponent<PlayerClassSystem>();
                bool isHealer = classSystem != null && classSystem.CurrentClass == PlayerClass.Healer;
                lineRenderer.enabled = enabled && isHealer;
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
    /// Get the current attack range
    /// </summary>
    public float AttackRange => attackRange;
    
    /// <summary>
    /// Get ray count
    /// </summary>
    public int RayCount => rayCount;
    
    /// <summary>
    /// Get ray range
    /// </summary>
    public float RayRange => rayRange;
    
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

