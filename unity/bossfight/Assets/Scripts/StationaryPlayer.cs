using UnityEngine;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(HealthSystem))]
public class StationaryPlayer : MonoBehaviour
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
        
        // Ensure we have a capsule collider (CharacterController uses capsule internally)
        // If no visual representation, add a capsule mesh renderer
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
    
    void OnGUI()
    {
        if (healthSystem == null) return;
        
        // Debug display in top left corner
        GUIStyle style = new GUIStyle();
        style.normal.textColor = Color.white;
        style.fontSize = 16;
        style.fontStyle = FontStyle.Bold;
        style.alignment = TextAnchor.UpperLeft;
        
        float yPos = 10f;
        float lineHeight = 25f;
        
        // Health display
        GUI.Label(new Rect(10, yPos, 300, 30), $"Health: {healthSystem.CurrentHealth:F1} / {healthSystem.MaxHealth:F1}", style);
        yPos += lineHeight;
        
        // In Lava status
        if (healthSystem.GetIsInLava())
        {
            style.normal.textColor = Color.red;
            GUI.Label(new Rect(10, yPos, 300, 30), "Status: IN LAVA (Taking damage!)", style);
            yPos += lineHeight;
            style.normal.textColor = Color.yellow;
            GUI.Label(new Rect(10, yPos, 300, 30), $"Damage: {healthSystem.lavaDamagePerTick} every {healthSystem.lavaTickInterval}s", style);
            yPos += lineHeight;
        }
        else
        {
            style.normal.textColor = Color.green;
            GUI.Label(new Rect(10, yPos, 300, 30), "Status: Safe", style);
            yPos += lineHeight;
        }
        
        // Burning status
        if (healthSystem.IsBurning)
        {
            style.normal.textColor = new Color(1f, 0.5f, 0f, 1f); // Orange color
            float burnTimeLeft = healthSystem.GetBurnTimer();
            GUI.Label(new Rect(10, yPos, 300, 30), $"BURNING: {burnTimeLeft:F1}s remaining", style);
            yPos += lineHeight;
            style.normal.textColor = Color.yellow;
            GUI.Label(new Rect(10, yPos, 300, 30), $"Burn Damage: {healthSystem.burnDamagePerTick} every {healthSystem.burnTickInterval}s", style);
            yPos += lineHeight;
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
        
        // Check if player is standing on lava by checking what they're grounded on
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
        
        // NO MOVEMENT - Player is stationary
        // Only apply gravity to keep them grounded
        velocity.y += gravity * Time.deltaTime;
        characterController.Move(velocity * Time.deltaTime);
    }
}

