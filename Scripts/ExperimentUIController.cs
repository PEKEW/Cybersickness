using System.Text;
using TMPro;
using UnityEngine;

/// <summary>
/// Controls UI elements during the experiment
/// </summary>
[RequireComponent(typeof(ExpMannger))]
public class ExperimentUIController : MonoBehaviour
{
    #region Private Fields
    
    // References
    private GameObject m_Mask;
    private TMP_Text m_MaskInfo;
    
    // Text builders
    private StringBuilder m_TimerDisplay;
    
    #endregion

    #region Public Methods
    
    /// <summary>
    /// Initializes the UI controller with required references
    /// </summary>
    /// <param name="_mask">The mask GameObject</param>
    /// <param name="_maskInfo">The text component for display</param>
    public void Initialize(GameObject _mask, TMP_Text _maskInfo)
    {
        m_Mask = _mask;
        m_MaskInfo = _maskInfo;
        
        // Initialize text builder
        m_TimerDisplay = new StringBuilder(100);
    }
    
    /// <summary>
    /// Shows the start experiment prompt
    /// </summary>
    public void ShowStartPrompt()
    {
        m_Mask.SetActive(true);
        m_MaskInfo.text = "Press [B] on right controller to start the experiment";
    }
    
    /// <summary>
    /// Shows the exit experiment prompt
    /// </summary>
    public void ShowExitPrompt()
    {
        m_Mask.SetActive(true);
        m_MaskInfo.text = "All exp have done, press [B] to exit";
    }
    
    /// <summary>
    /// Updates the timer display with current time remaining
    /// </summary>
    /// <param name="_phase">The current experiment phase</param>
    /// <param name="_remainingTime">Time remaining in seconds</param>
    public void UpdateTimer(string _phase, float _remainingTime)
    {
        m_TimerDisplay.Clear();
        m_TimerDisplay.AppendFormat("{0}: {1:F1}s", _phase, _remainingTime);
        m_Mask.SetActive(true);
        m_MaskInfo.text = m_TimerDisplay.ToString();
    }
    
    /// <summary>
    /// Hides the mask for tasks that don't need it
    /// </summary>
    public void HideMask()
    {
        m_Mask.SetActive(false);
    }
    
    /// <summary>
    /// Shows a custom message on the mask
    /// </summary>
    /// <param name="_message">The message to display</param>
    public void ShowMessage(string _message)
    {
        m_Mask.SetActive(true);
        m_MaskInfo.text = _message;
    }
    
    #endregion
    
    #region Private Methods
    
    /// <summary>
    /// Validates the UI elements
    /// </summary>
    private void ValidateUIElements()
    {
        if (m_Mask == null)
        {
            Debug.LogError($"[{nameof(ExperimentUIController)}] Mask GameObject reference is not set!");
        }
        
        if (m_MaskInfo == null)
        {
            Debug.LogError($"[{nameof(ExperimentUIController)}] MaskInfo TMP_Text reference is not set!");
        }
    }
    
    #endregion
    
    #region Unity Lifecycle
    
    private void OnEnable()
    {
        ValidateUIElements();
    }
    
    private void OnDestroy()
    {
        m_TimerDisplay = null;
    }
    
    #endregion
} 