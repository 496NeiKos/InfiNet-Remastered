using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Plays a small horizontal shake on any Transform to signal a blocked
/// uninstall or unplug attempt. Call UnableAnimation.Shake(transform) at
/// any point where an action is blocked and the user needs visual feedback.
/// Self-instantiates; no scene setup required.
/// </summary>
public class UnableAnimation : MonoBehaviour
{
    [SerializeField] private float duration  = 0.40f;
    [SerializeField] private float magnitude = 0.10f;
    [SerializeField] private float frequency = 28f;

    private static UnableAnimation _instance;

    private static UnableAnimation Instance
    {
        get
        {
            if (_instance != null) return _instance;
            var go = new GameObject("[UnableAnimation]");
            DontDestroyOnLoad(go);
            _instance = go.AddComponent<UnableAnimation>();
            return _instance;
        }
    }

    private readonly HashSet<Transform> _active = new HashSet<Transform>();

    public static void Shake(Transform target)
    {
        if (target == null) return;
        Instance.TryShake(target);
    }

    private void TryShake(Transform target)
    {
        if (_active.Contains(target)) return;
        StartCoroutine(ShakeRoutine(target));
    }

    private IEnumerator ShakeRoutine(Transform target)
    {
        _active.Add(target);

        Vector3 origin  = target.localPosition;
        float   elapsed = 0f;

        while (elapsed < duration)
        {
            if (target == null) break;

            float envelope = 1f - (elapsed / duration);
            float x = Mathf.Sin(elapsed * frequency * Mathf.PI * 2f) * magnitude * envelope;
            target.localPosition = origin + new Vector3(x, 0f, 0f);

            elapsed += Time.deltaTime;
            yield return null;
        }

        if (target != null)
            target.localPosition = origin;

        _active.Remove(target);
    }
}
