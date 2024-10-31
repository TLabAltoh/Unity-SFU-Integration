using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using TLab.VKeyborad;
using TLab.SFU.Network.Security;

namespace TLab.VRProjct
{
    public class Entrance : MonoBehaviour
    {
        [Header("Password")]
        public string password;
        public string passwordHash;

        [Header("Input")]
        [SerializeField] private InputField m_addrInput;

        public string THIS_NAME => "[" + this.GetType() + "] ";

        private IEnumerator ChangeScene(string scene)
        {
            yield return new WaitForSeconds(1.5f);

            SceneManager.LoadSceneAsync(scene, LoadSceneMode.Single);
        }

        public void Enter()
        {
            var (addr, argments) = CommandLine.Parse(m_addrInput.text);

            var scene = PlayManager.GUEST_SCENE;

            if (argments.ContainsKey("p"))
            {
                var password = argments["p"];

                scene = Authentication.ConfirmPassword(password, passwordHash) ? PlayManager.HOST_SCENE : scene;
            }

            StartCoroutine(ChangeScene(scene));
        }

        public void Exit()
        {
            Application.Quit();
        }
    }
}