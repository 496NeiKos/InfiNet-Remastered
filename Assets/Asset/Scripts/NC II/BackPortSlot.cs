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

    private void ApplySprite()
    {
        if (_sr == null) return;
        _sr.sprite = (_state == PortState.Uninstalled) ? uninstalledSprite : installedSprite;
    }
}