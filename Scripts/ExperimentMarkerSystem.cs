using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;
// TODO
using LSL;

/// <summary>
/// Handles EEG markers and cybersickness indicators during experiments
/// </summary>
[RequireComponent(typeof(ExpMannger))]
public class ExperimentMarkerSystem : MonoBehaviour
{
    #region Private Fields

    private const string c_StreamName = "EEGinUnity";
    private const string c_StreamType = "Markers";
    private static readonly int c_MaxLSLChannels = 1;
    
    // References
    private GameObject m_SicknessViewMarker;
    private SteamVR_Action_Boolean m_SicknessMarker;
    private float m_MarkerTime;
    
    // State tracking
    private bool m_MarkerClock;
    private bool m_IsEEGRecord;
    private bool m_FinishEEGRecord;
    
    // LSL streaming
    // TODO
    // private StreamOutlet m_Outlet;
    private readonly string[] m_Sample = {""};
    
    // Action processing
    private readonly Dictionary<string, Action> m_MarkerActions = new Dictionary<string, Action>();
    private readonly Queue<string> m_MarkerQueue = new Queue<string>();
    
    // Coroutines
    private Coroutine m_MarkerDisplayCoroutine;
    
    // Cached objects
    private WaitForSeconds m_MarkerWait;

    #endregion

    #region Events

    /// <summary>
    /// Event triggered when a sickness marker is recorded
    /// </summary>
    public event Action OnSicknessMarked;

    #endregion

    #region Public Methods

    /// <summary>
    /// Initializes the marker system with required references
    /// </summary>
    /// <param name="_sicknessViewMarker">The marker visual GameObject</param>
    /// <param name="_sicknessMarker">The SteamVR action for sickness marking</param>
    /// <param name="_markerTime">Duration to display the marker</param>
    public void Initialize(GameObject _sicknessViewMarker, SteamVR_Action_Boolean _sicknessMarker, float _markerTime)
    {
        m_SicknessViewMarker = _sicknessViewMarker;
        m_SicknessMarker = _sicknessMarker;
        m_MarkerTime = _markerTime;
        
        // Initialize cache
        m_MarkerWait = new WaitForSeconds(m_MarkerTime);
        
        // Initialize marker actions
        InitializeMarkerActions();
    }

    /// <summary>
    /// Configures the LSL stream for EEG markers
    /// </summary>
    public void ConfigureLSLStream()
    {
        var hash = new Hash128();
        hash.Append(c_StreamName);
        hash.Append(c_StreamType);
        
        // TODO
        // StreamInfo streamInfo = new StreamInfo(c_StreamName, c_StreamType, c_MaxLSLChannels, 
        //     LSL.LSL.IRREGULAR_RATE, channel_format_t.cf_string, hash.ToString());
        // m_Outlet = new StreamOutlet(streamInfo);
        
        Debug.Log($"[{nameof(ExperimentMarkerSystem)}] LSL stream configured: {c_StreamName} ({c_StreamType})");
    }

    /// <summary>
    /// Records a marker and queues its action for processing
    /// </summary>
    /// <param name="_marker">The marker to record</param>
    public void RecordMarker(string _marker)
    {
        Debug.Log($"[{nameof(ExperimentMarkerSystem)}] Recording marker: {_marker}");
        
        // Push the marker to LSL if available
        // TODO
        // if (m_Outlet != null)
        // {
        //     m_Sample[0] = _marker;
        //     m_Outlet.push_sample(m_Sample);
        // }
        
        // Queue the marker action for processing
        m_MarkerQueue.Enqueue(_marker);
    }

    /// <summary>
    /// Processes EEG markers based on experiment state
    /// </summary>
    public void ProcessEEGMarkers(bool _isExpStart, bool _isExpEnd)
    {
        // Record start marker when experiment begins
        if (_isExpStart && !m_IsEEGRecord)
        {
            m_IsEEGRecord = true;
            RecordMarker("Start");
            return;
        }
        
        // Record end marker when experiment ends
        if (_isExpEnd && m_IsEEGRecord && !m_FinishEEGRecord)
        {
            m_IsEEGRecord = false;
            m_FinishEEGRecord = true;
            RecordMarker("End");
        }
        
        // Process any queued markers
        ProcessMarkerQueue();
    }

