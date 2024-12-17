using UnityEngine;

namespace TLab.SFU
{
    [AddComponentMenu("TLab/SFU/Collider Surface (TLab)")]
    public class ColliderSurface : MonoBehaviour
    {
        [SerializeField] protected Collider m_collider;

        public virtual bool SkipRaycast() => m_collider == null || !m_collider.enabled;

        public virtual bool Raycast(Ray ray, out RaycastHit hit, float maxDistance)
        {
            if (SkipRaycast())
            {
                hit = new RaycastHit();
                return false;
            }

            return m_collider.Raycast(ray, out hit, maxDistance);
        }

        public virtual bool Spherecast(Vector3 point, out RaycastHit hit, float maxDistance)
        {
            if (SkipRaycast())
            {
                hit = new RaycastHit();
                return false;
            }

            var closestPoint = m_collider.ClosestPoint(point);
            hit = new RaycastHit();

            hit.distance = (point - closestPoint).magnitude;
            hit.point = closestPoint;

            return hit.distance < maxDistance;
        }
    }
}
