using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Net.NetworkInformation;
using System.Threading;
using System.Linq;
using UnityEngine;

namespace TmUDP
{
    [System.Serializable] public class ReceiveEvent : UnityEngine.Events.UnityEvent<byte[]> { }
    [System.Serializable] public class NumChangeEvent : UnityEngine.Events.UnityEvent<string[]> { }

    public class TmUDPModule : MonoBehaviour
    {
        static public readonly string IS_BROADCAST = "isBloadcast";
        static public readonly string KWD_QUIT = "QuitClient";
        static public readonly string KWD_INIT = "InitClient";
        [SerializeField, ReadOnly] internal bool m_isServer = true;

        [SerializeField, ReadOnly] internal string m_myIP = "";
        public string myIP { get { return m_myIP; } }
        [SerializeField, ReadOnlyWhenPlaying] internal string m_host = "localhost"; // m_isServer && "" > bloadcast
        public string host { get { return m_host; } }
        [SerializeField, ReadOnlyWhenPlaying] internal int m_sendPort = 7001;
        [SerializeField, ReadOnlyWhenPlaying] internal int m_receivePort = 7003;
        [SerializeField, ReadOnlyWhenPlaying] internal int m_receiveTimeout = 1000;
        [SerializeField] internal ReceiveEvent m_onReceiveEvnts = new ReceiveEvent();
        [SerializeField] internal NumChangeEvent m_onAddClientEvnts = new NumChangeEvent();
        [SerializeField] internal NumChangeEvent m_onRemoveClientEvnts = new NumChangeEvent();
        [SerializeField, ReadOnly, Tooltip("client list from base class")]
        internal List<string> m_clientList = null;
        internal UdpClient m_sendUdp;
        internal UdpClient m_receiveUdp;
        internal Thread m_thread;
        internal bool m_isReceiving;
        internal List<byte[]> m_thRecvList=null;
        internal List<string> m_thAaddedClientList = null;
        internal List<string> m_thRemovedClientList = null;

        // Start is called before the first frame update
        public virtual void Start()
        {
            m_sendUdp = null;
            m_receiveUdp = null;
            m_thread = null;
            m_myIP = TmUDPClient.GetIP();
            m_isReceiving = true;
            m_clientList = new List<string>();
            //--lock
            m_thRecvList = new List<byte[]>();
            m_thAaddedClientList = new List<string>();
            m_thRemovedClientList = new List<string>();
            //--!lock
            udpStart();
        }

        // Update is called once per frame
        public virtual void Update()
        {
            if (m_isReceiving)
            {
                udpStart();
            }
            else
            {
                udpStop();
            }

            lock (m_thread)
            { // m_thread.lock
                foreach (string dataStr in m_thAaddedClientList)
                {
                    string[] dataArr = dataStr.Split(',');
                    string ipStr = dataArr[0];
                    if (!m_clientList.Contains(ipStr))
                    {
                        m_onAddClientEvnts.Invoke(dataArr);
                        m_clientList.Add(ipStr);
                    }
                }
                m_thAaddedClientList.Clear();

                foreach (byte[] data in m_thRecvList)
                {
                    m_onReceiveEvnts.Invoke(data);
                }
                m_thRecvList.Clear();

                foreach (string dataStr in m_thRemovedClientList)
                {
                    string[] dataArr = dataStr.Split(',');
                    string ipStr = dataArr[0];
                    if (m_clientList.Contains(ipStr))
                    {
                        m_onRemoveClientEvnts.Invoke(dataArr);
                        m_clientList.Remove(ipStr);
                    }
                }
                m_thRemovedClientList.Clear();
            } // m_thread.resume
        }

        internal virtual void OnApplicationQuit()
        {
            udpStop();
        }

        internal virtual void OnDestroy()
        {
            udpStop();
        }

        internal virtual void OnApplicationPause(bool isPpause)
        {
#if (UNITY_ANDROID || UNITY_IPHONE) && !UNITY_EDITOR
            if (isPpause)
            {
                udpStop();
                Application.runInBackground = false;
                Application.Quit();
            }
#endif
        }

