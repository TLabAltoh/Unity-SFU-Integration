using System.Collections.Generic;
using UnityEngine;

namespace TLab.NetworkedVR.Interact
{
    [AddComponentMenu("TLab/NetworkedVR/Interactor (TLab)")]
    public class Interactor : MonoBehaviour
    {
        [Header("TLab XR Hand")]
        [SerializeField] protected TLabXRHand m_hand;

        [SerializeField] protected Transform m_pointer;

        protected static List<int> m_identifiers = new List<int>();

        protected static int GenerateIdentifier()
        {
            while (true)
            {
                int identifier = Random.Range(0, int.MaxValue);

                if (m_identifiers.Contains(identifier)) continue;

                m_identifiers.Add(identifier);

                return identifier;
            }
        }

        private string THIS_NAME => "[" + this.GetType().Name + "] ";

        protected int m_identifier;

        protected Interactable m_interactable;

        protected Interactable m_candidate;

        protected RaycastHit m_raycastHit;

        protected bool m_pressed = false;

        protected bool m_onPress = false;

        protected bool m_onRelease = false;

        protected float m_pressStrength = 0.0f;

        protected Vector3 m_angulerVelocity;

        protected Vector3 m_rotateVelocity;

        public int identifier => m_identifier;

        public Transform pointer => m_pointer;

        // Raycast

        public RaycastHit raycastHit => m_raycastHit;

        // Interactor input

        public float pressStrength => m_pressStrength;

        public bool pressed => m_pressed;

        public bool onPress => m_onPress;

        public bool onRelease => m_onRelease;

        public Vector3 angulerVelocity => m_angulerVelocity;

        public Vector3 rotateVelocity => m_rotateVelocity;

        protected virtual void UpdateRaycast() { }

        protected virtual void UpdateInput() { }

        protected virtual void Process()
        {
            if (m_interactable != null && m_interactable.IsSelectes(this))
            {
                m_interactable.WhileHovered(this);

                if (m_pressed)
                    m_interactable.WhileSelected(this);
                else
                    m_interactable.UnSelected(this);
            }
            else
            {
                UpdateRaycast();

                if (m_candidate != null)        // candidate was found
                {
                    if (m_interactable == m_candidate)
                    {
                        m_interactable.WhileHovered(this);

                        if (m_onPress)
                            m_interactable.Selected(this);
                    }
                    else
                    {
                        if (m_interactable != null)
                            m_interactable.UnHovered(this);

                        m_interactable = m_candidate;
                        m_interactable.Hovered(this);
                    }
                }
                else
                {
                    ForceClear();
                }
            }
        }

        protected virtual void ForceClear()
        {
            if (m_interactable != null)
            {
                if (m_interactable.IsSelectes(this))
                    m_interactable.UnSelected(this);

                m_interactable.UnHovered(this);
                m_interactable = null;
            }
        }

        protected virtual void Awake() => m_identifier = GenerateIdentifier();

        protected virtual void Start() => UpdateInput();

        protected virtual void Update()
        {
            UpdateInput();
            Process();
        }
    }
}
