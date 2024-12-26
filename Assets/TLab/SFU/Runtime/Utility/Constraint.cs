using UnityEngine;

namespace TLab.SFU
{
    using Registry = Registry<string, Constraint>;

    [AddComponentMenu("TLab/SFU/Constraint (TLab)")]
    public class Constraint : MonoBehaviour
    {
        public enum Direction
        {
            Send,
            Recv,
        };

        [SerializeField] private Direction m_direction;

        [SerializeField] private string m_id;

        [SerializeField] private bool m_pause = false;

        [SerializeField, Interface(typeof(IActiveState))] private Object m_activeStateObj;
        private IActiveState m_activeState;

        [Header("Translation")]
        [SerializeField] private Vector3 m_positionOffset;
        [SerializeField] private Vector3 m_rotationOffset;

        [SerializeField, Min(0f)] private float m_scale = 1f;

        private Constraint m_parent;

        public bool pause => m_pause;

        public float scale
        {
            get => m_scale;
            set
            {
                if (m_scale != value)
                {
                    m_scale = value;
                }
            }
        }

        private void Awake()
        {
            if (m_direction == Direction.Send)
                Registry.Register(m_id, this);
        }

        private void Start()
        {
            if (m_direction == Direction.Recv)
                m_parent = Registry.GetByKey(m_id);

            if (m_activeStateObj)
                m_activeState = (IActiveState)m_activeStateObj;
        }

        public void Pause(bool active) => m_pause = active;

        private bool IsActive() => !((m_activeState != null) && !m_activeState.Active);

        private bool IsParentPause() => (m_parent != null) && m_parent.pause;

        private void Update()
        {
            if (!IsActive())
                return;

            if ((m_direction == Direction.Recv) && !(m_pause || IsParentPause()))
            {
                transform.position = m_positionOffset + m_parent.transform.position;
                transform.rotation = m_parent.transform.rotation * Quaternion.Euler(m_rotationOffset);
                transform.localScale = m_parent.transform.localScale * scale;
            }
        }

        private void OnDestroy()
        {
            if (m_direction == Direction.Send)
                Registry.Unregister(m_id);
        }
    }
}
