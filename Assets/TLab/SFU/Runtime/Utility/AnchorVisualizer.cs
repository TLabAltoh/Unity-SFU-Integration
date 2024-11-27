using UnityEngine;

namespace TLab.SFU
{
    [AddComponentMenu("TLab/SFU/Anchor Visualizer (TLab)")]
    public class AnchorVisualizer : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float m_size = 0.1f;
        [SerializeField, Min(0f)] private float m_length = 4f;

        [SerializeField] bool m_gizmo = true;

        private void OnEnable()
        {
            {
                var tmp = m_gizmo;
                m_gizmo = tmp;
            }

            {
                var tmp = m_size;
                m_size = tmp;
            }

            {
                var tmp = m_length;
                m_length = tmp;
            }
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (!m_gizmo)
                return;

            var old = Gizmos.matrix;

            Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, transform.lossyScale);

            var hsize = m_size * 0.5f;

            var offset = hsize * (m_length + 1);

            Gizmos.color = Color.red;
            Gizmos.DrawCube(new Vector3(offset, 0, 0), new Vector3(m_length, 1f, 1f) * m_size);

            Gizmos.color = Color.green;
            Gizmos.DrawCube(new Vector3(0, offset, 0), new Vector3(1f, m_length, 1f) * m_size);

            Gizmos.color = Color.blue;
            Gizmos.DrawCube(new Vector3(0, 0, offset), new Vector3(1f, 1f, m_length) * m_size);

            Gizmos.color = Color.white;
            Gizmos.DrawCube(Vector3.zero, Vector3.one * m_size);

            Gizmos.matrix = old;
        }
#endif
    }
}
