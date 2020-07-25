using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MyUDPClient : TmUDP.TmUDPClient
{
    public static readonly bool USE_PLAYERPREFS = true;
    public static readonly string PREFS_KEY_HOST = "KeyHost";
    [System.Serializable]
    public class MyClientSettings
    {
        [Tooltip("Minimum distance to send message")]  public float minDist = 0.5f;
        [Tooltip("Minimum rotationY to send message")] public float minAngY = 5f;
        [Tooltip("Minimum time to send message again")] public float reloadTime = 0.2f;
        [Tooltip("Parent transform for offset")] public Transform parentTr = null;
    }
    [SerializeField,ReadOnly,Tooltip("clients except me")] List<MyUDPServer.MyClientInfo> m_plInfoList = null;
    [SerializeField,ReadOnlyWhenPlaying] GameObject m_clientMarkerPrefab = null;
    [SerializeField, ReadOnlyWhenPlaying] MyClientSettings m_settings = new MyClientSettings();
    Vector3 m_previousPos;
    Quaternion m_previousRot;
    float m_reloadTimer;

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
        base.Start();
        m_plInfoList = new List<MyUDPServer.MyClientInfo>();
        m_previousPos = transform.position;
        m_previousRot = transform.rotation;
        m_reloadTimer = 0f;
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
        PlayerPrefs.SetString(PREFS_KEY_HOST, _hostStr);
        Debug.Log("Change host > "+ _hostStr);
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    // for debug
    void OnGUI()
    {
        MyUDPServer.ONGUISub(this.host, this.myIP, m_plInfoList);
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
}
