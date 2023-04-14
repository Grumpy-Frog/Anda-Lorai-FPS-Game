using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using UnityEngine.SceneManagement;

public class LeaveRoom : MonoBehaviour
{
    bool gameOver = false;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void LeaveRoomFunc()
    {
        StartCoroutine(LeaveAndLoad());
    }

    IEnumerator LeaveAndLoad()
    {
        PhotonNetwork.LeaveRoom();

        while(PhotonNetwork.InRoom)
        {
            yield return null;  
        }

        SceneManager.LoadScene("Menu");
    }

    public void QuitGame()
    {
        Application.Quit();
    }

}
