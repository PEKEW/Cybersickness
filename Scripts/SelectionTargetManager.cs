using UnityEngine;
using Random = UnityEngine.Random;

/// <summary>
/// Manages the selection targets in the selection task
/// </summary>
[RequireComponent(typeof(SelectMod))]
public class SelectionTargetManager : MonoBehaviour
{
    #region Private Fields
    
    // Target management
    private Transform m_ParentTransform;
    private int m_CurrentTargetIndex;
    private Transform m_CurrentTarget;
    
    // Configuration
    private int m_RemainingSelections;
    private int m_TotalSelectionCount;
    
    // State tracking
    private bool m_IsInitialized;
    
    #endregion
    
    #region Public Properties
    
    /// <summary>
    /// Gets the current target transform
    /// </summary>
    public Transform CurrentTarget => m_CurrentTarget;
    
    /// <summary>
    /// Gets the remaining number of selections needed
    /// </summary>
    public int RemainingSelections => m_RemainingSelections;
    
    /// <summary>
    /// Gets whether the manager is initialized
    /// </summary>
    public bool IsInitialized => m_IsInitialized;
    
    #endregion
    
    #region Public Methods
    
    /// <summary>
    /// Initializes the target manager
    /// </summary>
    /// <param name="_parentTransform">The parent transform containing selectable objects</param>
    /// <param name="_selectionCount">The number of selections required to complete the task</param>
    public void Initialize(Transform _parentTransform, int _selectionCount)
    {
        m_ParentTransform = _parentTransform;
        m_TotalSelectionCount = _selectionCount;
        m_RemainingSelections = _selectionCount;
        
        // Validate the parent transform
        if (m_ParentTransform == null)
        {
            Debug.LogError($"[{nameof(SelectionTargetManager)}] Parent transform is null!");
            return;
        }
        
        if (m_ParentTransform.childCount == 0)
        {
            Debug.LogError($"[{nameof(SelectionTargetManager)}] Parent transform has no children!");
            return;
        }
        
        // Select the first target
        SelectRandomTarget();
        
        m_IsInitialized = true;
    }
    
    /// <summary>
    /// Selects the next target and returns whether the task is complete
    /// </summary>
    /// <returns>True if all selections are complete, false otherwise</returns>
    public bool SelectNextTarget()
    {
        m_RemainingSelections--;
        
        // Check if we've completed all selections
        if (m_RemainingSelections <= 0)
        {
            return true;
        }
        
        // Select a new target
        SelectRandomTarget();
        return false;
    }
    
    /// <summary>
    /// Resets the target selection state
    /// </summary>
    /// <param name="_selectionCount">Optional new selection count</param>
    public void Reset(int? _selectionCount = null)
    {
        if (_selectionCount.HasValue)
        {
            m_TotalSelectionCount = _selectionCount.Value;
        }
        
        m_RemainingSelections = m_TotalSelectionCount;
        
        if (m_ParentTransform != null)
        {
            SelectRandomTarget();
        }
        else
        {
            m_IsInitialized = false;
        }
    }
    
    #endregion
    
    #region Private Methods
    
    /// <summary>
    /// Selects a random target from the available objects
    /// </summary>
    private void SelectRandomTarget()
    {
        if (m_ParentTransform == null || m_ParentTransform.childCount == 0)
        {
            return;
        }
        
        // Find a new random index that's different from the current one
        int newIndex = m_CurrentTargetIndex;
        if (m_ParentTransform.childCount > 1)
        {
            while (newIndex == m_CurrentTargetIndex)
            {
                newIndex = Random.Range(0, m_ParentTransform.childCount);
            }
        }
        
        m_CurrentTargetIndex = newIndex;
        m_CurrentTarget = m_ParentTransform.GetChild(m_CurrentTargetIndex);
        
        Debug.Log($"[{nameof(SelectionTargetManager)}] New target selected: {m_CurrentTarget.name} ({m_RemainingSelections} remaining)");
    }
    
    #endregion
} 