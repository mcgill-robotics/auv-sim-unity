using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Auv;
using RosMessageTypes;



public class RosSubEx : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
      //  ROSConnection.GetOrCreateInstance().Subscribe<Wrench>("effort", wrench_to_thrust());
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
