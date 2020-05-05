using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;

public class PauseMenuController : MonoBehaviour
{
    public int landingMenuIdx;
    public GameObject[] menus;
    public PausingPlayer pauseBehaviourComponent;

    public void OnQuit()
    {
        Cursor.visible = true;
        Application.Quit(0);
    }

    public void OnQuitToMainMenu()
    {
        Cursor.visible = true;
        PhotonNetwork.Disconnect();
    }

    public void OnResume()
    {
        pauseBehaviourComponent.Resume();
    }

    public void ShowMainPauseMenu()
    {
        menus[0].SetActive(true);
        for (int i = 1; i < menus.Length; ++i)
            menus[i].SetActive(false);
    }
}
