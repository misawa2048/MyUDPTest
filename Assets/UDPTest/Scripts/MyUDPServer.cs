using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net; // IPEndPoint
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Networking;

public class MyUDPServer : TmUDP.TmUDPServer
{
    [System.Serializable] public class InitialObjsAddEvent : UnityEngine.Events.UnityEvent<MyUDPServer> { }
    [SerializeField] InitialObjsAddEvent m_toInitialObjsAddEvnts = new InitialObjsAddEvent();

    //public static readonly string KWD_QUIT = TmUDP.TmUDPClient.KWD_QUIT;
    public static readonly string KWD_POS = "Pos";   // [3]"KWD_POS, x,y,z"
    public static readonly string KWD_ANGY = "AngY"; // [1]"KWD_ANGY,y"
    public static readonly string KWD_QUAT = "Quat"; // [4]"KWD_QUAT,x,y,z,w"
    // MyUDPServer extend below
    public static readonly string KWDEX_OBJ = "Obj";   // [10]"KWD_OBJ,modelName, countModel, x,y,z,x,y,z,w,suffix" 
    public static readonly string KWDEX_REMOVEOBJ = "RemoveObj";   // [2]"KWDEX_REMOVEOBJ,modelName, countModel" 
#if true // update addedObjectList on server
    public static readonly string KWDEX_REQUESTOBJARR = "RequestObjArr";   // [1]"KWDEX_GETOBJARRAY,maxNum" 
    public static readonly string KWDEX_OBJARRAY = "ObjArray";   // [1]"KWDEX_GETOBJARRAY,jsonStr" 
#endif

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

#if true // update addedObjectList on server
    [System.Serializable]
    public class MyAddedObjInfo
    {
        [Tooltip("Added GameObject")] public GameObject gameObject = null;
        [Tooltip("IP addres")] public string ip = "";
        [Tooltip("object Name")] public string objName = "";
        [Tooltip("model count")] public int modelCount = 0;
        [Tooltip("some parameters")] public string suffix = "";
        [Tooltip("position")] public Vector3 pos = Vector3.zero;
        [Tooltip("rotation")] public Quaternion rot = Quaternion.identity;
        [Tooltip("userData")] public string userData = "";

        public MyAddedObjInfo(GameObject _go, string _ip, string _objName, int _modelCount, string _suffix, Vector3 _pos, Quaternion _rot)
        {
            gameObject = _go;
            ip = _ip;
            objName = _objName;
            modelCount = _modelCount;
            suffix = _suffix;
            pos = _pos;
            rot = _rot;
            userData = "";
        }
        public void SetPositionAndRotation(Vector3 _pos, Quaternion _rot)
        {
            pos = _pos;
            rot = _rot;
        }
    }

    [System.Serializable]
    public class MyAddedObjInfoForJson
    {
        [Tooltip("IP addres for JSON")] public string id = "";
        [Tooltip("suffix for JSON")] public string url = "";
        [Tooltip("image rotation")] public int rotation = 0;
        [Tooltip("positionX for JSON")] public float x = 0f;
        [Tooltip("positionY for JSON")] public float y = 0f;
        [Tooltip("positionZ for JSON")] public float z = 0f;
        [Tooltip("rotationX for JSON")] public float q_x = 0f;
        [Tooltip("rotationY for JSON")] public float q_y = 0f;
        [Tooltip("rotationZ for JSON")] public float q_z = 0f;
        [Tooltip("rotationW for JSON")] public float q_w = 0f;
        [Tooltip("model count for JSON")] public int countModel = 0;
        [Tooltip("object Name for JSON")] public string objName = "";
        [Tooltip("size of Data")] public int sizeData = 0;

