using System.Collections;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;

namespace TmUDPTest
{
    public class UDPSendTest : MonoBehaviour
    {
        readonly string KEY_NAME = "SendPort";
        public string host = "localhost";
        [SerializeField] InputField m_portField = null;
        [SerializeField] int m_port = 7001;
        private UdpClient m_udp = null;

        // Start is called before the first frame update
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
                int.TryParse(m_portField.text, out m_port);
                PlayerPrefs.SetInt(KEY_NAME, m_port);
            }
            if (m_udp == null)
            {
                m_udp = new UdpClient();
                m_udp.Connect(host, m_port);
                Debug.Log("UDPSend start.");
                StartCoroutine(udpSendCo());
            }

        }

        // Update is called once per frame
        void Update()
        {

        }

        public void OnEditChange(string _str)
        {
            string str = m_port.ToString();
            int.TryParse(m_portField.text, out m_port);
            PlayerPrefs.SetInt(KEY_NAME, m_port);
            str += "->" + m_port.ToString();
            Debug.Log("Change port:" + str);
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
    }
}
