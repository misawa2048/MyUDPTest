using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MyUDPServer : TmUDP.TmUDPServer
{
    [System.Serializable]
    public class MyPlInfo
    {
        public string uip="";
        public GameObject obj = null;
        public Vector3 pos = Vector3.zero;
        public MyPlInfo(string _uip, GameObject _obj, Vector3 _pos)
        {
            uip = _uip;
            obj = _obj;
            pos = _pos;
        }
    }

    [SerializeField] List<MyPlInfo> m_plInfoList = null;

    // Start is called before the first frame update
    public override void Start()
    {
        base.Start();
        // do anything here
        m_plInfoList = new List<MyPlInfo>();
    }

    // Update is called once per frame
    public override void Update()
    {
        // do anything here

        base.Update();
    }

    public void OnReceiveData(byte[] _data)
    {
        string text = System.Text.Encoding.UTF8.GetString(_data);
        Debug.Log("--MyUDPServerRecv:" + text);
    }
    public void OnAddIP(string[] _ip)
    {
        if (!m_plInfoList.Any(v => v.uip == _ip[0]))
        {
            m_plInfoList.Add(new MyPlInfo(_ip[0],null,Vector3.zero));
            Debug.Log("--MyUDPServerAdd:" + _ip[0].ToString());
        }
    }
    public void OnRemoveIP(string[] _ip)
    {
        if (m_plInfoList.Any(v => v.uip == _ip[0]))
        {
            Debug.Log("--MyUDPServerRemove:" + _ip[0].ToString());
        }
    }

    public void OnDebugRemoveIP(string _ip, string _ip2)
    {
        Debug.Log("--MyUDPServerRemove:" + _ip.ToString()+","+_ip2.ToString());
    }
}
