using UnityEngine;
using Valve.VR;

/// <summary>
/// Handles player input from VR controllers
/// </summary>
[RequireComponent(typeof(PlayerMovement))]
public class PlayerInputHandler : MonoBehaviour
{
    #region Private Fields
    
    // Input references
    private SteamVR_Action_Vector2 m_JoystickAction;
    
    // Input data
    private Vector2 m_CurrentJoystickInput;
    
    // State tracking
    private bool m_IsInitialized;
    
    // Configuration
    private const float c_DeadZone = 0.1f;
    
    #endregion
    
    #region Unity Lifecycle
    
    private void Update()
    {
        if (!m_IsInitialized || m_JoystickAction == null)
        {
            return;
        }
        
        // Update joystick input
        UpdateJoystickInput();
    }
    
    #endregion
    
    #region Public Methods
    
    /// <summary>
    /// Initializes the input handler with required references
    /// </summary>
    /// <param name="_joystickAction">The SteamVR joystick action reference</param>
    public void Initialize(SteamVR_Action_Vector2 _joystickAction)
    {
        m_JoystickAction = _joystickAction;
        
        if (m_JoystickAction == null)
        {
            Debug.LogError($"[{nameof(PlayerInputHandler)}] Joystick action is null!");
            return;
        }
        
        m_IsInitialized = true;
    }
    
    /// <summary>
    /// Gets the current joystick input vector
    /// </summary>
    /// <returns>The joystick input vector</returns>
    public Vector2 GetJoystickInput()
    {
        return m_CurrentJoystickInput;
    }
    
    /// <summary>
    /// Gets whether there is active joystick input above the deadzone
    /// </summary>
    /// <returns>True if there is significant joystick input</returns>
    public bool HasSignificantJoystickInput()
    {
        return m_CurrentJoystickInput.magnitude > c_DeadZone;
    }
    
    #endregion
    
    #region Private Methods
    
    /// <summary>
    /// Updates the joystick input from the SteamVR action
    /// </summary>
    private void UpdateJoystickInput()
    {
        // Get raw input from the right hand controller
        Vector2 rawInput = m_JoystickAction.GetAxis(SteamVR_Input_Sources.RightHand);
        
        // Apply deadzone
        if (rawInput.magnitude <= c_DeadZone)
        {
            m_CurrentJoystickInput = Vector2.zero;
        }
        else
        {
            // Normalize to get consistent speed regardless of how far the joystick is pushed
            m_CurrentJoystickInput = rawInput.normalized * Mathf.InverseLerp(c_DeadZone, 1.0f, rawInput.magnitude);
        }
    }
    
    #endregion
    
    #if UNITY_EDITOR
    [ContextMenu("Debug - Print Current Joystick Input")]
    private void DebugPrintJoystickInput()
    {
        Debug.Log($"Current joystick input: {m_CurrentJoystickInput} (magnitude: {m_CurrentJoystickInput.magnitude})");
    }
    #endif
} 