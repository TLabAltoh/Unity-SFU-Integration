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
            public UnityEvent onHovered;
            public UnityEvent onUnHovered;
            public UnityEvent whileHovered;
        }

        [System.Serializable]
        public class Select
        {
            public UnityEvent onSelected;
            public UnityEvent onUnSelected;
            public UnityEvent whileSelected;
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

        public virtual void Hovered(Interactor interactor)
        {
            m_hovereds.Add(interactor);

            m_hoverEvent.onHovered.Invoke();

            if (m_interactableChain != null)
                m_interactableChain.ForEach((s) => s.Hovered(interactor));
        }

        public virtual void WhileHovered(Interactor interactor)
        {
            m_hoverEvent.whileHovered.Invoke();

            if (m_interactableChain != null)
                m_interactableChain.ForEach((s) => s.WhileHovered(interactor));
        }

        public virtual void UnHovered(Interactor interactor)
        {
            m_hovereds.Remove(interactor);

            m_hoverEvent.onUnHovered.Invoke();

            if (m_interactableChain != null)
                m_interactableChain.ForEach((s) => s.UnHovered(interactor));
        }

        public virtual void Selected(Interactor interactor)
        {
            m_selecteds.Add(interactor);

            m_selectEvent.onSelected.Invoke();

            if (m_interactableChain != null)
                m_interactableChain.ForEach((s) => s.Selected(interactor));
        }

        public virtual void WhileSelected(Interactor interactor)
        {
            m_selectEvent.whileSelected.Invoke();

            if (m_interactableChain != null)
                m_interactableChain.ForEach((s) => s.WhileSelected(interactor));
        }

        public virtual void UnSelected(Interactor interactor)
        {
            m_selecteds.Remove(interactor);

            m_selectEvent.onUnSelected.Invoke();

            if (m_interactableChain != null)
                m_interactableChain.ForEach((s) => s.UnSelected(interactor));
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

        protected virtual void OnDisable() => Registry.UnRegister(this);

        protected virtual void Start() { }

        protected virtual void Update() { }
    }
}