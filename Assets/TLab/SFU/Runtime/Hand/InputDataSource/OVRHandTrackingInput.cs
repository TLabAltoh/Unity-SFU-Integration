using System.Collections.Generic;
using UnityEngine;
using Oculus.Interaction.Input;

namespace TLab.SFU.Input
{
    [AddComponentMenu("TLab/SFU/OVR Hand Tracking Input (TLab)")]
    public class OVRHandTrackingInput : InputDataSource
    {
        [System.Serializable]
        public struct Gesture
        {
            public string name;
            public List<Vector3> fingerDatas;
        }

        [Header("OVR Hand")]

        [SerializeField] private HandRef m_hand;

        [SerializeField] private FromOVRHandDataSource m_dataSource;

        [Header("Gesture")]

        [SerializeField] private OVRSkeleton m_skeleton;

        [SerializeField] private List<Gesture> m_gestures;

        [SerializeField] private List<string> m_grabGestures;

        [SerializeField] private List<string> m_rayHideGestures;

        [SerializeField] private float THRESHOLD = 0.1f;

#if UNITY_EDITOR
        [SerializeField] private bool m_editMode = false;

        [SerializeField] private KeyCode m_recordKey = KeyCode.Space;
#endif

        private bool m_skeltonInitialized = false;

        private bool m_grabFired = false;

        private bool m_triggerFired = false;

        private List<OVRBone> m_fingerBones;

        private const string DEFAULT_GESTUR_NAME = "New Gesture";

        private const float AVERAGE = 0.5f;

        public bool rayHide
        {
            get
            {
                bool active = false;
                m_rayHideGestures.ForEach((g) =>
                {
                    active |= (g == m_currentGesture);
                });

                return active;
            }
        }

        private string DetectGesture()
        {
            string result = null;

            float currentMin = Mathf.Infinity;

            foreach (var gesture in m_gestures)
            {
                float sumDistance = 0.0f;

                bool isDiscarded = false;

                for (int i = 0; i < m_fingerBones.Count; i++)
                {
                    Vector3 currentData = m_skeleton.transform.InverseTransformPoint(m_fingerBones[i].Transform.position);
                    float distance = Vector3.Distance(currentData, gesture.fingerDatas[i]);

                    if (distance > THRESHOLD)
                    {
                        isDiscarded = true;
                        break;
                    }

                    sumDistance += distance;
                }

                if (!isDiscarded && sumDistance < currentMin)
                {
                    currentMin = sumDistance;

                    result = gesture.name;
                }
            }

            return result;
        }

#if UNITY_EDITOR
        private void SavePose()
        {
            Gesture g = new Gesture();

            g.name = DEFAULT_GESTUR_NAME;

            List<Vector3> data = new List<Vector3>();
            foreach (var bone in m_fingerBones)
            {
                data.Add(m_skeleton.transform.InverseTransformPoint(bone.Transform.position));
            }

            g.fingerDatas = data;
            m_gestures.Add(g);
        }
#endif

        void Start()
        {

        }

        void Update()
        {
#if UNITY_EDITOR
            if (m_editMode)
            {
                if (UnityEngine.Input.GetKeyDown(m_recordKey))
                {
                    SavePose();
                }
            }
#endif

            // Update Pose

            var dataAsset = m_dataSource.GetData();

            m_hand.GetRootPose(out m_rootPose);

            m_hand.GetPointerPose(out m_pointerPose);

            // Detect Trigger Press

            bool triggerFired = dataAsset.IsFingerPinching[(int)OVRHand.HandFinger.Index];
            bool prevTriggerFired = m_triggerFired;

            m_triggerFired = triggerFired;

            m_pressed = m_triggerFired;

            m_onPress = false;

            m_onRelease = false;

            if (triggerFired && !prevTriggerFired)
            {
                m_onPress = true;
            }
            else if (!triggerFired && prevTriggerFired)
            {
                m_onRelease = true;
            }

            // Detect Grab Press

            if ((m_skeleton != null) && m_skeltonInitialized)
            {
                m_currentGesture = DetectGesture();

                bool grabFired = false;

                m_grabGestures.ForEach((g) =>
                {
                    grabFired = grabFired || (m_currentGesture == g);
                });

                bool prevGrabFired = m_grabFired;

                m_grabFired = grabFired;

                m_grabbed = m_grabFired;

                m_onGrab = false;

                m_onFree = false;

                if (grabFired && !prevGrabFired)
                {
                    m_onGrab = true;
                }
                else if (!grabFired && prevGrabFired)
                {
                    m_onFree = true;
                }
            }
            else
            {
                // https://communityforums.atmeta.com/t5/Unity-VR-Development/Bones-list-is-empty/td-p/880261
                if (m_skeleton.Bones.Count > 0)
                {
                    m_fingerBones = new List<OVRBone>(m_skeleton.Bones);
                    m_skeltonInitialized = true;
                }
            }
        }
    }
}
