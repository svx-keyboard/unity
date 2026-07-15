using UnityEngine;

public class ArmCollisionChecker : MonoBehaviour
{
    [Header("Все коллайдеры руки")]
    public Collider[] armColliders;

    [Header("Слои, с которыми запрещено пересекаться")]
    public LayerMask forbiddenLayers;

    public bool HasCollision()
    {
        Physics.SyncTransforms();

        foreach (Collider own in armColliders)
        {
            if (own == null)
                continue;

            Collider[] hits = Physics.OverlapBox(
                own.bounds.center,
                own.bounds.extents,
                own.transform.rotation,
                forbiddenLayers);

            foreach (Collider hit in hits)
            {
                if (hit == own)
                    continue;

                if (hit.transform.IsChildOf(transform))
                    continue;

                Vector3 dir;
                float dist;

                if (Physics.ComputePenetration(
                    own, own.transform.position, own.transform.rotation,
                    hit, hit.transform.position, hit.transform.rotation,
                    out dir,
                    out dist))
                {
                    return true;
                }
            }
        }

        return false;
    }
}
