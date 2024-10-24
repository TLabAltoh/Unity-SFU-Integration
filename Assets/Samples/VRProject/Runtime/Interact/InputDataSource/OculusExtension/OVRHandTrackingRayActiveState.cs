using UnityEngine;
using Oculus.Interaction;

namespace TLab.VRProjct
{
    public class OVRHandTrackingRayActiveState : MonoBehaviour, IActiveState
    {
        [SerializeField] private OVRHandTrackingInput m_input;

        public bool Active => !m_input.rayHide;
    }
}
