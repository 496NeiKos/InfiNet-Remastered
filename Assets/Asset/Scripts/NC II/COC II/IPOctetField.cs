/*
 * ================================================================
 *  UNITY SETUP GUIDE — IPOctetField
 * ================================================================
 *  COMPONENT PLACEMENT
 *    Add this script to each TMP_InputField GameObject that
 *    represents one octet of an IP address (0–255 range).
 *    You need FOUR per IP row (IP Address, Gateway, DNS, etc.).
 *
 *  HIERARCHY EXAMPLE (one IP row)
 *    IP Address Row
 *      ├── Octet1  (TMP_InputField + this script, index=0)
 *      ├── Dot1    (TMP_Text ".")
 *      ├── Octet2  (TMP_InputField + this script, index=1)
 *      ├── Dot2    (TMP_Text ".")
 *      ├── Octet3  (TMP_InputField + this script, index=2)
 *      ├── Dot3    (TMP_Text ".")
 *      └── Octet4  (TMP_InputField + this script, index=3)
 *
 *  INSPECTOR ASSIGNMENTS
 *    nextField   → the next octet's TMP_InputField (leave null for last octet)
 *    isReadOnly  → true for Subnet Mask (auto-filled, not editable)
 *
 *  HOW IT WORKS
 *    Uses onValidateInput (not ContentType.IntegerNumber) so the dot key
 *    can be intercepted before TMP's built-in filter discards it.
 *    Accepts digits 0–9.  "." or "," (numpad decimal) advances to nextField
 *    without inserting the character.  3 digits also auto-advance.
 *    OnEndEdit clamps the value to 0–255.
 *    IPv4PropertiesController reads the text values directly.
 * ================================================================
 */

using TMPro;
using UnityEngine;

[RequireComponent(typeof(TMP_InputField))]
public class IPOctetField : MonoBehaviour
{
    [SerializeField] private TMP_InputField nextField;
    [SerializeField] private bool isReadOnly;

    private TMP_InputField _field;

    private void Awake()
    {
        _field = GetComponent<TMP_InputField>();
        _field.characterLimit  = 3;
        // ContentType.Custom lets us own all character filtering via onValidateInput.
        // IntegerNumber would reject the dot before our code ever sees it, making
        // dot-to-advance impossible.
        _field.contentType     = TMP_InputField.ContentType.Custom;
        _field.onValidateInput = ValidateChar;
        _field.readOnly        = isReadOnly;

        _field.onValueChanged.AddListener(OnValueChanged);
        _field.onEndEdit.AddListener(OnEndEdit);
    }

    private void OnDestroy()
    {
        _field.onValidateInput = null;
        _field.onValueChanged.RemoveListener(OnValueChanged);
        _field.onEndEdit.RemoveListener(OnEndEdit);
    }

    // ----------------------------------------------------------------

    // Called by TMP for every character attempt before the field text changes.
    // Returns the character to insert, or '\0' to reject it.
    private char ValidateChar(string text, int charIndex, char addedChar)
    {
        if (addedChar >= '0' && addedChar <= '9') return addedChar;

        // Period (standard keyboard) or comma (numpad decimal on some locales) → advance.
        if (addedChar == '.' || addedChar == ',')
        {
            AdvanceToNext();
            return '\0'; // don't insert the dot
        }

        return '\0'; // reject all other characters
    }

    private void OnValueChanged(string value)
    {
        if (string.IsNullOrEmpty(value)) return;

        // Auto-advance after 3 digits
        if (value.Length == 3)
            AdvanceToNext();
    }

    private void OnEndEdit(string value)
    {
        if (string.IsNullOrEmpty(value)) return;

        if (int.TryParse(value, out int n))
        {
            n = Mathf.Clamp(n, 0, 255);
            _field.text = n.ToString();
        }
        else
        {
            _field.text = "0";
        }
    }

    private void AdvanceToNext()
    {
        if (nextField != null)
        {
            nextField.Select();
            nextField.ActivateInputField();
        }
    }

    // External read helper
    public string Value => _field != null ? _field.text : "";
    public void SetValue(string v)
    {
        if (_field != null) _field.text = v;
    }
    public void SetInteractable(bool value)
    {
        if (_field != null) _field.interactable = value && !isReadOnly;
    }
}
