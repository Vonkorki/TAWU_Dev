using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class IsMineCheck : MonoBehaviour
{
    [SerializeField] private PlayerMoves move;
    [SerializeField] private PhotonView _photonView;
    [SerializeField] private AudioListener audioListener;
    [SerializeField] private Camera _camera;

    void Start()
    {
        //Все атрибуты другого игрока
        if (!_photonView.IsMine)
        {
            _camera.enabled = false;
            move.enabled = false;
            audioListener.enabled = false;
        }
    }
}
