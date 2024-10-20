/**
 * This code is mostly based on the AudioSpectrumView from com.unity.webrtc.
 **/

using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TLab.SFU.Sample
{
    internal static class AudioSettingsUtility
    {
        static Dictionary<AudioSpeakerMode, int> pairs =
            new Dictionary<AudioSpeakerMode, int>()
        {
            {AudioSpeakerMode.Mono, 1},
            {AudioSpeakerMode.Stereo, 2},
            {AudioSpeakerMode.Quad, 4},
            {AudioSpeakerMode.Surround, 5},
            {AudioSpeakerMode.Mode5point1, 6},
            {AudioSpeakerMode.Mode7point1, 8},
            {AudioSpeakerMode.Prologic, 2},
        };
        public static int SpeakerModeToChannel(AudioSpeakerMode mode)
        {
            return pairs[mode];
        }
    }

    public class AudioSpectrumView : MonoBehaviour
    {
        [SerializeField] private AudioSource m_target;

        private LineRenderer m_line;
        private Vector3 m_rectAnchor;
        private Vector2 m_rectSize;

        private const float X_RATIO = 1f;
        private const float Y_RATIO = 1f;
        private readonly Color[] m_lineColors = new Color[] { Color.green, Color.yellow };

        private const int POSIITON_COUNT = 256;
        private float[] m_spectrum = new float[2048];

        private Vector3[] m_array;
        private List<LineRenderer> m_lines = new List<LineRenderer>();

        private void OnDestroy()
        {
            AudioSettings.OnAudioConfigurationChanged -= OnAudioConfigurationChanged;
        }

        private void OnAudioConfigurationChanged(bool deviceChanged)
        {
            var conf = AudioSettings.GetConfiguration();
            var count = AudioSettingsUtility.SpeakerModeToChannel(conf.speakerMode);
            ResetLines(count);
        }

        private void ResetLines(int channelCount)
        {
            foreach (var line in m_lines)
            {
                Object.Destroy(line.gameObject);
            }
            m_lines.Clear();
            for (int i = 0; i < channelCount; i++)
            {
                var line_ = GameObject.Instantiate(m_line, m_line.transform.parent);
                line_.gameObject.SetActive(true);
                line_.positionCount = POSIITON_COUNT;
                line_.startColor = m_lineColors[i];
                line_.endColor = m_lineColors[i];
                m_lines.Add(line_);
            }
        }

        private void RecalculateRect()
        {
            var count = m_target.transform.parent.childCount;

            int offset = 0;

            for (int i = 0; i < count; i++)
                if (m_target.transform.parent.GetChild(i) == m_target.transform)
                    offset = i;

            var lb = Camera.main.ViewportToWorldPoint(new Vector3((offset + 0) / (float)count, 0.0f, 3));
            var rb = Camera.main.ViewportToWorldPoint(new Vector3((offset + 1) / (float)count, 0.0f, 3));
            var lt = Camera.main.ViewportToWorldPoint(new Vector3((offset + 0) / (float)count, 0.5f, 3));

            m_rectAnchor = lb;
            m_rectSize = new Vector2(rb.x - lb.x, lt.y - lb.y);
        }

        private void Start()
        {
            m_line = GetComponentInChildren<LineRenderer>();

            m_array = new Vector3[POSIITON_COUNT];

            RecalculateRect();

            // This line object is used as a template.
            if (m_line.gameObject.activeInHierarchy)
                m_line.gameObject.SetActive(false);

            var conf = AudioSettings.GetConfiguration();
            var count = AudioSettingsUtility.SpeakerModeToChannel(conf.speakerMode);
            ResetLines(count);

            AudioSettings.OnAudioConfigurationChanged += OnAudioConfigurationChanged;
        }

        private void Update()
        {
            for (int lineIndex = 0; lineIndex < m_lines.Count; lineIndex++)
            {
                m_target.GetSpectrumData(m_spectrum, lineIndex, FFTWindow.Rectangular);
                m_array[0] = new Vector3(m_rectAnchor.x, m_rectAnchor.y, m_rectAnchor.z);
                for (int i = 1; i < m_array.Length; i++)
                {
                    var x = m_rectSize.x * i / m_array.Length * X_RATIO;
                    var y = m_rectSize.y * Mathf.Log(m_spectrum[i] + 1) * Y_RATIO;
                    m_array[i] = new Vector3(m_rectAnchor.x + x, m_rectAnchor.y + y, m_rectAnchor.z);
                }
                m_lines[lineIndex].SetPositions(m_array);
            }
        }
    }
}
