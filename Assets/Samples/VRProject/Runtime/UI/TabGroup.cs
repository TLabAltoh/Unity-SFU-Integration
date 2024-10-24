using UnityEngine;

namespace TLab.VRProjct
{
    public class TabGroup : MonoBehaviour
    {
        [SerializeField] private GameObject[] m_tabs;

        public void SetActive(int index)
        {
            for (int i = 0; i < m_tabs.Length; i++)
                m_tabs[i].SetActive(i == index);
        }
    }
}
