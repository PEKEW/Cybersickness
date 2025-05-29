using System;
using System.Collections;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

/// <summary>
/// Manages the execution and flow of experiment tasks
/// </summary>
[RequireComponent(typeof(ExpMannger))]
public class ExperimentTaskManager : MonoBehaviour
{
    #region Private Fields
    
    // References
    private GameObject m_PathObj;
    private SelectMod m_SelectController;
    private ManiMod m_ManiController;
    
    // Task completion callbacks
    private Action m_OnVisitComplete;
    private Action m_OnSelectComplete;
    private Action m_OnManiComplete;
    
    // Cached objects
    private readonly WaitForEndOfFrame m_EndOfFrameWait = new WaitForEndOfFrame();
    
    #endregion

    #region Events
    
    /// <summary>
    /// Event triggered when a task begins
    /// </summary>
    public event Action<string> OnTaskBegin;
    
    /// <summary>
    /// Event triggered when a task ends
    /// </summary>
    public event Action<string> OnTaskEnd;
    
    #endregion

    #region Public Methods
    
    /// <summary>
    /// Initializes the task manager with required references
    /// </summary>
    /// <param name="_pathObj">The path object for visit task</param>
    /// <param name="_selectController">The selection task controller</param>
    /// <param name="_maniController">The manipulation task controller</param>
    public void Initialize(GameObject _pathObj, SelectMod _selectController, ManiMod _maniController)
    {
        m_PathObj = _pathObj;
        m_SelectController = _selectController;
        m_ManiController = _maniController;
        
        // Set up task completion callbacks
        ConfigureTaskCallbacks();
    }
    
    /// <summary>
    /// Executes the mindfulness training phase
    /// </summary>
    /// <param name="_duration">The duration of the mindfulness phase</param>
    /// <param name="_uiController">The UI controller for display updates</param>
    public IEnumerator ExecuteMindfulnessPhase(float _duration, ExperimentUIController _uiController)
    {
        OnTaskBegin?.Invoke("Mindfulness");
        
        float startTime = Time.time;
        while (Time.time - startTime < _duration)
        {
            float remainingTime = _duration - (Time.time - startTime);
            _uiController.UpdateTimer("Mindfulness", remainingTime);
            yield return m_EndOfFrameWait;
        }
        
        OnTaskEnd?.Invoke("Mindfulness");
    }
    
    /// <summary>
    /// Executes a rest phase between tasks
    /// </summary>
    /// <param name="_duration">The duration of the rest phase</param>
    /// <param name="_nextTaskName">The name of the upcoming task</param>
    /// <param name="_uiController">The UI controller for display updates</param>
    public IEnumerator ExecuteRestPhase(float _duration, string _nextTaskName, ExperimentUIController _uiController)
    {
        OnTaskBegin?.Invoke("Rest");
        
        float startTime = Time.time;
        while (Time.time - startTime < _duration)
        {
            float remainingTime = _duration - (Time.time - startTime);
            _uiController.UpdateTimer($"Rest ({_nextTaskName})", remainingTime);
            yield return m_EndOfFrameWait;
        }
        
        OnTaskEnd?.Invoke("Rest");
    }
    
    /// <summary>
    /// Executes the visit task
    /// </summary>
    /// <param name="_uiController">The UI controller for display updates</param>
    public IEnumerator ExecuteVisitTask(ExperimentUIController _uiController)
    {
        OnTaskBegin?.Invoke("Visit");
        
        m_PathObj.SetActive(true);
        Debug.Log("[VisitTask] Task started");
        _uiController.HideMask();
        
        TaskCompletionSource<bool> taskCompletion = new TaskCompletionSource<bool>();
        
        // In a real implementation, this would be connected to an actual event
        // For demonstration purposes, we're using a key press
        StartCoroutine(MonitorVisitTaskCompletion(taskCompletion));
        
        // Wait for task completion
        yield return new WaitUntil(() => taskCompletion.Task.IsCompleted);
        
        // Execute completion callback
        m_OnVisitComplete?.Invoke();
        
        OnTaskEnd?.Invoke("Visit");
    }
    
    /// <summary>
    /// Executes the selection task
    /// </summary>
    /// <param name="_uiController">The UI controller for display updates</param>
    public IEnumerator ExecuteSelectTask(ExperimentUIController _uiController)
    {
        OnTaskBegin?.Invoke("Select");
        
        Debug.Log("[SelectTask] Task started");
        _uiController.HideMask();
        
        TaskCompletionSource<bool> taskCompletion = new TaskCompletionSource<bool>();
        
        if (m_SelectController != null)
        {
            m_SelectController.enabled = true;
            
            // Subscribe to the selection event
            m_SelectController.onSelectEvent += () =>
            {
                Debug.Log("[SelectTask] Task completed via selection event");
                taskCompletion.TrySetResult(true);
            };
            
            // Wait for task completion
            yield return new WaitUntil(() => taskCompletion.Task.IsCompleted);
            
            // Execute completion callback
            m_OnSelectComplete?.Invoke();
        }
        else
        {
            Debug.LogError("[SelectTask] SelectController is null");
            yield return null;
        }
        
        OnTaskEnd?.Invoke("Select");
    }
    
    /// <summary>
    /// Executes the manipulation task
    /// </summary>
    /// <param name="_uiController">The UI controller for display updates</param>
    public IEnumerator ExecuteManipulationTask(ExperimentUIController _uiController)
    {
        OnTaskBegin?.Invoke("Manipulation");
        
        Debug.Log("[ManiTask] Task started");
        _uiController.HideMask();
        
        TaskCompletionSource<bool> taskCompletion = new TaskCompletionSource<bool>();
        
        if (m_ManiController != null)
        {
            m_ManiController.enabled = true;
            
            // Subscribe to the manipulation event
            m_ManiController.onManiEvent += () =>
            {
                Debug.Log("[ManiTask] Task completed via manipulation event");
                taskCompletion.TrySetResult(true);
            };
            
            // Wait for task completion
            yield return new WaitUntil(() => taskCompletion.Task.IsCompleted);
            
            // Execute completion callback
            m_OnManiComplete?.Invoke();
        }
        else
        {
            Debug.LogError("[ManiTask] ManiController is null");
            yield return null;
        }
        
        OnTaskEnd?.Invoke("Manipulation");
    }
    
    #endregion

    #region Private Methods
    
    /// <summary>
    /// Sets up callbacks for task completion
    /// </summary>
    private void ConfigureTaskCallbacks()
    {
        m_OnVisitComplete = () => 
        {
            Debug.Log("Visit task completed");
            m_PathObj.SetActive(false);
        };
        
        m_OnSelectComplete = () => 
        {
            Debug.Log("Selection task completed");
            if (m_SelectController != null)
            {
                m_SelectController.enabled = false;
            }
        };
        
        m_OnManiComplete = () => 
        {
            Debug.Log("Manipulation task completed");
            if (m_ManiController != null)
            {
                m_ManiController.enabled = false;
            }
        };
    }
    
    /// <summary>
    /// Monitors for visit task completion
    /// </summary>
    /// <param name="_taskCompletion">Task completion source</param>
    private IEnumerator MonitorVisitTaskCompletion(TaskCompletionSource<bool> _taskCompletion)
    {
        while (!_taskCompletion.Task.IsCompleted)
        {
            if (Input.GetKeyDown(KeyCode.Backspace))
            {
                Debug.Log("[VisitTask] Task completed via backspace key");
                _taskCompletion.SetResult(true);
            }
            yield return null;
        }
    }
    
    #endregion
} 