using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Buoyancy : MonoBehaviour
{
	public float underwaterDrag;
	public float airDrag;
	public float underwaterAngularDrag;
	public float airAngularDrag;
	Rigidbody auv;
	bool underwater;
	public float waterHeight;
	public float floatPower;	



    // Start is called before the first frame update
    void Start()
    {
        auv = GetComponent<Rigidbody>();
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        float level = transform.position.y - waterHeight;
        
        if (level < 0) {
auv.AddForceAtPosition(Vector3.up * floatPower* Mathf.Abs(level), transform.position, ForceMode.Force);
       	 if (!underwater) {
			underwater = true;
			SwitchState(true);
        	}
        	else if (underwater) {
        		underwater = false;
        		SwitchState(false);
        	}
        
    	}
    }
    
    void SwitchState(bool underwater) {
    	if (underwater) {
    		auv.drag = underwaterDrag;
    		auv.angularDrag = underwaterAngularDrag;
    	}
    	else {
		auv.drag = airDrag;
		auv.angularDrag = airAngularDrag;
	}
    
    
    }
    
}
