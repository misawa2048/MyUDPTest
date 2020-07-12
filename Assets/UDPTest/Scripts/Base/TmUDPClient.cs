using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Net.NetworkInformation;
using System.Threading;
using System.Text; // for encoding
using UnityEngine;

namespace TmUDP
{
    public class TmUDPClient : MonoBehaviour
    {
        static public readonly string KWD_QUIT = "QuitClient";

        [System.Serializable] public class ReceiveEvent : UnityEngine.Events.UnityEvent<byte[]> { }
        [System.Serializable] public class NumChangeEvent : UnityEngine.Events.UnityEvent<string[]> { }

        [SerializeField, ReadOnly] string m_myIP = "";
        public string myIP { get { return m_myIP; } }
        [SerializeField, ReadOnlyWhenPlaying] string m_host = "localhost";
        [SerializeField,ReadOnlyWhenPlaying] int m_sendPort = 7003;
        [SerializeField, ReadOnlyWhenPlaying] int m_receivePort = 7001;
        [SerializeField] ReceiveEvent m_onReceiveEvnts = new ReceiveEvent();
        [SerializeField] NumChangeEvent m_onAddClientEvnts = new NumChangeEvent();
        [SerializeField] NumChangeEvent m_onRemoveClientEvnts = new NumChangeEvent();
        //[SerializeField, ReadOnly, Tooltip("client list from base class")]
        List<string> m_clientList = null;
        private UdpClient m_sendUdp;
        private UdpClient m_receiveUdp;
        private Thread m_thread;
        private bool m_isReceiving;
        private List<byte[]> m_thRecvList = null;
        private List<string> m_thAaddedClientList = null;
        private List<string> m_thRemovedClientList = null;

        // Start is called before the first frame update
        public virtual void Start()
        {
            m_sendUdp = null;
            m_receiveUdp = null;
            m_thread = null;
            m_isReceiving = true;
            m_myIP = GetIP();
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

        internal void SendData(byte[] _data)
        {
            m_sendUdp.Send(_data, _data.Length);
        }

        void OnApplicationQuit()
        {
            udpStop();
        }

        private void OnDestroy()
        {
            udpStop();
        }

        private void udpStart()
        {
            if (m_sendUdp == null)
            {
                m_sendUdp = new UdpClient();
                m_sendUdp.Connect(m_host, m_sendPort);
                Debug.Log("UDPClientSend start.");
            }

            if (m_receiveUdp == null)
            {
                m_receiveUdp = new UdpClient(m_receivePort);
                m_receiveUdp.Client.ReceiveTimeout = 1000;
                Debug.Log("UDPClientReceive start.");
            }

            if (m_thread == null)
            {
                m_thread = new Thread(new ThreadStart(ThreadMethod));
                m_thread.Start();
            }
        }

        private void udpStop()
        {
            if (m_sendUdp != null)
            {
                string str = m_myIP + "," + KWD_QUIT;
                byte[] data = System.Text.Encoding.UTF8.GetBytes(str);
                m_sendUdp.Send(data, data.Length);
            }

            if (m_thread != null)
            {
                m_thread.Abort();
                m_thread = null;
                Debug.Log("UDPClientThreadStopped");
            }
            if (m_sendUdp != null)
            {
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
                    if (dataArr[1].StartsWith(TmUDPClient.KWD_QUIT))
                    {
                        if (m_clientList.Contains(dataArr[0]))
                        {
                            m_thRemovedClientList.Add(_text);
                        }
                    }
                }
            }
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
                            thUpdate(data);
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

        // https://stackoverflow.com/questions/51975799/how-to-get-ip-address-of-device-in-unity-2018
        public enum ADDRESSFAM { IPv4, IPv6 }
        public static string GetIP(ADDRESSFAM Addfam = ADDRESSFAM.IPv4)
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

        public static string Vector3ToFormatedStr(Vector3 _vec, int _numDecimalPoint)
        {
            string fmt = "F" + _numDecimalPoint.ToString();
            string str = _vec.x.ToString(fmt) + ","+_vec.y.ToString(fmt)+","+_vec.z.ToString(fmt);
            return str;
        }
        public static string AngleYToFormatedStr(float _angY, int _numDecimalPoint)
        {
            string fmt = "F" + _numDecimalPoint.ToString();
            string str = _angY.ToString(fmt);
            return str;
        }
        public static string QuaternionToFormatedStr(Quaternion _rot, int _numDecimalPoint)
        {
            string fmt = "F" + _numDecimalPoint.ToString();
            string str = _rot.x.ToString(fmt) + "," +_rot.y.ToString(fmt) + ","
                +_rot.z.ToString(fmt) + "," +_rot.w.ToString(fmt);
            return str;
        }
    }
}
