using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PauseMenuController : MonoBehaviour
{
    public int landingMenuIdx;
    public GameObject[] menus;
    public PausingPlayer pauseBehaviourComponent;

    public void OnQuit()
    {
        Application.Quit(0);
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
