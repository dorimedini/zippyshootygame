using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

public class NetworkManager : MonoBehaviourPunCallbacks
{
    public GameObject playerPrefab;
    public GameObject standbyCamera;

    public bool offlineMode;

    GameObject myPlayer;
    SpawnManager spawner;

    public void OnGUI()
    {
        GUILayout.Label(PhotonNetwork.NetworkClientState.ToString());
        if (!PhotonNetwork.IsConnected)
        {
            if (GUILayout.Button("Multiplayer"))
            {
                PhotonNetwork.ConnectUsingSettings();
            }
            else if (GUILayout.Button("Singleplayer"))
            {
                PhotonNetwork.OfflineMode = true;
                OnJoinedLobby();
            }
        }
    }

    public override void OnConnectedToMaster()
    {
        if (PhotonNetwork.OfflineMode) return;
        if (!PhotonNetwork.JoinLobby())
            Debug.LogError(string.Format("OnConnectedToMaster, JoinLobby() returned false!"));
    }

    public override void OnJoinedLobby()
    {
        PhotonNetwork.JoinRandomRoom();
    }

    public override void OnJoinRandomFailed(short code, string msg)
    {
        RoomOptions options = new RoomOptions();
        options.PublishUserId = true;
        Debug.Log("OnJoinRandomFailed, creating and joining room");
        if (!PhotonNetwork.CreateRoom(null, options))
            Debug.LogError("Failed to create a room!");
    }

    public override void OnCreateRoomFailed(short code, string msg)
    {
        Debug.LogError(string.Format("OnCreateRoomFailed. Code: {0}, message: '{1}'", code, msg));
    }

    public override void OnJoinedRoom()
    {
        Debug.Log("OnJoinedRoom");
        InitSpawner();
        SpawnMyPlayer();
    }

    void InitSpawner()
    {
        spawner = GameObject.Find("_SCRIPTS").GetComponent<SpawnManager>();
        if (spawner == null)
            Debug.LogError("Got null SpawnManager");
    }

    void SpawnMyPlayer()
    {
        myPlayer = spawner.SpawnMyself();
        standbyCamera.SetActive(false);
    }
}
