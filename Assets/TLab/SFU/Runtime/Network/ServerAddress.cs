using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

namespace TLab.SFU.Network
{
    public enum Protocol
    {
        WebSocket,
        HTTP,
        HTTPS
    }

    [CreateAssetMenu(menuName = "TLab/SFU/Server Address")]
    public class ServerAddress : ScriptableObject
    {
        public new string name;
        public string addr;
        public Protocol protocol;

        public void UpdateConfig(string name, string addr, Protocol protocol)
        {
            this.name = name;
            this.addr = addr;
            this.protocol = protocol;
        }

        public void UpdateConfig(string addr, string port = null)
        {
            string newAddr = PROTOCOL[protocol] + "://" + addr;

            if (port != null)
            {
                newAddr += ":" + port;
            }

            this.addr = newAddr;
        }

        public static Dictionary<Protocol, string> PROTOCOL = new Dictionary<Protocol, string>
        {
            { Protocol.WebSocket, "ws" },
            { Protocol.HTTP, "http" },
            { Protocol.HTTPS, "https" }
        };

        public static bool IsMatch(Protocol type, string addr)
        {
            if (Regex.IsMatch(addr, PROTOCOL[type] + @"://\d{1,3}(\.\d{1,3}){3}(:\d{1,7})?"))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public static ServerAddress CreateAddress(string name, Protocol protocol, string addr, string port = null)
        {
            var address = new ServerAddress();

            string newAddr = PROTOCOL[protocol] + "://" + addr;

            if (port != null)
            {
                newAddr += ":" + port;
            }

            address.name = name;
            address.addr = newAddr;
            address.protocol = protocol;

            return address;
        }
    }
}