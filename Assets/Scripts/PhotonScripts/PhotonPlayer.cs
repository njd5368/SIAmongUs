using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using System.IO;
using Photon.Realtime;

public class PhotonPlayer : MonoBehaviour {
    PhotonView myPV;
    GameObject myAvatar;

    Player[] allPlayers;
    int myNumberInRoom;

    // Start is called before the first frame update
    void Start() {
        myPV = GetComponent<PhotonView>();

        allPlayers = PhotonNetwork.PlayerList;
        foreach(Player p in allPlayers) {
            if(p == PhotonNetwork.LocalPlayer) {
                break;
            }
            myNumberInRoom++;
        }
        if(myPV.IsMine) {
            myAvatar = PhotonNetwork.Instantiate(Path.Combine("PhotonPrefabs", "AU Player"), AU_GameController.instance.spawnPoints[myNumberInRoom].position, Quaternion.identity);
        }
    }
}
