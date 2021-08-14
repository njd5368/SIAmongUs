using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Task : MonoBehaviour {
    [SerializeField] GameObject miniGame;
    GameObject hightlight;

    private void OnEnable(){
        hightlight = transform.GetChild(0).gameObject;
    }

    private void OnTriggerEnter(Collider other) {
        if(other.tag == "Player") {
            hightlight.SetActive(true);
        }
    }

    private void OnTriggerExit(Collider other) {
        if(other.tag == "Player") {
            hightlight.SetActive(false);
        }
    }

    public void PlayMiniGame() {
        miniGame.SetActive(true);
    }
}
