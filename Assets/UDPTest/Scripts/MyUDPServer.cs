using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MyUDPServer : TmUDP.TmUDPServer
{
    [System.Serializable]
    public class MyPlInfo
    {
        public string uuid="";
        public GameObject obj = null;
        public Vector3 pos = Vector3.zero;
    }

    // Start is called before the first frame update
    public override void Start()
    {
        base.Start();
        // do anything here

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
        Debug.Log("--MyUDPServerAdd:" + _ip[0].ToString());
    }
    public void OnRemoveIP(string[] _ip)
    {
        Debug.Log("--MyUDPServerRemove:" + _ip[0].ToString());
    }

    public void OnDebugRemoveIP(string _ip, string _ip2)
    {
        Debug.Log("--MyUDPServerRemove:" + _ip.ToString()+","+_ip2.ToString());
    }
}