    /// <summary>
    /// Checks if the sickness marker trigger has been activated
    /// </summary>
    public void CheckSicknessMarker()
    {
        if (!m_MarkerClock && m_SicknessMarker.GetState(SteamVR_Input_Sources.RightHand))
        {
            m_MarkerClock = true;
            RecordMarker("Sickness");
            
            // Show visual feedback for sickness marker
            if (m_MarkerDisplayCoroutine != null)
            {
                StopCoroutine(m_MarkerDisplayCoroutine);
            }
            m_MarkerDisplayCoroutine = StartCoroutine(DisplaySicknessMarker());
            
            // Reset marker clock after delay
            StartCoroutine(ResetMarkerLock());
        }
    }

    /// <summary>
    /// Forces a sickness marker to be recorded programmatically
    /// </summary>
    public void ForceSicknessMarker()
    {
        if (!m_MarkerClock)
        {
            m_MarkerClock = true;
            RecordMarker("Sickness");
            StartCoroutine(DisplaySicknessMarker());
            StartCoroutine(ResetMarkerLock());
        }
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// Initializes the marker action dictionary for efficient marker processing
    /// </summary>
    private void InitializeMarkerActions()
    {
        m_MarkerActions.Add("Start", () => { Debug.Log("Experiment started"); });
        m_MarkerActions.Add("End", () => { Debug.Log("Experiment ended"); });
        m_MarkerActions.Add("MindfulnessBegin", () => { /* Handle mindfulness begin */ });
        m_MarkerActions.Add("MindfulnessEnd", () => { /* Handle mindfulness end */ });
        m_MarkerActions.Add("VisitBegin", () => { /* Handle visit begin */ });
        m_MarkerActions.Add("VisitEnd", () => { /* Handle visit end */ });
        m_MarkerActions.Add("SelectBegin", () => { /* Handle select begin */ });
        m_MarkerActions.Add("SelectEnd", () => { /* Handle select end */ });
        m_MarkerActions.Add("ManiBegin", () => { /* Handle manipulation begin */ });
        m_MarkerActions.Add("ManiEnd", () => { /* Handle manipulation end */ });
        m_MarkerActions.Add("RestBegin", () => { /* Handle rest begin */ });
        m_MarkerActions.Add("RestEnd", () => { /* Handle rest end */ });
        m_MarkerActions.Add("Sickness", () => 
        { 
            Debug.Log("Sickness marker recorded");
            OnSicknessMarked?.Invoke();
        });
    }

    /// <summary>
    /// Processes any queued marker actions
    /// </summary>
    private void ProcessMarkerQueue()
    {
        if (m_MarkerQueue.Count > 0)
        {
            string marker = m_MarkerQueue.Dequeue();
            if (m_MarkerActions.TryGetValue(marker, out Action action))
            {
                action?.Invoke();
            }
            else
            {
                Debug.LogWarning($"[{nameof(ExperimentMarkerSystem)}] Unknown marker: {marker}");
            }
        }
    }

    /// <summary>
    /// Displays the sickness marker for a fixed duration
    /// </summary>
    private IEnumerator DisplaySicknessMarker()
    {
        m_SicknessViewMarker.SetActive(true);
        yield return m_MarkerWait;
        m_SicknessViewMarker.SetActive(false);
    }

    /// <summary>
    /// Resets the marker lock after a delay to prevent rapid marker triggering
    /// </summary>
    private IEnumerator ResetMarkerLock()
    {
        yield return m_MarkerWait;
        m_MarkerClock = false;
    }

    #endregion

    #region Unity Lifecycle
    
    private void OnDestroy()
    {
        // TODO: Close LSL outlet if it exists
        // if (m_Outlet != null)
        // {
        //     m_Outlet.close();
        //     m_Outlet = null;
        // }
    }
    
    #endregion
} 