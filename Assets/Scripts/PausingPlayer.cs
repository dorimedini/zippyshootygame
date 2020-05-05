using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PausingPlayer : MonoBehaviour
{
    public PauseMenuController pauseMenu;

    public MouseLookController mouseLookChar;
    public PlayerMovementController movementChar;
    public GrapplingCharacter grappleChar;
    public ShootingCharacter shootingChar;

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
        SetPlayerActive(false);
        pauseMenu.gameObject.SetActive(true);
        pauseMenu.ShowMainPauseMenu();
    }

    public void Resume()
    {
        SetPlayerActive(true);
        pauseMenu.gameObject.SetActive(false);
    }

    void SetPlayerActive(bool active)
    {
        Cursor.visible = !active;
        mouseLookChar.Pause(!active);
        movementChar.Pause(!active);
        grappleChar.Pause(!active);
        shootingChar.Pause(!active);
    }
}
