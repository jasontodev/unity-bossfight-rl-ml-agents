/*
using UnityEngine;

[RequireComponent(typeof(BossController))]
[RequireComponent(typeof(LIDARSystem))]
public class BossWallSystem : MonoBehaviour
{
    [Header("Wall Interaction Settings")]
    [SerializeField] private float pickupRange = 2f;
    [SerializeField] private KeyCode pickupKey = KeyCode.E;
    [SerializeField] private KeyCode placeKey = KeyCode.Q;
    
    private Wall carriedWall = null;
    private BossController bossController;
    private LIDARSystem lidarSystem;
    
    void Start()
    {
        bossController = GetComponent<BossController>();
        lidarSystem = GetComponent<LIDARSystem>();
        
        if (lidarSystem == null)
        {
            Debug.LogError("BossWallSystem requires LIDARSystem component!");
        }
    }
    
    void Update()
    {
        // Pick up wall
        if (Input.GetKeyDown(pickupKey) && carriedWall == null)
        {
            TryPickupWall();
        }
        
        // Place wall
        if (Input.GetKeyDown(placeKey) && carriedWall != null)
        {
            PlaceWall();
        }
    }
    
    void TryPickupWall()
    {
        if (lidarSystem == null) return;
        
        // Get character center position for raycast (same as LIDAR system)
        CharacterController charController = GetComponent<CharacterController>();
        Vector3 rayOrigin = transform.position;
        if (charController != null)
        {
            rayOrigin = transform.position + charController.center;
        }
        
        // Use the same forward ray detection as LIDAR system (orange ray range)
        // Get all ray hits from LIDAR system to check forward ray
        RaycastHit[] allHits = lidarSystem.GetAllRayHits();
        if (allHits != null && allHits.Length > 0)
        {
            // Ray 0 is the forward ray (orange ray)
            RaycastHit forwardHit = allHits[0];
            if (forwardHit.collider != null)
            {
                Wall wall = forwardHit.collider.GetComponent<Wall>();
                if (wall != null && !wall.IsBeingCarried())
                {
                    // Wall is in front within orange ray range
                    wall.PickUp(gameObject);
                    carriedWall = wall;
                    Debug.Log("Boss picked up a wall!");
                    return;
                }
            }
        }
        
        // Fallback: Cast ray forward if LIDAR system doesn't have the hit info
        Vector3 forwardDirection = transform.forward;
        RaycastHit hit;
        if (Physics.Raycast(rayOrigin, forwardDirection, out hit, pickupRange))
        {
            Wall wall = hit.collider.GetComponent<Wall>();
            if (wall != null && !wall.IsBeingCarried())
            {
                wall.PickUp(gameObject);
                carriedWall = wall;
                Debug.Log("Boss picked up a wall!");
            }
        }
    }
    
    void PlaceWall()
    {
        if (carriedWall != null)
        {
            carriedWall.PlaceDown();
            carriedWall = null;
            Debug.Log("Boss placed down a wall!");
        }
    }
    
    void OnGUI()
    {
        GUIStyle style = new GUIStyle();
        style.normal.textColor = Color.white;
        style.fontSize = 14;
        style.fontStyle = FontStyle.Bold;
        style.alignment = TextAnchor.UpperRight;
        
        float yPos = Screen.height - 100f;
        float lineHeight = 20f;
        float xPos = Screen.width - 250f;
        
        if (carriedWall == null)
        {
            style.normal.textColor = Color.cyan;
            GUI.Label(new Rect(xPos, yPos, 240, 30), $"Press {pickupKey} to Pick Up Wall", style);
        }
        else
        {
            style.normal.textColor = Color.green;
            GUI.Label(new Rect(xPos, yPos, 240, 30), $"Carrying Wall - Press {placeKey} to Place", style);
            yPos += lineHeight;
            style.normal.textColor = Color.yellow;
            GUI.Label(new Rect(xPos, yPos, 240, 30), "Move to position, then place", style);
        }
    }
    
    void OnDrawGizmosSelected()
    {
        // Visualize pickup range
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, pickupRange);
    }
}


*/