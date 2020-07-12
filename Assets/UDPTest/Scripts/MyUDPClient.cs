using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MyUDPClient : TmUDP.TmUDPClient
{
    [SerializeField,ReadOnly] List<MyUDPServer.MyClientInfo> m_plInfoList = null;
    [SerializeField,ReadOnlyWhenPlaying] GameObject m_clientMarkerPrefab = null;

    // Start is called before the first frame update
    public override void Start()
    {
        base.Start();
        m_plInfoList = new List<MyUDPServer.MyClientInfo>();
        StartCoroutine(udpDbgSendCo());
    }

    // Update is called once per frame
    public override void Update()
    {
        base.Update();
    }

    public void OnReceiveData(byte[] _data)
    {
        string text = System.Text.Encoding.UTF8.GetString(_data);
        string[] dataArr = text.Split(',');
        if (dataArr.Length > 0)
        {
            string ipStr = dataArr[0];
            if (ipStr != this.myIP)
            {
                MyUDPServer.MyClientInfo info = MyUDPServer.GetInfoByIP(dataArr[0], m_plInfoList);
                if (info != null)
                {
                    Vector3 pos = Vector3.zero;
                    bool result = MyUDPServer.TryGetPosFromData(dataArr, out pos);
                    if (result)
                    {
                        info.pos = pos;
                        info.obj.transform.position = info.pos;
                    }
                }
                Debug.Log("----MyUDPClientOtherRecv:" + text);
            }
            else
            {
                // do nothing now
                Debug.Log("----MyUDPClientEchoRecv:" + text);
            }
        }
    }

    public void OnAddClient(string[] _dataArr)
    {
        string ipStr = _dataArr[0];
        if (ipStr == this.myIP)
            return;

        if (!m_plInfoList.Any(v => v.uip == ipStr))
        {
            MyUDPServer.MyClientInfo info = MyUDPServer.CreateClientMarker(_dataArr, m_clientMarkerPrefab);
            m_plInfoList.Add(info);
            Debug.Log("----MyUDPClientAdd:" + ipStr.ToString());
        }
    }

    public void OnRemoveClient(string[] _dataArr)
    {
        string ipStr = _dataArr[0];
        if (ipStr == this.myIP)
            return;

        MyUDPServer.MyClientInfo tgt = m_plInfoList.First(v => v.uip == ipStr);
        if (tgt != null)
        {
            if (tgt.obj != null)
                Destroy(tgt.obj);

            m_plInfoList.Remove(tgt);
            Debug.Log("----MyUDPClientRemove:" + ipStr.ToString());
        }
    }

    IEnumerator udpDbgSendCo()
    {
        while (true)
        {
            yield return new WaitForSeconds(1.0f);
            Vector3 val = new Vector3(Random.value, 0.1f, Random.value);
            string valStr = TmUDP.TmUDPClient.Vector3ToFormatedStr(val, 2);
            string str = this.myIP + ","+ MyUDPServer.KWD_POS+"," + valStr;
            byte[] data = System.Text.Encoding.UTF8.GetBytes(str);
            this.SendData(data);
            Debug.Log(str);
        }
    }
}
