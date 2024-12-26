using UnityEngine;
using TLab.SFU;
using TLab.SFU.Network;

namespace TLab.VRProjct
{
    // Hand and head tracking is disabled when playing
    // from a PC (without Oculus Link) or Android
    // mobile platform. This means that the tracking
    // result will always be Vector3.zero. So here we
    // have to move the avator's head position to the
    // intended position to avoid the avator's head
    // being embedded in the ground.

    public class OVRPlatformBehavior : MonoBehaviour, INetworkClientEventHandler
    {
        [Header("Avator")]
        [SerializeField] private OVRCameraRig m_cameraRig;
        [SerializeField] private CharacterController m_controller;
        [SerializeField, Min(0)] private float m_height = 1.6f;
        [SerializeField] private Constraint[] m_constraints;

        public void OnExit() { }

        public void OnExit(int userId) { }

        public void OnJoin()
        {
            if (!OVRManager.isHmdPresent)
            {
                var newPosition = m_cameraRig.transform.position;
                newPosition.y = m_height;
                m_cameraRig.transform.position = newPosition;

                m_controller.height = 0.2f;
            }
            else
                m_constraints.Foreach((t) => t.Pause(false));
        }

        public void OnJoin(int userId) { }

        protected virtual void OnEnable()
        {
            NetworkClient.RegisterOnJoin(OnJoin, OnJoin);
            NetworkClient.RegisterOnExit(OnExit, OnExit);
        }

        protected virtual void OnDisable()
        {
            NetworkClient.UnRegisterOnJoin(OnJoin, OnJoin);
            NetworkClient.UnRegisterOnExit(OnExit, OnExit);
        }
    }
}
