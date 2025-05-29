using System.Collections;
using UnityEngine;

/// <summary>
/// Manages visual effects for the selection task
/// </summary>
[RequireComponent(typeof(SelectMod))]
public class SelectionVisualManager : MonoBehaviour
{
    #region Private Fields
    
    // Materials
    private Material m_OriginalMaterial;
    private Material m_ToSelectMaterial;
    private Material m_SelectedMaterial;
    
    // Effect configuration
    private const float c_HighlightDuration = 0.5f;
    
    // Effect coroutines
    private Coroutine m_ActiveEffectCoroutine;
    
    // Cached WaitForSeconds for performance
    private readonly WaitForSeconds m_HighlightWait;
    
    #endregion
    
    #region Constructors
    
    /// <summary>
    /// Initializes the visual manager
    /// </summary>
    public SelectionVisualManager()
    {
        m_HighlightWait = new WaitForSeconds(c_HighlightDuration);
    }
    
    #endregion
    
    #region Public Methods
    
    /// <summary>
    /// Initializes the materials
    /// </summary>
    public void Initialize()
    {
        // Load or create materials
        m_OriginalMaterial = new Material(Shader.Find("Standard"));
        m_ToSelectMaterial = new Material(Shader.Find("Custom/ToSelect"));
        m_SelectedMaterial = new Material(Shader.Find("Custom/Selected"));
        
        // Validate materials
        if (m_SelectedMaterial == null || m_ToSelectMaterial == null)
        {
            Debug.LogWarning($"[{nameof(SelectionVisualManager)}] One or more required shaders not found!");
        }
    }
    
    /// <summary>
    /// Applies the selection effect to the target renderer
    /// </summary>
    /// <param name="_renderer">The renderer to apply the effect to</param>
    public void ApplySelectionEffect(Renderer _renderer)
    {
        if (_renderer == null)
        {
            Debug.LogWarning($"[{nameof(SelectionVisualManager)}] Renderer is null!");
            return;
        }
        
        // Cache the original material if not already cached
        if (m_OriginalMaterial == null)
        {
            m_OriginalMaterial = new Material(_renderer.material);
        }
        
        // Apply the selected material
        _renderer.material = m_SelectedMaterial;
        
        // Start the effect coroutine
        if (m_ActiveEffectCoroutine != null)
        {
            StopCoroutine(m_ActiveEffectCoroutine);
        }
        
        m_ActiveEffectCoroutine = StartCoroutine(RestoreOriginalMaterial(_renderer));
    }
    
    /// <summary>
    /// Applies the to-select effect to the target renderer
    /// </summary>
    /// <param name="_renderer">The renderer to apply the effect to</param>
    public void ApplyToSelectEffect(Renderer _renderer)
    {
        if (_renderer == null || m_ToSelectMaterial == null)
        {
            return;
        }
        
        // Cache the original material if not already cached
        if (m_OriginalMaterial == null)
        {
            m_OriginalMaterial = new Material(_renderer.material);
        }
        
        // Apply the to-select material
        _renderer.material = m_ToSelectMaterial;
    }
    
    /// <summary>
    /// Restores the original material on the target renderer
    /// </summary>
    /// <param name="_renderer">The renderer to restore</param>
    public void RestoreOriginalMaterial(Renderer _renderer)
    {
        if (_renderer == null || m_OriginalMaterial == null)
        {
            return;
        }
        
        _renderer.material = m_OriginalMaterial;
    }
    
    #endregion
    
    #region Private Methods
    
    /// <summary>
    /// Restores the original material after a delay
    /// </summary>
    /// <param name="_renderer">The renderer to restore</param>
    private IEnumerator RestoreOriginalMaterial(Renderer _renderer)
    {
        yield return m_HighlightWait;
        
        RestoreOriginalMaterial(_renderer);
        
        m_ActiveEffectCoroutine = null;
    }
    
    #endregion
    
    #region Unity Lifecycle
    
    private void OnDestroy()
    {
        // Clean up materials
        if (m_OriginalMaterial != null)
        {
            Destroy(m_OriginalMaterial);
        }
        
        if (m_ToSelectMaterial != null)
        {
            Destroy(m_ToSelectMaterial);
        }
        
        if (m_SelectedMaterial != null)
        {
            Destroy(m_SelectedMaterial);
        }
    }
    
    #endregion
} 