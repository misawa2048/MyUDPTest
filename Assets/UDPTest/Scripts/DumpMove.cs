using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DumpMove : MonoBehaviour
{
    [SerializeField] Transform m_targetTr = null;
    [SerializeField,Range(0f,1f)] float m_dumpRate = 0.1f;
    Vector3 m_previousPosition;
    Quaternion m_previousRotation;

    // Start is called before the first frame update
    void Start()
    {
        m_previousPosition = transform.position;
        m_previousRotation = transform.rotation;
    }

    // Update is called once per frame
    void Update()
    {
        transform.position = Vector3.Lerp(m_previousPosition, m_targetTr.position, m_dumpRate);
        m_previousPosition = transform.position;
        transform.rotation = Quaternion.Lerp(m_previousRotation, m_targetTr.rotation, m_dumpRate);
        m_previousRotation = transform.rotation;
    }
}
