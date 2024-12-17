using UnityEngine;

namespace TLab.VRProjct
{
    public class Permission : MonoBehaviour
    {
        public void RequestMicPermission()
        {
#if UNITY_EDITOR && UNITY_ANDROID
            if (!UnityEngine.Android.Permission.HasUserAuthorizedPermission(UnityEngine.Android.Permission.Microphone))
                UnityEngine.Android.Permission.RequestUserPermission(UnityEngine.Android.Permission.Microphone);
#endif
        }
    }
}
