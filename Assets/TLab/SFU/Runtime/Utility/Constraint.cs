using UnityEngine;
using TLab.SFU.Network;

namespace TLab.SFU
{
    using Registry = Registry<string, Constraint>;

    [AddComponentMenu("TLab/SFU/Constraint (TLab)")]
    public class Constraint : MonoBehaviour
    {
        [SerializeField] private Direction m_direction;

        [SerializeField] private string m_id;

        [SerializeField, Min(0f)] private float m_scale = 1f;

        private Constraint m_parent;

        public float scale { get => m_scale; set => m_scale = value; }

        private void Awake()
        {
            if (Const.Send.HasFlag(m_direction))
                Registry.Register(m_id, this);
        }

        private void Start()
        {
            if (Const.Recv.HasFlag(m_direction))
                m_parent = Registry.GetByKey(m_id);
        }

        private void Update()
        {
            if (Const.Recv.HasFlag(m_direction) && (m_parent != null))
            {
                transform.position = m_parent.transform.position;
                transform.rotation = m_parent.transform.rotation;
                transform.localScale = m_parent.transform.localScale * scale;
            }
        }

        private void OnDestroy()
        {
            if (Const.Send.HasFlag(m_direction))
                Registry.UnRegister(m_id);
        }
    }
}
