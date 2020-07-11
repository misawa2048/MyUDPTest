using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MyUDPServer : TmUDP.TmUDPServer
{
    [System.Serializable]
    public class MyClientInfo
    {
        public string uip="";
        public GameObject obj = null;
        public Vector3 pos = Vector3.zero;
        public MyClientInfo(string _uip, GameObject _obj, Vector3 _pos)
        {
            uip = _uip;
            obj = _obj;
            pos = _pos;
        }
    }

    [SerializeField] List<MyClientInfo> m_plInfoList = null;

    // Start is called before the first frame update
    public override void Start()
    {
        base.Start();
        // do anything here
        m_plInfoList = new List<MyClientInfo>();
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

    public void OnAddClient(string[] _ip)
    {
        if (!m_plInfoList.Any(v => v.uip == _ip[0]))
        {
            m_plInfoList.Add(new MyClientInfo(_ip[0],null,Vector3.zero));
            Debug.Log("--MyUDPServerAdd:" + _ip[0].ToString());
        }
    }

    public void OnRemoveClient(string[] _ip)
    {
        MyClientInfo tgt = m_plInfoList.First(v => v.uip == _ip[0]);
        if (tgt!=null)
        {
            m_plInfoList.Remove(tgt);
            Debug.Log("--MyUDPServerRemove:" + _ip[0].ToString());
        }
    }
}
