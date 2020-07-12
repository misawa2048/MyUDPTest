using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerCtrlWithButton : MonoBehaviour
{
    [SerializeField] Transform m_targetTr = null;
    [SerializeField,Tooltip("L/R,Jump,and F/W")] Vector3 m_moveSped = new Vector3(1f, 1f, 1f);
    Rigidbody m_rigidbody = null;
    Vector3 m_speed;
    // Start is called before the first frame update
    void Start()
    {
        m_rigidbody = m_targetTr.GetComponent<Rigidbody>();
        m_speed = Vector3.zero;
    }

    // Update is called once per frame
    void Update()
    {
        if (m_rigidbody==null)
        {
            Debug.Log("Err.TargetTr needs rigidbody");
            return;
        }
        m_rigidbody.MovePosition(m_targetTr.position + m_targetTr.forward * m_speed.z * m_moveSped.z*Time.deltaTime);
        m_rigidbody.MoveRotation(m_targetTr.rotation * Quaternion.AngleAxis(m_speed.x * m_moveSped.x * Time.deltaTime,Vector3.up));
        if (m_speed.y > 0f)
        {
            m_speed.y = 0f;
            m_rigidbody.AddForce(Vector3.up* m_moveSped.y,ForceMode.Impulse);
        }
    }

    public void OnForwardButtonDown()
    {
        m_speed.z = 1f;
        Debug.Log("OnForwardButtonDown");
    }
    public void OnForwardButtonUp()
    {
        m_speed.z = 0f;
        Debug.Log("OnForwardButtonUp");
    }
    public void OnBackButtonDown()
    {
        m_speed.z = -1f;
        Debug.Log("OnBackButtonDown");
    }
    public void OnBackButtonUp()
    {
        m_speed.z = 0f;
        Debug.Log("OnBackButtonUp");
    }
    public void OnLeftButtonDown()
    {
        m_speed.x = -1f;
        Debug.Log("OnLeftButtonDown");
    }
    public void OnLeftButtonUp()
    {
        m_speed.x = 0f;
        Debug.Log("OnLeftButtonUp");
    }
    public void OnRightButtonDown()
    {
        m_speed.x = 1f;
        Debug.Log("OnRightButtonDown");
    }
    public void OnRightButtonUp()
    {
        m_speed.x = 0f;
        Debug.Log("OnRightButtonUp");
    }
    public void OnJumpButtonClick()
    {
        m_speed.y = 1f;
        Debug.Log("OnJumpButtonClick");
    }
}
