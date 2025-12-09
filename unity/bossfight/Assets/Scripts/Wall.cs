using UnityEngine;

public class Wall : MonoBehaviour
{
    [Header("Wall Settings")]
    [SerializeField] private float wallHeight = 1.5f;
    
    private bool isBeingCarried = false;
    private GameObject carriedBy = null;
    private Rigidbody rb;
    private BoxCollider boxCollider;
    
    void Start()
    {
        // Add Rigidbody for physics (start kinematic to prevent falling)
        rb = gameObject.AddComponent<Rigidbody>();
        rb.isKinematic = true; // Start kinematic so walls don't fall
        rb.mass = 10f; // Walls have some weight
        rb.freezeRotation = true; // Freeze rotation to prevent falling over
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ | RigidbodyConstraints.FreezePositionY;
        
        // Ensure we have a BoxCollider
        boxCollider = GetComponent<BoxCollider>();
        if (boxCollider == null)
        {
            boxCollider = gameObject.AddComponent<BoxCollider>();
        }
        // Set collider size to 1, 1, 1
        boxCollider.size = new Vector3(1f, 1f, 1f);
        boxCollider.center = Vector3.zero;
        
        // Ensure wall is properly positioned on ground (only if not already positioned correctly)
        // Check if wall bottom is at ground level (y=0)
        float expectedBottomY = transform.position.y - wallHeight / 2f;
        if (Mathf.Abs(expectedBottomY) > 0.1f) // If not close to ground level
        {
            RaycastHit hit;
            if (Physics.Raycast(transform.position + Vector3.up * 5f, Vector3.down, out hit, 10f))
            {
                // Position wall so bottom is on the ground
                transform.position = new Vector3(transform.position.x, hit.point.y + wallHeight / 2f, transform.position.z);
            }
        }
    }
    
    public void PickUp(GameObject carrier)
    {
        if (isBeingCarried) return;
        
        isBeingCarried = true;
        carriedBy = carrier;
        
        // Keep collider enabled for collision
        if (boxCollider != null)
        {
            boxCollider.enabled = true;
        }
        
        // Use non-kinematic with constraints for proper collision with CharacterControllers
        // CharacterControllers collide better with non-kinematic rigidbodies
        if (rb != null)
        {
            rb.isKinematic = false; // Non-kinematic for proper collision
            rb.useGravity = false; // No gravity when carried
            rb.drag = 50f; // High drag to keep it controlled
            rb.angularDrag = 50f; // High angular drag
            rb.freezeRotation = false; // Allow Y rotation
            rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ; // Only allow Y rotation
        }
        
        // Don't parent to avoid CharacterController collision issues
        // Instead, we'll follow the carrier in Update() using MovePosition
        transform.parent = null;
    }
    
    public void PlaceDown()
    {
        if (!isBeingCarried) return;
        
        isBeingCarried = false;
        GameObject previousCarrier = carriedBy;
        carriedBy = null;
        
        // Ensure not parented
        transform.parent = null;
        
        // Position on ground (raycast down to find ground)
        RaycastHit hit;
        if (Physics.Raycast(transform.position, Vector3.down, out hit, 10f))
        {
            transform.position = hit.point + new Vector3(0f, wallHeight / 2f, 0f);
        }
        
        // Keep collider enabled (it was already enabled when picked up)
        if (boxCollider != null)
        {
            boxCollider.enabled = true;
        }
        
        // Keep kinematic and frozen to prevent falling over
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.freezeRotation = true;
            rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ | RigidbodyConstraints.FreezePositionY;
        }
    }
    
    public bool IsBeingCarried()
    {
        return isBeingCarried;
    }
    
    void FixedUpdate()
    {
        // Keep wall following the carrier and rotating with them when being carried
        // Use FixedUpdate for physics-based movement
        if (isBeingCarried && carriedBy != null && rb != null)
        {
            // Calculate position in front of carrier (lower height when carried)
            Vector3 targetPosition = carriedBy.transform.position + 
                                    carriedBy.transform.forward * 1f + 
                                    Vector3.up * 0.5f; // Lowered from 1.5f to 0.5f
            
            // Use MovePosition to respect collisions
            rb.MovePosition(Vector3.Lerp(transform.position, targetPosition, Time.fixedDeltaTime * 10f));
            
            // Rotate wall to match carrier's Y rotation (but keep X and Z at 0 to keep it upright)
            float carrierYRotation = carriedBy.transform.eulerAngles.y;
            Quaternion targetRotation = Quaternion.Euler(0f, carrierYRotation, 0f);
            rb.MoveRotation(Quaternion.Lerp(transform.rotation, targetRotation, Time.fixedDeltaTime * 10f));
        }
    }
}

