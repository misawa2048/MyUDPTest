using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MyUDPClient : TmUDP.TmUDPClient
{
    [System.Serializable]
    public class MyClientSettings
    {
        [Tooltip("Minimum distance to send message")]  public float minDist = 0.5f;
        [Tooltip("Minimum rotationY to send message")] public float minAngY = 5f;
        [Tooltip("Minimum time to send message again")] public float reloadTime = 0.2f;
    }
    [SerializeField,ReadOnly,Tooltip("clients except me")] List<MyUDPServer.MyClientInfo> m_plInfoList = null;
    [SerializeField,ReadOnlyWhenPlaying] GameObject m_clientMarkerPrefab = null;
    [SerializeField, ReadOnlyWhenPlaying] MyClientSettings m_settings = new MyClientSettings();
    Vector3 m_previousPos;
    float m_previousAngY;
    float m_reloadTimer;

    public void SetPosition(Vector3 _pos) { transform.position = _pos; }
    public void SetAngleY(float _angY) { transform.rotation = Quaternion.AngleAxis(_angY,Vector3.up); }

    // Start is called before the first frame update
    public override void Start()
    {
        base.Start();
        m_plInfoList = new List<MyUDPServer.MyClientInfo>();
        m_previousPos = transform.position;
        m_previousAngY = transform.rotation.eulerAngles.y;
        m_reloadTimer = 0f;
        //StartCoroutine(udpDbgSendCo());
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
                m_previousPos = transform.position;
                string valStr = TmUDP.TmUDPClient.Vector3ToFormatedStr(transform.position, 2);
                string str = this.myIP + "," + MyUDPServer.KWD_POS + "," + valStr;
                byte[] data = System.Text.Encoding.UTF8.GetBytes(str);
                this.SendData(data);
                m_reloadTimer = m_settings.reloadTime;
                Debug.Log(str);
            }
            float angY = transform.rotation.eulerAngles.y;
            float diffAngY = getDiffAngleY(m_previousAngY, angY);
            if (diffAngY >= m_settings.minAngY)
            {
                m_previousAngY = angY;
                string valStr = TmUDP.TmUDPClient.AngleYToFormatedStr(angY, 2);
                string str = this.myIP + "," + MyUDPServer.KWD_RORY + "," + valStr;
                byte[] data = System.Text.Encoding.UTF8.GetBytes(str);
                this.SendData(data);
                m_reloadTimer = m_settings.reloadTime;
                Debug.Log(str);
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
                    bool posResult = MyUDPServer.TryGetPosFromData(dataArr, out pos);
                    if (posResult)
                    {
                        info.pos = pos;
                        info.obj.transform.position = info.pos;
                    }

                    float angY=0f;
                    bool angResult = MyUDPServer.TryGetAngleYFromData(dataArr, out angY);
                    if (angResult)
                    {
                        info.obj.transform.rotation = Quaternion.AngleAxis(angY,Vector3.up);
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

        if (!m_plInfoList.Any(v => v.uip == ipStr))
        {
            MyUDPServer.MyClientInfo info = MyUDPServer.CreateClientMarker(_dataArr, m_clientMarkerPrefab);
            m_plInfoList.Add(info);
            Debug.Log("----MyUDPClientAdd:" + ipStr.ToString());
        }
    }

    public void OnRemoveClient(string[] _dataArr)
    {
        string ipStr = _dataArr[0];
        if (ipStr == this.myIP)
            return;

        MyUDPServer.MyClientInfo tgt = m_plInfoList.First(v => v.uip == ipStr);
        if (tgt != null)
        {
            if (tgt.obj != null)
                Destroy(tgt.obj);

            m_plInfoList.Remove(tgt);
            Debug.Log("----MyUDPClientRemove:" + ipStr.ToString());
        }
    }

    float getDiffAngleY(float _angY0, float _angY1) {
        float diff = Mathf.Repeat((_angY1-_angY0) + 180f, 360f) - 180f;
        return Mathf.Abs(diff);
    }

    IEnumerator udpDbgSendCo()
    {
        while (true)
        {
            yield return new WaitForSeconds(1.0f);
            Vector3 val = new Vector3(Random.value, 0.1f, Random.value);
            SetPosition(val);
            SetAngleY(0f);
        }
    }

    // for debug
    void OnGUI()
    {
        GUIStyle customGuiStyle = new GUIStyle();
        customGuiStyle.fontSize = 32;
        customGuiStyle.alignment = TextAnchor.UpperRight;
        GUILayout.BeginArea(new Rect(Screen.width-310, 0, 300, Screen.height));
        GUILayout.BeginVertical();
        GUILayout.TextArea("host:" + this.host, customGuiStyle);
        GUILayout.TextArea("myIP:" + this.myIP, customGuiStyle);
        foreach (MyUDPServer.MyClientInfo info in m_plInfoList)
        {
            GUILayout.TextArea(info.uip, customGuiStyle);
        }
        GUILayout.EndVertical();
        GUILayout.EndArea();
    }
}
