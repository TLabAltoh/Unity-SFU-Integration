using UnityEngine;
using UnityEditor;

namespace TLab.SFU.Network.Editor
{
    [CustomEditor(typeof(NetworkedId))]
    public class NetworkedIdEditor : UnityEditor.Editor
    {
        private NetworkedId m_instance;

        private void OnEnable() => m_instance = target as NetworkedId;

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            if (GUILayout.Button("Generate Address"))
                m_instance.GenerateAddress();

            if (GUILayout.Button("For All of Scene Object"))
            {
                var ids = FindObjectsOfType<NetworkedId>();
                foreach (var id in ids)
                    id.GenerateAddress();
            }
        }
    }
}
