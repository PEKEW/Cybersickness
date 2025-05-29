using UnityEngine;

/// <summary>
/// Calculates player movement direction based on controller input and camera orientation
/// </summary>
[RequireComponent(typeof(PlayerMovement))]
public class PlayerMovementCalculator : MonoBehaviour
{
    #region Private Fields
    
    // References
    private Transform m_CameraTransform;
    
    // Movement calculation
    private Vector3 m_ForwardDirection;
    private Vector3 m_RightDirection;
    private Vector3 m_MovementDirection;
    
    // State tracking
    private bool m_IsInitialized;
    
    #endregion
    
    #region Unity Lifecycle
    
    private void OnEnable()
    {
        if (m_CameraTransform == null)
        {
            Debug.LogWarning($"[{nameof(PlayerMovementCalculator)}] Camera transform not set!");
        }
    }
    
    #endregion
    
    #region Public Methods
    
    /// <summary>
    /// Initializes the movement calculator with required references
    /// </summary>
    /// <param name="_cameraTransform">The camera transform for direction reference</param>
    public void Initialize(Transform _cameraTransform)
    {
        m_CameraTransform = _cameraTransform;
        
        if (m_CameraTransform == null)
        {
            Debug.LogError($"[{nameof(PlayerMovementCalculator)}] Camera transform is null!");
            return;
        }
        
        m_IsInitialized = true;
    }
    
    /// <summary>
    /// Calculates the movement direction based on joystick input
    /// </summary>
    /// <param name="_joystickInput">The joystick input vector</param>
    /// <returns>The calculated movement direction in world space</returns>
    public Vector3 CalculateMovementDirection(Vector2 _joystickInput)
    {
        if (!m_IsInitialized || m_CameraTransform == null)
        {
            return Vector3.zero;
        }
        
        // Calculate forward and right directions relative to camera
        CalculateMovementAxes();
        
        // Combine directions based on joystick input
        m_MovementDirection = (m_ForwardDirection * _joystickInput.y + m_RightDirection * _joystickInput.x).normalized;
        
        return m_MovementDirection;
    }
    
    #endregion
    
    #region Private Methods
    
    /// <summary>
    /// Calculates the forward and right movement axes based on camera orientation
    /// </summary>
    private void CalculateMovementAxes()
    {
        // Project camera forward and right onto the horizontal plane and normalize
        m_ForwardDirection = Vector3.ProjectOnPlane(m_CameraTransform.forward, Vector3.up).normalized;
        m_RightDirection = Vector3.ProjectOnPlane(m_CameraTransform.right, Vector3.up).normalized;
    }
    
    /// <summary>
    /// Gets the camera relative forward direction
    /// </summary>
    /// <returns>The forward direction vector</returns>
    public Vector3 GetForwardDirection()
    {
        CalculateMovementAxes();
        return m_ForwardDirection;
    }
    
    /// <summary>
    /// Gets the camera relative right direction
    /// </summary>
    /// <returns>The right direction vector</returns>
    public Vector3 GetRightDirection()
    {
        CalculateMovementAxes();
        return m_RightDirection;
    }
    
    #endregion
    
    #if UNITY_EDITOR
    [ContextMenu("Debug - Print Movement Axes")]
    private void DebugPrintMovementAxes()
    {
        if (m_CameraTransform == null)
        {
            Debug.LogWarning("Camera transform not set!");
            return;
        }
        
        CalculateMovementAxes();
        Debug.Log($"Forward Direction: {m_ForwardDirection}\nRight Direction: {m_RightDirection}");
    }
    #endif
} 