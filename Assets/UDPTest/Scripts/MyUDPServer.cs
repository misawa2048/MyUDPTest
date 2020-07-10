using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MyUDPServer : TmUDP.TmUDPServer
{
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
    }
}
