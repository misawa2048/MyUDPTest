using System.Collections;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;

namespace TmUDPTest
{
    public class UDPReceiveTest : MonoBehaviour
    {
        readonly string KEY_NAME = "ReceivePort";
        static readonly int RingSize = 16;
        [SerializeField] InputField m_portField = null;
        [SerializeField] InputField m_outputField = null;
        [SerializeField] int m_port = 7001;
        private UdpClient m_udp;
        private Thread m_thread;
        private string[] m_recvTextArr;
        private int m_buffPtr;
        private bool m_isReceiving;

        public string[] recvTextArr { get { return m_recvTextArr; } }

        private void buffClear()
        {
            m_recvTextArr = new string[RingSize];
            m_buffPtr = 0;
            for (int i = 0; i < m_recvTextArr.Length; ++i)
            {
                m_recvTextArr[i] = "";
            }
        }

        void Start()
        {
            if (PlayerPrefs.HasKey(KEY_NAME))
            {
                Debug.Log("HasKey"+ KEY_NAME);
                m_port = PlayerPrefs.GetInt(KEY_NAME);
                m_portField.text = m_port.ToString();
            }
            else
            {
                Debug.Log("!HasKey"+ KEY_NAME);
                int.TryParse( m_portField.text, out m_port);
                PlayerPrefs.SetInt(KEY_NAME, m_port);
            }
            m_isReceiving = true;
            buffClear();
            m_udp = null;
            m_thread = null;
            udpStart();
        }

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

            int ptr = m_buffPtr;
            string outStr = "";
            for (int i = 0; i < m_recvTextArr.Length; ++i)
            {
                outStr += ((i > 0) ? "\n" : "") + m_recvTextArr[ptr];
                ptr--;
                if (ptr < 0)
                {
                    ptr += RingSize;
                }
            }
            m_outputField.text = outStr;
        }

        void OnApplicationQuit()
        {
            udpStop();
        }

        private void OnDestroy()
        {
            udpStop();
        }

        public void OnEditChange(string _str)
        {
            string str = m_port.ToString();
            int.TryParse(m_portField.text, out m_port);
            PlayerPrefs.SetInt(KEY_NAME, m_port);
            str += "->"+m_port.ToString();
            Debug.Log("Change port:" + str);
        }

        private void udpStart()
        {
            if (m_udp == null)
            {
                m_udp = new UdpClient(m_port);
                m_udp.Client.ReceiveTimeout = 1000;
            }
            if (m_thread == null)
            {
                m_thread = new Thread(new ThreadStart(ThreadMethod));
                m_thread.Start();
            }
        }
        private void udpStop()
        {
            Debug.Log("udpStop");
            if (m_thread != null)
            {
                m_thread.Abort();
                m_thread = null;
            }
            if (m_udp != null)
            {
                m_udp.Close();
                m_udp = null;
            }
        }

        private void ThreadMethod()
        {
            Debug.Log(KEY_NAME);
            while (true)
            {
                if (m_isReceiving)
                {
                    try
                    {
                        IPEndPoint remoteEP = null;
                        byte[] data = m_udp.Receive(ref remoteEP);
                        m_buffPtr = (m_buffPtr + 1) % RingSize;
                        m_recvTextArr[m_buffPtr] = Encoding.ASCII.GetString(data);
                        //Debug.Log("Recv:" + m_recvTextArr[m_buffPtr]);
                    }
                    catch (SocketException e)
                    {
                        Debug.Log(e.ToString());
                    }
                }
                else
                {
                }
            }
        }
    }
}
