using UnityEngine;

namespace TLab.SFU
{
    [System.Serializable]
    public struct Address32
    {
        public static bool operator ==(Address32 a, Address32 b) => a.Equals(b);
        public static bool operator !=(Address32 a, Address32 b) => !a.Equals(b);

        [SerializeField] private byte m_a0;
        [SerializeField] private byte m_a1;
        [SerializeField] private byte m_a2;
        [SerializeField] private byte m_a3;

        [SerializeField] private int m_hash;

        public byte a0 => m_a0;
        public byte a1 => m_a1;
        public byte a2 => m_a2;
        public byte a3 => m_a3;

        public int hash => m_hash;

        public Address32(byte a0, byte a1, byte a2, byte a3)
        {
            m_a0 = a0;
            m_a1 = a1;
            m_a2 = a2;
            m_a3 = a3;
            m_hash = Cryptography.MD5From(m_a0, m_a1, m_a2, m_a3);
        }

        public void Update(byte a0, byte a1, byte a2, byte a3)
        {
            m_a0 = a0;
            m_a1 = a1;
            m_a2 = a2;
            m_a3 = a3;
            m_hash = Cryptography.MD5From(m_a0, m_a1, m_a2, m_a3);
        }

        public unsafe void CopyTo(byte* ptr)
        {
            ptr[0] = m_a0;
            ptr[1] = m_a1;
            ptr[2] = m_a2;
            ptr[3] = m_a3;
        }

        public void Copy(in Address32 from) => Update(from.a0, from.a1, from.a2, from.a3);

        public unsafe void Copy(byte* from) => Update(from[0], from[1], from[2], from[3]);

        public override int GetHashCode() => m_hash;

        public override bool Equals(object obj)
        {
            if (obj is Address32)
                return Equals((Address32)obj);
            return false;
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
        public static bool operator ==(Address64 a, Address64 b) => a.Equals(b);
        public static bool operator !=(Address64 a, Address64 b) => !a.Equals(b);

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

        public int hash => m_hash;

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

            m_hash = Cryptography.MD5From(m_a0, m_a1, m_a2, m_a3, m_a4, m_a5, m_a6, m_a7);
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

            m_hash = Cryptography.MD5From(m_a0, m_a1, m_a2, m_a3, m_a4, m_a5, m_a6, m_a7);
        }

        public void UpdateUpper32(byte a0, byte a1, byte a2, byte a3)
        {
            m_a0 = a0;
            m_a1 = a1;
            m_a2 = a2;
            m_a3 = a3;
            m_hash = Cryptography.MD5From(m_a0, m_a1, m_a2, m_a3, m_a4, m_a5, m_a6, m_a7);
        }

        public void UpdateLower32(byte a4, byte a5, byte a6, byte a7)
        {
            m_a4 = a4;
            m_a5 = a5;
            m_a6 = a6;
            m_a7 = a7;
            m_hash = Cryptography.MD5From(m_a0, m_a1, m_a2, m_a3, m_a4, m_a5, m_a6, m_a7);
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

        public void Copy(in Address64 from) => Update(from.a0, from.a1, from.a2, from.a3, from.a4, from.a5, from.a6, from.a7);

        public unsafe void Copy(byte* from) => Update(from[0], from[1], from[2], from[3], from[4], from[5], from[6], from[7]);

        public void CopyUpper32(in Address32 from) => UpdateUpper32(from.a0, from.a1, from.a2, from.a3);

        public void CopyLower32(in Address32 from) => UpdateLower32(from.a0, from.a1, from.a2, from.a3);

        public override int GetHashCode() => m_hash;

        public override bool Equals(object obj)
        {
            if (obj is Address64)
                return Equals((Address64)obj);
            return false;
        }

        public bool Equals(in Address64 address)
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
    }
}
