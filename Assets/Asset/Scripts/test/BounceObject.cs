using UnityEngine;
using System.Collections;

public class BounceObject : MonoBehaviour
{
    [Header("Bounce Settings")]
    public float bounceHeight = 0.5f;   // How high it bounces
    public float bounceDuration = 0.5f; // Time to go up/down
    public float bounceInterval = 2f;   // Time between bounces

    private Vector3 startPos;

    void Start()
    {
        startPos = transform.localPosition;
        StartCoroutine(BounceRoutine());
    }

    private IEnumerator BounceRoutine()
    {
        while (true)
        {
            // Drop down (simulate gravity)
            yield return MoveY(startPos.y, startPos.y - bounceHeight, bounceDuration);

            // Bounce back up
            yield return MoveY(startPos.y - bounceHeight, startPos.y, bounceDuration);

            // Wait before next bounce
            yield return new WaitForSeconds(bounceInterval);
        }
    }

    private IEnumerator MoveY(float fromY, float toY, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float newY = Mathf.Lerp(fromY, toY, t);
            transform.localPosition = new Vector3(startPos.x, newY, startPos.z);
            yield return null;
        }
    }
}
