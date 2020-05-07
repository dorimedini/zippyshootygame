using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

public class NetworkManager : MonoBehaviourPunCallbacks
{
    public GameObject playerPrefab;
    public GameObject geospherePrefab;
    public GameObject standbyCamera;
    public ExplosionController explosionCtrl;
    public PillarExtensionController pillarCtrl;
    public SpawnManager spawner;
    public MainMenuController mainMenu;

    public bool offlineMode;

    GameObject myPlayer;
    GeoSphereGenerator arena;
    List<RoomInfo> rooms;

    void Start()
    {
        //UserDefinedConstants.LoadFromPlayerPrefs();
        UserDefinedConstants.LoadDefaultValues(false);
        PhotonNetwork.LocalPlayer.NickName = UserDefinedConstants.nickname;
        rooms = new List<RoomInfo>();
    }

    void OnDestroy()
    {
        UserDefinedConstants.SaveToPlayerPrefs();
    }

    public void OnGUI()
    {
        GUILayout.Label(PhotonNetwork.NetworkClientState.ToString());
    }

    public void StartSingleplayer()
    {
        PhotonNetwork.OfflineMode = true;
        OnJoinedLobby();
    }

    public void StartMultiplayer()
    {
        PhotonNetwork.ConnectUsingSettings();
    }

    public List<RoomInfo> GetRooms() { return rooms; }

    public override void OnRoomListUpdate(List<RoomInfo> roomList)
    {
        base.OnRoomListUpdate(roomList);
        rooms = new List<RoomInfo>();
        foreach (RoomInfo room in roomList)
            rooms.Add(room);
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
        if (!PhotonNetwork.CreateRoom(null, options))
            Debug.LogError("Failed to create a room!");
    }

    public override void OnCreateRoomFailed(short code, string msg)
    {
        Debug.LogError(string.Format("OnCreateRoomFailed. Code: {0}, message: '{1}'", code, msg));
    }

    public override void OnJoinedRoom()
    {
        InitArena();
        SpawnMyPlayer();
    }

    public override void OnLeftRoom()
    {
        base.OnLeftRoom();
        if (standbyCamera != null) // Player didn't quit, just left the room
            standbyCamera.SetActive(true);
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        base.OnDisconnected(cause);
        DestroyArena();
        if (mainMenu != null && mainMenu.gameObject != null)
            mainMenu.gameObject.SetActive(true);
    }

    void SpawnMyPlayer()
    {
        myPlayer = spawner.SpawnMyself();
        standbyCamera.SetActive(false);
        explosionCtrl.localUserId = myPlayer.GetComponent<PhotonView>().Owner.UserId;
    }

    void InitArena()
    {
        GameObject gsg = Instantiate(geospherePrefab, Vector3.zero, Quaternion.identity);
        arena = gsg.GetComponent<GeoSphereGenerator>();
        pillarCtrl.Init(arena.GetPillars());
        spawner.Init(arena.GetPillars());
    }

    void DestroyArena()
    {
        Destroy(arena.gameObject);
    }
}
