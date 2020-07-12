using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MyUDPServer : TmUDP.TmUDPServer
{
    //public static readonly string KWD_QUIT = TmUDP.TmUDPClient.KWD_QUIT;
    public static readonly string KWD_POS = "Pos";
    public static readonly string KWD_RORY = "RotY";

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
        MyClientInfo info = GetInfoByIP(dataArr[0], m_plInfoList);
        if (info != null)
        {
            Vector3 pos = Vector3.zero;
            bool result = TryGetPosFromData(dataArr, out pos);
            if (result)
            {
                info.pos = pos;
                info.obj.transform.position = info.pos;
            }
        }

        Debug.Log("--MyUDPServerRecv:" + text);
    }

    public void OnAddClient(string[] _dataArr)
    {
        string ipStr = _dataArr[0];
        if (!m_plInfoList.Any(v => v.uip == ipStr))
        {
            MyClientInfo info = CreateClientMarker(_dataArr,m_clientMarkerPrefab);
            m_plInfoList.Add(info);
            Debug.Log("--MyUDPServerAdd:" + ipStr.ToString());
        }
    }

    public void OnRemoveClient(string[] _dataArr)
    {
        string ipStr = _dataArr[0];
        MyClientInfo tgt = m_plInfoList.First(v => v.uip == ipStr);
        if (tgt!=null)
        {
            if (tgt.obj != null)
                Destroy(tgt.obj);

            m_plInfoList.Remove(tgt);
            Debug.Log("--MyUDPServerRemove:" + ipStr.ToString());
        }
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


    static public MyClientInfo CreateClientMarker(string[] _dataArr,GameObject _prefab)
    {
        Vector3 pos=Vector3.zero;
        bool result = TryGetPosFromData(_dataArr, out pos);
        GameObject go = Instantiate(_prefab, pos, Quaternion.identity);
        MyClientInfo info = new MyClientInfo(_dataArr[0], go, pos);
        return info;
    }

    static public bool TryGetPosFromData(string[] _dataArr, out Vector3 _pos)
    {
        bool ret = false;
        _pos = Vector3.zero;
        int index = 0;
        try
        {
            index = _dataArr.Select((dat, idx) => new LinqSch(idx, dat)).FirstOrDefault(e => e.data.Equals(KWD_POS)).index;
        }
        catch (System.Exception e) { Debug.Log(e); }

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
            index = _dataArr.Select((dat, idx) => new { Idx = idx, Dat = dat }).First(e => e.Dat.Equals(KWD_RORY)).Idx;
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
        float angY = 0f;
        _rot = Quaternion.identity;
        if (TryGetAngleYFromData(_dataArr,out angY)){
            ret = true;
            _rot = Quaternion.AngleAxis(angY, Vector3.up);
        }
        return ret;
    }
}
