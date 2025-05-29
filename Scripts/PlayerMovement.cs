using UnityEngine;
using Valve.VR;

/// <summary>
/// Controls player movement based on VR controller input
/// </summary>
[DisallowMultipleComponent]
[AddComponentMenu("Player/Player Movement Controller")]
public class PlayerMovement : MonoBehaviour
{
    #region Serialized Fields
    
    [Header("Input Configuration")]
    [Tooltip("SteamVR action for joystick input")]
    [SerializeField] private SteamVR_Action_Vector2 m_JoystickAction;
    
    [Header("Movement Settings")]
    [Tooltip("Player movement speed in units per second")]
    [SerializeField, Range(0.1f, 10.0f)] private float m_PlayerMoveSpeed = 1.0f;
    
    [Header("References")]
    [Tooltip("The main camera transform for directional movement")]
    [SerializeField] private Transform m_MainCameraTrans;
    
    #endregion
    
    #region Private Fields
    
    // Component references
    private PlayerInputHandler m_InputHandler;
    private PlayerMovementCalculator m_MovementCalculator;
    
    #endregion
    
    #region Unity Lifecycle
    
    private void Awake()
    {
        // Create and initialize sub-components
        InitializeComponents();
    }
    
    private void Start()
    {
        ValidateReferences();
    }
    
    private void Update()
    {
        // Get joystick input
        Vector2 joystickInput = m_InputHandler.GetJoystickInput();
        
        // Only process movement if there's significant input
        if (joystickInput.magnitude > 0.1f)
        {
            // Calculate movement direction and apply it
            Vector3 movementDirection = m_MovementCalculator.CalculateMovementDirection(joystickInput);
            ApplyMovement(movementDirection);
        }
    }
    
    private void OnDestroy()
    {
        // Clean up sub-components
        if (m_InputHandler != null)
        {
            Destroy(m_InputHandler);
        }
        
        if (m_MovementCalculator != null)
        {
            Destroy(m_MovementCalculator);
        }
    }
    
    #endregion
    
    #region Private Methods
    
    /// <summary>
    /// Initializes the component dependencies
    /// </summary>
    private void InitializeComponents()
    {
        // Create input handler
        m_InputHandler = gameObject.AddComponent<PlayerInputHandler>();
        m_InputHandler.Initialize(m_JoystickAction);
        
        // Create movement calculator
        m_MovementCalculator = gameObject.AddComponent<PlayerMovementCalculator>();
        m_MovementCalculator.Initialize(m_MainCameraTrans);
    }
    
    /// <summary>
    /// Validates required references
    /// </summary>
    private void ValidateReferences()
    {
        if (m_JoystickAction == null)
        {
            Debug.LogError($"[{nameof(PlayerMovement)}] Joystick action is not assigned!");
        }
        
        if (m_MainCameraTrans == null)
        {
            Debug.LogError($"[{nameof(PlayerMovement)}] Main camera transform is not assigned!");
        }
    }
    
    /// <summary>
    /// Applies movement to the player's position
    /// </summary>
    /// <param name="_direction">The normalized direction vector</param>
    private void ApplyMovement(Vector3 _direction)
    {
        transform.position += m_PlayerMoveSpeed * Time.deltaTime * _direction;
    }
    
    #endregion
    
    #region Public Methods
    
    /// <summary>
    /// Gets the current movement speed
    /// </summary>
    public float GetMovementSpeed()
    {
        return m_PlayerMoveSpeed;
    }
    
    /// <summary>
    /// Sets the movement speed
    /// </summary>
    /// <param name="_speed">The new speed value</param>
    public void SetMovementSpeed(float _speed)
    {
        m_PlayerMoveSpeed = Mathf.Clamp(_speed, 0.1f, 10.0f);
    }
    
    #endregion
    
    #if UNITY_EDITOR
    [ContextMenu("Debug - Double Movement Speed")]
    private void DebugDoubleSpeed()
    {
        SetMovementSpeed(m_PlayerMoveSpeed * 2.0f);
        Debug.Log($"Movement speed doubled to {m_PlayerMoveSpeed}");
    }
    
    [ContextMenu("Debug - Reset Movement Speed")]
    private void DebugResetSpeed()
    {
        SetMovementSpeed(1.0f);
        Debug.Log("Movement speed reset to default");
    }
    #endif
}
