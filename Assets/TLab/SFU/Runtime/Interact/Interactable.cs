using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace TLab.SFU.Interact
{
    using Registry = Registry<Interactable>;

    public static class InteractionEvent
    {
        [System.Serializable]
        public class Hover
        {
            public UnityEvent onHover;
            public UnityEvent onUnhover;
            public UnityEvent whileHover;
        }

        [System.Serializable]
        public class Select
        {
            public UnityEvent onSelect;
            public UnityEvent onUnselect;
            public UnityEvent whileSelect;
        }
    }

    [AddComponentMenu("TLab/SFU/Interactable (TLab)")]
    public class Interactable : MonoBehaviour
    {
        [SerializeField] protected ColliderSurface m_surface;

        [Header("Interaction Event")]
        [SerializeField] protected InteractionEvent.Hover m_hoverEvent = new InteractionEvent.Hover();
        [SerializeField] protected InteractionEvent.Select m_selectEvent = new InteractionEvent.Select();

        [Header("Chain")]
        [SerializeField] protected List<Interactable> m_interactableChain;

        protected List<Interactor> m_hovereds = new List<Interactor>();
        protected List<Interactor> m_selecteds = new List<Interactor>();

        private string THIS_NAME => "[" + this.GetType().Name + "] ";

        public List<Interactor> hovereds => m_hovereds;

        public List<Interactor> selecteds => m_selecteds;

        public ColliderSurface surface => m_surface;

        public virtual bool IsHovered() => m_hovereds.Count > 0;

        public virtual bool IsHovered(Interactor interactor) => m_hovereds.Contains(interactor);

        public virtual bool IsSelected() => m_selecteds.Count > 0;

        public virtual bool IsSelected(Interactor interactor) => m_selecteds.Contains(interactor);

        public virtual void OnHover(Interactor interactor)
        {
            m_hovereds.Add(interactor);

            m_hoverEvent.onHover.Invoke();

            if (m_interactableChain != null)
                m_interactableChain.ForEach((s) => s.OnHover(interactor));
        }

        public virtual void WhileHover(Interactor interactor)
        {
            m_hoverEvent.whileHover.Invoke();

            if (m_interactableChain != null)
                m_interactableChain.ForEach((s) => s.WhileHover(interactor));
        }

        public virtual void OnUnhover(Interactor interactor)
        {
            m_hovereds.Remove(interactor);

            m_hoverEvent.onUnhover.Invoke();

            if (m_interactableChain != null)
                m_interactableChain.ForEach((s) => s.OnUnhover(interactor));
        }

        public virtual void OnSelect(Interactor interactor)
        {
            m_selecteds.Add(interactor);

            m_selectEvent.onSelect.Invoke();

            if (m_interactableChain != null)
                m_interactableChain.ForEach((s) => s.OnSelect(interactor));
        }

        public virtual void WhileSelect(Interactor interactor)
        {
            m_selectEvent.whileSelect.Invoke();

            if (m_interactableChain != null)
                m_interactableChain.ForEach((s) => s.WhileSelect(interactor));
        }

        public virtual void OnUnselect(Interactor interactor)
        {
            m_selecteds.Remove(interactor);

            m_selectEvent.onUnselect.Invoke();

            if (m_interactableChain != null)
                m_interactableChain.ForEach((s) => s.OnUnselect(interactor));
        }

        #region RAYCAST

        public virtual bool SkipRaycast() => !this.gameObject.activeInHierarchy || m_surface == null;

        public virtual bool Raycast(Ray ray, out RaycastHit hit, float maxDistance)
        {
            if (SkipRaycast())
            {
                hit = new RaycastHit();
                return false;
            }

            return m_surface.Raycast(ray, out hit, maxDistance);
        }

        public virtual bool Spherecast(Vector3 point, out RaycastHit hit, float maxDistance)
        {
            if (SkipRaycast())
            {
                hit = new RaycastHit();
                return false;
            }

            return m_surface.Spherecast(point, out hit, maxDistance);
        }

        #endregion RAYCAST

        protected virtual void OnEnable() => Registry.Register(this);

        protected virtual void OnDisable() => Registry.Unregister(this);

        protected virtual void Start() { }

        protected virtual void Update() { }
    }
}