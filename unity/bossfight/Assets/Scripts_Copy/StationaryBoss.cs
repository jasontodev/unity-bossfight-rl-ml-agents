/*
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(HealthSystem))]
public class StationaryBoss : MonoBehaviour
{
    private CharacterController characterController;
    private HealthSystem healthSystem;
    private bool isGrounded;
    private Vector3 velocity;
    
    [Header("Gravity Settings")]
    [SerializeField] private float gravity = -9.81f;
    
    void Start()
    {
        characterController = GetComponent<CharacterController>();
        healthSystem = GetComponent<HealthSystem>();
        
        // Ensure we have a capsule visual representation
        if (GetComponentInChildren<MeshRenderer>() == null)
        {
            GameObject capsuleVisual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            capsuleVisual.name = "Capsule Visual";
            capsuleVisual.transform.parent = transform;
            capsuleVisual.transform.localPosition = Vector3.zero;
            capsuleVisual.transform.localRotation = Quaternion.identity;
            capsuleVisual.transform.localScale = Vector3.one;
            
            // Remove the collider from the visual since CharacterController handles collision
            Destroy(capsuleVisual.GetComponent<Collider>());
        }
    }
    
    // Use OnControllerColliderHit to detect when CharacterController touches lava
    void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (hit.collider.CompareTag("Lava"))
        {
            if (healthSystem != null)
            {
                healthSystem.SetInLava(true);
            }
        }
    }
    
    // Detect when entering void trigger (instant death)
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Void"))
        {
            if (healthSystem != null)
            {
                healthSystem.TakeDamage(healthSystem.MaxHealth);
            }
        }
    }
    
    void Update()
    {
        // Ground check
        isGrounded = characterController.isGrounded;
        
        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f; // Small negative value to keep grounded
        }
        
        // Check if boss is standing on lava by checking what they're grounded on
        if (isGrounded)
        {
            Vector3 rayStart = transform.position + characterController.center - new Vector3(0, characterController.height / 2f, 0);
            RaycastHit hit;
            float rayDistance = 0.2f;
            if (Physics.Raycast(rayStart, Vector3.down, out hit, rayDistance))
            {
                if (hit.collider.CompareTag("Lava"))
                {
                    if (healthSystem != null)
                    {
                        healthSystem.SetInLava(true);
                    }
                }
                else
                {
                    if (healthSystem != null && healthSystem.GetIsInLava())
                    {
                        healthSystem.SetInLava(false);
                    }
                }
            }
            else
            {
                if (healthSystem != null && healthSystem.GetIsInLava())
                {
                    healthSystem.SetInLava(false);
                }
            }
        }
        
        // NO MOVEMENT - Boss is stationary
        // Only apply gravity to keep them grounded
        velocity.y += gravity * Time.deltaTime;
        characterController.Move(velocity * Time.deltaTime);
    }
}


*/