        public MyAddedObjInfoForJson(MyAddedObjInfo _info)
        {
            id = _info.ip;
            url = _info.suffix;
            rotation = 0;
            x = Mathf.Round(_info.pos.x * 100f) * 0.01f;
            y = Mathf.Round(_info.pos.y * 100f) * 0.01f;
            z = Mathf.Round(_info.pos.z * 100f) * 0.01f;
            q_x = Mathf.Round(_info.rot.x * 100f) * 0.01f;
            q_y = Mathf.Round(_info.rot.y * 100f) * 0.01f;
            q_z = Mathf.Round(_info.rot.z * 100f) * 0.01f;
            q_w = Mathf.Round(_info.rot.w * 100f) * 0.01f;
            countModel = _info.modelCount;
            objName = _info.objName;
            sizeData = 0;
        }
        public static MyAddedObjInfoForJson CreateFromJSON(string _jsonString)
        {
            return JsonUtility.FromJson<MyAddedObjInfoForJson>(_jsonString);
        }
        public Vector3 pos { get { return new Vector3(x, y, z); } }
        public Quaternion rot { get { return new Quaternion(q_x, q_y, q_z, q_w); } }
    }
    [System.Serializable]
    public class MyAddedObjInfoArrayForJson
    {
        public MyAddedObjInfoForJson[] infoArray=null;
        public MyAddedObjInfo[] ToMyAddedObjInfoArray()
        {
            MyAddedObjInfo[] array = new MyAddedObjInfo[infoArray.Length];
            for(int i = 0; i < infoArray.Length; ++i)
            {
                array[i] = new MyAddedObjInfo(
                    null,
                    infoArray[i].id,
                    infoArray[i].objName,
                    infoArray[i].countModel,
                    infoArray[i].url,
                    new Vector3(infoArray[i].x, infoArray[i].y, infoArray[i].z),
                    new Quaternion(infoArray[i].q_x, infoArray[i].q_y, infoArray[i].q_z, infoArray[i].q_w)
                );
            }
            return array;
        }
    }
#endif

    public struct LinqSch
    {
        public int index;
        public string data;
        public LinqSch(int _idx=0, string _dat=""){ index = _idx; data = _dat; }
    }

#if false // do nothing on server
    [SerializeField, ReadOnlyWhenPlaying] TmUDP.ObjPrefabArrScrObj m_prefabInfo=null;
#endif
    [SerializeField,ReadOnly] List<MyClientInfo> m_plInfoList = null;
    [SerializeField,ReadOnlyWhenPlaying] GameObject m_clientMarkerPrefab = null;
    [SerializeField, ReadOnly, Tooltip("GameObjects I instantiate")] List<MyAddedObjInfo> m_AddedObjList = null;

    // Start is called before the first frame update
    public override void Start()
    {
        Application.runInBackground = true;
        if (MyUDPClient.USE_PLAYERPREFS && PlayerPrefs.HasKey(MyUDPClient.PREFS_KEY_HSEND_PORT))
            this.m_sendPort = PlayerPrefs.GetInt(MyUDPClient.PREFS_KEY_HSEND_PORT);
        if (MyUDPClient.USE_PLAYERPREFS && PlayerPrefs.HasKey(MyUDPClient.PREFS_KEY_HRECV_PORT))
            this.m_receivePort = PlayerPrefs.GetInt(MyUDPClient.PREFS_KEY_HRECV_PORT);

        base.Start();
        // do anything here
        m_AddedObjList = new List<MyUDPServer.MyAddedObjInfo>();
        m_plInfoList = new List<MyClientInfo>();
        m_toInitialObjsAddEvnts.Invoke(this); // will calls OnSetInitialObjList()
    }

    // Update is called once per frame
    public override void Update()
    {
        // do anything here

        base.Update();
    }

