using UnityEngine;
using UnityEditor;
using System.Reflection;

namespace TLab.Editor
{
    [SerializeField]
    public class SaveTransform
    {
        [SerializeField] private Vector3 position;
        [SerializeField] private Quaternion rotation;
        [SerializeField] private Vector3 scale;
        public Transform GetValue(Transform t)
        {
            t.localPosition = position;
            t.localRotation = rotation;
            t.localScale = scale;
            return t;
        }

        public void SetValue(Transform t)
        {
            position = t.localPosition;
            rotation = t.localRotation;
            scale = t.localScale;
        }
    }

    [CustomEditor(typeof(Transform), true)]
    [CanEditMultipleObjects]
    public class TransformCustomInspectorEditor : UnityEditor.Editor
    {
        /**
         * Created to reflect changes in Transform.localPosition while the scene is running, even after play ends
         * Created to reflect changes in Transform.localPosition while the scene is running, even after play ends
         */

        private UnityEditor.Editor m_editor;
        private Transform m_param;
        private bool m_set;

        private void OnEnable()
        {
            Transform transform = target as Transform;
            m_param = transform;

            System.Type t = typeof(UnityEditor.EditorApplication).Assembly.GetType("UnityEditor.TransformInspector");
            m_editor = UnityEditor.Editor.CreateEditor(m_param, t);
        }

        private void OnDisable()
        {
            MethodInfo disableMethod = m_editor.GetType().GetMethod("OnDisable", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            if (disableMethod != null)
            {
                disableMethod.Invoke(m_editor, null);
            }
            m_param = null;
            DestroyImmediate(m_editor);
        }

        public override void OnInspectorGUI()
        {
            m_editor.OnInspectorGUI();
            if (EditorApplication.isPlaying || EditorApplication.isPaused)
            {
                if (GUILayout.Button("Save Current State"))
                {
                    SaveTransform s = new SaveTransform();
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
                Transform transform = target as Transform;
                string key = "Save Param " + transform.GetInstanceID().ToString();
                string json = EditorPrefs.GetString(key);
                SaveTransform t = JsonUtility.FromJson<SaveTransform>(json);
                EditorPrefs.DeleteKey(key);
                transform = t.GetValue(transform);
                EditorUtility.SetDirty(target);
                EditorApplication.playModeStateChanged -= OnChangedPlayMode;
            }
        }
    }
}