using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Networking;

public class MyUDPClient : TmUDP.TmUDPClient
{
    public static readonly bool USE_PLAYERPREFS = true;
    public static readonly string PREFS_KEY_HOST = "KeyHost";
    public static readonly string PREFS_KEY_HSEND_PORT = "KeyHSendPort"; // ServerSendPort
    public static readonly string PREFS_KEY_HRECV_PORT = "KeyHRecvPort"; // ServerRecvPort
    [System.Serializable]
    public class MyClientSettings
    {
        [Tooltip("Minimum distance to send message")]  public float minDist = 0.5f;
        [Tooltip("Minimum rotationY to send message")] public float minAngY = 5f;
        [Tooltip("Minimum time to send message again")] public float reloadTime = 0.2f;
        [Tooltip("Parent transform for offset")] public Transform parentTr = null;
    }
    [System.Serializable]
    public class MyAddedObjInfo
    {
        [Tooltip("Added GameObject")] public GameObject gameObject = null;
        [Tooltip("IP addres")] public string ip = "";
        [Tooltip("object Name")] public string objName = "";
        [Tooltip("model count")] public int modelCount = 0;
        [Tooltip("some parameters")] public string suffix = "";

        public MyAddedObjInfo(GameObject _go, string _ip, string _objName, int _modelCount, string _suffix)
        {
            gameObject = _go;
            ip = _ip;
            objName = _objName;
            modelCount = _modelCount;
            suffix = _suffix;
        }

    }

    [SerializeField,ReadOnlyWhenPlaying] GameObject m_clientMarkerPrefab = null;
    [SerializeField, ReadOnlyWhenPlaying] MyClientSettings m_settings = new MyClientSettings();
    [SerializeField, ReadOnlyWhenPlaying] TmUDP.ObjPrefabArrScrObj m_prefabInfo=null;
    [SerializeField, ReadOnly, Tooltip("clients except me")] List<MyUDPServer.MyClientInfo> m_plInfoList = null;
    [SerializeField, ReadOnly, Tooltip("GameObjects I instantiate")] List<MyAddedObjInfo> m_AddedObjList = null;
    public List<MyAddedObjInfo> addedObjList { get { return m_AddedObjList; } }
    Vector3 m_previousPos;
    Quaternion m_previousRot;
    float m_reloadTimer;
    int m_modelCount; // increment when create gameObject from prefab 
    public int modelCount { get { return m_modelCount; } }

    //public void SetPosition(Vector3 _pos) { transform.position = _pos; }
    //public void SetAngleY(float _angY) { transform.rotation = Quaternion.AngleAxis(_angY,Vector3.up); }

    // Start is called before the first frame update
    public override void Start()
    {
        if (USE_PLAYERPREFS && PlayerPrefs.HasKey(PREFS_KEY_HOST))
        {
            this.m_host = PlayerPrefs.GetString(PREFS_KEY_HOST);
            Debug.Log("Change host");
        }
        if (USE_PLAYERPREFS && PlayerPrefs.HasKey(PREFS_KEY_HSEND_PORT))
            this.m_receivePort = PlayerPrefs.GetInt(PREFS_KEY_HSEND_PORT); // serverSend=clientRecv
        if (USE_PLAYERPREFS && PlayerPrefs.HasKey(PREFS_KEY_HRECV_PORT))
            this.m_sendPort = PlayerPrefs.GetInt(PREFS_KEY_HRECV_PORT); // serverSend=clientRecv

        base.Start();
        m_plInfoList = new List<MyUDPServer.MyClientInfo>();
        m_AddedObjList = new List<MyAddedObjInfo>();
        m_previousPos = transform.position;
        m_previousRot = transform.rotation;
        m_reloadTimer = 0f;
        m_modelCount = 0;
        // send init when start
        this.SendDataFromDataStr(this.myIP + "," + TmUDP.TmUDPModule.KWD_INIT+","+ Vector3ToFormatedStr(transform.position,2));
    }

