using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LogicManager1 : MonoBehaviour
{
    public GameObject downCam;
    public GameObject frontCam;
    public GameObject freeCam;
    public GameObject followCam;
    public Transform auv;
    public float distanceToAUVWhenSnapping;



    // Start is called before the first frame update
    void Start()
    {
        activateFollowCam();
        activateFreeCam();
    }

    public void activateDownCam() {
        downCam.SetActive(true);
        frontCam.SetActive(false);
        freeCam.SetActive(false);
        followCam.SetActive(false);
    }

    public void activateFrontCam() {
        downCam.SetActive(false);
        frontCam.SetActive(true);
        freeCam.SetActive(false);
        followCam.SetActive(false);
    }

    public void activateFreeCam() {
        downCam.SetActive(false);
        frontCam.SetActive(false);
        freeCam.SetActive(true);
        followCam.SetActive(false);
    }
    
    public void activateFollowCam() {
        downCam.SetActive(false);
        frontCam.SetActive(false);
        freeCam.SetActive(false);
        followCam.SetActive(true);
    }

    public void snapFreeCam() {
        if (freeCam.activeSelf) {
            freeCam.transform.LookAt(auv);
            Vector3 directionFromTarget = freeCam.transform.position - auv.position;
            freeCam.transform.position = auv.position + directionFromTarget.normalized * distanceToAUVWhenSnapping;
        }
    }
    
    public void reloadScene()
    {
        Scene currentScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(currentScene.name);
    }

}
