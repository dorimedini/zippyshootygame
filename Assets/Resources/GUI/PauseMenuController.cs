using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PauseMenuController : MonoBehaviour
{
    public GameObject menuRoot;
    public PausingPlayer pauseBehaviourComponent;

    public void OnQuit()
    {
        // TODO: Quit game
    }

    public void OnResume()
    {
        pauseBehaviourComponent.Resume();
    }
}
