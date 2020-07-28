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
    [SerializeField,ReadOnlyWhenPlaying] GameObject m_clientMarkerPrefab = null;
    [SerializeField, ReadOnlyWhenPlaying] MyClientSettings m_settings = new MyClientSettings();
    [SerializeField, ReadOnlyWhenPlaying] TmUDP.ObjPrefabArrScrObj m_prefabInfo=null;
    [SerializeField, ReadOnly, Tooltip("clients except me")] List<MyUDPServer.MyClientInfo> m_plInfoList = null;
    Vector3 m_previousPos;
    Quaternion m_previousRot;
    float m_reloadTimer;
    int m_modelCount; // increment when create gameObject from prefab 

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
                    int count = 0;
                    bool objResult = MyUDPServer.TryGetObjectNameFromData(dataArr, out objName, out count, out pos, out rot);
                    if (objResult)
                    {
                        int prefabId = MyUDPServer.GetPrefabIdFromName(objName, m_prefabInfo);
                        if ((prefabId>=0) && m_prefabInfo.objInfoArr.Length > prefabId)
                        {   // Instantiate OBJ
                            GameObject go = Instantiate(m_prefabInfo.objInfoArr[prefabId].prefab);
                            go.name = m_prefabInfo.objInfoArr[prefabId].name + "_" + count + "_C";
                            go.transform.position = pos;
                            go.transform.rotation = rot;
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
        int prefabId = MyUDPServer.GetPrefabIdFromName(_objName, m_prefabInfo);
        if ((prefabId >= 0) && m_prefabInfo.objInfoArr.Length > prefabId)
        {   // Instantiate OBJ
            GameObject go = Instantiate(m_prefabInfo.objInfoArr[prefabId].prefab);
            go.name = m_prefabInfo.objInfoArr[prefabId].name + "_" + m_modelCount;
            go.transform.position = transform.position;
            go.transform.rotation = transform.rotation;

            string str = GetDataStrFromObjName(this.myIP, _objName, m_modelCount, transform.position, transform.rotation, m_prefabInfo);
            this.SendDataFromDataStr(str);
            m_modelCount++; // increment when create gameObject from prefab 
        }
    }

    public void OnAddImage(string _url)
    {
        StartCoroutine(setImageCo(_url));
    }

    public void OnMoveGameObject(GameObject _go)
    {
        // do not sync object after instantiate, but send to server.
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


    static public string GetDataStrFromObjName(string _ip, string _objName, int _countModel, Vector3 _pos, Quaternion _rot, TmUDP.ObjPrefabArrScrObj _prefabArr)
    {
        string valStr = _objName + "," + _countModel.ToString() + ",";
        valStr += TmUDP.TmUDPClient.Vector3ToFormatedStr(_pos, 2) + ",";
        valStr += TmUDP.TmUDPClient.QuaternionToFormatedStr(_rot, 2);
        return _ip + "," + MyUDPServer.KWDEX_OBJ + "," + valStr;
    }

    IEnumerator setImageCo(string _url)
    {
        using (UnityWebRequest uwr = UnityWebRequestTexture.GetTexture(_url))
        {
            yield return uwr.SendWebRequest();

            if (!(uwr.isNetworkError || uwr.isHttpError))
            {
                Texture2D texture = DownloadHandlerTexture.GetContent(uwr);
                GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                Destroy(go.GetComponent<BoxCollider>());
                go.transform.position = transform.position;
                go.transform.rotation = transform.rotation;
                go.transform.localScale = Vector3.one * 0.1f;
                go.GetComponent<MeshRenderer>().material.SetTexture("_MainTex", texture);
            }
            else
            {
                Debug.Log(uwr.error);
            }

        }
    }
}
