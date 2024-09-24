using UnityEngine;

namespace TLab.NetworkedVR.Network
{
    [System.Serializable]
    [CreateAssetMenu(fileName = "Room Config", menuName = "TLab/NetworkedVR/Room Config")]
    public class RoomConfig : ScriptableObject
    {
        [System.Serializable]
        public class CreateOffer
        {
            public string room_name = "default";

            public uint room_capacity = 2;

            public string room_pass = "password";

            public bool needs_host = false;

            public bool is_public = false;

            public string master_key = "password";

            public string description = "description";
        }

        [System.Serializable]
        public class CreateAnswer
        {
            public int room_id;
            public string room_name;
            public uint room_capacity;
            public string description;
        };

        [System.Serializable]
        public class DeleteOffer
        {
            public int room_id;
            public string master_key;
        }

        public string address = "http://127.0.0.1:7777";

        public CreateOffer createOffer;

        public RoomConfig(string address, CreateOffer createOffer)
        {
            this.address = address;
            this.createOffer = createOffer;
        }

        public RoomConfig GetClone()
        {
            return new RoomConfig(this.address, this.createOffer);
        }
    }
}
