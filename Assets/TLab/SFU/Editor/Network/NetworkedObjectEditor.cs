using UnityEngine;
using UnityEditor;

namespace TLab.SFU.Network.Editor
{
    [CustomEditor(typeof(NetworkedObject))]
    public class NetworkedObjectEditor : UnityEditor.Editor
    {
        private NetworkedObject m_instance;

        private void OnEnable()
        {
            m_instance = target as NetworkedObject;
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
        }
    }
}
