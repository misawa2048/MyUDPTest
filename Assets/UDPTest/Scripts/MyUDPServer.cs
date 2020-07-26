﻿using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MyUDPServer : TmUDP.TmUDPServer
{
    //public static readonly string KWD_QUIT = TmUDP.TmUDPClient.KWD_QUIT;
    public static readonly string KWD_POS = "Pos";
    public static readonly string KWD_ANGY = "AngY";
    public static readonly string KWD_QUAT = "Quat";

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

    public struct LinqSch
    {
        public int index;
        public string data;
        public LinqSch(int _idx=0, string _dat=""){ index = _idx; data = _dat; }
    }

    [SerializeField,ReadOnly] List<MyClientInfo> m_plInfoList = null;
    [SerializeField,ReadOnlyWhenPlaying] GameObject m_clientMarkerPrefab = null;

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

    public void OnReceiveData(byte[] _rawData)
    {
        string text = System.Text.Encoding.UTF8.GetString(_rawData);
        string[] dataArr = text.Split(',');
        if (dataArr.Length > 0)
        {
            MyClientInfo info = GetInfoByIP(dataArr[0], m_plInfoList);
            if (info != null)
            {
                Vector3 pos = Vector3.zero;
                bool isInit = false;
                bool result = TryGetPosFromData(dataArr, out pos, out isInit);
                if (result)
                {
                    info.pos = pos;
                    info.obj.transform.localPosition = info.pos;
                }
                Quaternion rot = Quaternion.identity;
                result = TryGetQuatFromData(dataArr, out rot);
                if (result)
                {
                    info.obj.transform.localRotation = rot;
                }
            }
            Debug.Log("--MyUDPServerRecv:" + text);
        }
    }

    public void OnAddClient(string[] _dataArr)
    {
        if(OnAddClientSub(_dataArr, m_plInfoList, m_clientMarkerPrefab, transform.parent))
        {
            Debug.Log("--MyUDPServerAdd:" + _dataArr[0].ToString());
        }
    }

    public void OnRemoveClient(string[] _dataArr)
    {
        if(OnRemoveClientSub(_dataArr, m_plInfoList))
        {
            Debug.Log("--MyUDPServerRemove:" + _dataArr[0].ToString());
        }
    }

    // for debug
    void OnGUI()
    {
        MyUDPServer.ONGUISub(this.host, this.sendPort, this.receivePort, this.myIP, m_plInfoList);
    }

    static public bool OnAddClientSub(string[] _dataArr, List<MyClientInfo> _infoList, GameObject _makerPrefab, Transform _parent=null)
    {
        bool ret = false;
        string ipStr = _dataArr[0];
        if (!_infoList.Any(v => v.uip == ipStr))
        {
            MyClientInfo info = CreateClientMarker(_dataArr, _makerPrefab, _parent);
            _infoList.Add(info);
            ret = true;
        }
        return ret;
    }

    static public bool OnRemoveClientSub(string[] _dataArr, List<MyClientInfo> _infoList)
    {
        bool ret = false;
        string ipStr = _dataArr[0];
        MyClientInfo tgt = _infoList.First(v => v.uip == ipStr);
        if (tgt != null)
        {
            if (tgt.obj != null)
                Destroy(tgt.obj);

            _infoList.Remove(tgt);
            ret = true;
        }
        return ret;
    }

    static public MyClientInfo GetInfoByIP(string _ipStr, List<MyClientInfo> _plInfoList)
    {
        MyClientInfo ret = null;
        foreach(MyClientInfo info in _plInfoList)
        {
            if (info.uip.Equals(_ipStr))
            {
                ret = info;
                break;
            }
        }
        return ret;
    }


    static public MyClientInfo CreateClientMarker(string[] _dataArr,GameObject _prefab, Transform _parent=null)
    {
        Vector3 pos=Vector3.zero;
        bool isInit = false;
        bool result = TryGetPosFromData(_dataArr, out pos, out isInit);
        GameObject go = Instantiate(_prefab, pos, Quaternion.identity);
        go.transform.SetParent(_parent);
        MyClientInfo info = new MyClientInfo(_dataArr[0], go, pos);
        return info;
    }

    static public bool TryGetPosFromData(string[] _dataArr, out Vector3 _pos, out bool _isInit)
    {
        bool ret = false;
        _pos = Vector3.zero;
        _isInit = false;
        int index = 0;
        try
        {
            index = _dataArr.Select((dat, idx) => new LinqSch(idx, dat)).FirstOrDefault(e => e.data.Equals(KWD_POS)).index;
        }
        catch (System.Exception e) { Debug.Log(e); }
        if (index == 0)
        {
            try
            {
                index = _dataArr.Select((dat, idx) => new LinqSch(idx, dat)).FirstOrDefault(e => e.data.Equals(KWD_INIT)).index;
            }
            catch (System.Exception e) { Debug.Log(e); }
            if (index > 0)
            {
                _isInit = true;
            }
        }

        if ((index > 0) && (_dataArr.Length > index + 3))
        { // Pos,x,y,z
            ret = true;
            float.TryParse(_dataArr[index + 1], out _pos.x);
            float.TryParse(_dataArr[index + 2], out _pos.y);
            float.TryParse(_dataArr[index + 3], out _pos.z);
            Debug.Log("Pos=" + _dataArr[index + 1] + "," + _dataArr[index + 2] + "," + _dataArr[index + 3]);
        }
        return ret;
    }

    static public bool TryGetAngleYFromData(string[] _dataArr, out float _angY)
    {
        bool ret = false;
        _angY = 0f;
        int index = 0;
        try
        {
            index = _dataArr.Select((dat, idx) => new { Idx = idx, Dat = dat }).First(e => e.Dat.Equals(KWD_ANGY)).Idx;
        }
        catch (System.Exception e) { Debug.Log(e); }

        if ((index > 0) && (_dataArr.Length > index + 1))
        { // rotY
            ret = true;
            float.TryParse(_dataArr[index + 1], out _angY);
            Debug.Log("RotY=" + _dataArr[index + 1]);
        }
        return ret;
    }

    static public bool TryGetQuatFromData(string[] _dataArr, out Quaternion _rot)
    {
        bool ret = false;
        _rot = Quaternion.identity;

        int index = 0;
        try
        {
            index = _dataArr.Select((dat, idx) => new { Idx = idx, Dat = dat }).First(e => e.Dat.Equals(KWD_QUAT)).Idx;
        }
        catch (System.Exception e) { Debug.Log(e); }

        if ((index > 0) && (_dataArr.Length > index + 4))
        { // rotY
            ret = true;
            float.TryParse(_dataArr[index + 1], out _rot.x);
            float.TryParse(_dataArr[index + 2], out _rot.y);
            float.TryParse(_dataArr[index + 3], out _rot.z);
            float.TryParse(_dataArr[index + 4], out _rot.w);
            Debug.Log("Quat=" + _rot.ToString());
        }
        return ret;
    }

    static public void ONGUISub(string _host, int _hSendPort, int _hRecvPort, string _myIP, List<MyClientInfo> _infoList)
    {
        GUIStyle customGuiStyle = new GUIStyle();
        customGuiStyle.fontSize = 32;
        customGuiStyle.alignment = TextAnchor.UpperRight;
        customGuiStyle.normal.textColor = Color.black;
        GUILayout.BeginArea(new Rect(Screen.width - 310, 0, 300, Screen.height));
        GUILayout.BeginVertical();
        GUILayout.TextArea("host:" + _host, customGuiStyle);
        GUILayout.TextArea("S:" + _hSendPort + " R:"+ _hRecvPort, customGuiStyle);
        GUILayout.TextArea("myIP:" + _myIP, customGuiStyle);
        foreach (MyClientInfo info in _infoList)
        {
            customGuiStyle.normal.textColor = new Color(0.1f, 0.1f, 0.1f);
            GUILayout.TextArea(info.uip, customGuiStyle);
            customGuiStyle.normal.textColor = new Color(0f, 0f, 0.4f);
            GUILayout.TextArea(info.pos.ToString(), customGuiStyle);
        }
        GUILayout.EndVertical();
        GUILayout.EndArea();
    }
}
