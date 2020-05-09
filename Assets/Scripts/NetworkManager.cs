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

    public MessagesController msg;

    public static GameObject DummyPlayer;

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

    public void JoinRoom()
    {
        mainMenu.gameObject.SetActive(false);
        PhotonNetwork.JoinRandomRoom();
    }

    public override void OnRoomListUpdate(List<RoomInfo> roomList)
    {
        base.OnRoomListUpdate(roomList);
        msg.AppendMessage("Got room list update, now see " + roomList.Count + " rooms");
        rooms = new List<RoomInfo>();
        foreach (RoomInfo room in roomList)
            rooms.Add(room);
    }

    public override void OnConnectedToMaster()
    {
        msg.AppendMessage("Connected to master");
        msg.AppendMessage("Region: " + PhotonNetwork.CloudRegion);
        msg.AppendMessage("Game version: " + PhotonNetwork.GameVersion);
        if (PhotonNetwork.OfflineMode) return;
        if (!PhotonNetwork.JoinLobby())
            Debug.LogError(string.Format("OnConnectedToMaster, JoinLobby() returned false!"));
    }

    public override void OnJoinedLobby()
    {
        msg.AppendMessage("Joined lobby '" + PhotonNetwork.CurrentLobby.Name + "' of type '" + PhotonNetwork.CurrentLobby.Type + "'");
        mainMenu.ShowRoomButtons();
    }

    public override void OnJoinRandomFailed(short code, string message)
    {
        msg.AppendMessage("Failed to join random room, creating one...");
        RoomOptions options = new RoomOptions();
        options.IsOpen = true;
        options.IsVisible = true;
        options.PublishUserId = true;
        if (!PhotonNetwork.CreateRoom(null, options))
            Debug.LogError("Failed to create a room!");
    }

    public override void OnCreateRoomFailed(short code, string message)
    {
        Debug.LogError(string.Format("OnCreateRoomFailed. Code: {0}, message: '{1}'", code, message));
    }

    public override void OnCreatedRoom()
    {
        base.OnCreatedRoom();
        msg.AppendMessage("Created room");
    }

    public override void OnJoinedRoom()
    {
        msg.AppendMessage("Joined room " + PhotonNetwork.CurrentRoom.Name);
        InitArena();
        SpawnMyPlayer();
        if (UserDefinedConstants.spawnDummyPlayer)
            DummyPlayer = spawner.SpawnDummyPlayer();
    }

    public override void OnLeftRoom()
    {
        if (msg != null)
            msg.AppendMessage("Left room");
        base.OnLeftRoom();
        if (standbyCamera != null) // Player didn't quit, just left the room
            standbyCamera.SetActive(true);
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        if (msg != null)
            msg.AppendMessage("Disconnected");
        base.OnDisconnected(cause);
        DestroyArena();
        if (mainMenu != null && mainMenu.gameObject != null)
        {
            mainMenu.gameObject.SetActive(true);
            mainMenu.HideRoomButtons();
            mainMenu.ShowSingleMultiButtons();
        }
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
        if (arena != null && arena.gameObject != null)
            Destroy(arena.gameObject);
    }
}
