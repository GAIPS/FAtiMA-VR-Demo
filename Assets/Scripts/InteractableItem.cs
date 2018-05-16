using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions.Comparers;

public class InteractableItem : MonoBehaviour
{

    public Rigidbody rigidbody;
    private bool currentlyInteracting;

    private OneController attachedHand;
    private MouseClickWorldSpace attachedMouse;

    private Vector3 posDelta;
    private float velocityFactor = 10000f;
    private Quaternion rotationDelta;
    private float rotationFactor = 600f;
    private float angle;
    private Vector3 axis;
    private Vector3 original_pos;
    private Quaternion original_rot;
    private bool flagRotate = false;

    private Transform interactionPoint;
	// Use this for initialization
	void Start ()
	{

	    rigidbody = GetComponent<Rigidbody>();
        interactionPoint = new GameObject().transform;
	    velocityFactor /= rigidbody.mass;
        rotationFactor /= rigidbody.mass;
       
        
   
       // Debug.Log(" orignal first " + original_pos);


    }
	
	// Update is called once per frame
	void Update () {

	    if (attachedHand && currentlyInteracting == true)
	    {
	        posDelta = attachedHand.transform.position - interactionPoint.position;
	        this.rigidbody.velocity = posDelta*velocityFactor*Time.deltaTime; // it might be wrong....use fixed update maybe

	        rotationDelta = attachedHand.transform.rotation*Quaternion.Inverse(interactionPoint.rotation);
            rotationDelta.ToAngleAxis(out angle, out axis);

	        if (angle > 180)
	        {
	            angle -= 360;
	        }

	        this.rigidbody.angularVelocity = (Time.fixedDeltaTime*angle*axis)*rotationFactor;

	    }

        if(attachedMouse && currentlyInteracting == true)
        {
            //    this.transform.SetParent(GameObject.FindGameObjectWithTag("Hold").transform, true);
          
            this.transform.position = GameObject.FindGameObjectWithTag("Hold").transform.position;
            if (!this.gameObject.name.Contains("notebook"))
                this.transform.rotation = GameObject.FindGameObjectWithTag("Hold").transform.rotation;
            else
            {
                this.transform.rotation = GameObject.FindGameObjectWithTag("Hold").transform.rotation;
                this.transform.Rotate(0, 90.0f, 0.0f);
            }
            /*  if (flagRotate == false)
              {
                  this.transform.Rotate(60.0f, 0.0f, 0.0f);
                  flagRotate = true;
              }
              */
        }
    }

    public void BeginInteraction(OneController hand)
    {
     //   Debug.Log("Interaction Begin");
        attachedHand = hand;
        interactionPoint.position = hand.transform.position;
        interactionPoint.rotation = hand.transform.rotation;
        interactionPoint.SetParent(transform, true);

        currentlyInteracting = true;
    }

    public void EndInteraction(OneController hand)
    {
    //    Debug.Log("Interaction End");
        if (attachedHand == hand)
        {
            attachedHand = null;
            currentlyInteracting = false;
        }

        flagRotate = false;
    }

    public bool isInteracting()
    {
        return currentlyInteracting;
        
    }
    public void EndMouseInteraction(MouseClickWorldSpace hand)
    {
        //    Debug.Log("Interaction End");
      //  this.transform.parent = null;

     
        this.transform.position = original_pos;
        this.transform.rotation = original_rot;
        attachedMouse = null;
        currentlyInteracting = false;
        
    }
    public void BeginMouseInteraction(MouseClickWorldSpace hand)
    {
        original_pos = this.transform.position;
        original_rot = this.transform.rotation;
        attachedMouse = hand;
      
       
        currentlyInteracting = true;
    }
}
