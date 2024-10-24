using UnityEngine;
using UnityEditor;
using TLab.SFU.Network.Security;

namespace TLab.VRProjct.Editor
{
    [CustomEditor(typeof(Entrance))]
    [CanEditMultipleObjects]

    public class EntranceEditor : UnityEditor.Editor
    {
        private Entrance m_instance;

        private void OnEnable()
        {
            m_instance = target as Entrance;
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            if (GUILayout.Button("Regist Password"))
            {
                var hash = Authentication.GetHashString(m_instance.password);
                m_instance.passwordHash = hash;

                m_instance.password = "";

                EditorUtility.SetDirty(m_instance);
            }
        }
    }
}
