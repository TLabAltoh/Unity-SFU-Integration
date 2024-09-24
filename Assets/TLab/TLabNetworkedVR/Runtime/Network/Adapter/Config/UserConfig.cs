using UnityEngine;

namespace TLab.NetworkedVR.Network
{
    [System.Serializable]
    [CreateAssetMenu(fileName = "User Config", menuName = "TLab/NetworkedVR/User Config")]
    public class UserConfig : ScriptableObject
    {
        public string userName;

        [System.Serializable]
        public class JoinOffer
        {
            public string user_name;
            public int room_id;
            public string room_pass;
            public string master_key;
        }

        [System.Serializable]
        public class JoinAnswer
        {
            public int user_id;
            public uint user_token;
        }

        [System.Serializable]
        public class ExitOffer
        {
            public int room_id;
            public string room_pass;
            public int user_id;
            public uint user_token;
        }

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
