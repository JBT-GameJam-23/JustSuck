using System;
using Sucker;
using UnityEngine;
using UnityEngine.SceneManagement;


public class RestartScene : MonoBehaviour
{
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.R) || Input.GetKeyDown(KeyCode.F1))
        {
            Destroy(SuckerManager.Instance.gameObject);
            SceneManager.LoadScene("start");
        }
    }
}
