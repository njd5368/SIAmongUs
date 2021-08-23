using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LobbyController : MonoBehaviourPunCallbacks {

    [SerializeField] private GameObject lobbyConnectButton;
    [SerializeField] private GameObject lobbyPanel;
    [SerializeField] private GameObject mainPanel;
    [SerializeField] private InputField playerNameInput; // was an input field so check when the code starts throwing errors XP

    private string roomName;
    private int roomSize;

    private List<RoomInfo> roomListings;
    [SerializeField] private Transform roomsContainer;
    [SerializeField] private GameObject roomListingsPrefab;

    public override void OnConnectedToMaster() {
        PhotonNetwork.AutomaticallySyncScene = true;
        lobbyConnectButton.SetActive(true);
        roomListings = new List<RoomInfo>();

        if(PlayerPrefs.HasKey("NickName")) {
            if(PlayerPrefs.GetString("NickName") == "") {
                PhotonNetwork.NickName = "Player " + Random.Range(0, 1000);
            }
            else {
                PhotonNetwork.NickName = PlayerPrefs.GetString("NickName");
            }
        }
        else {
            PhotonNetwork.NickName = "Player " + Random.Range(0, 1000);
        }
        playerNameInput.text = PhotonNetwork.NickName;
    }

    public void PlayerNameUpdate(string nameInput) {
        PhotonNetwork.NickName = nameInput;
        PlayerPrefs.SetString("NickName", nameInput);
    }

    public void JoinLobbyOnClick() {
        mainPanel.SetActive(false);
        lobbyPanel.SetActive(true);
        PhotonNetwork.JoinLobby();
    }

    public override void OnRoomListUpdate(List<RoomInfo> roomList) {
        int tmpIndex;
        foreach(RoomInfo room in roomList) {
            if(roomListings != null) {
                tmpIndex = roomListings.FindIndex(ByName(room.Name));
            }
            else {
                tmpIndex = -1;
            }
            if(tmpIndex != -1) {
                roomListings.RemoveAt(tmpIndex);
                Destroy(roomsContainer.GetChild(tmpIndex).gameObject);
            }
            if(room.PlayerCount > 0) {
                roomListings.Add(room);
                ListRoom(room);
            }
        }
    }

    static System.Predicate<RoomInfo> ByName(string name) {
        return delegate (RoomInfo room) {
            return room.Name == name;
        };
    }

    void ListRoom(RoomInfo room) {
        if(room.IsOpen && room.IsVisible) {
            GameObject tmpListing = Instantiate(roomListingsPrefab, roomsContainer);
            RoomButton tmpButton = tmpListing.GetComponent<RoomButton>();
            tmpButton.SetRoom(room.Name, room.MaxPlayers, room.PlayerCount);
        }
    }

    public void OnRoomNameChanged(string nameIn) {
        roomName = nameIn;
    }

    public void OnRoomSizeChanged(string sizeIn) {
        roomSize = int.Parse(sizeIn);
    }

    public void CreateRoom() {
        Debug.Log("Creating new room...");
        RoomOptions roomOptions = new RoomOptions()  {IsVisible = true, IsOpen = true, MaxPlayers = (byte)roomSize};
        PhotonNetwork.CreateRoom(roomName, roomOptions);
    }

    public override void OnCreateRoomFailed(short returnCode, string message) {
        Debug.Log("Room creation failed. Perhaps there is already a room with this name?");
    }

    public void MatchmakingCancel() {
        mainPanel.SetActive(true);
        lobbyPanel.SetActive(false);
        PhotonNetwork.LeaveLobby();
    }
}
