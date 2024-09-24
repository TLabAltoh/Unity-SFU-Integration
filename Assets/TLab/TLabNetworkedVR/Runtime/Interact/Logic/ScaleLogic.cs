using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TLab.NetworkedVR.Interact
{
    [System.Serializable]
    public class ScaleLogic
    {
        public class LinkPair
        {
            public enum HandleAxis
            {
                NONE,
                X,
                Y,
                Z
            };

            public HandleAxis handleAxis;
            public Vector3 iniScale;
            public GameObject handle;
        }

        [SerializeField] private bool m_enabled = true;
        [SerializeField] private bool m_smooth = false;

        [SerializeField] private Vector2 m_scaleXLim = new Vector2(0.5f, 2.0f);
        [SerializeField] private Vector2 m_scaleYLim = new Vector2(0.5f, 2.0f);
        [SerializeField] private Vector2 m_scaleZLim = new Vector2(0.5f, 2.0f);

        [SerializeField]
        [Range(0.01f, 1f)]
        private float m_lerp = 0.1f;

        [SerializeField] private bool m_useLinkHandle = true;
        [SerializeField] private bool m_useEdgeHandle = true;
        [SerializeField] private bool m_useCornerHandle = true;

        [SerializeField] private GameObject m_linkHandle;
        [SerializeField] private GameObject m_edgeHandle;
        [SerializeField] private GameObject m_cornerHandle;

        [SerializeField] private Vector3 m_boundBoxSize = Vector3.one;

        private Transform m_targetTransform;
        private Rigidbody m_targetRigidbody;

        private Vector3 m_linkHandleIniScale;
        private Vector3 m_edgeHandleIniScale;
        private Vector3 m_cornerHandleIniScale;

        private Vector3 m_initialGrabPoint;
        private Vector3 m_currentGrabPoint;

        private Vector3 m_initialScaleOnGrabStart;
        private Vector3 m_initialPositionOnGrabStart;

        private Vector3 m_diagonalDir;
        private Vector3 m_oppositeCorner;

        private Interactor m_mainHand;
        private Interactor m_subHand;

        private const int REST_INTERVAL = 2;
        private int m_fcnt = 0;

        private float m_initialDist = 0.0f;

        private List<LinkPair> m_linkHandles = new List<LinkPair>();
        private List<GameObject> m_edgeHandles = new List<GameObject>();
        private List<GameObject> m_cornerHandles = new List<GameObject>();
        private ScaleHandle m_handleSelected;

        public bool enabled
        {
            get => m_enabled;
            set
            {
                m_enabled = value;

                m_cornerHandles.ForEach((obj) => obj.SetActive(m_enabled && m_useCornerHandle));

                m_edgeHandles.ForEach((obj) => obj.SetActive(m_enabled && m_useEdgeHandle));

                m_linkHandles.ForEach((pair) => pair.handle.SetActive(m_enabled && m_useLinkHandle));
            }
        }

        public bool smooth
        {
            get => m_smooth;
            set
            {
                m_smooth = value;
            }
        }

        public float lerp
        {
            get => m_lerp;
            set
            {
                m_lerp = Mathf.Clamp(0.01f, 1f, value);
            }
        }

        public bool useLinkHandle
        {
            get => m_useLinkHandle;
            set
            {
                m_useLinkHandle = value;
                enabled = enabled;
            }
        }

        public bool useCornerHandle
        {
            get => m_useCornerHandle;
            set
            {
                m_useCornerHandle = value;
                enabled = enabled;
            }
        }

        public bool useEdgeHandle
        {
            get => m_useEdgeHandle;
            set
            {
                m_useEdgeHandle = value;
                enabled = enabled;
            }
        }

        public bool selected { get => m_handleSelected != null; }

        public void OnMainHandGrabbed(Interactor interactor)
        {
            m_mainHand = interactor;
        }

        public void OnSubHandGrabbed(Interactor interactor)
        {
            m_subHand = interactor;

            if (m_mainHand != null)
            {
                m_initialDist = Vector3.Distance(m_mainHand.pointer.position, m_subHand.pointer.position);

                m_initialScaleOnGrabStart = m_targetTransform.localScale;
            }
        }

        public void OnMainHandReleased(Interactor interactor)
        {
            if (m_mainHand == interactor)
            {
                m_mainHand = null;
            }
        }

        public void OnSubHandReleased(Interactor interactor)
        {
            if (m_subHand == interactor)
            {
                m_subHand = null;
            }
        }

        private void UpdateHandleScale()
        {
            var lossyScale = m_targetTransform.lossyScale;
            var size = Vector3.one;
            size.x /= lossyScale.x;
            size.y /= lossyScale.y;
            size.z /= lossyScale.z;

            m_edgeHandles.ForEach((obj) =>
            {
                obj.transform.localScale = Vector3.Scale(m_edgeHandleIniScale, size);
            });

            m_cornerHandles.ForEach((obj) =>
            {
                obj.transform.localScale = Vector3.Scale(m_cornerHandleIniScale, size);
            });

            m_linkHandles.ForEach((pair) =>
            {
                var scale = pair.iniScale;
                switch (pair.handleAxis)
                {
                    case LinkPair.HandleAxis.X:
                        scale.y *= size.y;
                        scale.z *= size.z;
                        break;
                    case LinkPair.HandleAxis.Y:
                        scale.x *= size.x;
                        scale.z *= size.z;
                        break;
                    case LinkPair.HandleAxis.Z:
                        scale.x *= size.x;
                        scale.y *= size.y;
                        break;
                }
                pair.handle.transform.localScale = scale;
            });
        }

        public void UpdateTwoHandLogic()
        {
            if (m_enabled && m_mainHand != null && m_subHand != null && m_fcnt == 0)
            {
                var currentDist = Vector3.Distance(m_mainHand.pointer.position, m_subHand.pointer.position);

                var scaleFactor = currentDist / m_initialDist;

                Vector3 newScale;

                if (m_smooth)
                {
                    newScale = Vector3.Lerp(m_targetTransform.localScale, m_initialScaleOnGrabStart * scaleFactor, m_lerp);
                }
                else
                {
                    newScale = m_initialScaleOnGrabStart * scaleFactor;
                }

                m_targetTransform.localScale = ClampScale(newScale);

                UpdateHandleScale();
            }

            m_fcnt += 1;
            m_fcnt %= REST_INTERVAL;
        }

        public void UpdateOneHandLogic()
        {

        }

        private Vector3 ClampScale(Vector3 newScale)
        {
            newScale.x = Mathf.Clamp(newScale.x, m_scaleXLim.x, m_scaleXLim.y);
            newScale.y = Mathf.Clamp(newScale.y, m_scaleYLim.x, m_scaleYLim.y);
            newScale.z = Mathf.Clamp(newScale.z, m_scaleZLim.x, m_scaleZLim.y);

            return newScale;
        }

        public bool UpdateHandleLogic()
        {
            bool updated = false;

            if (m_enabled && m_handleSelected != null && m_fcnt == 0)
            {
                m_currentGrabPoint = m_handleSelected.handPos;

                float initialDist = Vector3.Dot(m_initialGrabPoint - m_oppositeCorner, m_diagonalDir);
                float currentDist = Vector3.Dot(m_currentGrabPoint - m_oppositeCorner, m_diagonalDir);
                float scaleFactorUniform = 1 + (currentDist - initialDist) / initialDist;

                var scaleFactor = new Vector3(scaleFactorUniform, scaleFactorUniform, scaleFactorUniform);
                scaleFactor.x = Mathf.Abs(scaleFactor.x);
                scaleFactor.y = Mathf.Abs(scaleFactor.y);
                scaleFactor.z = Mathf.Abs(scaleFactor.z);

                // Move the offset by the magnified amount
                var originalRelativePosition = m_targetTransform.InverseTransformDirection(m_initialPositionOnGrabStart - m_oppositeCorner);

                var newPosition = m_targetTransform.TransformDirection(Vector3.Scale(originalRelativePosition, scaleFactor)) + m_oppositeCorner;

                m_targetTransform.position = newPosition;

                var newScale = Vector3.Scale(m_initialScaleOnGrabStart, scaleFactor);

                m_targetTransform.localScale = ClampScale(newScale);

                UpdateHandleScale();

                updated = true;
            }

            m_fcnt += 1;
            m_fcnt %= REST_INTERVAL;

            return updated;
        }

        public void HandleGrabbed(ScaleHandle handle)
        {
            if (m_handleSelected == null)
            {
                m_handleSelected = handle;

                m_initialGrabPoint = handle.handPos;

                m_initialScaleOnGrabStart = m_targetTransform.localScale;

                m_initialPositionOnGrabStart = m_targetTransform.position;

                m_oppositeCorner = m_targetTransform.TransformPoint(-handle.transform.localPosition);

                m_diagonalDir = (handle.transform.position - m_targetTransform.position).normalized;
            }
        }

        public void HandleUnGrabbed(ScaleHandle handle)
        {
            if (m_handleSelected == handle)
            {
                m_handleSelected = null;
            }
        }

        private GameObject CreateHandle(Vector3 corner, Quaternion rotation, GameObject handlePrefab)
        {
            var obj = Object.Instantiate(handlePrefab, m_targetTransform);
            obj.hideFlags = HideFlags.HideInHierarchy;
            obj.transform.localPosition = corner;
            obj.transform.localRotation = rotation * Quaternion.identity;

            var collider = obj.GetComponent<BoxCollider>();
            var size = m_boundBoxSize;

            var lossyScale = m_targetTransform.lossyScale;
            size.x /= lossyScale.x;
            size.y /= lossyScale.y;
            size.z /= lossyScale.z;
            obj.transform.localScale = size;

            var handle = obj.GetComponent<ScaleHandle>();
            handle.RegistScalable(this);

            return obj;
        }

        IEnumerator Initialize(Transform targetTransform, Rigidbody targetRigidbody = null)
        {
            yield return null;

            m_targetTransform = targetTransform;
            m_targetRigidbody = targetRigidbody;

            float halfX = m_boundBoxSize.x * 0.5f;
            float halfY = m_boundBoxSize.y * 0.5f;
            float halfZ = m_boundBoxSize.z * 0.5f;

            if (m_cornerHandle != null)
            {
                m_cornerHandleIniScale = m_cornerHandle.transform.localScale;

                for (float x = -halfX; x <= halfX; x += 2 * halfX)
                {
                    for (float y = -halfY; y <= halfY; y += 2 * halfY)
                    {
                        for (float z = -halfZ; z <= halfZ; z += 2 * halfZ)
                        {
                            m_cornerHandles.Add(CreateHandle(new Vector3(x, y, z), Quaternion.identity, m_cornerHandle));

                            yield return null;
                        }
                    }
                }
            }

            if (m_edgeHandle != null)
            {
                m_edgeHandleIniScale = m_edgeHandle.transform.localScale;

                for (float x = -halfX; x <= halfX; x += halfX)
                {
                    for (float y = -halfY; y <= halfY; y += halfY)
                    {
                        for (float z = -halfZ; z <= halfZ; z += halfZ)
                        {
                            int dirX = (int)(x / Mathf.Abs(halfX));
                            int dirY = (int)(y / Mathf.Abs(halfY));
                            int dirZ = (int)(z / Mathf.Abs(halfZ));
                            if (Mathf.Abs(dirX) + Mathf.Abs(dirY) + Mathf.Abs(dirZ) != 2)
                            {
                                continue;
                            }

                            m_edgeHandles.Add(CreateHandle(new Vector3(x, y, z), Quaternion.LookRotation(new Vector3(dirX, dirY, dirZ).normalized), m_edgeHandle));

                            yield return null;
                        }
                    }
                }
            }

            if (m_linkHandle != null)
            {
                m_linkHandleIniScale = m_linkHandle.transform.localScale;

                for (float x = -halfX; x <= halfX; x += halfX)
                {
                    for (float y = -halfY; y <= halfY; y += halfY)
                    {
                        for (float z = -halfZ; z <= halfZ; z += halfZ)
                        {
                            int dirX = (int)(x / Mathf.Abs(halfX));
                            int dirY = (int)(y / Mathf.Abs(halfY));
                            int dirZ = (int)(z / Mathf.Abs(halfZ));
                            if (Mathf.Abs(dirX) + Mathf.Abs(dirY) + Mathf.Abs(dirZ) != 2)
                            {
                                continue;
                            }

                            var handleAxis = LinkPair.HandleAxis.NONE;
                            var localScale = m_linkHandleIniScale;
                            if (dirX == 0)
                            {
                                handleAxis = LinkPair.HandleAxis.X;
                                localScale.x = Mathf.Abs(m_boundBoxSize.x);
                            }
                            else if (dirY == 0)
                            {
                                handleAxis = LinkPair.HandleAxis.Y;
                                localScale.y = Mathf.Abs(m_boundBoxSize.y);
                            }
                            else if (dirZ == 0)
                            {
                                handleAxis = LinkPair.HandleAxis.Z;
                                localScale.z = Mathf.Abs(m_boundBoxSize.z);
                            }

                            var handle = CreateHandle(new Vector3(x, y, z), Quaternion.identity, m_linkHandle);
                            var handlePair = new LinkPair() { handleAxis = handleAxis, iniScale = localScale, handle = handle };

                            m_linkHandles.Add(handlePair);

                            yield return null;
                        }
                    }
                }
            }

            UpdateHandleScale();

            enabled = m_enabled;
        }

        public void Start(Transform targetTransform, Rigidbody targetRigidbody = null)
        {
            CoroutineHandler.StartStaticCoroutine(Initialize(targetTransform, targetRigidbody));
        }
    }
}
