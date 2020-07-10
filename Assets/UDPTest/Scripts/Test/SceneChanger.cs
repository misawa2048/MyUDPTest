using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TmUDPTest
{
    public class SceneChanger : MonoBehaviour
    {
        public void OnSceeneChangeButton(string _sceneName)
        {
            SceneManager.LoadScene(_sceneName, LoadSceneMode.Single);
        }

        static public void Reload()
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }
}
