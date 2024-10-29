using UnityEngine;

namespace TLab.SFU.Network
{
    [System.Serializable]
    [CreateAssetMenu(fileName = "User Config", menuName = "TLab/SFU/User Config")]
    public class UserConfig : ScriptableObject
    {
        public string userName;

        public void Init(string userName)
        {
            this.userName = userName;
        }

        public UserConfig GetClone()
        {
            var instance = CreateInstance<UserConfig>();

            instance.Init(this.userName);

            return instance;
        }
    }
}
