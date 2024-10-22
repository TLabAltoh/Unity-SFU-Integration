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

    [System.Serializable]
    public struct Address32
    {
        [SerializeField] private byte m_a0;
        [SerializeField] private byte m_a1;
        [SerializeField] private byte m_a2;
        [SerializeField] private byte m_a3;

        [SerializeField] private int m_hash;

        public byte a0 => m_a0;
        public byte a1 => m_a1;
        public byte a2 => m_a2;
        public byte a3 => m_a3;

        public Address32(byte a0, byte a1, byte a2, byte a3)
        {
            m_a0 = a0;
            m_a1 = a1;
            m_a2 = a2;
            m_a3 = a3;
            m_hash = System.HashCode.Combine(m_a0, m_a1, m_a2, m_a3);
        }

        public void Update(byte a0, byte a1, byte a2, byte a3)
        {
            m_a0 = a0;
            m_a1 = a1;
            m_a2 = a2;
            m_a3 = a3;
            m_hash = System.HashCode.Combine(m_a0, m_a1, m_a2, m_a3);
        }

        public unsafe void CopyTo(byte* ptr)
        {
            ptr[0] = m_a0;
            ptr[1] = m_a1;
            ptr[2] = m_a2;
            ptr[3] = m_a3;
        }

        public void Copy(in Address32 from)
        {
            Update(from.a0, from.a1, from.a2, from.a3);
        }

        public unsafe void Copy(byte* from)
        {
            Update(from[0], from[1], from[2], from[3]);
        }

        public override int GetHashCode()
        {
            return m_hash;
        }

        public bool Equals(Address32 address)
        {
            return
                (address.m_a0 == m_a0) &&
                (address.m_a1 == m_a1) &&
                (address.m_a2 == m_a2) &&
                (address.m_a3 == m_a3);
        }
    }

    [System.Serializable]
    public struct Address64
    {
        [SerializeField] private byte m_a0;
        [SerializeField] private byte m_a1;
        [SerializeField] private byte m_a2;
        [SerializeField] private byte m_a3;

        [SerializeField] private byte m_a4;
        [SerializeField] private byte m_a5;
        [SerializeField] private byte m_a6;
        [SerializeField] private byte m_a7;

        [SerializeField] private int m_hash;

        public byte a0 => m_a0;
        public byte a1 => m_a1;
        public byte a2 => m_a2;
        public byte a3 => m_a3;

        public byte a4 => m_a4;
        public byte a5 => m_a5;
        public byte a6 => m_a6;
        public byte a7 => m_a7;

        public Address64(byte a0, byte a1, byte a2, byte a3, byte a4, byte a5, byte a6, byte a7)
        {
            m_a0 = a0;
            m_a1 = a1;
            m_a2 = a2;
            m_a3 = a3;

            m_a4 = a4;
            m_a5 = a5;
            m_a6 = a6;
            m_a7 = a7;

            m_hash = System.HashCode.Combine(m_a0, m_a1, m_a2, m_a3, m_a4, m_a5, m_a6, m_a7);
        }

        public void Update(byte a0, byte a1, byte a2, byte a3, byte a4, byte a5, byte a6, byte a7)
        {
            m_a0 = a0;
            m_a1 = a1;
            m_a2 = a2;
            m_a3 = a3;

            m_a4 = a4;
            m_a5 = a5;
            m_a6 = a6;
            m_a7 = a7;

            m_hash = System.HashCode.Combine(m_a0, m_a1, m_a2, m_a3, m_a4, m_a5, m_a6, m_a7);
        }

        public void UpdateUpper32(byte a0, byte a1, byte a2, byte a3)
        {
            m_a0 = a0;
            m_a1 = a1;
            m_a2 = a2;
            m_a3 = a3;
            m_hash = System.HashCode.Combine(m_a0, m_a1, m_a2, m_a3, m_a4, m_a5, m_a6, m_a7);
        }

        public void UpdateLower32(byte a4, byte a5, byte a6, byte a7)
        {
            m_a4 = a4;
            m_a5 = a5;
            m_a6 = a6;
            m_a7 = a7;
            m_hash = System.HashCode.Combine(m_a0, m_a1, m_a2, m_a3, m_a4, m_a5, m_a6, m_a7);
        }

        public unsafe void CopyTo(byte* ptr)
        {
            ptr[0] = m_a0;
            ptr[1] = m_a1;
            ptr[2] = m_a2;
            ptr[3] = m_a3;
            ptr[4] = m_a4;
            ptr[5] = m_a5;
            ptr[6] = m_a6;
            ptr[7] = m_a7;
        }

        public void Copy(in Address64 from)
        {
            Update(from.a0, from.a1, from.a2, from.a3, from.a4, from.a5, from.a6, from.a7);
        }

        public unsafe void Copy(byte* from)
        {
            Update(from[0], from[1], from[2], from[3], from[4], from[5], from[6], from[7]);
        }

        public void CopyUpper32(in Address32 from)
        {
            UpdateUpper32(from.a0, from.a1, from.a2, from.a3);
        }

        public void CopyLower32(in Address32 from)
        {
            UpdateLower32(from.a0, from.a1, from.a2, from.a3);
        }

        public override int GetHashCode()
        {
            return m_hash;
        }

        public bool Equals(Address64 address)
        {
            return
                (address.m_a0 == m_a0) &&
                (address.m_a1 == m_a1) &&
                (address.m_a2 == m_a2) &&
                (address.m_a3 == m_a3) &&
                (address.m_a4 == m_a4) &&
                (address.m_a5 == m_a5) &&
                (address.m_a6 == m_a6) &&
                (address.m_a7 == m_a7);
        }
    };

    public interface Packetable
    {
        public const int HEADER_SIZE = 9;   // typ (1) + from (4) + to (4)

        public byte[] Marshall();

        public void UnMarshall(byte[] bytes);
    }
}
