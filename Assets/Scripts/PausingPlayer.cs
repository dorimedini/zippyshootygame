using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PausingPlayer : MonoBehaviour
{
    public PauseMenuController pauseMenu;

    // Update is called once per frame
    void Update()
    {
        if (Input.GetButtonDown("Cancel"))  // "Cancel" is mapped to Esc, this is the right button
        {
            if (pauseMenu.gameObject.activeInHierarchy)
            {
                Resume();
            }
            else
            {
                Pause();
            }
        }
    }

    public void Pause()
    {
        // TODO: Disable player controls
        pauseMenu.gameObject.SetActive(true);
    }

    public void Resume()
    {
        // TODO: Reactivate player
        pauseMenu.gameObject.SetActive(false);
    }
}
