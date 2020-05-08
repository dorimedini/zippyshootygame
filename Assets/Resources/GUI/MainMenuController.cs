using Photon.Realtime;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System.Linq;

public class MainMenuController : MonoBehaviour
{
    public SliderInputController radiusInput, EHNInput;
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
        ApplySettings();
        HideSingleMultiButtons();
        netMngr.StartSingleplayer();
    }

    public void OnMultiplayer()
    {
        ApplySettings();
        HideSingleMultiButtons();
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
        netMngr.JoinRoom();
    }

    void ApplySettings()
    {
        UserDefinedConstants.sphereRadius = radiusInput.value;
        UserDefinedConstants.EHN = (int)EHNInput.value;
    }

    void ReadSettings()
    {
        radiusInput.value = UserDefinedConstants.sphereRadius;
        EHNInput.value = UserDefinedConstants.EHN;
    }

    public void ResetToDefaults()
    {
        radiusInput.value = UserDefinedConstants.GetFloatEntries()["sphereRadius"]._default_val;
        EHNInput.value = UserDefinedConstants.GetIntEntries()["EHN"]._default_val;
        ApplySettings();
    }

    public void HideSingleMultiButtons()
    {
        singleButton.SetActive(false);
        multiButton.SetActive(false);
    }

    public void ShowSingleMultiButtons()
    {
        singleButton.SetActive(true);
        multiButton.SetActive(true);
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