    public void OnReceiveData(byte[] _rawData, IPEndPoint _remoteEP)
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
                if (TryGetPosFromData(dataArr, out pos, out isInit))
                {
                    info.pos = pos;
                    info.obj.transform.localPosition = info.pos;
                }
                Quaternion rot = Quaternion.identity;
                if (TryGetQuatFromData(dataArr, out rot))
                {
                    info.obj.transform.localRotation = rot;
                }
#if true // update addedObjectList on server
                string objName = "";
                string suffix = "";
                int count = 0;
                if (TryGetObjectNameFromData(dataArr, out objName, out count, out pos, out rot, out suffix))
                {
                    string ipStr = dataArr[0];
                    if (HasAddedObjectInAddedList(m_AddedObjList, ipStr, objName, count))
                    { // update only
                        MyAddedObjInfo existInfo = GetInfoFromInfo(m_AddedObjList, ipStr, objName, count);
                        if (existInfo != null)
                            existInfo.SetPositionAndRotation(pos, rot);
                    }
                    else
                    { // add to list
                        m_AddedObjList.Add(new MyAddedObjInfo(null, ipStr, objName, count, suffix, pos, rot));
                    }
                }
                objName = "";
                count = 0;
                if (TryGetRemoveObjectFromData(dataArr, out objName, out count))
                {
                    string ipStr = dataArr[0];
                    if (HasAddedObjectInAddedList(m_AddedObjList, ipStr, objName, count))
                    {
                        MyAddedObjInfo existInfo = GetInfoFromInfo(m_AddedObjList, ipStr, objName, count);
                        if (existInfo != null)
                            m_AddedObjList.Remove(existInfo);
                    }
                }
                string jsonPartStr = ""; // stringで送って、clientで処理する
                if (TryGetReqestAddedObjArray(dataArr, m_AddedObjList, out jsonPartStr))
                {
                    if (jsonPartStr != "")
                    {
                        string outStr = "{\"infoArray\":[" + jsonPartStr.Replace(',','%') + "]}";
                        outStr = dataArr[0] + "," + MyUDPServer.KWDEX_OBJARRAY + "," + outStr;
                        this.SendData(System.Text.Encoding.UTF8.GetBytes(outStr), _remoteEP);
                    }
                    else {
                        Debug.Log("Json is null. nothing to do.");
                    }
                }
#endif
            }
            //Debug.Log("--MyUDPServerRecv:" + text);
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

    public void OnChangePort(string _portStr)
    {
        ChangePort(_portStr, true);
    }
    public void ChangePort(string _portsStr, bool _restart)
    {
        string[] hostInfoStrArr = _portsStr.Split(':'); // 7001:7003
        if (hostInfoStrArr.Length > 1)
        {
            int hSp = this.m_sendPort;    // host's send port
            int hRp = this.m_receivePort; // host's receive port
            int.TryParse(hostInfoStrArr[0], out hSp);
            int.TryParse(hostInfoStrArr[1], out hRp);
            if (MyUDPClient.USE_PLAYERPREFS)
            {
                PlayerPrefs.SetInt(MyUDPClient.PREFS_KEY_HSEND_PORT, hSp);
                PlayerPrefs.SetInt(MyUDPClient.PREFS_KEY_HRECV_PORT, hRp);
            }
            if(_restart)
                SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }

    // for debug
    void OnGUI()
    {
        //if (MyUDPClient.USE_PLAYERPREFS && (PlayerPrefs.GetInt(MyUDPClient.PREFS_KEY_DEBUGDISP)!=0))
            ONGUISub(this.host, this.sendPort, this.receivePort, this.myIP, m_plInfoList, m_AddedObjList,true);
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
        MyClientInfo tgt = null;
        for (int i = 0; i < _infoList.Count; ++i)
        {
            if (_infoList[i].uip == ipStr)
            {
                tgt = _infoList[i];
                break;
            }
        }
        if (tgt != null)
        {
            if (tgt.obj != null)
                Destroy(tgt.obj);

            _infoList.Remove(tgt);
            ret = true;
        }
        return ret;
    }

    // will called from m_toInitialObjsAddEvnts.
    public void OnSetInitialObjList(List<MyAddedObjInfo> _addedObjList)
    {
        if (_addedObjList != null)
            m_AddedObjList.AddRange(_addedObjList);
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
        go.name = _prefab.name + "_" + _dataArr[0]; // name_uuip
        go.transform.SetParent(_parent);
        MyClientInfo info = new MyClientInfo(_dataArr[0], go, pos);
        return info;
    }

    static public bool HasAddedObjectInAddedList(List<MyAddedObjInfo> _list, string _ip, string _objName, int _count)
    {
        return _list.Any<MyAddedObjInfo>(e => (e.objName == _objName && e.modelCount == _count && e.ip == _ip));
    }

    static public MyAddedObjInfo GetInfoFromInfo(List<MyAddedObjInfo> _list, string _ip, string _objName, int _count)
    {
        MyAddedObjInfo info = null;
        for (int i = 0; i < _list.Count; ++i)
        {
            if (_list[i].ip == _ip && _list[i].objName == _objName && _list[i].modelCount == _count)
            {
                info = _list[i];
                break;
            }
        }
        return info;
    }

    static public bool TryGetPosFromData(string[] _dataArr, out Vector3 _pos, out bool _isInit)
    {
        bool ret = false;
        _pos = Vector3.zero;
        _isInit = false;
        int index = GetIdxByKWD(_dataArr, KWD_POS);
        if (index <= 0)
        {
            index = GetIdxByKWD(_dataArr, KWD_INIT);
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
            Debug.Log(_dataArr[0] + "Pos=" + _dataArr[index + 1] + "," + _dataArr[index + 2] + "," + _dataArr[index + 3]);
        }
        return ret;
    }

    static public bool TryGetAngleYFromData(string[] _dataArr, out float _angY)
    {
        bool ret = false;
        _angY = 0f;
        int index = GetIdxByKWD(_dataArr, KWD_ANGY);
        if ((index > 0) && (_dataArr.Length > index + 1))
        { // rotY
            ret = true;
            float.TryParse(_dataArr[index + 1], out _angY);
            Debug.Log(_dataArr[0] + "RotY=" + _dataArr[index + 1]);
        }
        return ret;
    }

    static public bool TryGetQuatFromData(string[] _dataArr, out Quaternion _rot)
    {
        bool ret = false;
        _rot = Quaternion.identity;
        int index = GetIdxByKWD(_dataArr, KWD_QUAT);
        if ((index > 0) && (_dataArr.Length > index + 4))
        { // rotY
            ret = true;
            float.TryParse(_dataArr[index + 1], out _rot.x);
            float.TryParse(_dataArr[index + 2], out _rot.y);
            float.TryParse(_dataArr[index + 3], out _rot.z);
            float.TryParse(_dataArr[index + 4], out _rot.w);
            Debug.Log(_dataArr[0]+"Quat=" + _rot.ToString());
        }
        return ret;
    }

    static public bool TryGetObjectNameFromData(string[] _dataArr, out string _objName, out int _count, out Vector3 _pos, out Quaternion _rot, out string _suffix)
    {
        bool ret = false;
        _objName = "";
        _count = 0;
        _pos = Vector3.zero;
        _rot = Quaternion.identity;
        _suffix = "";
        int index = GetIdxByKWD(_dataArr, MyUDPServer.KWDEX_OBJ);
        if ((index > 0) && (_dataArr.Length > index + 10))
        { // rotY
            ret = true;
            _objName = _dataArr[index + 1];
            int.TryParse(_dataArr[index + 2], out _count);
            float.TryParse(_dataArr[index + 3], out _pos.x);
            float.TryParse(_dataArr[index + 4], out _pos.y);
            float.TryParse(_dataArr[index + 5], out _pos.z);
            float.TryParse(_dataArr[index + 6], out _rot.x);
            float.TryParse(_dataArr[index + 7], out _rot.y);
            float.TryParse(_dataArr[index + 8], out _rot.z);
            float.TryParse(_dataArr[index + 9], out _rot.w);
            _suffix = _dataArr[index + 10];
            Debug.Log(_dataArr[0] + "Model=" + _objName + "_" + _count.ToString() + " " + _suffix);
        }
        return ret;
    }

    static public bool TryGetRemoveObjectFromData(string[] _dataArr, out string _objName, out int _count)
    {
        bool ret = false;
        _objName = "";
        _count = 0;

        int index = GetIdxByKWD(_dataArr, MyUDPServer.KWDEX_REMOVEOBJ);
        if ((index > 0) && (_dataArr.Length > index + 2))
        {
            ret = true;
            _objName = _dataArr[index + 1];
            int.TryParse(_dataArr[index + 2], out _count);
            Debug.Log(_dataArr[0] + "RemoveModel=" + _objName + "_" + _count.ToString());
        }
        return ret;
    }

#if true // update addedObjectList on server
    static public bool TryGetReqestAddedObjArray(string[] _dataArr, List<MyUDPServer.MyAddedObjInfo> _addedObjList, out string _jsonStr)
    {
        bool ret = false;
        _jsonStr = "";
        int index = GetIdxByKWD(_dataArr, MyUDPServer.KWDEX_REQUESTOBJARR);
        if ((index > 0) && (_dataArr.Length > index + 1))
        {
            ret = true;
            int maxNum = 10;
            int.TryParse(_dataArr[index + 1], out maxNum);
            _jsonStr = GetJsonPartStrFromAddedObjList(_addedObjList, maxNum);
        }
        return ret;
    }

    static public bool TryGetAddedObjArray(string[] _dataArr, out MyAddedObjInfo[] _addedObjInfoArr)
    {
        bool ret = false;
        _addedObjInfoArr = null;
        int index = GetIdxByKWD(_dataArr, MyUDPServer.KWDEX_OBJARRAY);
        if ((index > 0) && (_dataArr.Length > index + 1))
        {
            ret = true;
            string jsonStr = _dataArr[index + 1].Replace('%', ',');
            Debug.Log(_dataArr[0] + "jsonStr=" + jsonStr);
            MyAddedObjInfoArrayForJson tmpOut = JsonUtility.FromJson<MyAddedObjInfoArrayForJson>(jsonStr);
            _addedObjInfoArr = tmpOut.ToMyAddedObjInfoArray();
        }
        return ret;
    }
#endif

    static public int GetIdxByKWD(string[] _dataArr, string _kwd)
    {
        int index = -1;
        for(int i=0;i< _dataArr.Length; ++i)
        {
            if (_dataArr[i].Equals(_kwd))
            {
                index = i;
                break;
            }
        }
        return index;
    }

    static public GameObject GetPrefabFromName(string _modelName, TmUDP.ObjPrefabArrScrObj _prefabArr)
    {
        GameObject ret = null;
        for (int i = 0; i < _prefabArr.objInfoArr.Length; ++i)
        {
            if (_modelName.StartsWith(_prefabArr.objInfoArr[i].name))
            {
                ret = _prefabArr.objInfoArr[i].prefab;
                break;
            }
        }
        return ret;
    }
    static public int GetPrefabIdFromName(string _modelName, TmUDP.ObjPrefabArrScrObj _prefabArr)
    {
        int ret = -1;
        for(int i = 0; i < _prefabArr.objInfoArr.Length; ++i)
        {
            if (_modelName.StartsWith(_prefabArr.objInfoArr[i].name))
            {
                ret = i;
                break;
            }
        }
        return ret;
    }
    static public string GetNameFromPrefabId(int _prefabId, TmUDP.ObjPrefabArrScrObj _prefabArr)
    {
        string ret = "UNKNOWN_PREFAB";
        if(_prefabArr.objInfoArr.Length > _prefabId)
            ret = _prefabArr.objInfoArr[_prefabId].name;
        return ret;
    }

#if true // update addedObjectList on server
    static public string GetJsonPartStrFromAddedObjList(List<MyAddedObjInfo> _addedObjList, int _maxNum)
    {
        string outStr = "";
        for (int i=0; i< Mathf.Min(_addedObjList.Count,_maxNum); ++i)
        {
            MyAddedObjInfoForJson data = new MyAddedObjInfoForJson(_addedObjList[_addedObjList.Count-1-i]); // latest-
            outStr += JsonUtility.ToJson(data);
            if(i< _addedObjList.Count - 1)
                outStr += ",";
        }
        return outStr;
    }
#endif

    static public void ONGUISub(string _host, int _hSendPort, int _hRecvPort, string _myIP, List<MyClientInfo> _infoList, List<MyAddedObjInfo> _AddedObjList, bool _isDetailMode)
    {
        GUIStyle customGuiStyle = new GUIStyle();
        customGuiStyle.fontSize = 32;
        customGuiStyle.alignment = TextAnchor.UpperRight;
        customGuiStyle.normal.textColor = MyUDPClient.IsLocalIP(_myIP) ? Color.white : Color.blue;
        GUILayout.BeginArea(new Rect(Screen.width - 310, 20, 300, Screen.height-20));
        GUILayout.BeginVertical();
        GUILayout.Label("host:" + _host+"  ", customGuiStyle); // "  " for round corner
        GUILayout.Label("S:" + _hSendPort + " R:"+ _hRecvPort, customGuiStyle);
        GUILayout.Label("myIP:" + _myIP, customGuiStyle);
        customGuiStyle.normal.textColor = Color.green;
        GUILayout.Label("Objs/Users:" + _AddedObjList.Count+"/"+ _infoList.Count, customGuiStyle);
        if (_isDetailMode)
        {
            foreach (MyClientInfo info in _infoList)
            {
                customGuiStyle.normal.textColor = new Color(0.8f, 0.8f, 0.8f);
                GUILayout.Label(info.uip, customGuiStyle);
                customGuiStyle.normal.textColor = new Color(0.2f, 0.2f, 0.6f);
                GUILayout.Label(info.pos.ToString(), customGuiStyle);
            }
        }
        GUILayout.EndVertical();
        GUILayout.EndArea();
    }
}
