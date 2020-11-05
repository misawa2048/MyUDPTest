using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Net; // IPEndPoint

namespace TmUDPTest
{
    public class RceiveDataMark : MonoBehaviour
    {
        [SerializeField] Image m_markerImg = null;
        float m_amount;

        // Start is called before the first frame update
        void Start()
        {
            m_amount = 0f;
            if (m_markerImg != null)
                m_markerImg.fillAmount = m_amount;
        }

        // Update is called once per frame
        void Update()
        {
            if ((m_markerImg!=null)&&(m_amount > 0f))
            {
                m_amount = Mathf.Max(0f, m_amount - Time.deltaTime*2f);
                m_markerImg.fillAmount = m_amount;
            }
        }

        public void OnReceiveData(byte[] _data, IPEndPoint _remoteEP)
        {
            m_amount = 1f;
        }
    }
}
