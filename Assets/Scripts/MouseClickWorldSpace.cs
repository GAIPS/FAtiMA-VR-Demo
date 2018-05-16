using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MouseClickWorldSpace : MonoBehaviour
{

    HashSet<InteractableItem> objectsHoveringOver = new HashSet<InteractableItem>();
    private InteractableItem closestItem;
    private InteractableItem interactingItem;

    public float distance = 50f;
    //replace Update method in your class with this one
    void Update()
    {
        //if mouse button (left hand side) pressed instantiate a raycast
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        bool bHit = Physics.Raycast(ray, out hit);
        if (bHit)
        {
            if (hit.collider.gameObject.transform.parent)
                if (hit.collider.gameObject.transform.parent.name == "MenuButton(Clone)")
                {
                    hit.collider.gameObject.GetComponent<MenuButtonScript>().coliding = true;
                }
                else if (hit.collider.gameObject.transform.parent.name == "DialogButton(Clone)")
                {
                   
                  if(hit.collider.gameObject.GetComponent<DialogButtonScript>() != null)
                        hit.collider.gameObject.GetComponent<DialogButtonScript>().coliding = true;

                }
                else if (hit.collider.gameObject.transform.parent.name == "FinalScoreButton")
                {

                    hit.collider.gameObject.GetComponent<MenuButtonScript>().coliding = true;

                }
                }

        if (Input.GetMouseButtonUp(0) && interactingItem != null)
        {
            Debug.Log(" up and not interacting ");
            interactingItem.EndMouseInteraction(this);
            interactingItem = null;

        } else if (Input.GetMouseButtonDown(0))
        {   
            //create a ray cast and set it to the mouses cursor position in game
            ray = Camera.main.ScreenPointToRay(Input.mousePosition);


            bHit = Physics.Raycast(ray, out hit);
            if (bHit)
            {
                if (hit.collider.gameObject.transform.parent)
                {
                    if (hit.collider.gameObject.transform.parent.name == "MenuButton(Clone)")
                    {


                        hit.collider.gameObject.GetComponent<MenuButtonScript>().coliding = true;
                        hit.collider.gameObject.GetComponent<MenuButtonScript>().click = true;
                    }


                    else if (hit.collider.gameObject.transform.parent.name == "DialogButton(Clone)")
                    {
                        Debug.Log("dialog buttons");
                        hit.collider.gameObject.GetComponent<DialogButtonScript>().coliding = true;
                        hit.collider.gameObject.GetComponent<DialogButtonScript>().click = true;
                    }
                    else if (hit.collider.gameObject.transform.parent.name == "FinalScoreButton")
                    {

                        hit.collider.gameObject.GetComponent<MenuButtonScript>().coliding = true;
                        hit.collider.gameObject.GetComponent<MenuButtonScript>().click = true;

                    }
                }
                if (hit.collider.gameObject.GetComponent<InteractableItem>() != null)
                {
                    interactingItem = hit.collider.gameObject.GetComponent<InteractableItem>();

                    Debug.Log("interacting " + interactingItem.isInteracting());
                    if (!interactingItem.isInteracting()) // this shouldn't happen that often...
                    {

                        interactingItem.BeginMouseInteraction(this);
                    }

                }
                else if (hit.collider.gameObject != null)
                    Debug.Log("hit this object " + hit.collider.gameObject.name);
            }

           

        }
        if (Input.GetKeyDown("a"))
        {
            GameObject.FindGameObjectWithTag("MainCamera").GetComponent<Transform>().Translate(-0.1f, 0, 0);
        }
        else if (Input.GetKeyDown("d"))
        {
            GameObject.FindGameObjectWithTag("MainCamera").GetComponent<Transform>().Translate(0.1f, 0, 0);
        }



    }


}
