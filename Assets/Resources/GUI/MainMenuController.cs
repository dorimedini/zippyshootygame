using Photon.Realtime;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System.Linq;
using UnityEngine.UI;

public class MainMenuController : MonoBehaviour
{
    public SliderInputController radiusInput, EHNInput;
    public ToggleInputController dummyPlayerInput;
    public NetworkManager netMngr;

    public TextMeshProUGUI roomListArea;

    public GameObject singleButton, multiButton, showRoomsButton, joinButton;

    void Start()
    {
        ReadSettings();
    }

    void OnEnabled()
    {
        ReadSettings();
    }

    public void OnSingleplayer()
    {
        DisableSingleMultiButtons();
        netMngr.StartSingleplayer();
    }

    public void OnMultiplayer()
    {
        DisableSingleMultiButtons();
        netMngr.StartMultiplayer();
    }

    public void OnQuit()
    {
        Application.Quit(0);
    }

    public void OnShowRooms()
    {
        List<RoomInfo> rooms = netMngr.GetRooms();
        roomListArea.text = rooms.Count == 0 ?
            "No rooms found" :
            string.Join("\n", rooms.Select(room => room.Name));
    }

    public void OnJoinRoom()
    {
        ApplySettings();
        netMngr.JoinRoom();
    }

    void ApplySettings()
    {
        UserDefinedConstants.spawnDummyPlayer = dummyPlayerInput.value;
        UserDefinedConstants.sphereRadius = radiusInput.value;
        UserDefinedConstants.EHN = (int)EHNInput.value;
    }

    void ReadSettings()
    {
        dummyPlayerInput.value = UserDefinedConstants.spawnDummyPlayer;
        radiusInput.value = UserDefinedConstants.sphereRadius;
        EHNInput.value = UserDefinedConstants.EHN;
    }

    public void ResetToDefaults()
    {
        dummyPlayerInput.value = UserDefinedConstants.GetBoolEntries()["spawnDummyPlayer"]._default_val;
        radiusInput.value = UserDefinedConstants.GetFloatEntries()["sphereRadius"]._default_val;
        EHNInput.value = UserDefinedConstants.GetIntEntries()["EHN"]._default_val;
        ApplySettings();
    }

    public void HideSingleMultiButtons()
    {
        singleButton.SetActive(false);
        multiButton.SetActive(false);
    }
    public void DisableSingleMultiButtons()
    {
        singleButton.GetComponent<Button>().interactable = false;
        multiButton.GetComponent<Button>().interactable = false;
    }
    public void ShowSingleMultiButtons()
    {
        singleButton.SetActive(true);
        multiButton.SetActive(true);
    }
    public void EnableSingleMultiButtons()
    {
        singleButton.GetComponent<Button>().interactable = true;
        multiButton.GetComponent<Button>().interactable = true;
    }


    public void HideRoomButtons()
    {
        joinButton.SetActive(false);
        showRoomsButton.SetActive(false);
    }
    public void ShowRoomButtons()
    {
        joinButton.SetActive(true);
        showRoomsButton.SetActive(true);
    }
}
