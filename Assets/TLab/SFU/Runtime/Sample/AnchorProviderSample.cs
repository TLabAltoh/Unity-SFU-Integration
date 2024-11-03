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
            [Header("Curve Settings")]
            [Min(0f)] public float radius;
            public float height;
            [MinMaxRange(0f, 1f)] public Vector2 range;
            [Range(0f, 1f)] public float angle = 0.4f;
            [Min(0f)] public Vector2Int users;

            [Header("Gizmo Settings")]
            public bool draw = true;
            public Color color = Color.green;
        }

        [Header("Anchor Settings")]

        [SerializeField] private Transform m_host;

        [SerializeField] private AnchorCurve[] m_curves;

        [Header("Gizmo Settings")]
        [Range(1, 5), SerializeField] private int m_testId;
        [Range(0, 1), SerializeField] private float m_size = 0.5f;

        public override bool Get(int userId, out WebTransform anchor)
        {
            if (userId == 0)
            {
                anchor = new WebTransform();
                return false;
            }

            var curves = m_curves.Where((c) => (userId >= c.users.x && userId <= c.users.y)).ToArray();

            if (curves.Length == 0)
            {
                anchor = new WebTransform();
                return false;
            }

            var curve = curves[0];

            var t = (float)(userId - curve.users.x) / (curve.users.y - curve.users.x);

            var offset = (curve.range.y - curve.range.x) * t;
            var forward = Quaternion.AngleAxis(2 * Mathf.PI * Mathf.Rad2Deg * (curve.angle + curve.range.x + offset - 0.5f), Vector3.up) * transform.forward;

            anchor = new WebTransform(forward * curve.radius + new Vector3(0, curve.height, 0), Quaternion.LookRotation(-forward));
            return true;
        }

        public override bool Get(Address32 id, out WebTransform anchor)
        {
            throw new System.NotImplementedException();
        }

        public override bool Get(Address64 id, out WebTransform anchor)
        {
            throw new System.NotImplementedException();
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            var color = Gizmos.color;

            m_curves.Foreach((c) =>
            {
                if (!c.draw)
                    return;

                Gizmos.color = c.color;
                var start = transform.rotation * Quaternion.AngleAxis(2 * Mathf.PI * Mathf.Rad2Deg * (c.angle + c.range.x - 0.5f), Vector3.up);
                var angle = 2 * Mathf.PI * Mathf.Rad2Deg * (c.range.y - c.range.x);
                var pos = transform.position + new Vector3(0f, c.height, 0f);
                GizmosExtensions.DrawWireArc(pos, c.radius, angle, rotation: start);
            });

            if (Get(m_testId, out var anchor))
            {
                var old = Gizmos.matrix;

                Gizmos.matrix = Matrix4x4.TRS(anchor.position.raw, anchor.rotation.rotation, anchor.scale.raw);
                Gizmos.DrawCube(Vector3.zero, Vector3.one * m_size);

                Gizmos.matrix = old;
            }

            Gizmos.color = color;
        }
#endif
    }
}