        private void udpStart()
        {
            if (m_sendUdp == null)
            {
                m_sendUdp = new UdpClient();
                if (m_isServer)
                { //-- for server --
                    if ((m_host == "") || (m_host == IS_BROADCAST))
                    {
                        m_host = IS_BROADCAST;
                        m_sendUdp.Connect(IPAddress.Broadcast, m_sendPort);
                        Debug.Log("UDPServerBroadcast start." + m_sendUdp.EnableBroadcast);
                    }
                    else
                    {
                        m_sendUdp.Connect(m_host, m_sendPort);
                        Debug.Log("UDPServerSend start." + m_sendUdp.EnableBroadcast);
                    }
                }
                else
                { // -- for client --
                    m_sendUdp.Connect(m_host, m_sendPort);
                    Debug.Log("UDPClientSend start.");
                }
            }

            if (m_receiveUdp == null)
            {
                m_receiveUdp = new UdpClient(m_receivePort);
                m_receiveUdp.Client.ReceiveTimeout = m_receiveTimeout;
                if (m_isServer)
                { //-- for server --
                    Debug.Log("UDPServerReceive start.");
                }
                else
                { // -- for client --
                    Debug.Log("UDPClientReceive start.");
                }
            }
            if (m_thread == null)
            {
                m_thread = new Thread(new ThreadStart(ThreadMethod));
                m_thread.Start();
            }
        }

        private void udpStop()
        {
            m_isReceiving = false;
            if (m_thread != null)
            {
                m_thread.Abort();
                m_thread = null;
                if (m_isServer)
                { //-- for server --
                    Debug.Log("UDPServerThreadStopped");
                }
                else
                { // -- for client --
                    Debug.Log("UDPClientThreadStopped");
                }
            }
            if (m_sendUdp != null)
            {
                if (m_isServer)
                { //-- for server --
                }
                else
                { // -- for client --
                    string str = m_myIP + "," + TmUDPModule.KWD_QUIT;
                    byte[] data = System.Text.Encoding.UTF8.GetBytes(str);
                    m_sendUdp.Send(data, data.Length);
                }
                m_sendUdp.Close();
                m_sendUdp = null;
            }
            if (m_receiveUdp != null)
            {
                m_receiveUdp.Close();
                m_receiveUdp = null;
            }
        }

        private void ThreadMethod()
        {
            void thManageClient(string _text)
            {
                string[] dataArr = _text.Split(',');

                // ADD
                if (dataArr.Length > 0)
                {
                    if (!m_clientList.Contains(dataArr[0]))
                    {
                        m_thAaddedClientList.Add(_text);
                    }
                }

                // REMOVE
                if (dataArr.Length > 1)
                {
                    if (dataArr[1].StartsWith(KWD_QUIT))
                    {
                        if (m_clientList.Contains(dataArr[0]))
                        {
                            m_thRemovedClientList.Add(_text);
                        }
                    }
                }
            }
            // -- server only --
            void thBroadcast(byte[] _data)
            {
                m_thRecvList.Add(_data);
                if (m_sendUdp.EnableBroadcast)
                {
                    m_sendUdp.Send(_data, _data.Length);
                }
                else
                {
                    foreach (string client in m_clientList)
                    {
                        //m_sendUdp.Send(_data, _data.Length, client, m_sendPort);
                        m_sendUdp.Connect(client, m_sendPort);
                        m_sendUdp.Send(_data, _data.Length);
                    }
                }
            }
            // -- client only --
            void thUpdate(byte[] _data)
            {
                m_thRecvList.Add(_data);
            }

            while (true)
            {
                if (m_isReceiving)
                {
                    if (m_receiveUdp.Available>0)
                    {
                        try
                        {
                            IPEndPoint remoteEP = null;
                            byte[] data = m_receiveUdp.Receive(ref remoteEP);
                            string text = System.Text.Encoding.UTF8.GetString(data);
                            thManageClient(text);
                            if (m_isServer)
                            { //-- for server --
                                thBroadcast(data);
                            }
                            else
                            { // -- for client --
                                thUpdate(data);
                            }
                        }
                        catch (SocketException e)
                        {
                            Debug.Log(e.ToString());
                        }
                    }
                }
                else
                {
                }
            }
        }

