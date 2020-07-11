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
                Debug.Log("ClientOtherRecv:" + text);
            }
            else
            {
                Debug.Log("ClientEchoRecv:" + text);
            }
        }
    }

    IEnumerator udpDbgSendCo()
    {
        while (true)
        {
            yield return new WaitForSeconds(1.0f);
            Vector3 val = new Vector3(Random.value, 0f, 0f);
            string valStr = TmUDP.TmUDPClient.Vector3ToFormatedStr(val, 2);
            string str = this.myIP + ",Pos," + valStr;
            byte[] data = System.Text.Encoding.UTF8.GetBytes(str);
            this.SendData(data);
            Debug.Log(str);
        }
    }
}
