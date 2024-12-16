using UnityEngine;
using UnityEngine.SceneManagement;

namespace TLab.VRProjct
{
    public class WorldLauncher : MonoBehaviour
    {
        private static WorldLauncher m_instance;
        public static WorldLauncher instance => m_instance;

        public static void Move(string world) => SceneManager.LoadSceneAsync(world, LoadSceneMode.Single);

        private void Awake() => m_instance = this;
    }
}