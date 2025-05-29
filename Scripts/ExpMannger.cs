using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using Valve.VR;
// TODO
using LSL;

/// <summary>
/// Manages the experiment flow, stages, and data collection
/// </summary>
[ExecuteAlways]
[DisallowMultipleComponent]
[RequireComponent(typeof(SelectMod), typeof(ManiMod))]
[AddComponentMenu("Experiment/Experiment Manager")]
public class ExpMannger : MonoBehaviour
{
    #region Serialized Fields

    [Header("Experiment Timing Settings")]
    [Tooltip("Duration of the mindfulness training phase in seconds")]
    [SerializeField, Range(10.0f, 300.0f)] private float m_MindfulnessTime = 30.0f;
    
    [Tooltip("Rest duration between different experiment tasks")]
    [SerializeField, Range(10.0f, 300.0f)] private float m_RestTime = 30.0f;
    
    [Header("Task Configuration")]
    [Tooltip("Enable the visit task")]
    [SerializeField] private bool m_IsVisit = true;
    
    [Tooltip("Enable the selection task")]
    [SerializeField] private bool m_IsSelect = true;
    
    [Tooltip("Enable the manipulation task")]
    [SerializeField] private bool m_IsMani = true;
    
    [Header("UI References")]
    [Tooltip("The mask GameObject used for displaying instructions")]
    [SerializeField] private GameObject m_Mask = null;
    
    [Tooltip("Text component for displaying instructions")]
    [SerializeField] private TMP_Text m_MaskInfo = null;
    
    [Header("Input Configuration")]
    [Tooltip("SteamVR action for triggering experiment events")]
    [SerializeField] private SteamVR_Action_Boolean m_XTrigger = null;
    
    [Header("Environment References")]
    [Tooltip("The path object to be activated during visit task")]
    [SerializeField] private GameObject m_PathObj = null;
    
    [Tooltip("Visual marker for cybersickness indication")]
    [SerializeField] private GameObject m_SicknessViewMarker = null;

    [Header("EEG Configuration")]
    [Tooltip("SteamVR action for marking sickness events")]
    [SerializeField] private SteamVR_Action_Boolean m_SicknessMarker = null;
    
    [Tooltip("Duration to display the sickness marker")]
    [SerializeField, Range(0.1f, 5.0f)] private float m_MarkerTime = 0.5f;

    #endregion

    #region Private Fields
    
    // Task controllers
    private SelectMod m_SelectController = null;
    private ManiMod m_ManiController = null;
    
    // State tracking
    private bool m_IsExpStart = false;
    private bool m_IsExpEnd = false;
    
    // Performance optimizations
    private StringBuilder m_TimerDisplay = null;
    private Coroutine m_ActiveTaskCoroutine = null;
    
    // Task timing data
    private float m_TaskStartTime = 0f;
    private readonly Dictionary<string, float> m_TaskDurations = new Dictionary<string, float>();

    // Component references
    private ExperimentMarkerSystem m_MarkerSystem;
    private ExperimentTaskManager m_TaskManager;
    private ExperimentUIController m_UIController;

    #endregion

    #region Public Properties
    
    /// <summary>
    /// Gets or sets the duration of the mindfulness training phase
    /// </summary>
    public float MindfulnessTime
    {
        get => m_MindfulnessTime;
        set => m_MindfulnessTime = Mathf.Clamp(value, 10.0f, 300.0f);
    }
    
    /// <summary>
    /// Gets or sets the duration of rest periods between tasks
    /// </summary>
    public float RestTime
    {
        get => m_RestTime;
        set => m_RestTime = Mathf.Clamp(value, 10.0f, 300.0f);
    }
    
    /// <summary>
    /// Gets whether the experiment is currently running
    /// </summary>
    public bool IsExperimentRunning => m_IsExpStart && !m_IsExpEnd;
    
    /// <summary>
    /// Gets or sets whether the visit task is enabled
    /// </summary>
    public bool IsVisitEnabled
    {
        get => m_IsVisit;
        set => m_IsVisit = value;
    }
    
    /// <summary>
    /// Gets or sets whether the select task is enabled
    /// </summary>
    public bool IsSelectEnabled
    {
        get => m_IsSelect;
        set => m_IsSelect = value;
    }
    
