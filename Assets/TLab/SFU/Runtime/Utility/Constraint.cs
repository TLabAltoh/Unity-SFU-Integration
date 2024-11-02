using System.Collections;
using System.Linq;
using UnityEngine;
using TLab.SFU.Network;

namespace TLab.SFU
{
    [AddComponentMenu("TLab/SFU/Constraint (TLab)")]
    public class Constraint : MonoBehaviour
    {
        #region REGISTRY

        private static Hashtable m_registry = new Hashtable();

        public static void Register(string id, Constraint @object)
        {
            if (!m_registry.ContainsKey(id))
                m_registry[id] = @object;
        }

        public static void UnRegister(string id)
        {
            if (m_registry.ContainsKey(id))
                m_registry.Remove(id);
        }

        public static void ClearRegistry()
        {
            var gameObjects = m_registry.Values.Cast<Constraint>().Select((t) => t.gameObject);

            foreach (var gameObject in gameObjects)
                Destroy(gameObject);

            m_registry.Clear();
        }

        public static Constraint GetById(string id) => m_registry[id] as Constraint;

        #endregion REGISTRY

        [SerializeField] private Direction m_direction;

        [SerializeField] private string m_id;

        private Constraint m_parent;

        private void Awake()
        {
            if (Const.SEND.HasFlag(m_direction))
                Register(m_id, this);
        }

        private void Start()
        {
            if (Const.RECV.HasFlag(m_direction))
                m_parent = GetById(m_id);
        }

        private void Update()
        {
            if (Const.SEND.HasFlag(m_direction) && (m_parent != null))
            {
                // TODO:
            }
        }

        private void OnDestroy()
        {
            if (Const.SEND.HasFlag(m_direction))
                UnRegister(m_id);
        }
    }
}
