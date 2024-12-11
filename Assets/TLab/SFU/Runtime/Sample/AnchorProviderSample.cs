using System.Linq;
using UnityEngine;
using TLab.SFU.Network;

namespace TLab.SFU.Sample
{
    public class AnchorProviderSample : BaseAnchorProvider
    {
        [System.Serializable]
        public class AnchorCurve
        {
            [Min(0f)] public float radius;
            public float height;
            [MinMaxRange(0f, 1f)] public Vector2 range;
            [Range(0f, 1f)] public float angle = 0.4f;
            public bool flipZ = false;
            public bool flipY = false;
            [Min(0f)] public Vector2Int users;
        }

        [Header("Anchor Settings")]

        [SerializeField] private Transform m_host;

        [SerializeField] private AnchorCurve[] m_curves;

        [Header("Gizmo Settings")]

        [SerializeField] private bool m_gizmo = true;

        [SerializeField] private Color m_gizmoColor = Color.green;

        private void OnEnable()
        {
            {
                var tmp = m_gizmo;
                m_gizmo = tmp;
            }

            {
                var tmp = m_gizmoColor;
                m_gizmoColor = tmp;
            }
        }

        public override bool Get(int userId, out SerializableTransform anchor)
        {
            if (userId == 0)
            {
                anchor = m_host.ToSerializableTransform();
                return true;
            }

            var curves = m_curves.Where((c) => (userId >= c.users.x && userId <= c.users.y)).ToArray();

            if (curves.Length == 0)
            {
                anchor = new SerializableTransform();
                return false;
            }

            var curve = curves[0];

            var t = (float)(userId - curve.users.x) / (curve.users.y - curve.users.x);

            var offset = (curve.range.y - curve.range.x) * t;
            var forward = Quaternion.AngleAxis(2 * Mathf.PI * Mathf.Rad2Deg * (curve.angle + curve.range.x + offset - 0.5f), Vector3.up) * transform.forward;

            anchor = new SerializableTransform(forward * curve.radius + new Vector3(0, curve.height, 0), Quaternion.LookRotation(-forward * (curve.flipZ ? -1 : 1)));
            return true;
        }

        public override bool Get(in Address32 id, out SerializableTransform anchor)
        {
            throw new System.NotImplementedException();
        }

        public override bool Get(in Address64 id, out SerializableTransform anchor)
        {
            throw new System.NotImplementedException();
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (!m_gizmo)
                return;

            var color = Gizmos.color;

            Gizmos.color = m_gizmoColor;

            m_curves?.Foreach((c) =>
            {
                var start = transform.rotation * Quaternion.AngleAxis(2 * Mathf.PI * Mathf.Rad2Deg * (c.angle + c.range.x - 0.5f), Vector3.up);
                var angle = 2 * Mathf.PI * Mathf.Rad2Deg * (c.range.y - c.range.x);
                var pos = transform.position + new Vector3(0f, c.height, 0f);
                GizmosExtensions.DrawWireArc(pos, c.radius, angle, rotation: start);
            });

            Gizmos.color = color;
        }
#endif
    }
}
