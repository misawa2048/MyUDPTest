using UnityEngine;

namespace TmUDP
{
    public class TmUDPServer : TmUDPModule
    {
        // Start is called before the first frame update
        public override void Start()
        {
            m_isServer = true;
            base.Start();
        }
    }
}
