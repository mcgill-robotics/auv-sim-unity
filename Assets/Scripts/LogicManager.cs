using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LogicManager1 : MonoBehaviour
{
    public GameObject downCam;
    public GameObject frontCam;
    public GameObject mainCam;



    // Start is called before the first frame update
    void Start()
    {
        activateMainCam();
    }

    public void activateDownCam() {
        downCam.SetActive(true);
        frontCam.SetActive(false);
        mainCam.SetActive(false);
    }

    public void activateFrontCam() {
        downCam.SetActive(false);
        frontCam.SetActive(true);
        mainCam.SetActive(false);
    }
    public void activateMainCam() {
        downCam.SetActive(false);
        frontCam.SetActive(false);
        mainCam.SetActive(true);
    }


}
