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
                Debug.Log("ClientRecv:" + text);
            }
            else
            {
                Debug.Log("EchoRecv:" + text);
            }
        }
    }

    IEnumerator udpDbgSendCo()
    {
        while (true)
        {
            yield return new WaitForSeconds(1.0f);
            string str = this.myIP + ",Pos," + Random.value + ",0.2,:0.3";
            byte[] data = System.Text.Encoding.UTF8.GetBytes(str);
            this.SendData(data);
            Debug.Log(str);
        }
    }
}
