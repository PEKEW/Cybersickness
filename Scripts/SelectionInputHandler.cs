using System;
using UnityEngine;
using Valve.VR.Extras;

/// <summary>
/// Handles input for the selection task
/// </summary>
[RequireComponent(typeof(SelectMod))]
public class SelectionInputHandler : MonoBehaviour
{
    #region Private Fields
    
    // Input references
    private SteamVR_LaserPointer m_LaserPointer;
    
    // Input state
    private bool m_IsInitialized;
    
    #endregion
    
    #region Events
    
    /// <summary>
    /// Event triggered when a target is selected
    /// </summary>
    public event Action<GameObject> OnTargetSelected;
    
    #endregion
    
    #region Public Methods
    
    /// <summary>
    /// Initializes the input handler
    /// </summary>
    /// <param name="_laserPointer">The laser pointer reference</param>
    public void Initialize(SteamVR_LaserPointer _laserPointer)
    {
        m_LaserPointer = _laserPointer;
        
        if (m_LaserPointer == null)
        {
            Debug.LogError($"[{nameof(SelectionInputHandler)}] Laser pointer is null!");
            return;
        }
        
        // Subscribe to the laser pointer events
        m_LaserPointer.PointerClick += HandlePointerClick;
        
        // Enable keyboard input for debugging
        EnableKeyboardInput();
        
        m_IsInitialized = true;
    }
    
    /// <summary>
    /// Cleans up the input handler
    /// </summary>
    public void Cleanup()
    {
        if (m_LaserPointer != null)
        {
            m_LaserPointer.PointerClick -= HandlePointerClick;
        }
        
        m_IsInitialized = false;
    }
    
    #endregion
    
    #region Private Methods
    
    /// <summary>
    /// Handles the pointer click event
    /// </summary>
    /// <param name="_sender">The event sender</param>
    /// <param name="_eventArgs">The event arguments</param>
    private void HandlePointerClick(object _sender, PointerEventArgs _eventArgs)
    {
        if (_eventArgs.target == null)
        {
            return;
        }
        
        // Notify that a target was selected
        OnTargetSelected?.Invoke(_eventArgs.target.gameObject);
    }
    
    /// <summary>
    /// Enables keyboard input for debugging
    /// </summary>
    private void EnableKeyboardInput()
    {
        // This is handled in the Update method
    }
    
    #endregion
    
    #region Unity Lifecycle
    
    private void Update()
    {
        // Debug: Allow using backspace key to trigger selection
        if (Input.GetKeyDown(KeyCode.Backspace))
        {
            // Get the current target from the SelectMod
            SelectMod selectMod = GetComponent<SelectMod>();
            if (selectMod != null && selectMod.CurrentTarget != null)
            {
                OnTargetSelected?.Invoke(selectMod.CurrentTarget.gameObject);
            }
        }
    }
    
    private void OnDestroy()
    {
        Cleanup();
    }
    
    #endregion
} 