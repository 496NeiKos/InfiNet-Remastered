using UnityEngine;

/// <summary>
/// On each back port object (PSUBackPort, VGAPort, etc.).
/// Tracks installed/uninstalled state. Used by SystemUnitConditionChecker.
/// </summary>
public class BackPortSlot : MonoBehaviour
{
    public enum PortState { Installed, Uninstalled }

    [Header("Sprites")]
    [SerializeField] private Sprite installedSprite;
    [SerializeField] private Sprite uninstalledSprite;

    [Header("Cross-device cable types this port also accepts")]
    [SerializeField] private string[] additionalCableTypes;

    private SpriteRenderer _sr;
    private PortState _state = PortState.Installed;

    public bool IsInstalled => _state == PortState.Installed;
    public bool IsUninstalled => _state == PortState.Uninstalled;

    private void Awake()
    {
        _sr = GetComponent<SpriteRenderer>();
        ApplySprite();
    }

    public void SetInstalled()
    {
        _state = PortState.Installed;
        ApplySprite();
        Debug.Log($"[BackPortSlot] {gameObject.name} → Installed");
    }

    public void SetUninstalled()
    {
        _state = PortState.Uninstalled;
        ApplySprite();
        Debug.Log($"[BackPortSlot] {gameObject.name} → Uninstalled");
    }

    /// <summary>
    /// Returns true if this port can accept a cable with the given cableType.
    /// Matches when the port name equals/contains the type, or the type is in additionalCableTypes.
    /// </summary>
    public bool CanAcceptCable(string cableType)
    {
        if (string.IsNullOrEmpty(cableType)) return false;
        if (gameObject.name == cableType || gameObject.name.Contains(cableType)) return true;
        if (additionalCableTypes != null)
            foreach (string t in additionalCableTypes)
                if (!string.IsNullOrEmpty(t) && cableType == t) return true;
        return false;
    }

    private void ApplySprite()
    {
        if (_sr == null) return;
        _sr.sprite = (_state == PortState.Uninstalled) ? uninstalledSprite : installedSprite;
    }
}