using UnityEngine;

namespace TmUDP
{
    [System.Serializable]
    public class PrefabInfo
    {
        [Tooltip("DO NOT USE confusing name by 'StartWith()', For exanple, 'red' and 'red2'.")]
        public string name;
        public GameObject prefab;
    }

    [CreateAssetMenu(menuName = "Params/ObjPrefabList")]
    public class ObjPrefabArrScrObj : ScriptableObject
    {
        public PrefabInfo photoPrefabInfo;
        public PrefabInfo[] objInfoArr;
    }
}
