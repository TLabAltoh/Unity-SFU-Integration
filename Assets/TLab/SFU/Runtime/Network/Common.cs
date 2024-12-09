using UnityEngine;

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

    [System.Serializable]
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