    /// <summary>
    /// Gets or sets whether the manipulation task is enabled
    /// </summary>
    public bool IsManipulationEnabled
    {
        get => m_IsMani;
        set => m_IsMani = value;
    }

    #endregion

    #region Events
    
    /// <summary>
    /// Event triggered when a task begins
    /// </summary>
    public event Action<string> OnTaskBegin;
    
    /// <summary>
    /// Event triggered when a task ends
    /// </summary>
    public event Action<string, float> OnTaskEnd;
    
    /// <summary>
    /// Event triggered when the entire experiment ends
    /// </summary>
    public event Action OnExperimentComplete;

    #endregion

    #region Unity Lifecycle

    private void Awake()
    {
        // Component retrieval and validation
        TryGetRequiredComponents();
        
        // Initialize supporting components
        InitializeComponents();
        
        // Initialize timer display
        m_TimerDisplay = new StringBuilder(100);
        
        // Verify required dependencies
        ValidateRequiredReferences();
    }

    private void OnValidate()
    {
        // Clamp values when changed in inspector
        m_MindfulnessTime = Mathf.Clamp(m_MindfulnessTime, 10.0f, 300.0f);
        m_RestTime = Mathf.Clamp(m_RestTime, 10.0f, 300.0f);
        m_MarkerTime = Mathf.Clamp(m_MarkerTime, 0.1f, 5.0f);
    }

    private void OnEnable()
    {
        // Subscribe to events
        if (m_SelectController != null)
        {
            m_SelectController.enabled = false;
        }
        
        if (m_ManiController != null)
        {
            m_ManiController.enabled = false;
        }
        
        if (m_MarkerSystem != null)
        {
            m_MarkerSystem.OnSicknessMarked += HandleSicknessMarked;
        }
        
        if (m_TaskManager != null)
        {
            m_TaskManager.OnTaskBegin += HandleTaskBegin;
            m_TaskManager.OnTaskEnd += HandleTaskEnd;
        }
    }

    private void OnDisable()
    {
        // Unsubscribe from events and clean up
        StopAllCoroutines();
        m_ActiveTaskCoroutine = null;
        
        if (m_MarkerSystem != null)
        {
            m_MarkerSystem.OnSicknessMarked -= HandleSicknessMarked;
        }
        
        if (m_TaskManager != null)
        {
            m_TaskManager.OnTaskBegin -= HandleTaskBegin;
            m_TaskManager.OnTaskEnd -= HandleTaskEnd;
        }
    }

    private void Start()
    {
        // Initialize experiment
        InitializeExperiment();
    }

    private void Update()
    {
        // Check EEG markers
        m_MarkerSystem.ProcessEEGMarkers(m_IsExpStart, m_IsExpEnd);
        
        // Only check for experiment start if not already started
        if (!m_IsExpStart)
        {
            CheckExperimentStart();
        }

        // Check for sickness marker
        m_MarkerSystem.CheckSicknessMarker();
    }

    private void OnDestroy()
    {
        // Clean up resources
        m_TimerDisplay = null;
        
        // Destroy child components
        if (m_MarkerSystem != null)
        {
            Destroy(m_MarkerSystem);
        }
        
        if (m_TaskManager != null)
        {
            Destroy(m_TaskManager);
        }
        
        if (m_UIController != null)
        {
            Destroy(m_UIController);
        }
    }

    #endregion

    #region Initialization Methods

    /// <summary>
    /// Tries to get all required components and logs errors if any are missing
    /// </summary>
    private void TryGetRequiredComponents()
    {
        if (!TryGetComponent(out m_SelectController))
        {
            Debug.LogError($"[{nameof(ExpMannger)}] Required component {nameof(SelectMod)} not found!");
        }
        
        if (!TryGetComponent(out m_ManiController))
        {
            Debug.LogError($"[{nameof(ExpMannger)}] Required component {nameof(ManiMod)} not found!");
        }
    }

