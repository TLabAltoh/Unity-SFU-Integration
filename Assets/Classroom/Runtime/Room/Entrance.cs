using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using TLab.VKeyborad;
using TLab.NetworkedVR.Network;
using TLab.NetworkedVR.Network.Util;
using TLab.NetworkedVR.Network.Security;

namespace TLab.VRClassroom
{
    public class Entrance : MonoBehaviour
    {
        [Header("Config")]
        [SerializeField] private RoomConfig m_roomConfig;
        [SerializeField] private UserConfig m_userConfig;

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

        public void UpdateConfig(string ipAddr)
        {

        }

        public string GetIPAddr()
        {
            var splited = m_addrInput.text.Split(" ");

            return splited[0];
        }

        public Dictionary<string, string> GetOptions()
        {
            // 192.168.1.1 -p 1234 -a 1234 -b 1234 ...

            var splited = m_addrInput.text.Split(" ");

            var argment = "";
            for (int i = 1; i < splited.Length; i++)
            {
                argment += splited[i] + " ";
            }

            var options = Command.ParseCommand(argment);

            return options;
        }

        public void EnterDemoScene()
        {
            string scene = Classroom.DEMO_SCENE;

            UpdateConfig(GetIPAddr());

            StartCoroutine(ChangeScene(scene));
        }

        public void Enter()
        {
            var ipAddr = GetIPAddr();
            var options = GetOptions();

            var scene = Classroom.GUEST_SCENE;

            if (options.ContainsKey("p"))
            {
                var password = options["p"];

                scene = Authentication.ConfirmPassword(password, passwordHash) ? Classroom.HOST_SCENE : scene;
            }

            UpdateConfig(ipAddr);

            StartCoroutine(ChangeScene(scene));
        }

#if UNITY_EDITOR
        public void PasswordTest(string argments)
        {
            var ipAddr = GetIPAddr();

            Debug.Log(THIS_NAME + $"Ip addr: {ipAddr}");

            var options = GetOptions();

            var password = options["p"];

            Debug.Log(THIS_NAME + $"Password: {password}");

            var scene = Authentication.ConfirmPassword(password, passwordHash) ? Classroom.HOST_SCENE : Classroom.GUEST_SCENE;

            Debug.Log(THIS_NAME + $"Scene: {scene}");
        }
#endif

        public void Exit()
        {
            Application.Quit();
        }
    }
}