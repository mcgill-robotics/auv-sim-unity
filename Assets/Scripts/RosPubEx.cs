using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Auv;

public class RosPublisherExample : MonoBehaviour
{
    ROSConnection ros;
    public string topicName = "thrust";

    // The game object 
    public GameObject auv;
    public GameObject t1;
    public GameObject t2;
    public GameObject t3;
    public GameObject t4;
    public GameObject t5;
    public GameObject t6;
    public GameObject t7;
    public GameObject t8;

    // Publish the cube's position and rotation every N seconds
    public float publishMessageFrequency = 0.5f;

    // Used to determine how much time has elapsed since the last message was published
    private float timeElapsed;

     
   
    void Start()
    {
        // start the ROS connection
        ros = ROSConnection.GetOrCreateInstance();
        ros.RegisterPublisher<ThrusterForcesMsg>(topicName);
    }

    private void Update()
    {
        timeElapsed += Time.deltaTime;

        if (timeElapsed > publishMessageFrequency)
        {
            auv.transform.rotation = Random.rotation;

            ThrusterForcesMsg pos = new ThrusterForcesMsg(
                auv.transform.position.x,
                auv.transform.position.y,
                auv.transform.position.z,
                auv.transform.rotation.x,
                auv.transform.rotation.y,
                auv.transform.rotation.z,
                auv.transform.rotation.w,
                auv.transform.rotation.x );
            

            // Finally send the message to server_endpoint.py running in ROS
            ros.Publish(topicName, pos);

            timeElapsed = 0;
        }
    }
    
    

    
     }
     
    
