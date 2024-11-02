using UnityEngine;

namespace TLab.SFU.Network
{
    public enum Direction
    {
        SENDONLY,
        RECVONLY,
        SENDRECV,
    };

    public static class Const
    {
        public const Direction SEND = Direction.SENDRECV | Direction.SENDONLY;
        public const Direction RECV = Direction.SENDONLY | Direction.RECVONLY;
    }

    [System.Serializable]
    public struct WebVector3
    {
        public float x;
        public float y;
        public float z;

        public WebVector3(float x, float y, float z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }

        public Vector3 raw => new Vector3(x, y, z);
    }

    [System.Serializable]
    public struct WebVector4
    {
        public float x;
        public float y;
        public float z;
        public float w;

        public WebVector4(float x, float y, float z, float w)
        {
            this.x = x;
            this.y = y;
            this.z = z;
            this.w = w;
        }

        public Vector4 raw => new Vector4(x, y, z, w);

        public Quaternion rotation => new Quaternion(x, y, z, w);
    }

    [System.Serializable]
    public struct WebTransform
    {
        public WebVector3 position;
        public WebVector4 rotation;
        public WebVector3 scale;

        public WebTransform(WebVector3 position, WebVector4 rotation)
        {
            this.position = position;
            this.rotation = rotation;
            this.scale = new WebVector3(1, 1, 1);
        }

        public WebTransform(WebVector3 position, WebVector4 rotation, WebVector3 scale)
        {
            this.position = position;
            this.rotation = rotation;
            this.scale = scale;
        }

        public WebTransform(Vector3 position, Quaternion rotation)
        {
            this.position = new WebVector3(position.x, position.y, position.z);
            this.rotation = new WebVector4(rotation.x, rotation.y, rotation.z, rotation.w);
            this.scale = new WebVector3(1, 1, 1);
        }

        public WebTransform(Vector3 position, Quaternion rotation, Vector3 scale)
        {
            this.position = new WebVector3(position.x, position.y, position.z);
            this.rotation = new WebVector4(rotation.x, rotation.y, rotation.z, rotation.w);
            this.scale = new WebVector3(scale.x, scale.y, scale.z);
        }
    }

    public enum WebAction
    {
        REGIST,
        REGECT,
        ACEPT,
        EXIT,
        GUEST_DISCONNECT,
        GUEST_PARTICIPATION,
        REFLESH,
        UNI_REFLESH_TRANSFORM,
        UNI_REFLESH_ANIM,
    }

    [System.Serializable]
    public class RequestAuth
    {
        public int room_id;
        public string room_key;
        public int user_id;
        public uint user_token;

        public RequestAuth(int room_id, string room_key, int user_id, uint user_token)
        {
            this.room_id = room_id;
            this.room_key = room_key;
            this.user_id = user_id;
            this.user_token = user_token;
        }

        public RequestAuth(RequestAuth auth)
        {
            this.room_id = auth.room_id;
            this.room_key = auth.room_key;
            this.user_id = auth.user_id;
            this.user_token = auth.user_token;
        }
    }
}
