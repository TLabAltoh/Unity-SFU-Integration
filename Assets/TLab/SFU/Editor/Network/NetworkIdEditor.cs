using UnityEngine;
using UnityEditor;

namespace TLab.SFU.Network.Editor
{
    [CustomEditor(typeof(NetworkId))]
    public class NetworkIdEditor : UnityEditor.Editor
    {
        private NetworkId m_instance;

        private void OnEnable() => m_instance = target as NetworkId;

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            if (GUILayout.Button("Generate Address"))
                m_instance.GenerateAddress();

            if (GUILayout.Button("For All of Scene Object"))
            {
                var ids = FindObjectsOfType<NetworkId>();
                foreach (var id in ids)
                    id.GenerateAddress();
            }
        }
    }
}
