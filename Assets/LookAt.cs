using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LookAt : MonoBehaviour {


    public Transform target;

	// Use this for initialization
	void Start () {


        target = GameObject.FindGameObjectWithTag("MainCamera").transform;
       // target.transform.Rotate(45, 0, 0);
	}
	
	// Update is called once per frame
	void Update () {

        if (target != null) {

            Vector3 delta = new Vector3(target.transform.position.x - transform.position.x, 0.0f, target.transform.position.z - transform.position.z);
            transform.rotation = Quaternion.LookRotation(delta);

        }


      //      transform.LookAt(target);
		
	}
}
