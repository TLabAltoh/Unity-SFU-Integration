using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace TLab.SFU.Interact
{
    [AddComponentMenu("TLab/SFU/Interactable (TLab)")]
    public class Interactable : MonoBehaviour
    {
        #region REGISTRY

        private static List<Interactable> m_registry = new List<Interactable>();

        public static List<Interactable> registry => m_registry;

        protected static string REGISTRY = "[registry] ";

        protected static void Register(Interactable interactable)
        {
            if (!m_registry.Contains(interactable)) m_registry.Add(interactable);
        }

        protected static void UnRegister(Interactable interactable)
        {
            if (m_registry.Contains(interactable)) m_registry.Remove(interactable);
        }

        #endregion

        [Header("Raycat target")]
        [SerializeField] protected bool m_colliderEnable = false;
        [SerializeField] protected Collider m_collider;

        [Header("Chain interactables")]
        [SerializeField] protected List<Interactable> m_interactableChain;

        protected List<Interactor> m_hovereds = new List<Interactor>();
        protected List<Interactor> m_selecteds = new List<Interactor>();

        private string THIS_NAME => "[" + this.GetType().Name + "] ";

        public List<Interactor> hovereds => m_hovereds;

        public List<Interactor> selecteds => m_selecteds;

        public Collider srufaceCollider => m_collider;

        public bool enableCollision { get => m_colliderEnable; set => m_colliderEnable = value; }

        public virtual bool IsHovered() => m_hovereds.Count > 0;

        public virtual bool IsHoveres(Interactor interactor) => m_hovereds.Contains(interactor);

        public virtual bool IsSelected() => m_selecteds.Count > 0;

        public virtual bool IsSelectes(Interactor interactor) => m_selecteds.Contains(interactor);

        public virtual void Hovered(Interactor interactor)
        {
            m_hovereds.Add(interactor);

            if (m_interactableChain != null)
                m_interactableChain.ForEach((s) => s.Hovered(interactor));
        }

        public virtual void WhileHovered(Interactor interactor)
        {
            if (m_interactableChain != null)
                m_interactableChain.ForEach((s) => s.WhileHovered(interactor));
        }

        public virtual void UnHovered(Interactor interactor)
        {
            m_hovereds.Remove(interactor);

            if (m_interactableChain != null)
                m_interactableChain.ForEach((s) => s.UnHovered(interactor));
        }

        public virtual void Selected(Interactor interactor)
        {
            m_selecteds.Add(interactor);

            if (m_interactableChain != null)
                m_interactableChain.ForEach((s) => s.Selected(interactor));
        }

        public virtual void WhileSelected(Interactor interactor)
        {
            if (m_interactableChain != null)
                m_interactableChain.ForEach((s) => s.WhileSelected(interactor));
        }

        public virtual void UnSelected(Interactor interactor)
        {
            m_selecteds.Remove(interactor);

            if (m_interactableChain != null)
                m_interactableChain.ForEach((s) => s.UnSelected(interactor));
        }

        #region RAYCAST

        public virtual bool Raycast(Ray ray, out RaycastHit hit, float maxDistance)
        {
            if (m_collider == null || !m_collider.enabled || !m_colliderEnable)
            {
                hit = new RaycastHit();
                return false;
            }

            return m_collider.Raycast(ray, out hit, maxDistance);
        }

        public virtual bool Spherecast(Vector3 point, out RaycastHit hit, float maxDistance)
        {
            if (m_collider == null || !m_collider.enabled || !m_colliderEnable)
            {
                hit = new RaycastHit();
                return false;
            }

            var closestPoint = m_collider.ClosestPoint(point);
            hit = new RaycastHit();

            hit.distance = (point - closestPoint).magnitude;
            hit.point = closestPoint;

            return hit.distance < maxDistance;
        }

        #endregion RAYCAST

        protected virtual void OnEnable() => Interactable.Register(this);

        protected virtual void OnDisable() => Interactable.UnRegister(this);

        protected virtual void Start() { }

        protected virtual void Update() { }

#if UNITY_EDITOR
        protected virtual void OnValidate()
        {
            if (m_collider == null)
            {
                m_collider = GetComponent<Collider>();

                if (m_collider != null)
                    EditorUtility.SetDirty(this);
            }
        }
#endif
    }
}