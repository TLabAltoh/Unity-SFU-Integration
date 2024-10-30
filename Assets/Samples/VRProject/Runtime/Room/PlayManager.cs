using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using TLab.VKeyborad;
using TLab.SFU.Network;

namespace TLab.VRProjct
{
    public class PlayManager : MonoBehaviour
    {
        [Header("Menu Panel")]
        [SerializeField] private Transform m_centerEyeAnchor;
        [SerializeField] private Transform m_targetPanel;

        [Header("Keyborad")]
        [SerializeField] private TLabVKeyborad m_keyborad;

        [Header("Network")]
        [SerializeField] private SyncClient m_syncClient;

        private const float HALF = 0.5f;

        public static string ENTRY_SCENE = "ENTRY";
        public static string HOST_SCENE = "HOST";
        public static string GUEST_SCENE = "GUEST";

        private Vector3 cameraPos => Camera.main.transform.position;

        private IEnumerator ExitTask()
        {
            Registry<SyncTransformer>.ClearRegistry();
            Registry<SyncAnimator>.ClearRegistry();

            yield return new WaitForSeconds(0.5f);

            m_syncClient.CloseRTC();

            yield return new WaitForSeconds(0.5f);

            m_syncClient.CloseWS();

            yield return new WaitForSeconds(2.5f);
        }

        private IEnumerator ReEnterTask()
        {
            // TODO:

            //string scene = SyncClient.instance.isHost ? HOST_SCENE : GUEST_SCENE;

            //yield return ExitTask();

            //SceneManager.LoadSceneAsync(scene, LoadSceneMode.Single);

            yield break;
        }

        private IEnumerator BackToTheEntryTask()
        {
            yield return ExitTask();

            SceneManager.LoadSceneAsync(ENTRY_SCENE, LoadSceneMode.Single);

            yield break;
        }

        public void ReEnter()
        {
            StartCoroutine(ReEnterTask());
        }

        public void Exit()
        {
            StartCoroutine(BackToTheEntryTask());
        }

        private Vector3 GetEyeDirectionPos(float xOffset = 0f, float yOffset = 0f, float zOffset = 0f)
        {
            return m_centerEyeAnchor.position +
                    m_centerEyeAnchor.right * xOffset +
                    m_centerEyeAnchor.up * yOffset +
                    m_centerEyeAnchor.forward * zOffset;
        }

        private void SwitchPanel(Transform target, bool active, Vector3 offset)
        {
            target.gameObject.SetActive(active);

            if (active)
            {
                target.transform.position = GetEyeDirectionPos(xOffset: offset.x, yOffset: offset.y, zOffset: offset.z);
                target.LookAt(cameraPos, Vector3.up);
            }
        }

        private bool SwitchPanel(Transform target)
        {
            bool active = target.gameObject.activeSelf;
            SwitchPanel(target, !active, new Vector3(0f, 0f, HALF));

            return !active;
        }

        public void SwitchKeyboradVisibility()
        {
            if (m_keyborad == null)
                return;

            m_keyborad.SetVisibility(!m_keyborad.isVisible);

            if (m_keyborad.isVisible)
            {
                const float SCALE = 0.75f;
                var position = GetEyeDirectionPos(0f, -HALF * SCALE, HALF * SCALE);
                m_keyborad.SetTransform(position, cameraPos, Vector3.up);
            }
        }

        private void Start()
        {
            if (m_targetPanel != null)
                SwitchPanel(m_targetPanel, false, Vector3.zero);
        }

        private void Update()
        {
            if (OVRInput.GetDown(OVRInput.Button.Start))
            {
                if (!SwitchPanel(m_targetPanel))
                {
                    // Guest only has menu panel
                    // TODO:
                    //if (!SyncClient.instance.isHost)
                    //{
                    //    m_keyborad.Hide(true);
                    //}
                }
            }
        }
    }
}
