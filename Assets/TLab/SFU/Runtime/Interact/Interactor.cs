using System.Collections.Generic;
using UnityEngine;

namespace TLab.SFU.Interact
{
    [AddComponentMenu("TLab/SFU/Interactor (TLab)")]
    public abstract class Interactor : MonoBehaviour, IActiveState
    {
        public enum ActivateOption
        {
            NONE,
            HOVER,
            SELECT
        };

        [SerializeField] protected InteractDataSource m_interactDataSource;

        [SerializeField] protected Transform m_pointer;

        [SerializeField] protected ActivateOption m_activateOption = ActivateOption.NONE;

        [SerializeField, Interface(typeof(IActiveState))]
        private Object m_parentState;
        public IActiveState parentState;

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

        private delegate bool GetActiveState();
        private GetActiveState m_getActiveState;

        public virtual bool Active => m_getActiveState.Invoke();

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

        protected abstract void UpdateRaycast();

        protected abstract void UpdateInput();

        protected virtual void Process()
        {
            if ((m_interactable != null) && m_interactable.IsSelected(this))
            {
                m_interactable.WhileHovered(this);

                if (m_pressed)
                    m_interactable.WhileSelected(this);
                else
                    m_interactable.UnSelected(this);
            }
            else
            {
                if ((parentState != null) && parentState.Active)
                    m_candidate = null;
                else
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
                    ForceRelease();
            }
        }

        protected virtual void ForceRelease()
        {
            if (m_interactable != null)
            {
                if (m_interactable.IsSelected(this))
                    m_interactable.UnSelected(this);

                m_interactable.UnHovered(this);
                m_interactable = null;
            }
        }

        protected virtual void Awake() => m_identifier = GenerateIdentifier();

        protected virtual void Start()
        {
            if (m_parentState)
                parentState = m_parentState as IActiveState;

            switch (m_activateOption)
            {
                case ActivateOption.HOVER:
                    m_getActiveState = () => (m_interactable != null);
                    break;
                case ActivateOption.SELECT:
                    m_getActiveState = () =>
                    {
                        if (m_interactable == null)
                            return false;
                        return m_interactable.IsSelected(this);
                    };
                    break;
                default:
                    m_getActiveState = () => false;
                    break;
            }
        }

        protected virtual void Update()
        {
            UpdateInput();
            Process();
        }
    }
}
