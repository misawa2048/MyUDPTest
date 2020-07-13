using UnityEngine;

namespace TmUDP
{
    public class TmUDPClient : TmUDPModule
    {
        // Start is called before the first frame update
        public override void Start()
        {
            m_isServer = false;
            base.Start();
        }
    }
}
