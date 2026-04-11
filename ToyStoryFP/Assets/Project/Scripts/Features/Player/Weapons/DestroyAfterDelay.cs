using UnityEngine;

[AddComponentMenu("FX/Destroy After Delay")]
public class DestroyAfterDelay : MonoBehaviour
{
    [SerializeField] private float delaySeconds = 2f;

    protected virtual void Start()
    {
        Destroy(gameObject, Mathf.Max(0f, delaySeconds));
    }
}