    /// <summary>
    /// Initializes supporting components
    /// </summary>
    private void InitializeComponents()
    {
        // Create marker system
        m_MarkerSystem = gameObject.AddComponent<ExperimentMarkerSystem>();
        m_MarkerSystem.Initialize(m_SicknessViewMarker, m_SicknessMarker, m_MarkerTime);
        
        // Create task manager
        m_TaskManager = gameObject.AddComponent<ExperimentTaskManager>();
        m_TaskManager.Initialize(m_PathObj, m_SelectController, m_ManiController);
        
        // Create UI controller
        m_UIController = gameObject.AddComponent<ExperimentUIController>();
        m_UIController.Initialize(m_Mask, m_MaskInfo);
    }

    /// <summary>
    /// Validates that all required references are set
    /// </summary>
    private void ValidateRequiredReferences()
    {
        if (m_Mask == null)
        {
            Debug.LogError($"[{nameof(ExpMannger)}] Mask GameObject reference is not set!");
        }
        
        if (m_MaskInfo == null)
        {
            Debug.LogError($"[{nameof(ExpMannger)}] MaskInfo TMP_Text reference is not set!");
        }
        
        if (m_XTrigger == null)
        {
            Debug.LogError($"[{nameof(ExpMannger)}] XTrigger SteamVR_Action_Boolean reference is not set!");
        }
        
        if (m_PathObj == null)
        {
            Debug.LogError($"[{nameof(ExpMannger)}] PathObj GameObject reference is not set!");
        }
        
        if (m_SicknessViewMarker == null)
        {
            Debug.LogError($"[{nameof(ExpMannger)}] SicknessViewMarker GameObject reference is not set!");
        }
        
        if (m_SicknessMarker == null)
        {
            Debug.LogError($"[{nameof(ExpMannger)}] SicknessMarker SteamVR_Action_Boolean reference is not set!");
        }
    }

    /// <summary>
    /// Initializes the experiment state
    /// </summary>
    private void InitializeExperiment()
    {
        m_SicknessViewMarker.SetActive(false);
        m_UIController.ShowStartPrompt();
        m_PathObj.SetActive(false);
        
        // Initialize marker system
        m_MarkerSystem.ConfigureLSLStream();
    }

    #endregion

    #region Event Handlers

    /// <summary>
    /// Handles sickness marker events
    /// </summary>
    private void HandleSicknessMarked()
    {
        Debug.Log($"[{nameof(ExpMannger)}] Sickness marker recorded");
    }
    
    /// <summary>
    /// Handles task begin events
    /// </summary>
    private void HandleTaskBegin(string _taskName)
    {
        m_TaskStartTime = Time.time;
        OnTaskBegin?.Invoke(_taskName);
    }
    
    /// <summary>
    /// Handles task end events
    /// </summary>
    private void HandleTaskEnd(string _taskName)
    {
        float duration = Time.time - m_TaskStartTime;
        m_TaskDurations[_taskName] = duration;
        OnTaskEnd?.Invoke(_taskName, duration);
    }

    #endregion

    #region Input Handling

    /// <summary>
    /// Checks if the experiment start trigger has been activated
    /// </summary>
    private void CheckExperimentStart()
    {
        if (m_XTrigger.GetState(SteamVR_Input_Sources.RightHand))
        {
            m_IsExpStart = true;
            m_ActiveTaskCoroutine = StartCoroutine(ExecuteExperimentSequence());
        }
    }

    #endregion

    #region Experiment Flow

