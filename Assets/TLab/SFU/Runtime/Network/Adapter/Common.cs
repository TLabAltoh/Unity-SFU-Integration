using System;
using UnityEngine;

namespace TLab.SFU.Network.Json
{
    public interface IRequest
    {
        public string ToJson();
    }

    public interface IResponse<T> where T : class
    {
        public void FromJsonOverwrite(string json);
    }

    public static class CreateRoom
    {
        [Serializable]
        public class Request : IRequest
        {
            public string name = "default";

            public uint capacity = 2;

            public bool needsHost = false;

            public bool isPublic = true;

            public string sharedKey = "password";

            public string masterKey = "password";

            [TextArea()]
            public string description = "description";

            public Request(string name, uint capacity = 2, bool needsHost = false, bool isPublic = true, string sharedKey = "password", string masterKey = "password", string description = "description")
            {
                this.name = name;
                this.capacity = capacity;
                this.needsHost = needsHost;
                this.isPublic = isPublic;
                this.sharedKey = sharedKey;
                this.masterKey = masterKey;
                this.description = description;
            }

            public class RustFormat
            {
                public string name = "default";

                public uint capacity = 2;

                public bool needs_host = false;

                public bool is_public = true;

                public string shared_key = "password";

                public string master_key = "password";

                public string description = "description";

                public RustFormat(string name, uint capacity, bool needsHost, bool isPublic, string sharedKey, string masterKey, string description)
                {
                    this.name = name;
                    this.capacity = capacity;
                    this.needs_host = needsHost;
                    this.is_public = isPublic;
                    this.shared_key = sharedKey;
                    this.master_key = masterKey;
                    this.description = description;
                }
            }

            public string ToJson() => JsonUtility.ToJson(new RustFormat(name, capacity, needsHost, isPublic, sharedKey, masterKey, description));
        }

        [Serializable]
        public class Response : IResponse<Response>
        {
            public int id;
            public string name;
            public uint capacity;
            public string description;

            public Response(string json) => FromJsonOverwrite(json);

            public void FromJsonOverwrite(string json) => JsonUtility.FromJsonOverwrite(json, this);
        }
    }

    public static class DeleteRoom
    {
        [Serializable]
        public class Request : IRequest
        {
            public int id;
            public string masterKey;

            public Request(int id, string masterKey)
            {
                this.id = id;
                this.masterKey = masterKey;
            }

            [Serializable]
            public class RustFormat
            {
                public int id;
                public string master_key;

                public RustFormat(int id, string masterKey)
                {
                    this.id = id;
                    this.master_key = masterKey;
                }
            }

            public string ToJson() => JsonUtility.ToJson(new RustFormat(id, masterKey));
        }
    }

    public static class JoinRoom
    {
        [Serializable]
        public class Request : IRequest
        {
            public string name;
            public int id;
            public string sharedKey;
            public string masterKey;

            public Request(string name, int id, string sharedKey, string masterKey)
            {
                this.name = name;
                this.id = id;
                this.sharedKey = sharedKey;
                this.masterKey = masterKey;
            }

            [Serializable]
            public class RustFormat
            {
                public string name;
                public int id;
                public string shared_key;
                public string master_key;

                public RustFormat(string name, int id, string sharedKey, string masterKey)
                {
                    this.name = name;
                    this.id = id;
                    this.shared_key = sharedKey;
                    this.master_key = masterKey;
                }
            }

            public string ToJson() => JsonUtility.ToJson(new RustFormat(name, id, sharedKey, masterKey));
        }

        [Serializable]
        public class Response : IResponse<Response>
        {
            public int id;
            public uint token;

            public Response(string json) => FromJsonOverwrite(json);

            public void FromJsonOverwrite(string json) => JsonUtility.FromJsonOverwrite(json, this);
        }
    }

    public static class ExitRoom
    {
        [Serializable]
        public class Request : IRequest
        {
            public int roomId;
            public string sharedKey;
            public int userId;
            public uint token;

            public Request(int roomId, string sharedKey, int userId, uint token)
            {
                this.roomId = roomId;
                this.sharedKey = sharedKey;
                this.userId = userId;
                this.token = token;
            }

            [Serializable]
            public class RustFormat
            {
                public int room_id;
                public string shared_key;
                public int user_id;
                public uint token;

                public RustFormat(int roomId, string sharedKey, int userId, uint token)
                {
                    this.room_id = roomId;
                    this.shared_key = sharedKey;
                    this.user_id = userId;
                    this.token = token;
                }
            }

            public string ToJson() => JsonUtility.ToJson(new RustFormat(roomId, sharedKey, userId, token));
        }
    }

    [Serializable]
    public class RoomInfos : IResponse<RoomInfos>
    {
        public RoomInfo[] infos;

        public RoomInfos(string json) => FromJsonOverwrite(json);

        public void FromJsonOverwrite(string json) => JsonUtility.FromJsonOverwrite(json, this);
    }

    [Serializable]
    public class RoomInfo : IResponse<RoomInfo>
    {
        public int id;
        public string name;
        public uint capacity;
        public string description;

        public RoomInfo(string json) => FromJsonOverwrite(json);

        public void FromJsonOverwrite(string json) => JsonUtility.FromJsonOverwrite(json, this);
    }
}
