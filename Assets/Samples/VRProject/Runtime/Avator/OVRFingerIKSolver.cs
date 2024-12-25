using System;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace TLab.VRProjct.Avator
{
    [ExecuteInEditMode]
    public class OVRFingerIKSolver : MonoBehaviour
    {
        [Serializable]
        public class Bone
        {
            public Transform bone;
            public Transform target;

            [HideInInspector] public float length;
            [HideInInspector] public Vector3 origPos, origScale;
            [HideInInspector] public Quaternion origRot;
        }

        public enum HandType
        {
            LeftHand,
            RightHand,
        };

        [Header("Bones - Leaf to Root")]
        [Tooltip("Make sure you assign them in leaf to root order only...")]
        public Bone[] bones;
        [Tooltip("The end point of the leaf bone positioned at tip of the chain to get its orientation...")]
        public Transform endPointOfLastBone;

        [Header("Settings")]
        [Tooltip("Bend chain towards this target when target is closer than total chain length... Make sure you place it far than total chain length for better accuracy...")]
        public Transform poleTarget;
        public Transform axisTarget;
        [Tooltip("More precision...")]
        public int iterations;
        public HandType handType;

        [Header("EditMode")]
        public bool enable;

        [HideInInspector]
        public bool needResetOption = false;

        private Vector3 m_lastTargetPosition;
        private bool m_editorInitialized = false;

        private void Start()
        {
            m_lastTargetPosition = transform.position;
            if (Application.isPlaying && !m_editorInitialized)
                Initialize();
        }

        public void Setup()
        {
            this.transform.parent.position = bones[bones.Length - 1].bone.position;
            this.transform.parent.rotation = bones[bones.Length - 1].bone.rotation;

            this.transform.position = bones[0].target.position;
            this.transform.rotation = bones[bones.Length - 1].target.rotation;

            axisTarget.position = bones[bones.Length - 1].bone.position;
            axisTarget.rotation = bones[bones.Length - 1].bone.rotation;

#if UNITY_EDITOR
            EditorUtility.SetDirty(this);
#endif

            for (int i = 0; i < bones.Length; i++)
            {
                bones[i].bone.position = bones[i].target.position;
                bones[i].bone.rotation = bones[i].target.rotation;

#if UNITY_EDITOR
                EditorUtility.SetDirty(bones[i].bone);
#endif
            }

            endPointOfLastBone.position = bones[0].target.position;
            endPointOfLastBone.rotation = bones[0].target.rotation;

            poleTarget.position = bones[1].target.position;
            poleTarget.rotation = bones[1].target.rotation;

#if UNITY_EDITOR
            EditorUtility.SetDirty(endPointOfLastBone);
            EditorUtility.SetDirty(poleTarget);
#endif
        }

        private void Update()
        {
            if (Application.isEditor && enable && !m_editorInitialized)
            {
                if (enable)
                {
                    if (bones.Length == 0)
                    {
                        enable = false;
                        return;
                    }
                    for (int i = 0; i < bones.Length; i++)
                    {
                        if (bones[i].bone == null)
                        {
                            enable = false;
                            return;
                        }
                    }
                    if (endPointOfLastBone == null)
                    {
                        enable = false;
                        return;
                    }
                    if (poleTarget == null)
                    {
                        enable = false;
                        return;
                    }
                }
                Initialize();
            }
            if (m_lastTargetPosition != transform.position)
            {
                if (Application.isPlaying || (Application.isEditor && enable))
                    Solve();
            }
        }

        private void Initialize()
        {
            bones[0].origPos = bones[0].bone.position;
            bones[0].origScale = bones[0].bone.localScale;
            bones[0].origRot = bones[0].bone.rotation;
            bones[0].length = Vector3.Distance(endPointOfLastBone.position, bones[0].bone.position);

            var g = new GameObject();
            g.name = bones[0].bone.name;
            g.transform.position = bones[0].bone.position;
            g.transform.forward = -(endPointOfLastBone.position - bones[0].bone.position).normalized;
            g.transform.parent = bones[0].bone.parent;

            bones[0].bone.parent = g.transform;
            bones[0].bone = g.transform;

            for (int i = 1; i < bones.Length; i++)
            {
                bones[i].origPos = bones[i].bone.position;
                bones[i].origScale = bones[i].bone.localScale;
                bones[i].origRot = bones[i].bone.rotation;
                bones[i].length = Vector3.Distance(bones[i - 1].bone.position, bones[i].bone.position);

                g = new GameObject();
                g.name = bones[i].bone.name;
                g.transform.position = bones[i].bone.position;
                g.transform.forward = -(bones[i - 1].bone.position - bones[i].bone.position).normalized;
                g.transform.parent = bones[i].bone.parent;

                bones[i].bone.parent = g.transform;
                bones[i].bone = g.transform;
            }
            m_editorInitialized = true;
            needResetOption = true;
        }

        private Quaternion LookForward(Vector3 forward, Vector3 left) => Quaternion.LookRotation(forward, Vector3.Cross(forward, left).normalized);

        private void OnDrawGizmos()
        {
            if (bones.Length == 0 || axisTarget == null)
                return;

            var root = bones[bones.Length - 1].bone.position;
            var left = Vector3.Cross(axisTarget.up, (transform.position - root).normalized).normalized;

            Gizmos.color = Color.white;
            Gizmos.DrawLine(axisTarget.position, axisTarget.position + left * 0.01f);

            Gizmos.color = Color.magenta;
            Gizmos.DrawLine(axisTarget.position, axisTarget.position + axisTarget.up * 0.01f);

            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(axisTarget.position, transform.position);
        }

        private void Solve()
        {
            var root = bones[bones.Length - 1].bone.position;
            var left = Vector3.Cross(axisTarget.up, (transform.position - axisTarget.position).normalized).normalized;

            bones[bones.Length - 1].bone.rotation = LookForward(-(poleTarget.position - root).normalized, left);
            for (int i = bones.Length - 2; i >= 0; i--)
            {
                bones[i].bone.position = bones[i + 1].bone.position + -(Vector3.ProjectOnPlane(bones[i + 1].bone.forward, left) * bones[i + 1].length);
                bones[i].bone.rotation = LookForward(-(poleTarget.position - bones[i].bone.position).normalized, left);
            }
            for (int i = 0; i < iterations; i++)
            {
                bones[0].bone.rotation = LookForward(-(transform.position - bones[0].bone.position).normalized, left);
                bones[0].bone.position = transform.position + (Vector3.ProjectOnPlane(bones[0].bone.forward, left) * bones[0].length);
                for (int j = 1; j < bones.Length; j++)
                {
                    bones[j].bone.rotation = LookForward(-(bones[j - 1].bone.position - bones[j].bone.position).normalized, left);
                    bones[j].bone.position = bones[j - 1].bone.position + (Vector3.ProjectOnPlane(bones[j].bone.forward, left) * bones[j].length);
                }

                bones[bones.Length - 1].bone.position = root;
                for (int j = bones.Length - 2; j >= 0; j--)
                    bones[j].bone.position = bones[j + 1].bone.position - (Vector3.ProjectOnPlane(bones[j + 1].bone.forward, left) * bones[j + 1].length);
            }
            m_lastTargetPosition = transform.position;

            RotateTarget();
        }

        public void RotateTarget()
        {
            var root = bones[bones.Length - 1].bone.position;
            var root2Handle = transform.position - root;
            var root2Pole = poleTarget.position - root;

            var left = Vector3.Cross(root2Handle, root2Pole).normalized;

            var flipY = (handType == HandType.RightHand) ? -1 : 1;

            //for (int i = 0; i < bones.Length; i++)
            //{
            //    var boneForward = flipY * bones[i].bone.forward;
            //    bones[i].target.right = boneForward;
            //    var targetLeft = bones[i].target.forward;
            //    var angle = Vector3.SignedAngle(targetLeft, left, boneForward);
            //    var rot = Quaternion.AngleAxis(angle, boneForward);
            //    bones[i].target.rotation = rot * bones[i].target.rotation;
            //}
        }

        /// <summary>
        /// Do not ever call this in Play mode. It will mess up the IK system.
        /// </summary>
        public void ResetHierarchy()
        {
            for (int i = 0; i < bones.Length; i++)
            {
                var t = bones[i].bone.GetChild(0);
                bones[i].bone.GetChild(0).parent = bones[i].bone.parent;
                if (Application.isPlaying)
                    Destroy(bones[i].bone.gameObject);
                else
                    DestroyImmediate(bones[i].bone.gameObject);
                bones[i].bone = t;
                t.position = bones[i].origPos;
                t.rotation = bones[i].origRot;
                t.localScale = bones[i].origScale;
            }
            m_lastTargetPosition = Vector3.zero;
            enable = false;
            m_editorInitialized = false;
            needResetOption = false;
        }
    }
}