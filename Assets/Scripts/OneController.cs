using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Windows.Speech;

public class OneController : MonoBehaviour
{

    private Valve.VR.EVRButtonId gripButtonId = Valve.VR.EVRButtonId.k_EButton_Grip;
    private Valve.VR.EVRButtonId triggerButtonId = Valve.VR.EVRButtonId.k_EButton_SteamVR_Trigger;
    private Valve.VR.EVRButtonId trackpadButtonId = Valve.VR.EVRButtonId.k_EButton_SteamVR_Touchpad;
    private SteamVR_TrackedObject trackedObject;
    
    // If I have multiple objects near me I need to know which one is close to me
    HashSet<InteractableItem> objectsHoveringOver = new HashSet<InteractableItem>();

    private InteractableItem closestItem;
    private InteractableItem interactingItem;

    public bool gripButtonDown = false;
    public bool gripButtonUp = false;
    public bool gripButtonPressed = false;

    public bool trackpadButtonDown = false;
    public bool trackpadButtonUp = false;
    public bool trackpadButtonPressed = false;

    public bool triggerButtonDown = false;
    public bool triggerButtonUp = false;
    public bool triggerButtonPressed = false;

    private SteamVR_LaserPointer pointer;
    private GameObject pickup;

    private bool pressedRecord = false;

    private SteamVR_Controller.Device controller { get { return SteamVR_Controller.Input((int) trackedObject.index); } }


    
	// Use this for initialization
	void Start ()
	{
	    trackedObject = GetComponent<SteamVR_TrackedObject>();
        pointer = GetComponent<SteamVR_LaserPointer>();

       

    }


    // Update is called once per frame
    void Update () {
	    if (controller == null)
	    {
	        Debug.Log("Controller not initialized");
	        return;
	    }

	    gripButtonDown = controller.GetPressDown(gripButtonId);
	    gripButtonUp = controller.GetPressUp(gripButtonId);
        gripButtonPressed = controller.GetPress(gripButtonId);

        triggerButtonDown = controller.GetPressDown(triggerButtonId);
        triggerButtonUp = controller.GetPressUp(triggerButtonId);
        triggerButtonPressed = controller.GetPress(triggerButtonId);

        trackpadButtonDown = controller.GetPressDown(trackpadButtonId);
        trackpadButtonUp = controller.GetPressUp(trackpadButtonId); 
        trackpadButtonPressed = controller.GetPress(trackpadButtonId); 

        if (gripButtonDown)
	    {
           
        }
        if (trackpadButtonUp)
        {
                GameObject.Find("Button_StartRecord").GetComponent<Button>().onClick.Invoke();
                pressedRecord = true;
            
        }
	    if (triggerButtonDown)
	    {
           // Debug.Log("triggerbutton was just pressed");
          if(pointer)
              pointer.PulledTrigger();
        }

        if (triggerButtonUp)
        {
          //  Debug.Log("triggerbutton was just unpressed");
        }


	    if (triggerButtonDown)
	    {
	        float minDistance = float.MaxValue;

	        float distance;

	        foreach (InteractableItem item in objectsHoveringOver)
	        {
	            distance = (item.transform.position - transform.position).sqrMagnitude; // sqr because it could also be negative
	            if (distance < minDistance)
	            {
	                minDistance = distance;
	                closestItem = item;
	            }
	        }

	        interactingItem = closestItem;
	        closestItem = null;

	        if (interactingItem)
	        {
	            if (interactingItem.isInteracting()) // this shouldn't happen that often...
	            {
	                interactingItem.EndInteraction(this);
	            }

                interactingItem.BeginInteraction(this);
	        }
	    }


	    if (triggerButtonUp && interactingItem != null)
	    {
	        interactingItem.EndInteraction(this);
	    }

    }

    private void OnTriggerEnter(Collider collider)
    {
     //   pickup = collider.gameObject;
      //  Debug.Log("Trigger Entered");

        InteractableItem collidedItem = collider.GetComponent<InteractableItem>();
        if (collidedItem)
        {
            objectsHoveringOver.Add(collidedItem);



        }
    }

    private void OnTriggerExit(Collider collider)
    {
      //  pickup = null;
      //  Debug.Log("Trigger Exited");

        InteractableItem collidedItem = collider.GetComponent<InteractableItem>();
        if (collidedItem)
        {
            objectsHoveringOver.Remove(collidedItem);


        }
    }

    private void HandlePadClicked(object sender, ClickedEventArgs e)
    {
        
    }
}
