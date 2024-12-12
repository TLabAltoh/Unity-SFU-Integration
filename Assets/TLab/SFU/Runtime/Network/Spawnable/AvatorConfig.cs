using UnityEngine;

namespace TLab.SFU.Network
{
    [CreateAssetMenu(fileName = "Avator Config", menuName = "TLab/SFU/Avator Config")]
    public class AvatorConfig : ScriptableObject
    {
        [SerializeField, Min(0)] private int m_avatorId;

        public int avatorId => m_avatorId;
    }
}
