/*
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(HealthSystem))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float rotationSpeed = 180f; // 180 degrees per second
    
    [Header("Gravity Settings")]
    [SerializeField] private float gravity = -9.81f;
    
    private CharacterController characterController;
    private HealthSystem healthSystem;
    private Vector3 velocity;
    private bool isGrounded;
    
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
    // This works with solid (non-trigger) colliders
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
                // Instantly kill by dealing max health damage
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
        
        // Class display
        PlayerClassSystem classSystem = GetComponent<PlayerClassSystem>();
        if (classSystem != null)
        {
            style.normal.textColor = classSystem.GetClassColor();
            GUI.Label(new Rect(10, yPos, 300, 30), $"Class: {classSystem.CurrentClass}", style);
            yPos += lineHeight;
            
            // Show damage reduction for Tank
            if (classSystem.CurrentClass == PlayerClass.Tank)
            {
                style.normal.textColor = new Color(0.8f, 0.2f, 0.2f, 1f);
                GUI.Label(new Rect(10, yPos, 300, 30), $"Damage Reduction: {classSystem.DamageReduction * 100f:F0}%", style);
                yPos += lineHeight;
            }
        }
        
        // Health display
        GUI.Label(new Rect(10, yPos, 300, 30), $"Health: {healthSystem.CurrentHealth:F1} / {healthSystem.MaxHealth:F1}", style);
        yPos += lineHeight;
        
        // Threat display
        ThreatSystem threatSystem = ThreatSystem.Instance;
        if (threatSystem != null)
        {
            float threat = threatSystem.GetThreat(gameObject);
            style.normal.textColor = new Color(1f, 0.8f, 0f, 1f); // Gold color
            GUI.Label(new Rect(10, yPos, 300, 30), $"Threat: {threat:F1}", style);
            yPos += lineHeight;
        }
        
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
            // Use a downward raycast from the bottom of the CharacterController
            // to check what surface we're standing on
            Vector3 rayStart = transform.position + characterController.center - new Vector3(0, characterController.height / 2f, 0);
            RaycastHit hit;
            float rayDistance = 0.2f; // Small distance to check what's directly below
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
                    // Only set to false if we were in lava (to trigger burn)
                    if (healthSystem != null && healthSystem.GetIsInLava())
                    {
                        healthSystem.SetInLava(false);
                    }
                }
            }
            else
            {
                // Not grounded on anything, set to false if was in lava
                if (healthSystem != null && healthSystem.GetIsInLava())
                {
                    healthSystem.SetInLava(false);
                }
            }
        }
        
        // Get input - A/D for rotation, W/S for forward/backward
        float rotateInput = Input.GetAxis("Horizontal"); // A/D keys
        float moveInput = Input.GetAxis("Vertical"); // W/S keys
        
        // Rotate with A/D keys
        if (Mathf.Abs(rotateInput) >= 0.1f)
        {
            float rotationAmount = rotateInput * rotationSpeed * Time.deltaTime;
            transform.Rotate(0f, rotationAmount, 0f);
        }
        
        // Move forward/backward with W/S keys (relative to facing direction)
        if (Mathf.Abs(moveInput) >= 0.1f)
        {
            Vector3 moveDirection = transform.forward * moveInput; // Forward is the direction the character is facing
            characterController.Move(moveDirection * moveSpeed * Time.deltaTime);
        }
        
        // Apply gravity
        velocity.y += gravity * Time.deltaTime;
        characterController.Move(velocity * Time.deltaTime);
    }
}


*/