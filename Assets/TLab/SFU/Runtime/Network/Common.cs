using System.Text;
using UnityEngine;

namespace TLab.SFU.Network
{
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
        public string room_pass;
        public int user_id;
        public uint user_token;

        public RequestAuth(int room_id, string room_pass, int user_id, uint user_token)
        {
            this.room_id = room_id;
            this.room_pass = room_pass;
            this.user_id = user_id;
            this.user_token = user_token;
        }

        public RequestAuth(RequestAuth auth)
        {
            this.room_id = auth.room_id;
            this.room_pass = auth.room_pass;
            this.user_id = auth.user_id;
            this.user_token = auth.user_token;
        }
    }

    public interface Packetable
    {
        public const int HEADER_SIZE = 9;   // typ (1) + from (4) + to (4)

        public byte[] Marshall();

        public void UnMarshall(byte[] bytes);

        public static byte[] MarshallJson(int pktId, in object @object)
        {
            var json = JsonUtility.ToJson(@object);
            return UnsafeUtility.Combine(pktId, Encoding.UTF8.GetBytes(json));
        }

        public static void UnMarshallJson(byte[] bytes, in object @object)
        {
            var json = Encoding.UTF8.GetString(bytes, SyncClient.PAYLOAD_OFFSET, bytes.Length - SyncClient.PAYLOAD_OFFSET);
            JsonUtility.FromJsonOverwrite(json, @object);
        }
    }
}
