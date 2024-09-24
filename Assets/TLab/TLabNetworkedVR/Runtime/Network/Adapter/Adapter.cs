using UnityEngine;
using UnityEngine.Events;

namespace TLab.NetworkedVR.Network
{
    [CreateAssetMenu(fileName = "Adapter", menuName = "TLab/NetworkedVR/Adapter")]
    public class Adapter : ScriptableObject
    {
        [SerializeField] private RoomAdapter m_room;
        [SerializeField] private UserAdapter m_user;

        public RoomAdapter room => m_room;
        public UserAdapter user => m_user;

        public void Init(RoomAdapter room, UserAdapter user)
        {
            m_room = room;
            m_user = user;
        }

        public Adapter GetClone()
        {
            var instance = CreateInstance<Adapter>();

            instance.Init(m_room.GetClone(), m_user.GetClone());

            return instance;
        }

        public void CreateRoom(MonoBehaviour mono, UnityAction<string> callback)
        {
            m_room.CreateRoom(mono, callback);
        }

        public void DeleteRoom(MonoBehaviour mono, UnityAction<string> callback)
        {
            m_room.DeleteRoom(mono, callback);
        }

        public void JoinRoom(MonoBehaviour mono, UnityAction<string> callback)
        {
            m_user.JoinRoom(m_room, mono, callback);
        }

        public void ExitRoom(MonoBehaviour mono, UnityAction<string> callback)
        {
            m_user.ExitRoom(m_room, mono, callback);
        }
    }
}
