using UnityEditor;

namespace TLab.SFU.Network.Editor
{
    [CustomEditor(typeof(NetworkObject), true)]
    [CanEditMultipleObjects]
    public class NetworkObjectEditor : UnityEditor.Editor
    {
        private NetworkObject m_object;

        protected void OnEnable() => Init();

        protected virtual void Init() => m_object = target as NetworkObject;
    }
}