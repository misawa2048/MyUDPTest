using System.Collections;
using System.Net;
using System.Net.Sockets;
using System.Net.NetworkInformation;
using System.Threading;
using System.Text; // for encoding
using UnityEngine;

namespace TmUDP
{
    public class TmUDPServer : MonoBehaviour
    {
        readonly string IS_BROADCAST = "isBloadcast";
        [SerializeField] string m_myIP = "";
        [SerializeField] string m_host = "localhost";
        [SerializeField] int m_sendPort = 7001;
        [SerializeField] int m_receivePort = 7003;
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
            m_myIP = TmUDPClient.GetIP();
            m_isReceiving = true;
            udpStart();
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
                if ((m_host == "") || (m_host == IS_BROADCAST))
                {
                    m_sendUdp.Connect(IPAddress.Broadcast, m_sendPort);
                    Debug.Log("UDPBroadcast start.");
                }
                else
                {
                    m_sendUdp.Connect(m_host, m_sendPort);
                    Debug.Log("UDPSend start.");
                }
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
                        Debug.Log("ServerRecv:");
                        try
                        {
                            IPEndPoint remoteEP = null;
                            byte[] data = m_receiveUdp.Receive(ref remoteEP);
                            string text = System.Text.Encoding.UTF8.GetString(data);
                            Debug.Log("ServerRecv:" + text);
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
    }
}
