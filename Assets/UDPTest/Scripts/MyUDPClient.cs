using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MyUDPClient : TmUDP.TmUDPClient
{
    // Start is called before the first frame update
    public override void Start()
    {
        base.Start();
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
            if (dataArr[0] != this.myIP)
            {
                Debug.Log("----MyUDPClientOtherRecv:" + text);
            }
            else
            {
                Debug.Log("----MyUDPClientEchoRecv:" + text);
            }
        }
    }

    public void OnAddClient(string[] _dataArr)
    {
        string ipStr = _dataArr[0];
        Debug.Log("----MyUDPClientAdd:" + ipStr.ToString());
    }

    public void OnRemoveClient(string[] _dataArr)
    {
        string ipStr = _dataArr[0];
        Debug.Log("----MyUDPClientRemove:" + ipStr.ToString());
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
