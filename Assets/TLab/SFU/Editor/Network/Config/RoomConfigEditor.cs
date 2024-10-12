using UnityEngine;
using UnityEditor;

namespace TLab.SFU.Network.Editor
{
    [CustomEditor(typeof(RoomConfig))]
    public class RoomConfigEditor : UnityEditor.Editor
    {
        private RoomConfig m_instance;

        private void OnEnable()
        {
            m_instance = target as RoomConfig;
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
        }
    }
}
