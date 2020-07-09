using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TmUDPTest
{
    public class SceneChanger : MonoBehaviour
    {
        // Start is called before the first frame update
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {

        }

        public void OnSceeneChangeButton(string _sceneName)
        {
            SceneManager.LoadScene(_sceneName, LoadSceneMode.Single);
        }
    }
}
