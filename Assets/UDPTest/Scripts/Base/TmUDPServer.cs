using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Net.NetworkInformation;
using System.Threading;
using System.Text; // for encoding
using System.Linq;
using UnityEngine;

namespace TmUDP
{
    public class TmUDPServer : MonoBehaviour
    {
        [System.Serializable] public class ReceiveEvent : UnityEngine.Events.UnityEvent<byte[]> { }
        [System.Serializable] public class NumChangeEvent : UnityEngine.Events.UnityEvent<string[]> { }

        readonly string IS_BROADCAST = "isBloadcast";
        [SerializeField] string m_myIP = "";
        [SerializeField] string m_host = ""; // bloadcast
        [SerializeField] int m_sendPort = 7001;
        [SerializeField] int m_receivePort = 7003;
        [SerializeField] ReceiveEvent m_onReceiveEvnts = new ReceiveEvent();
        [SerializeField] NumChangeEvent m_onAddClientEvnts = new NumChangeEvent();
        [SerializeField] NumChangeEvent m_onRemoveClientEvnts = new NumChangeEvent();
        [SerializeField, Tooltip("client list from base class")] List<string> m_clientList = null;
        private UdpClient m_sendUdp;
        private UdpClient m_receiveUdp;
        private Thread m_thread;
        private bool m_isReceiving;
        private List<byte[]> m_thRecvList;
        private List<string> m_thAaddedClientList = null;
        private List<string> m_thRemovedClientList = null;
        //public List<byte[]> clientList { get { return m_clientList; } }

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
                    m_host = IS_BROADCAST;
                    m_sendUdp.Connect(IPAddress.Broadcast, m_sendPort);
                    Debug.Log("UDPServerBroadcast start."+ m_sendUdp.EnableBroadcast);
                }
                else
                {
                    m_sendUdp.Connect(m_host, m_sendPort);
                    Debug.Log("UDPServerSend start."+ m_sendUdp.EnableBroadcast);
                }
            }

            if (m_receiveUdp == null)
            {
                m_receiveUdp = new UdpClient(m_receivePort);
                m_receiveUdp.Client.ReceiveTimeout = 1000;
                Debug.Log("UDPServerReceive start.");
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
                Debug.Log("UDPServerThreadStopped");
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
                    if (dataArr[1].StartsWith(TmUDPClient.CLIENT_QUIT))
                    {
                        if (m_clientList.Contains(dataArr[0]))
                        {
                            m_thRemovedClientList.Add(_text);
                        }
                    }
                }
            }
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
                            thBroadcast(data);
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
