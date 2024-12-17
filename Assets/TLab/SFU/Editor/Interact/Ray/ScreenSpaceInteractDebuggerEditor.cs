using UnityEditor;

namespace TLab.SFU.Interact.Editor
{
    [CustomEditor(typeof(ScreenSpaceInteractDebugger))]
    public class ScreenSpaceInteractDebuggerEditor : UnityEditor.Editor
    {
        private ScreenSpaceInteractDebugger m_interactor;

        private void OnEnable() => m_interactor = target as ScreenSpaceInteractDebugger;
    }
}
