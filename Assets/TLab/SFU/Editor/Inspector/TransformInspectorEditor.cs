using UnityEngine;
using UnityEditor;
using System.Reflection;

namespace TLab.SFU.Editor
{
    [SerializeField]
    public struct SerializableTransform
    {
        [SerializeField] private Vector3 m_position;
        [SerializeField] private Quaternion m_rotation;
        [SerializeField] private Vector3 m_scale;

        public Transform GetValue(Transform t)
        {
            t.localPosition = m_position;
            t.localRotation = m_rotation;
            t.localScale = m_scale;
            return t;
        }

        public void SetValue(Transform t)
        {
            m_position = t.localPosition;
            m_rotation = t.localRotation;
            m_scale = t.localScale;
        }
    }

    [CustomEditor(typeof(Transform), true)]
    [CanEditMultipleObjects]
    public class TransformInspectorEditor : UnityEditor.Editor
    {
        // Created to reflect changes in Transform.localPosition while the scene is running, even after play ends.

        private UnityEditor.Editor m_editor;
        private Transform m_param;
        private bool m_set;

        private void OnEnable()
        {
            var transform = target as Transform;
            m_param = transform;

            System.Type t = typeof(EditorApplication).Assembly.GetType("UnityEditor.TransformInspector");
            m_editor = CreateEditor(m_param, t);
        }

        private void OnDisable()
        {
            var disableMethod = m_editor.GetType().GetMethod("OnDisable", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            if (disableMethod != null)
                disableMethod.Invoke(m_editor, null);
            m_param = null;
            DestroyImmediate(m_editor);
        }

        public override void OnInspectorGUI()
        {
            m_editor.OnInspectorGUI();
            if (EditorApplication.isPlaying || EditorApplication.isPaused)
            {
                if (GUILayout.Button("Save"))
                {
                    var s = new SerializableTransform();
                    s.SetValue(m_param);
                    string json = JsonUtility.ToJson(s);
                    EditorPrefs.SetString("Save Param " + m_param.GetInstanceID().ToString(), json);
                    if (!m_set)
                    {
                        EditorApplication.playModeStateChanged += OnChangedPlayMode;
                        m_set = true;
                    }
                }
            }
        }

        private void OnChangedPlayMode(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredEditMode)
            {
                var transform = target as Transform;
                var key = "Save Param " + transform.GetInstanceID().ToString();
                var json = EditorPrefs.GetString(key);
                var t = JsonUtility.FromJson<SerializableTransform>(json);
                EditorPrefs.DeleteKey(key);
                transform = t.GetValue(transform);
                EditorUtility.SetDirty(target);
                EditorApplication.playModeStateChanged -= OnChangedPlayMode;
            }
        }
    }
}