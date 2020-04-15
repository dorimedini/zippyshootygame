using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

public class NetworkManager : MonoBehaviourPunCallbacks
{
    public GameObject playerPrefab;

    public void Start()
    {
        Connect();
    }

    public void OnGUI()
    {
        GUILayout.Label(PhotonNetwork.NetworkClientState.ToString());
    }

    void Connect()
    {
        PhotonNetwork.ConnectUsingSettings();
    }

    public override void OnConnectedToMaster()
    {
        if (!PhotonNetwork.JoinLobby())
            Debug.LogError(string.Format("OnConnectedToMaster, JoinLobby() returned false!"));
    }

    public override void OnJoinedLobby()
    {
        PhotonNetwork.JoinRandomRoom();
    }

    public override void OnJoinRandomFailed(short code, string msg)
    {
        Debug.Log("OnJoinRandomFailed, creating and joining room");
        if (!PhotonNetwork.CreateRoom(null))
            Debug.LogError("Failed to create a room!");
    }

    public override void OnCreateRoomFailed(short code, string msg)
    {
        Debug.LogError(string.Format("OnCreateRoomFailed. Code: {0}, message: '{1}'", code, msg));
    }

    public override void OnJoinedRoom()
    {
        Debug.Log("OnJoinedRoom");
        SpawnMyPlayer();
    }

    void SpawnMyPlayer()
    {
        GameObject geoSphere = GameObject.Find("GeoSphere");
        if (geoSphere == null)
        {
            Debug.LogError("GeoSphereGenerator not found on room join");
            return;
        }
        GeoSphereGenerator gsg = geoSphere.GetComponent<GeoSphereGenerator>();
        if (gsg == null)
        {
            Debug.LogError("GeoSphere game objects doesn't have a GeoSphereGenerator attached!");
            return;
        }
        // Get possible spawn points / rotations from geosphere script
        var spawnPoints = gsg.PlayerSpawnPoints();
        int idx = Random.Range(0, spawnPoints.Count);
        Vector3 spawnLoc = spawnPoints[idx].Item1;
        Quaternion spawnRot = spawnPoints[idx].Item2;
        Debug.Log(string.Format("Spawning player at location/rotation {0}/{1}", spawnLoc, spawnRot));
        PhotonNetwork.Instantiate("Player", spawnLoc, spawnRot);
    }
}
