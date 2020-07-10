using System.Collections;
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
        [SerializeField] string m_myIP = "";
        [SerializeField] string m_host = "localhost";
        [SerializeField] int m_sendPort = 7003;
        [SerializeField] int m_receivePort = 7001;
        private UdpClient m_sendUdp;
        private UdpClient m_receiveUdp;
        private Thread m_thread;
        private bool m_isReceiving;

        // Start is called before the first frame update
        void Start()
        {
            m_sendUdp = null;
            m_receiveUdp = null;
            m_thread = null;
            m_isReceiving = true;
            m_myIP = GetIP();
            udpStart();
            StartCoroutine(udpDbgSendCo());
        }

        // Update is called once per frame
        void Update()
        {
            if (m_isReceiving)
            {
                udpStart();
            }
            else
            {
                udpStop();
            }
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
                Debug.Log("UDPSend start.");
            }

            if (m_receiveUdp == null)
            {
                m_receiveUdp = new UdpClient(m_receivePort);
                m_receiveUdp.Client.ReceiveTimeout = 1000;
                Debug.Log("UDPReceive start.");
            }

            if (m_thread == null)
            {
                m_thread = new Thread(new ThreadStart(ThreadMethod));
                m_thread.Start();
            }
        }

        private void udpStop()
        {
            if (m_thread != null)
            {
                m_thread.Abort();
                m_thread = null;
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
            Debug.Log("udpStopped");
        }

        private void ThreadMethod()
        {
            while (true)
            {
                if (m_isReceiving)
                {
                    if (m_receiveUdp.Available>0)
                    {
                        Debug.Log("ClientRecv:");
                        try
                        {
                            IPEndPoint remoteEP = null;
                            byte[] data = m_receiveUdp.Receive(ref remoteEP);
                            string text = System.Text.Encoding.UTF8.GetString(data);
                            Debug.Log("ClientRecv:" + text);
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

        IEnumerator udpDbgSendCo()
        {
            while (true)
            {
                yield return new WaitForSeconds(1.0f);
                string str = m_myIP+",Pos," + Random.value + ",0.2,:0.3";
                byte[] data = System.Text.Encoding.UTF8.GetBytes(str);
                m_sendUdp.Send(data, data.Length);
                Debug.Log(str);
            }
        }
    }
}