    // Update is called once per frame
    public override void Update()
    {
        base.Update();
        m_reloadTimer = Mathf.Max(m_reloadTimer - Time.deltaTime, 0f);
        if (m_reloadTimer <= 0f)
        {
            float dist = (transform.position - m_previousPos).magnitude;
            if (dist >= m_settings.minDist)
            {
                m_reloadTimer = m_settings.reloadTime;
                m_previousPos = transform.position;
                this.SendDataFromDataStr(getDataStrFromPosition(this.myIP,transform.position));
            }
            Quaternion rot = transform.rotation;
            float diffAngY = GetDiffRot(m_previousRot, rot);
            if (diffAngY >= m_settings.minAngY)
            {
                m_reloadTimer = m_settings.reloadTime;
                m_previousRot = rot;
                this.SendDataFromDataStr(getDataStrFromRotation(this.myIP, rot));
            }
        }
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
                    bool isInit = false;
                    bool posResult = MyUDPServer.TryGetPosFromData(dataArr, out pos, out isInit);
                    if (posResult)
                    {
                        info.pos = pos;
                        info.obj.transform.position = info.pos;
                    }
                    if (isInit)
                    {
                        this.SendDataFromDataStr(getDataStrFromPosition(this.myIP, transform.position));
                        Debug.Log("-**-MyUDPClientRecvREQ:" + text);
                    }

                    float angY =0f;
                    bool angResult = MyUDPServer.TryGetAngleYFromData(dataArr, out angY);
                    if (angResult)
                    {
                        info.obj.transform.rotation = Quaternion.AngleAxis(angY,Vector3.up);
                    }

                    Quaternion rot = Quaternion.identity;
                    bool rotResult = MyUDPServer.TryGetQuatFromData(dataArr, out rot);
                    if (rotResult)
                    {
                        info.obj.transform.rotation = rot;
                    }

                    string objName = "";
                    string suffix = "";
                    int count = 0;
                    bool objResult = MyUDPServer.TryGetObjectNameFromData(dataArr, out objName, out count, out pos, out rot, out suffix);
                    if (objResult)
                    {
                        if(HasGameObjectInAddedList(ipStr, objName, count))
                        { // update only
                            MyAddedObjInfo existInfo = getInfoFromInfo(ipStr, objName, count);
                            if (existInfo != null)
                            {
                                    existInfo.gameObject.transform.SetPositionAndRotation(pos,rot);
                            }
                        }
                        else
                        { // add to list
                            GameObject go = InstantiateAndAddGameObject(ipStr, objName, count, pos, rot, suffix);
                        }
                    }

                    objName = "";
                    count = 0;
                    objResult = MyUDPServer.TryGetRemoveObjectFromData(dataArr, out objName, out count);
                    if (objResult)
                    {
                        if (HasGameObjectInAddedList(ipStr, objName, count))
                        {
                            MyAddedObjInfo existInfo = getInfoFromInfo(ipStr, objName, count);
                            if (existInfo != null)
                            {
                                Destroy(existInfo.gameObject);
                                m_AddedObjList.Remove(existInfo);
                            }
                        }
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

        if (MyUDPServer.OnAddClientSub(_dataArr, m_plInfoList, m_clientMarkerPrefab, m_settings.parentTr))
        {
            Debug.Log("----MyUDPClientAdd:" + _dataArr[0].ToString());
        }
    }

    public void OnRemoveClient(string[] _dataArr)
    {
        string ipStr = _dataArr[0];
        if (ipStr == this.myIP)
            return;

        if (MyUDPServer.OnRemoveClientSub(_dataArr, m_plInfoList))
        {
            Debug.Log("----MyUDPClientRemove:" + _dataArr[0].ToString());
        }
    }

    public void OnChangeHost(string _hostStr)
    {
        string[] hostInfoStrArr = _hostStr.Split(':');
        if (hostInfoStrArr.Length > 0)
        {
            PlayerPrefs.SetString(PREFS_KEY_HOST, hostInfoStrArr[0]);
            if (hostInfoStrArr.Length > 2)
            {
                int hSp = this.m_receivePort; // host's send port = client's receive port
                int hRp = this.m_sendPort;    // host's receive port = client's send port
                int.TryParse(hostInfoStrArr[1], out hSp);
                int.TryParse(hostInfoStrArr[2], out hRp);
                PlayerPrefs.SetInt(PREFS_KEY_HSEND_PORT, hSp);
                PlayerPrefs.SetInt(PREFS_KEY_HRECV_PORT, hRp);
            }
            Debug.Log("Change host > " + _hostStr);
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }

    public void OnAddGameObject(string _objName)
    {
        AddGameObject(_objName,"");
    }
    public GameObject AddGameObject(string _objName,string _suffix)
    {
        GameObject go = InstantiateAndAddGameObject(this.myIP, _objName, m_modelCount, transform.position+transform.forward*1f, transform.rotation, _suffix);
        if (go!=null)
        {
            string str = GetDataStrFromObjName(this.myIP, _objName, m_modelCount, transform.position, transform.rotation, _suffix);
            this.SendDataFromDataStr(str);
            m_modelCount++; // increment when create gameObject from prefab 
        }
        return go;
    }

    public GameObject InstantiateAndAddGameObject(string _ipStr, string _objName, int _count, Vector3 _pos, Quaternion _rot, string _suffix)
    {
        GameObject go = null;
        int prefabId = MyUDPServer.GetPrefabIdFromName(_objName, m_prefabInfo);
        if ((prefabId >= 0) && m_prefabInfo.objInfoArr.Length > prefabId)
        {   // Instantiate OBJ
            go = Instantiate(m_prefabInfo.objInfoArr[prefabId].prefab, _pos, _rot);
            go.name = _ipStr + "_" + m_prefabInfo.objInfoArr[prefabId].name + "_" + _count;
            go.name += (_suffix != "") ? "_" + _suffix : "";

            m_AddedObjList.Add(new MyAddedObjInfo(go, _ipStr, _objName, _count, _suffix));
        }
        return go;
    }

    public void OnRemoveGameObject(GameObject _go)
    {
        MyAddedObjInfo info = getInfoFromGameObject(_go);
        if (info != null)
        {
            string valStr = info.objName + "," + info.modelCount.ToString() + ",";
            this.SendDataFromDataStr(info.ip + "," + MyUDPServer.KWDEX_REMOVEOBJ + "," + valStr);
            m_AddedObjList.Remove(info);
            Destroy(_go);
        }
    }

    public void OnSetImage(MyAddedObjInfo _info, string _url)
    {
        StartCoroutine(setImageCo(_info,_url));
    }

    public void OnMoveGameObject(GameObject _go)
    {
        if (!IsMyGameObject(_go))
            return;

        MyAddedObjInfo info = getInfoFromGameObject(_go);
        if (info != null)
        {
            string str = GetDataStrFromObjName(this.myIP, info.objName, info.modelCount, _go.transform.position, _go.transform.rotation,"");
            this.SendDataFromDataStr(str);
        }
    }

    public bool HasGameObjectInAddedList(string _ip, string _objName,int _count)
    {
        return m_AddedObjList.Any<MyAddedObjInfo>(e => (e.objName==_objName && e.modelCount == _count && e.ip == _ip));
    }
    public bool HasGameObjectInAddedList(GameObject _go)
    {
        return m_AddedObjList.Any<MyAddedObjInfo>(e => (e.gameObject.Equals(_go)));
    }

    public bool IsMyGameObject(GameObject _go)
    {
        return m_AddedObjList.Any<MyAddedObjInfo>(e => (e.gameObject.Equals(_go)) && (e.ip == this.myIP));
    }

    private MyAddedObjInfo getInfoFromGameObject(GameObject _go)
    {
        MyAddedObjInfo info = null;
        for (int i = 0; i < m_AddedObjList.Count; ++i)
        {
            if (m_AddedObjList[i].gameObject.Equals(_go))
            {
                info = m_AddedObjList[i];
                break;
            }
        }
        return info;
    }

    private MyAddedObjInfo getInfoFromInfo(string _ip, string _objName, int _count)
    {
        MyAddedObjInfo info = null;
        for (int i = 0; i < m_AddedObjList.Count; ++i)
        {
            if (m_AddedObjList[i].ip == _ip && m_AddedObjList[i].objName == _objName && m_AddedObjList[i].modelCount == _count)
            {
                info = m_AddedObjList[i];
                break;
            }
        }
        return info;
    }


    // for debug
    void OnGUI()
    {
        MyUDPServer.ONGUISub(this.host, this.sendPort, this.receivePort, this.myIP, m_plInfoList);
    }

    static public float GetDiffAngleY(float _angY0, float _angY1) {
        float diff = Mathf.Repeat((_angY1-_angY0) + 180f, 360f) - 180f;
        return Mathf.Abs(diff);
    }

    static public float GetDiffRot(Quaternion _rot0, Quaternion _rot1)
    {
        float diff = Quaternion.Angle(_rot0,_rot1);
        return Mathf.Abs(diff);
    }


    static public string GetDataStrFromObjName(string _ip, string _objName, int _countModel, Vector3 _pos, Quaternion _rot, string _suffix)
    {
        string valStr = _objName + "," + _countModel.ToString() + ",";
        valStr += TmUDP.TmUDPClient.Vector3ToFormatedStr(_pos, 2) + ",";
        valStr += TmUDP.TmUDPClient.QuaternionToFormatedStr(_rot, 2);
        return _ip + "," + MyUDPServer.KWDEX_OBJ + "," + valStr + "," +_suffix;
    }

    IEnumerator setImageCo(MyAddedObjInfo _info, string _url)
    {
        using (UnityWebRequest uwr = UnityWebRequestTexture.GetTexture(_url))
        {
            yield return uwr.SendWebRequest();

            if (!(uwr.isNetworkError || uwr.isHttpError))
            {
                Texture2D texture = DownloadHandlerTexture.GetContent(uwr);
                GameObject go = _info.gameObject;
                go.transform.GetChild(0).GetComponent<MeshRenderer>().material.SetTexture("_MainTex", texture);
            }
            else
            {
                Debug.Log(uwr.error);
            }

        }
    }
}
