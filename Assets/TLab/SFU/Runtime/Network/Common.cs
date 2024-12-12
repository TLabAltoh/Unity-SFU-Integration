using System;
using UnityEngine;
using TLab.SFU.Network.Json;

namespace TLab.SFU.Network
{
    public enum Direction
    {
        SendOnly,
        RecvOnly,
        SendRecv,
    };

    public static class Const
    {
        public const Direction Send = Direction.SendRecv | Direction.SendOnly;
        public const Direction Recv = Direction.SendOnly | Direction.RecvOnly;
    }

    public static class SerializableTransformExtension
    {
        public static SerializableTransform ToSerializableTransform(this Transform transform) => new SerializableTransform(transform);
    }

    [Serializable]
    public struct SerializableTransform
    {
        public Vector3 position;
        public Vector4 rotation;
        public Vector3 localScale;

        public SerializableTransform(Transform transform)
        {
            this.position = transform.position;
            this.rotation = transform.rotation.ToVec();
            this.localScale = transform.localScale;
        }

        public SerializableTransform(Vector3 position, Quaternion rotation)
        {
            this.position = position;
            this.rotation = rotation.ToVec();
            this.localScale = Vector4.one;
        }

        public SerializableTransform(Vector3 position, Quaternion rotation, Vector3 localScale)
        {
            this.position = position;
            this.rotation = rotation.ToVec();
            this.localScale = localScale;
        }
    }

    [Serializable]
    public struct RigidbodyState
    {
        public static bool operator ==(RigidbodyState a, RigidbodyState b) => (a.used == b.used) && (a.gravity == b.gravity);
        public static bool operator !=(RigidbodyState a, RigidbodyState b) => (a.used != b.used) || (a.gravity != b.gravity);

        [SerializeField, HideInInspector] private bool m_used;

        [SerializeField, HideInInspector] private bool m_gravity;

        public bool used => m_used;

        public bool gravity => m_gravity;

        public RigidbodyState(bool used, bool gravity)
        {
            m_used = used;
            m_gravity = gravity;
        }

        public void Update(bool used, bool gravity)
        {
            m_used = used;
            m_gravity = gravity;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (obj is not RigidbodyState)
                return false;

            var tmp = (RigidbodyState)obj;
            return this == tmp;
        }
    }

    [Serializable]
    public class RequestAuth : IRequest
    {
        public int roomId;
        public int userId;
        public uint token;
        public string sharedKey;

        public RequestAuth(int roomId, string sharedKey, int userId, uint token)
        {
            this.roomId = roomId;
            this.userId = userId;
            this.token = token;
            this.sharedKey = sharedKey;
        }

        public RequestAuth(RequestAuth auth)
        {
            roomId = auth.roomId;
            userId = auth.userId;
            token = auth.token;
            sharedKey = auth.sharedKey;
        }

        [Serializable]
        public class RustFormat
        {
            public int room_id;
            public int user_id;
            public uint token;
            public string shared_key;

            public RustFormat(int roomId, string sharedKey, int userId, uint token)
            {
                this.room_id = roomId;
                this.user_id = userId;
                this.token = token;
                this.shared_key = sharedKey;
            }
        }

        public virtual string ToJson() => JsonUtility.ToJson(new RustFormat(roomId, sharedKey, userId, token));
    }
}
