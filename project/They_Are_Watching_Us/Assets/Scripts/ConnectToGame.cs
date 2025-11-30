using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

public class ConnectToGame : MonoBehaviourPunCallbacks
{
    [SerializeField]
    private int maxPlayers = 4;
    public string sceneToLoad = "Game";

    private void CreateRoom()
    {
        RoomOptions roomOptions = new RoomOptions();
        roomOptions.MaxPlayers = maxPlayers;
        PhotonNetwork.CreateRoom(null, roomOptions, null);
    }

    public void QuickMatch()
    {
        PhotonNetwork.JoinRandomRoom();
    }
    
    public override void OnJoinRandomFailed(short returnCode, string message)
    {
        CreateRoom();
    }

    public override void OnJoinedRoom()
    {
        PhotonNetwork.LoadLevel(sceneToLoad);
        Debug.Log("Connected to room");
    }
}
