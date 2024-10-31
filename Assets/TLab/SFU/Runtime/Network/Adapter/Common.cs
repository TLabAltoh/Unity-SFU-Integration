namespace TLab.SFU.Network.Offer
{
    [System.Serializable]
    public class Create
    {
        public string room_name = "default";

        public uint room_capacity = 2;

        public string room_key = "password";

        public bool needs_host = false;

        public bool is_public = true;

        public string master_key = "password";

        public string description = "description";
    }

    [System.Serializable]
    public class Delete
    {
        public int room_id;
        public string master_key;
    }

    [System.Serializable]
    public class Join
    {
        public string user_name;
        public int room_id;
        public string room_key;
        public string master_key;
    }

    [System.Serializable]
    public class Exit
    {
        public int room_id;
        public string room_key;
        public int user_id;
        public uint user_token;
    }
}

namespace TLab.SFU.Network.Answer
{
    [System.Serializable]
    public class Create
    {
        public int room_id;
        public string room_name;
        public uint room_capacity;
        public string description;
    };

    [System.Serializable]
    public class Join
    {
        public int user_id;
        public uint user_token;
    }

    [System.Serializable]
    public class Infos
    {
        public Info[] room_infos;
    }

    [System.Serializable]
    public class Info
    {
        public int room_id;
        public string room_name;
        public uint room_capacity;
        public string description;
    }
}
