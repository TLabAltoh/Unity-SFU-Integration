using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TLab.SFU.Network;

namespace TLab.VRProjct
{
    [RequireComponent(typeof(NetworkAnimator))]
    public class MiniTest : MonoBehaviour
    {
        [SerializeField] private NetworkAnimator m_windowAnim;
        [SerializeField] private NetworkAnimator[] m_graphs;

        [SerializeField] private ToggleGroup m_toggleGroup;
        [SerializeField] private Toggle m_corrent;

        [SerializeField] private Image m_maru;
        [SerializeField] private Image m_batu;

        private const string RESULT = "Result";

        private const string SCORE = "Score";

        private IEnumerator Seikai()
        {
            const float DUSCOREN = 0.5f;
            float remain = 0.0f;
            Color prev;

            while (remain < DUSCOREN)
            {
                remain += Time.deltaTime;
                prev = m_maru.color;
                prev.a = remain / DUSCOREN;
                m_maru.color = prev;
                yield return null;
            }

            prev = m_maru.color;
            prev.a = 1.0f;
            m_maru.color = prev;
        }

        private IEnumerator FuSeikai()
        {
            const float DUSCOREN = 0.5f;
            float remain = 0.0f;
            Color prev;

            while (remain < DUSCOREN)
            {
                remain += Time.deltaTime;
                prev = m_batu.color;
                prev.a = remain / DUSCOREN;
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
                StartCoroutine(Seikai());
            else
                StartCoroutine(FuSeikai());

            ScoreTabulation.instance.RegistScore(m_corrent.isOn ? 100 : 0);
        }

        public void SwitchWindow(bool active)
        {
            m_windowAnim.SetBool(RESULT, active);

            if (active)
                return;

            for (int i = 0; i < m_graphs.Length; i++)
            {
                var graph = m_graphs[i];
                graph.SetFloat(SCORE, ScoreTabulation.instance.GetScore(i + 1) / (float)100);
            }
        }

        void Reset()
        {
            if (m_windowAnim == null)
                m_windowAnim = GetComponent<NetworkAnimator>();
        }

        private void Start() => m_windowAnim.SetBool(RESULT, true);
    }
}
