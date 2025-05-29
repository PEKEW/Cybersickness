using System;
using UnityEngine;
using Valve.VR.Extras;

/// <summary>
/// Controls the selection task in the experiment
/// </summary>
[DisallowMultipleComponent]
[AddComponentMenu("Experiment/Selection Task Controller")]
public class SelectMod : MonoBehaviour
{
    #region Serialized Fields

    [Header("References")]
    [Tooltip("The set of objects that can be selected")]
    [SerializeField] private GameObject m_SelectableObjSet;
    
    [Tooltip("The player object for line drawing")]
    [SerializeField] private GameObject m_Player;
    
    [Tooltip("The line renderer for guidance")]
    [SerializeField] private LineRenderer m_LineRenderer;
    
    [Tooltip("The laser pointer for selection")]
    [SerializeField] private SteamVR_LaserPointer m_Laser;
    
    [Header("Configuration")]
    [Tooltip("Number of objects to select before completion")]
    [SerializeField, Range(1, 20)] private int m_RequiredSelectionCount = 5;

    #endregion
    
    #region Private Fields
    
    // Component references
    private SelectionTargetManager m_TargetManager;
    private SelectionVisualManager m_VisualManager;
    private SelectionInputHandler m_InputHandler;
    
    #endregion

    #region Public Events

    /// <summary>
    /// Event triggered when all required selections are completed
    /// </summary>
    public event Action onSelectEvent;

    #endregion

    #region Public Properties

    /// <summary>
    /// Gets the remaining number of objects to select
    /// </summary>
    public int RemainingSelections => m_TargetManager?.RemainingSelections ?? m_RequiredSelectionCount;
    
    /// <summary>
    /// Gets the current target transform
    /// </summary>
    public Transform CurrentTarget => m_TargetManager?.CurrentTarget;
    
    /// <summary>
    /// Gets the player transform
    /// </summary>
    public Transform PlayerTransform => m_Player?.transform;

    #endregion

    #region Unity Lifecycle

    private void Awake()
    {
        // Task is disabled by default until activated by the experiment manager
        enabled = false;
        
        // Create sub-components
        InitializeComponents();
    }

    private void Start()
    {
        ValidateReferences();
        
        if (isActiveAndEnabled)
        {
            StartSelectionTask();
        }
    }

    private void OnEnable()
    {
        // Subscribe to events
        if (m_InputHandler != null)
        {
            m_InputHandler.OnTargetSelected += HandleTargetSelected;
        }
        
        if (m_LineRenderer != null)
        {
            m_LineRenderer.enabled = true;
        }
        
        // Start the task if already initialized
        if (m_TargetManager != null && m_TargetManager.IsInitialized)
        {
            StartSelectionTask();
        }
    }

    private void OnDisable()
    {
        // Unsubscribe from events
        if (m_InputHandler != null)
        {
            m_InputHandler.OnTargetSelected -= HandleTargetSelected;
        }
        
        if (m_LineRenderer != null)
        {
            m_LineRenderer.enabled = false;
        }
    }

    private void Update()
    {
        if (m_TargetManager != null && m_TargetManager.CurrentTarget != null)
        {
            UpdateGuidanceLine();
        }
    }

    private void OnDestroy()
    {
        // Clean up sub-components
        if (m_TargetManager != null)
        {
            Destroy(m_TargetManager);
        }
        
        if (m_VisualManager != null)
        {
            Destroy(m_VisualManager);
        }
        
        if (m_InputHandler != null)
        {
            Destroy(m_InputHandler);
        }
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// Initializes the sub-components
    /// </summary>
    private void InitializeComponents()
    {
        // Create target manager
        m_TargetManager = gameObject.AddComponent<SelectionTargetManager>();
        
        // Create visual manager
        m_VisualManager = gameObject.AddComponent<SelectionVisualManager>();
        
        // Create input handler
        m_InputHandler = gameObject.AddComponent<SelectionInputHandler>();
    }

    /// <summary>
    /// Validates that all required references are set
    /// </summary>
    private void ValidateReferences()
    {
        if (m_SelectableObjSet == null)
        {
            Debug.LogError($"[{nameof(SelectMod)}] Selectable object set is not assigned!");
        }
        
        if (m_Player == null)
        {
            Debug.LogError($"[{nameof(SelectMod)}] Player reference is not assigned!");
        }
        
        if (m_LineRenderer == null)
        {
            Debug.LogError($"[{nameof(SelectMod)}] Line renderer is not assigned!");
        }
        
        if (m_Laser == null)
        {
            Debug.LogError($"[{nameof(SelectMod)}] Laser pointer is not assigned!");
        }
    }

    /// <summary>
    /// Starts the selection task
    /// </summary>
    private void StartSelectionTask()
    {
        if (m_SelectableObjSet != null)
        {
            m_SelectableObjSet.SetActive(true);
        }
        
        // Initialize the target manager
        if (m_TargetManager != null)
        {
            m_TargetManager.Initialize(m_SelectableObjSet.transform, m_RequiredSelectionCount);
        }
        
        // Initialize the visual manager
        if (m_VisualManager != null)
        {
            m_VisualManager.Initialize();
        }
        
        // Initialize the input handler
        if (m_InputHandler != null)
        {
            m_InputHandler.Initialize(m_Laser);
        }
    }

    /// <summary>
    /// Updates the guidance line between player and target
    /// </summary>
    private void UpdateGuidanceLine()
    {
        if (m_LineRenderer != null && m_Player != null && m_TargetManager.CurrentTarget != null)
        {
            m_LineRenderer.SetPosition(0, m_TargetManager.CurrentTarget.position);
            m_LineRenderer.SetPosition(1, m_Player.transform.position);
        }
    }

    /// <summary>
    /// Handles the target selected event
    /// </summary>
    /// <param name="_selectedTarget">The selected target GameObject</param>
    private void HandleTargetSelected(GameObject _selectedTarget)
    {
        if (m_TargetManager != null && _selectedTarget == m_TargetManager.CurrentTarget.gameObject)
        {
            // Apply visual feedback
            if (m_VisualManager != null)
            {
                m_VisualManager.ApplySelectionEffect(_selectedTarget.GetComponent<Renderer>());
            }
            
            // Update the target
            bool isTaskComplete = m_TargetManager.SelectNextTarget();
            
            // Check if task is complete
            if (isTaskComplete)
            {
                onSelectEvent?.Invoke();
            }
        }
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Manually selects the current target (for debugging or testing)
    /// </summary>
    public void ManuallySelectCurrentTarget()
    {
        if (m_TargetManager != null && m_TargetManager.CurrentTarget != null)
        {
            HandleTargetSelected(m_TargetManager.CurrentTarget.gameObject);
        }
    }

    /// <summary>
    /// Resets the selection task
    /// </summary>
    public void ResetTask()
    {
        if (m_TargetManager != null)
        {
            m_TargetManager.Initialize(m_SelectableObjSet.transform, m_RequiredSelectionCount);
        }
    }

    #endregion

    #if UNITY_EDITOR
    [ContextMenu("Debug - Force Select Current Target")]
    private void DebugSelectTarget()
    {
        ManuallySelectCurrentTarget();
    }
    
    [ContextMenu("Debug - Reset Selection Task")]
    private void DebugResetTask()
    {
        ResetTask();
    }
    #endif
}
