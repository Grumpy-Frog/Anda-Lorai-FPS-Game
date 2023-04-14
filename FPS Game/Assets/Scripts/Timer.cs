using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System;

public class Timer : MonoBehaviour
{
    bool startTimer = false;
    double timerIncrementValue;
    double startTime;
    [SerializeField] double timer = 150.0f;
    ExitGames.Client.Photon.Hashtable CustomeValue;

    [SerializeField] TMP_Text timer_text;

    void Start()
    {
        if (PhotonNetwork.LocalPlayer.IsMasterClient)
        {
            CustomeValue = new ExitGames.Client.Photon.Hashtable();
            startTime = PhotonNetwork.Time;
            startTimer = true;
            CustomeValue.Add("StartTime", startTime);
            PhotonNetwork.CurrentRoom.SetCustomProperties(CustomeValue);
        }
        else
        {
            bool isOk = false;
            try
            {
                startTime = double.Parse(PhotonNetwork.CurrentRoom.CustomProperties["StartTime"].ToString());
                isOk = true;
            }
            catch(Exception e)
            {
                Debug.Log(e);
                isOk= false;
            }

            if (isOk==false)
            {
                startTime = double.Parse(PhotonNetwork.Time.ToString());
                startTime -= 0.2f;
            }
            
            startTimer = true;
        }
    }

    void Update()
    {

        if (!startTimer) return;

        
        if (timerIncrementValue >= timer)
        {
            //Timer Completed
            //Do What Ever You What to Do Here
            Time.timeScale = 0;
        }
        else
        {
            timerIncrementValue = PhotonNetwork.Time - startTime;
            double remainingTime = timer - timerIncrementValue;
            int remTime = (int)remainingTime;
            timer_text.text = remTime.ToString();
        }
    }
}
