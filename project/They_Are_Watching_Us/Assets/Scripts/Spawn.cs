using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class Spawn : MonoBehaviour
{
    [SerializeField] private GameObject _player;
    [SerializeField] private GameObject ObjectCam;
    [SerializeField] private Transform _spawn;
    public static Camera mainCamera;

    void Start()
    {
        PhotonNetwork.Instantiate(_player.name, _spawn.position, Quaternion.identity);
        // Instantiate(ObjectCam, new Vector3(0, 0, 0), Quaternion.identity);
    }
    void Update()
    {
        // _player.GetComponent<PlayerMoves>().enabled = true;
    }
}