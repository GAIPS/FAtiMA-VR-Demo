using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MenuButtonScript : MonoBehaviour {


    private Color normalColor;
    private Color highlight;
    public bool coliding;
    public bool click;
	// Use this for initialization
	void Start () {

        normalColor = transform.parent.GetComponent<Button>().colors.normalColor;
        highlight = transform.parent.GetComponent<Button>().colors.highlightedColor;
        coliding = false;
        click = false;

    }
	
	// Update is called once per frame
	void Update () {

        if (coliding) { transform.parent.GetComponentInChildren<Text>().color = highlight;
           }
        else
            transform.parent.GetComponentInChildren<Text>().color = normalColor;



        coliding = false;


        if (click)
        {
            Debug.Log("invoke Menuu Button");
            transform.parent.GetComponentInChildren<Button>().onClick.Invoke();
            Destroy(this);
        }
    }
}