    /// <summary>
    /// Executes the full experiment sequence
    /// </summary>
    private IEnumerator ExecuteExperimentSequence()
    {
        // Mindfulness phase
        m_MarkerSystem.RecordMarker("MindfulnessBegin");
        yield return StartCoroutine(m_TaskManager.ExecuteMindfulnessPhase(m_MindfulnessTime, m_UIController));
        m_MarkerSystem.RecordMarker("MindfulnessEnd");
        
        // Visit task (if enabled)
        if (m_IsVisit)
        {
            m_MarkerSystem.RecordMarker("RestBegin");
            yield return StartCoroutine(m_TaskManager.ExecuteRestPhase(m_RestTime, "next : Visit", m_UIController));
            m_MarkerSystem.RecordMarker("RestEnd");
            
            m_MarkerSystem.RecordMarker("VisitBegin");
            yield return StartCoroutine(m_TaskManager.ExecuteVisitTask(m_UIController));
            m_MarkerSystem.RecordMarker("VisitEnd");
        }
        
        // Select task (if enabled)
        if (m_IsSelect)
        {
            m_MarkerSystem.RecordMarker("RestBegin");
            yield return StartCoroutine(m_TaskManager.ExecuteRestPhase(m_RestTime, "next : Select", m_UIController));
            m_MarkerSystem.RecordMarker("RestEnd");
            
            m_MarkerSystem.RecordMarker("SelectBegin");
            yield return StartCoroutine(m_TaskManager.ExecuteSelectTask(m_UIController));
            m_MarkerSystem.RecordMarker("SelectEnd");
        }
        
        // Manipulation task (if enabled)
        if (m_IsMani)
        {
            m_MarkerSystem.RecordMarker("RestBegin");
            yield return StartCoroutine(m_TaskManager.ExecuteRestPhase(m_RestTime, "next : Manipulation", m_UIController));
            m_MarkerSystem.RecordMarker("RestEnd");
            
            m_MarkerSystem.RecordMarker("ManiBegin");
            yield return StartCoroutine(m_TaskManager.ExecuteManipulationTask(m_UIController));
            m_MarkerSystem.RecordMarker("ManiEnd");
            
            // Final phase - experiment completion
            yield return StartCoroutine(CompleteExperiment());
        }
    }

    /// <summary>
    /// Completes the experiment and waits for user confirmation to exit
    /// </summary>
    private IEnumerator CompleteExperiment()
    {
        m_UIController.ShowExitPrompt();
        m_IsExpEnd = true;
        
        bool isConfirmed = false;
        while (!isConfirmed)
        {
            if (m_XTrigger.GetState(SteamVR_Input_Sources.RightHand))
            {
                isConfirmed = true;
                
                // Log experiment summary
                LogExperimentSummary();
                
                // Notify experiment completion
                OnExperimentComplete?.Invoke();
            }
            yield return null;
        }
    }

    /// <summary>
    /// Logs a summary of the experiment to the console
    /// </summary>
    private void LogExperimentSummary()
    {
        StringBuilder summary = new StringBuilder();
        summary.AppendLine("=== Experiment Summary ===");
        
        foreach (var taskDuration in m_TaskDurations)
        {
            summary.AppendLine($"{taskDuration.Key}: {taskDuration.Value:F2}s");
        }
        
        summary.AppendLine("========================");
        Debug.Log(summary.ToString());
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Forces the experiment to start programmatically
    /// </summary>
    public void StartExperiment()
    {
        if (!m_IsExpStart)
        {
            m_IsExpStart = true;
            StartCoroutine(ExecuteExperimentSequence());
        }
        else
        {
            Debug.LogWarning("[ExpMannger] Experiment is already running!");
        }
    }

    /// <summary>
    /// Forces a sickness marker to be recorded programmatically
    /// </summary>
    public void ForceSicknessMarker()
    {
        m_MarkerSystem.ForceSicknessMarker();
    }

    /// <summary>
    /// Gets the duration of a completed task
    /// </summary>
    /// <param name="_taskName">The name of the task</param>
    /// <returns>The duration of the task in seconds, or -1 if not found</returns>
    public float GetTaskDuration(string _taskName)
    {
        if (m_TaskDurations.TryGetValue(_taskName, out float duration))
        {
            return duration;
        }
        
        return -1f;
    }

    /// <summary>
    /// Gets a dictionary of all completed task durations
    /// </summary>
    /// <returns>A dictionary of task names and their durations</returns>
    public IReadOnlyDictionary<string, float> GetAllTaskDurations()
    {
        return m_TaskDurations;
    }

    #endregion

    #if UNITY_EDITOR
    [ContextMenu("Debug - Force Start Experiment")]
    private void DebugForceStart()
    {
        StartExperiment();
    }

    [ContextMenu("Debug - Force Sickness Marker")]
    private void DebugForceSicknessMarker()
    {
        ForceSicknessMarker();
    }
    
    [ContextMenu("Debug - Log Task Durations")]
    private void DebugLogTaskDurations()
    {
        LogExperimentSummary();
    }
    #endif
}
  
