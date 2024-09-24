using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TLab.NetworkedVR.Network;

namespace TLab.VRClassroom
{
    [RequireComponent(typeof(SyncAnimator))]
    public class MiniTest : MonoBehaviour
    {
        [SerializeField] private SyncAnimator m_windowAnim;
        [SerializeField] private SyncAnimator[] m_graphs;

        [SerializeField] private ToggleGroup m_toggleGroup;
        [SerializeField] private Toggle m_corrent;

        [SerializeField] private Image m_maru;
        [SerializeField] private Image m_batu;

        private const string SWITCH = "Switch";

        private const string RATIO = "Ratio";

        private const int PERFECT_SCORE = 100;

        private const int ZERO_SCORE = 0;

        private IEnumerator Seikai()
        {
            const float DURATION = 0.5f;
            float remain = 0.0f;
            Color prev;

            while (remain < DURATION)
            {
                remain += Time.deltaTime;
                prev = m_maru.color;
                prev.a = remain / DURATION;
                m_maru.color = prev;
                yield return null;
            }

            prev = m_maru.color;
            prev.a = 1.0f;
            m_maru.color = prev;
        }

        private IEnumerator FuSeikai()
        {
            const float DURATION = 0.5f;
            float remain = 0.0f;
            Color prev;

            while (remain < DURATION)
            {
                remain += Time.deltaTime;
                prev = m_batu.color;
                prev.a = remain / DURATION;
                m_batu.color = prev;
                yield return null;
            }

            prev = m_batu.color;
            prev.a = 1.0f;
            m_batu.color = prev;
        }

        public void AnswerCheck()
        {
            Color prev;
            prev = m_batu.color;
            prev.a = 0;
            m_batu.color = prev;

            prev = m_maru.color;
            prev.a = 0;
            m_maru.color = prev;

            if (m_corrent.isOn)
            {
                StartCoroutine(Seikai());
            }
            else
            {
                StartCoroutine(FuSeikai());
            }

            ScoreTabulation.instance.RegistScore(m_corrent.isOn ? PERFECT_SCORE : ZERO_SCORE);
        }

        public void SwitchWindow(bool active)
        {
            m_windowAnim.SetBool(SWITCH, active);

            if (active)
            {
                return;
            }

            for (int i = 0; i < m_graphs.Length; i++)
            {
                var graph = m_graphs[i];
                graph.SetFloat(RATIO, ScoreTabulation.instance.GetScore(i + 1) / (float)PERFECT_SCORE);
            }
        }

        void Reset()
        {
            if (m_windowAnim == null)
            {
                m_windowAnim = GetComponent<SyncAnimator>();
            }
        }

        private void Start()
        {
            m_windowAnim.SetBool(SWITCH, true);
        }
    }
}
