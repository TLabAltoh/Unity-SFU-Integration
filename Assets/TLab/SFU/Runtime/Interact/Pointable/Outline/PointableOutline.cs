using System;
using System.Collections.Generic;
using UnityEngine;

namespace TLab.SFU.Interact
{
    [AddComponentMenu("TLab/SFU/Pointable Outline (TLab)")]
    public class PointableOutline : Pointable
    {
        [SerializeField, Range(0f, 0.1f)] protected float m_outlineWidth = 0.025f;

        [SerializeField] protected Material m_material;

        protected static string OUTLINE_WIDTH = "_OutlineWidth";

        protected static float ZERO_WIDTH = 0.0f;

        public virtual Material outlineMat { get => m_material; set => m_material = value; }

        protected override void Start()
        {
            base.Start();

            var copy = new Material(m_material);
            var meshRenderer = this.GetComponent<MeshRenderer>();
            if (meshRenderer != null)
            {
                var materials = meshRenderer.sharedMaterials;
                var materialList = new List<Material>();

                foreach (var material in materials)
                {
                    if (material != m_material)
                        materialList.Add(material);
                }

                materialList.Add(copy);

                m_material = copy;
                meshRenderer.sharedMaterials = materialList.ToArray();
            }

            m_material.SetFloat(OUTLINE_WIDTH, ZERO_WIDTH);
        }

        public override void Hovered(Interactor interactor)
        {
            base.Hovered(interactor);

            m_material.SetFloat(OUTLINE_WIDTH, m_outlineWidth);
        }

        public override void UnHovered(Interactor interactor)
        {
            base.UnHovered(interactor);

            if (!IsHovered())
                m_material.SetFloat(OUTLINE_WIDTH, ZERO_WIDTH);
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();

            if (m_material != null)
            {
                m_material.SetFloat(OUTLINE_WIDTH, m_outlineWidth);
                UnityEditor.EditorUtility.SetDirty(m_material);
            }
        }
#endif
    }
}