        internal string getDataStrFromPosition(string _ip, Vector3 _position)
        {
            string valStr = TmUDP.TmUDPClient.Vector3ToFormatedStr(_position, 2);
            return _ip + "," + MyUDPServer.KWD_POS + "," + valStr;
        }
        internal string getDataStrFromAngY(string _ip, float _angY)
        {
            string valStr = TmUDP.TmUDPClient.AngleYToFormatedStr(_angY, 2);
            return _ip + "," + MyUDPServer.KWD_RORY + "," + valStr;
        }

        internal void SendDataFromDataStr(string _dataStr)
        {
            this.SendData(System.Text.Encoding.UTF8.GetBytes(_dataStr));
            Debug.Log(_dataStr);
        }
        internal void SendData(byte[] _data)
        {
            m_sendUdp.Send(_data, _data.Length);
        }

        // https://stackoverflow.com/questions/51975799/how-to-get-ip-address-of-device-in-unity-2018
        public enum ADDRESSFAM { IPv4, IPv6 }
        public static string GetIP(ADDRESSFAM Addfam)
        {
            //Return null if ADDRESSFAM is Ipv6 but Os does not support it
            if (Addfam == ADDRESSFAM.IPv6 && !Socket.OSSupportsIPv6)
            {
                return null;
            }

            string output = "";

            foreach (NetworkInterface item in NetworkInterface.GetAllNetworkInterfaces())
            {
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
                NetworkInterfaceType _type1 = NetworkInterfaceType.Wireless80211;
                NetworkInterfaceType _type2 = NetworkInterfaceType.Ethernet;

                if ((item.NetworkInterfaceType == _type1 || item.NetworkInterfaceType == _type2) && item.OperationalStatus == OperationalStatus.Up)
#endif
                {
                    foreach (UnicastIPAddressInformation ip in item.GetIPProperties().UnicastAddresses)
                    {
                        //IPv4
                        if (Addfam == ADDRESSFAM.IPv4)
                        {
                            if (ip.Address.AddressFamily == AddressFamily.InterNetwork)
                            {
                                output = ip.Address.ToString();
                            }
                        }

                        //IPv6
                        else if (Addfam == ADDRESSFAM.IPv6)
                        {
                            if (ip.Address.AddressFamily == AddressFamily.InterNetworkV6)
                            {
                                output = ip.Address.ToString();
                            }
                        }
                    }
                }
            }
            return output;
        }

        public static string GetLocalIP()
        {
            string ret = "localhost";
            IPHostEntry host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (IPAddress ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    ret = ip.ToString();
                    break;
                }
            }
            return ret;
        }

        public static string GetIP()
        {
#if UNITY_ANDROID || UNITY_IPHONE
            return GetLocalIP();
#else
            return GetLocalIP(ADDRESSFAM.IPv4);
#endif
        }


        internal static string Vector3ToFormatedStr(Vector3 _vec, int _numDecimalPoint)
        {
            string fmt = "F" + _numDecimalPoint.ToString();
            string str = _vec.x.ToString(fmt) + "," + _vec.y.ToString(fmt) + "," + _vec.z.ToString(fmt);
            return str;
        }
        internal static string AngleYToFormatedStr(float _angY, int _numDecimalPoint)
        {
            string fmt = "F" + _numDecimalPoint.ToString();
            string str = _angY.ToString(fmt);
            return str;
        }
        internal static string QuaternionToFormatedStr(Quaternion _rot, int _numDecimalPoint)
        {
            string fmt = "F" + _numDecimalPoint.ToString();
            string str = _rot.x.ToString(fmt) + "," + _rot.y.ToString(fmt) + ","
                + _rot.z.ToString(fmt) + "," + _rot.w.ToString(fmt);
            return str;
        }
    }
}
