using UnityEngine;

public class PowerButton : MonoBehaviour
{
    private bool _isPoweredOn = true;
    private float _holdTimer = 0f;
    private bool _isHolding = false;

    public bool IsPoweredOn => _isPoweredOn;

    private void Update()
    {
        if (Input.GetMouseButtonDown(0) && IsMouseOver())
        {
            _isHolding = true;
            _holdTimer = 0f;
        }

        if (_isHolding && Input.GetMouseButton(0))
        {
            _holdTimer += Time.deltaTime;
            if (_isPoweredOn && _holdTimer >= 3f)
            {
                _isHolding = false;
                _holdTimer = 0f;
                Debug.Log("[PowerButton] Restart triggered — no logic yet.");
            }
        }

        if (_isHolding && Input.GetMouseButtonUp(0))
        {
            if (_holdTimer < 3f)
                Toggle();
            _isHolding = false;
            _holdTimer = 0f;
        }
    }

    private void Toggle()
    {
        _isPoweredOn = !_isPoweredOn;
        Debug.Log($"[PowerButton] Power is now {(_isPoweredOn ? "ON" : "OFF")}");
    }

    private bool IsMouseOver()
    {
        Vector2 mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Collider2D col = GetComponent<Collider2D>();
        return col != null && col.OverlapPoint(mouseWorld);
    }
}