using System.Collections;
using UnityEngine;
using TLab.SFU.Network;

namespace TLab.SFU.Interact
{
    public class FloatingAnchor : MonoBehaviour
    {
        [SerializeField] private bool m_hideOnStart;
        [SerializeField] private Transform m_target;

        [Header("Offset")]
        [SerializeField] private float m_forward;
        [SerializeField] private float m_vertical;
        [SerializeField] private float m_horizontal;

        private SerializableTransform m_initial;

        const float DURATION = 0.25f;

#if UNITY_EDITOR
        public void SetTarget(Transform taregt)
        {
            m_target = taregt;
        }

        public void SetHideOnStart() => m_hideOnStart = true;
#endif

        private void LerpScale(Transform target, SerializableTransform start, SerializableTransform end, float lerpValue)
        {
            target.localScale = Vector3.Lerp(start.localScale, end.localScale, lerpValue);
        }

        private IEnumerator FadeTask(SerializableTransform target)
        {
            m_initial = new SerializableTransform(this.transform.localPosition, this.transform.localRotation, this.transform.localScale);

            var current = new SerializableTransform(transform.localPosition, transform.localRotation, transform.localScale);

            var time = 0.0f;
            while (time < DURATION)
            {
                time += Time.deltaTime;
                LerpScale(this.transform, current, target, time / DURATION);
                yield return null;
            }
        }

        public void FadeInAsync() => StartCoroutine(FadeTask(m_initial));

        public void FadeOutAsync() => StartCoroutine(FadeTask(new SerializableTransform(transform.localPosition, transform.localRotation, Vector3.zero)));

        public void FadeOutImmidiately()
        {
            m_initial = new SerializableTransform(this.transform.localPosition, this.transform.localRotation, this.transform.localScale);

            var target = new SerializableTransform(this.transform.localPosition, this.transform.localRotation, Vector3.zero);

            LerpScale(this.transform, m_initial, target, 1.0f);
        }

        protected void Start()
        {
            transform.parent = null;

            m_initial = new SerializableTransform(this.transform.localPosition, this.transform.localRotation, this.transform.localScale);

            if (m_hideOnStart)
                FadeOutImmidiately();
        }

        protected void Update()
        {
            var camera = Camera.main.transform;
            var diff = camera.position - m_target.position;
            var offset = diff.normalized * m_forward + Vector3.up * m_vertical + Vector3.Cross(diff.normalized, Vector3.up) * m_horizontal;

            transform.position = m_target.position + offset;
            transform.LookAt(camera, Vector3.up);
        }
    }
}
