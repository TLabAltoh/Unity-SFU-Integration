using UnityEngine;
using Oculus.Interaction.Input;
using TLab.SFU;

namespace TLab.VRProjct
{
    public class OVRHandActiveState : MonoBehaviour, IActiveState
    {
        [SerializeField] private Handedness m_hand;

        public bool Active
        {
            get
            {
                switch (m_hand)
                {
                    case Handedness.Left:
                        if (!OVRHandTrackingInput.left)
                            return false;
                        return OVRHandTrackingInput.left.isConnected;
                    case Handedness.Right:
                        if (!OVRHandTrackingInput.right)
                            return false;
                        return OVRHandTrackingInput.right.isConnected;
                }
                return false;
            }
        }
    }
}
