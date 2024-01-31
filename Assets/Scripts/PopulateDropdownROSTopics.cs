using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using TMPro;

public class PopulateDropdownROSTopics : MonoBehaviour
{
    ROSConnection ros;
    public TMP_Dropdown dropdown;

    // Start is called before the first frame update
    void Start()
    {
        ros = ROSConnection.GetOrCreateInstance();
        InvokeRepeating("UpdateDropdownOptions", 0f, 2.0f);
    }

    // Update is called once per frame
    void UpdateDropdownOptions()
    {
        List<TMPro.TMP_Dropdown.OptionData> dropDownOptions = new List<TMPro.TMP_Dropdown.OptionData>();
        dropDownOptions.Add(new TMP_Dropdown.OptionData(""));

        foreach (RosTopicState topicState in ros.AllTopics)
        {
            if (topicState.RosMessageName == "sensor_msgs/Image") {
                dropDownOptions.Add(new TMP_Dropdown.OptionData(topicState.Topic));
            }
        }

        dropdown.options = dropDownOptions;
    }
}
