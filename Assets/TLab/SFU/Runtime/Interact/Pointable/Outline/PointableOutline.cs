#define DEBUG_COLOR
#undef DEBUG_COLOR

using System;
using System.Collections.Generic;
using UnityEngine;

namespace TLab.SFU.Interact
{
    [AddComponentMenu("TLab/SFU/Pointable Outline (TLab)")]
    public class PointableOutline : Pointable
    {
        [SerializeField, Range(0f, 0.1f)] protected float m_outlineWidth = 0.025f;
        [SerializeField] protected Color m_hoverColor = new Color(0, 1, 1, 0.5f);
        [SerializeField] protected Color m_selectColor = Color.cyan;

        [SerializeField] protected Material m_material;

#if UNITY_EDITOR && DEBUG_COLOR
        [Header("Debug")]
        [SerializeField] protected Debug m_debug = Debug.None;

        public enum Debug
        {
            None,
            Hover,
            Select,
        };
#endif

        protected static readonly Color alphaZero = new Color(0, 0, 0, 0);

        internal static readonly int PROP_OUTLINE_COLOR = Shader.PropertyToID("_OutlineColor");
        internal static readonly int PROP_OUTLINE_WIDTH = Shader.PropertyToID("_OutlineWidth");

        public virtual Material outlineMat { get => m_material; set => m_material = value; }

        public float outlineWidth
        {
            get => m_outlineWidth;
            set
            {
                if (m_outlineWidth != value)
                {
                    m_outlineWidth = value;

                    SetAllDirty();
                }
            }
        }

        public Color hoverColor
        {
            get => m_hoverColor;
            set
            {
                if (m_hoverColor != value)
                {
                    m_hoverColor = value;

                    SetAllDirty();
                }
            }
        }

        public Color selectColor
        {
            get => m_selectColor;
            set
            {
                if (m_selectColor != value)
                {
                    m_selectColor = value;

                    SetAllDirty();
                }
            }
        }

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

            m_material.SetFloat(PROP_OUTLINE_WIDTH, 0.0f);
        }

        private void SetMaterialDirty()
        {
            if (IsSelected())
            {
                m_material.SetColor(PROP_OUTLINE_COLOR, m_selectColor);
                m_material.SetFloat(PROP_OUTLINE_WIDTH, m_outlineWidth);
                return;
            }

            if (IsHovered())
            {
                m_material.SetColor(PROP_OUTLINE_COLOR, m_hoverColor);
                m_material.SetFloat(PROP_OUTLINE_WIDTH, m_outlineWidth);
                return;
            }

            m_material.SetColor(PROP_OUTLINE_COLOR, alphaZero);
            m_material.SetFloat(PROP_OUTLINE_WIDTH, 0.0f);
        }

        private void SetAllDirty()
        {
            SetMaterialDirty();
        }

        public override void Hovered(Interactor interactor)
        {
            base.Hovered(interactor);

            SetAllDirty();
        }

        public override void UnHovered(Interactor interactor)
        {
            base.UnHovered(interactor);

            SetAllDirty();
        }

        public override void Selected(Interactor interactor)
        {
            base.Selected(interactor);

            SetAllDirty();
        }

        public override void UnSelected(Interactor interactor)
        {
            base.UnSelected(interactor);

            SetAllDirty();
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();

#if DEBUG_COLOR
            if (m_material != null)
            {
                switch (m_debug)
                {
                    case Debug.None:
                        m_material.SetColor(PROP_OUTLINE_COLOR, alphaZero);
                        m_material.SetFloat(PROP_OUTLINE_WIDTH, 0.0f);
                        break;
                    case Debug.Hover:
                        m_material.SetColor(PROP_OUTLINE_COLOR, m_hoverColor);
                        m_material.SetFloat(PROP_OUTLINE_WIDTH, m_outlineWidth);
                        break;
                    case Debug.Select:
                        m_material.SetColor(PROP_OUTLINE_COLOR, m_selectColor);
                        m_material.SetFloat(PROP_OUTLINE_WIDTH, m_outlineWidth);
                        break;
                }

                UnityEditor.EditorUtility.SetDirty(m_material);
            }
#endif
        }
#endif
    }
}