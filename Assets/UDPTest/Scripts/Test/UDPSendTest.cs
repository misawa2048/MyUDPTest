using System.Collections;
using System.Net;
using System.Net.Sockets;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace TmUDPTest
{
    public class UDPSendTest : MonoBehaviour
    {
        readonly string HOST_KEY_NAME = "sTESTSendHost";
        readonly string PORT_KEY_NAME = "iTESTSendPort";
        readonly string IS_BROADCAST = "isBloadcast";
        [SerializeField] string m_host = "localhost";
        [SerializeField] int m_port = 7001;
        [SerializeField] Text m_hostText = null;
        [SerializeField] InputField m_hostField = null;
        [SerializeField] InputField m_portField = null;
        [SerializeField] UDPReceiveTest m_receiveSce = null;
        private UdpClient m_udp = null;

        // Start is called before the first frame update
        void Start()
        {
            if (!m_receiveSce.isActiveAndEnabled)
            {
                m_receiveSce.InitPortSettings();
            }
            InitPortSettings();
            if (m_udp == null)
            {
                m_udp = new UdpClient();
                if ((m_host == "") || (m_host == IS_BROADCAST))
                {
                    m_udp.Connect(IPAddress.Broadcast, m_port);
                    Debug.Log("UDPBroadcast start.");
                }
                else
                {
                    m_udp.Connect(m_host, m_port);
                    Debug.Log("UDPSend start.");
                }
                StartCoroutine(udpSendCo());
            }

        }

        // Update is called once per frame
        void Update()
        {

        }

        public void OnHostEditChange(string _str)
        {
            string str = m_host.ToString();
            if (m_hostField.text == "")
            {
                m_hostField.text = IS_BROADCAST;
            }
            m_host = m_hostField.text;
            PlayerPrefs.SetString(HOST_KEY_NAME, m_host);
            str += "->" + m_host.ToString();
            Debug.Log("Change host:" + str);
            SceneChanger.Reload();
        }

        public void OnPortEditChange(string _str)
        {
            string str = m_port.ToString();
            int.TryParse(m_portField.text, out m_port);
            PlayerPrefs.SetInt(PORT_KEY_NAME, m_port);
            str += "->" + m_port.ToString();
            Debug.Log("Change port:" + str);
            SceneChanger.Reload();
        }

        public void InitPortSettings()
        {
            m_hostText.text = "Host("+ getIpString() +")";
            if (PlayerPrefs.HasKey(PORT_KEY_NAME))
            {
                Debug.Log("HasKey" + PORT_KEY_NAME);
                m_port = PlayerPrefs.GetInt(PORT_KEY_NAME);
                m_portField.text = m_port.ToString();
            }
            else
            {
                Debug.Log("!HasKey:" + PORT_KEY_NAME);
                int.TryParse(m_portField.text, out m_port);
                PlayerPrefs.SetInt(PORT_KEY_NAME, m_port);
            }

            if (PlayerPrefs.HasKey(HOST_KEY_NAME))
            {
                Debug.Log("HasKey:" + HOST_KEY_NAME);
                m_host = PlayerPrefs.GetString(HOST_KEY_NAME);
                m_hostField.text = m_host.ToString();
            }
            else
            {
                Debug.Log("!HasKey:" + HOST_KEY_NAME);
                PlayerPrefs.SetString(HOST_KEY_NAME, m_host);
            }
        }

        IEnumerator udpSendCo()
        {
            while (true)
            {
                yield return new WaitForSeconds(0.5f);
                string str = "x:" + Random.value + ",y:0.2,z:0.3";
                byte[] data = System.Text.Encoding.UTF8.GetBytes(str);
                m_udp.Send(data, data.Length);
            }
        }

        void OnApplicationQuit()
        {
            udpStop();
        }

        private void udpStop()
        {
            if (m_udp != null)
            {
                m_udp.Close();
                m_udp = null;
                Debug.Log("UDP closed.");
            }
        }

        private string getIpString()
        {
            string retStr = GetIP(); // "127.0.0.1";
            return retStr;
        }

        // https://stackoverflow.com/questions/51975799/how-to-get-ip-address-of-device-in-unity-2018
        public enum ADDRESSFAM{ IPv4, IPv6 }
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
    }
}
