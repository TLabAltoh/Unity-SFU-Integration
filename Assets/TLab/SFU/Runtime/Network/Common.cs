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

    public static class WebTransformExtension
    {
        public static WebTransform ToWebTransform(this Transform transform) => new WebTransform(transform);
    }

    [Serializable]
    public struct WebTransform
    {
        public Vector3 position;
        public Vector4 rotation;
        public Vector3 localScale;

        public WebTransform(Transform transform)
        {
            this.position = transform.position;
            this.rotation = transform.rotation.ToVec();
            this.localScale = transform.localScale;
        }

        public WebTransform(Vector3 position, Quaternion rotation)
        {
            this.position = position;
            this.rotation = rotation.ToVec();
            this.localScale = Vector4.one;
        }

        public WebTransform(Vector3 position, Quaternion rotation, Vector3 localScale)
        {
            this.position = position;
            this.rotation = rotation.ToVec();
            this.localScale = localScale;
        }
    }

    //public enum WebAction
    //{
    //    REGIST,
    //    REGECT,
    //    ACEPT,
    //    EXIT,
    //    GUEST_DISCONNECT,
    //    GUEST_PARTICIPATION,
    //    REFLESH,
    //    UNI_REFLESH_TRANSFORM,
    //    UNI_REFLESH_ANIM,
    //}

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
