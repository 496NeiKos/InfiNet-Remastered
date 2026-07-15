using UnityEngine;

/// <summary>
/// On CPUDetailed view object.
/// Handles thermal paste application and towel cloth removal via proximity trigger + hold timer.
/// Tag "ThermalPaste" → applies paste after 2s hold.
/// Tag "TowelCloth"   → removes paste after 2s hold.
/// </summary>
public class CPUDetailInteraction : MonoBehaviour
{
    [SerializeField] private CPUController cpuController;
    [SerializeField] private float holdDuration = 2f;

    private string _currentToolTag = "";
    private float _holdTimer = 0f;
    private bool _isHolding = false;

    private void Start()
    {
        if (cpuController == null)
            cpuController = GetComponentInParent<CPUController>();
    }

    private void Update()
    {
        if (!_isHolding) return;

        _holdTimer += Time.deltaTime;

        if (_holdTimer >= holdDuration)
        {
            _holdTimer = 0f;
            _isHolding = false;
            ApplyTool(_currentToolTag);
            _currentToolTag = "";
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("ThermalPaste") || other.CompareTag("TowelCloth"))
        {
            _currentToolTag = other.tag;
            _isHolding = true;
            _holdTimer = 0f;
            Debug.Log($"[CPUDetailInteraction] {_currentToolTag} contact — holding...");
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("ThermalPaste") || other.CompareTag("TowelCloth"))
        {
            _isHolding = false;
            _holdTimer = 0f;
            _currentToolTag = "";
            Debug.Log($"[CPUDetailInteraction] Tool removed — timer reset.");
        }
    }

    private void ApplyTool(string toolTag)
    {
        if (cpuController == null) return;

        if (toolTag == "ThermalPaste")
        {
            cpuController.ApplyThermalPaste();
        }
        else if (toolTag == "TowelCloth")
        {
            cpuController.RemoveThermalPaste();
        }
    }
}