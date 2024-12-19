#define DEBUG_OUTLINE
#undef DEBUG_OUTLINE

using System;
using System.Collections.Generic;
using UnityEngine;

namespace TLab.SFU.Interact
{
    [AddComponentMenu("TLab/SFU/Pointable Outline (TLab)")]
    public class PointableOutline : Pointable
    {
        [Header("Outline")]
        [SerializeField, Range(0f, 0.1f)] protected float m_width = 0.025f;
        [SerializeField] protected Color m_hoverColor = new Color(0, 1, 1, 0.5f);
        [SerializeField] protected Color m_selectColor = Color.cyan;

        [SerializeField] protected Material m_material;

#if UNITY_EDITOR && DEBUG_OUTLINE
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

        internal static readonly int PROP_COLOR = Shader.PropertyToID("_Color");
        internal static readonly int PROP_WIDTH = Shader.PropertyToID("_Width");

        public virtual Material material { get => m_material; set => m_material = value; }

        public float width
        {
            get => m_width;
            set
            {
                if (m_width != value)
                {
                    m_width = value;

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

            SetMaterialDirty();
        }

        private void SetMaterialDirty()
        {
            if (IsSelected())
            {
                m_material.SetColor(PROP_COLOR, m_selectColor);
                m_material.SetFloat(PROP_WIDTH, m_width);
                return;
            }

            if (IsHovered())
            {
                m_material.SetColor(PROP_COLOR, m_hoverColor);
                m_material.SetFloat(PROP_WIDTH, m_width);
                return;
            }

            m_material.SetColor(PROP_COLOR, alphaZero);
            m_material.SetFloat(PROP_WIDTH, 0.0f);
        }

        private void SetAllDirty()
        {
            SetMaterialDirty();
        }

        public override void OnHover(Interactor interactor)
        {
            base.OnHover(interactor);

            SetAllDirty();
        }

        public override void OnUnhover(Interactor interactor)
        {
            base.OnUnhover(interactor);

            SetAllDirty();
        }

        public override void OnSelect(Interactor interactor)
        {
            base.OnSelect(interactor);

            SetAllDirty();
        }

        public override void OnUnselect(Interactor interactor)
        {
            base.OnUnselect(interactor);

            SetAllDirty();
        }

#if UNITY_EDITOR
        protected void OnValidate()
        {
#if DEBUG_OUTLINE
            if (m_material != null)
            {
                switch (m_debug)
                {
                    case Debug.None:
                        m_material.SetColor(PROP_COLOR, alphaZero);
                        m_material.SetFloat(PROP_WIDTH, 0.0f);
                        break;
                    case Debug.Hover:
                        m_material.SetColor(PROP_COLOR, m_hoverColor);
                        m_material.SetFloat(PROP_WIDTH, m_width);
                        break;
                    case Debug.Select:
                        m_material.SetColor(PROP_COLOR, m_selectColor);
                        m_material.SetFloat(PROP_WIDTH, m_width);
                        break;
                }

                UnityEditor.EditorUtility.SetDirty(m_material);
            }
#endif
        }
#endif
    }
}