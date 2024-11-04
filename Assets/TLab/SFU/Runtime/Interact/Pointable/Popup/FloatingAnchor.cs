using System.Collections;
using UnityEngine;
using TLab.SFU.Network;

namespace TLab.SFU.Interact
{
    public class FloatingAnchor : NetworkTransform
    {
        public class TransformCache
        {
            public TransformCache(Vector3 localPosition, Vector3 localScale, Quaternion localRotation)
            {
                this.localPosotion = localPosition;
                this.localScale = localScale;
                this.localRotation = localRotation;
            }

            public Vector3 localPosotion;
            public Vector3 localScale;
            public Quaternion localRotation;
        }

        [SerializeField] private Transform m_target;

        [Header("Offset")]
        [SerializeField] private float m_forward;
        [SerializeField] private float m_vertical;
        [SerializeField] private float m_horizontal;

        private TransformCache m_initialTransform;

        const float DURATION = 0.25f;

#if UNITY_EDITOR
        public void SetTarget(Transform taregt)
        {
            m_target = taregt;
        }
#endif

        private void LerpScale(Transform target, TransformCache start, TransformCache end, float lerpValue)
        {
            target.localScale = Vector3.Lerp(start.localScale, end.localScale, lerpValue);
        }

        private IEnumerator FadeInTask()
        {
            var currentTransform = new TransformCache(
                transform.localPosition,
                transform.localScale,
                transform.localRotation);

            var current = 0.0f;
            while (current < DURATION)
            {
                current += Time.deltaTime;
                LerpScale(this.transform, currentTransform, m_initialTransform, current / DURATION);
                yield return null;
            }
        }

        private IEnumerator FadeOutTask()
        {
            var currentTransform = new TransformCache(
                transform.localPosition,
                transform.localScale,
                transform.localRotation);

            var targetTransform = new TransformCache(
                transform.localPosition,
                Vector3.zero,
                transform.localRotation);

            var current = 0.0f;
            while (current < DURATION)
            {
                current += Time.deltaTime;
                LerpScale(this.transform, currentTransform, targetTransform, current / DURATION);
                yield return null;
            }
        }

        public void FadeInAsync() => StartCoroutine(FadeInTask());

        public void FadeOutAsync() => StartCoroutine(FadeOutTask());

        public void FadeOutImmidiately()
        {
            m_initialTransform = new TransformCache(
                this.transform.localPosition,
                this.transform.localScale,
                this.transform.localRotation);

            var targetTransform = new TransformCache(
                this.transform.localPosition,
                Vector3.zero,
                this.transform.localRotation);

            LerpScale(this.transform, m_initialTransform, targetTransform, 1.0f);
        }

        protected override void Start()
        {
            base.Start();

            transform.parent = null;

            FadeOutImmidiately();
        }

        protected override void Update()
        {
            base.Update();

            if (!Const.SEND.HasFlag(m_direction))
                return;

            var mainCamera = Camera.main.transform;
            var diff = mainCamera.position - m_target.position;
            var offset = diff.normalized * m_forward + Vector3.up * m_vertical + Vector3.Cross(diff.normalized, Vector3.up) * m_horizontal;

            transform.position = m_target.position + offset;
            transform.LookAt(mainCamera, Vector3.up);
        }
    }
}
