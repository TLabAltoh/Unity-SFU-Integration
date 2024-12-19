using System.Collections;
using UnityEngine;
using TLab.SFU.Network;

namespace TLab.SFU.Interact
{
    public class FloatingAnchor : MonoBehaviour
    {
        [SerializeField] private bool m_show = true;
        [SerializeField] private bool m_fitScale = false;
        [SerializeField, Min(0f)] float m_duration = 0.25f;
        [SerializeField] private Transform m_target;

        [Header("Offset")]
        [SerializeField] private float m_forward;
        [SerializeField] private float m_vertical;
        [SerializeField] private float m_horizontal;

        private Vector3 m_originalLocalScale;
        private Vector3 m_targetOriginalScale;

        public bool show
        {
            get => m_show;
            set
            {
                if (m_show != value)
                {
                    m_show = value;

                    FadeImmidiately(m_show ? 1 : 0);
                }
            }
        }

#if UNITY_EDITOR
        public void SetHideOnStart() => m_show = false;

        public void SetTarget(Transform taregt) => m_target = taregt;
#endif

        private void LerpScale(Vector3 start, Vector3 end, float lerpValue)
        {
            this.transform.localScale = Vector3.Lerp(start, end, lerpValue);
        }

        private IEnumerator FadeTask(float t)
        {
            var start = transform.localScale;
            var target = t * m_originalLocalScale;

            var time = 0.0f;
            while (time < m_duration)
            {
                time += Time.deltaTime;
                LerpScale(start, target, time / m_duration);
                yield return null;
            }
        }

        public void Fade(float t) => StartCoroutine(FadeTask(t));

        public void FadeImmidiately(float t) => LerpScale(this.transform.localScale, Vector3.zero, t);

        protected void Start()
        {
            transform.parent = null;

            m_originalLocalScale = transform.localScale;
            m_targetOriginalScale = m_target.lossyScale;

            if (!m_show)
                FadeImmidiately(0);
        }

        protected void Update()
        {
            var camera = Camera.main.transform;
            var diff = camera.position - m_target.position;
            var offset = diff.normalized * m_forward + Vector3.up * m_vertical + Vector3.Cross(diff.normalized, Vector3.up) * m_horizontal;

            if (m_fitScale)
            {
                offset.x *= m_target.lossyScale.x / m_targetOriginalScale.x;
                offset.y *= m_target.lossyScale.y / m_targetOriginalScale.y;
                offset.z *= m_target.lossyScale.z / m_targetOriginalScale.z;
            }

            transform.position = m_target.position + offset;
            transform.LookAt(camera, Vector3.up);
        }
    }
}
