using UnityEngine;

[DisallowMultipleComponent]
public class EnemySpawnPoint : MonoBehaviour
{
    [SerializeField] private float gizmoRadius = 0.35f;
    [SerializeField] private Color gizmoColor = new Color(1f, 0.76f, 0.2f, 0.85f);

    public Vector3 Position => transform.position;
    public Quaternion Rotation => transform.rotation;

    void OnDrawGizmos()
    {
        Gizmos.color = gizmoColor;
        Gizmos.DrawSphere(transform.position, gizmoRadius);
        Gizmos.DrawWireSphere(transform.position, gizmoRadius * 1.5f);
    }
}
