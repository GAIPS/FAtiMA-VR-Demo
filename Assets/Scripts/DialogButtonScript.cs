using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DialogButtonScript : MonoBehaviour {

    private Color normalColor;
    private Color normalImageColor;
    public bool coliding;
    public bool click;
    // Use this for initialization
    void Start()
    {

        normalColor = transform.parent.GetComponentInChildren<Text>().color;


        normalImageColor = transform.GetComponentInChildren<Image>().color;
        coliding = false;
        click = false;

    }

    // Update is called once per frame
    void Update()
    {

        if (coliding)
        {
            transform.parent.GetComponentInChildren<Text>().color = new Color(0.0f, 1.0f, 0.0f);
            transform.GetComponentInChildren<Image>().color = new Color(0.0f, 1.0f, 0.0f);
        }
        else
        { transform.parent.GetComponentInChildren<Text>().color = normalColor;
            transform.GetComponentInChildren<Image>().color = normalImageColor;
        }


        coliding = false;


        if (click)
        {
           
            transform.parent.GetComponentInChildren<Button>().onClick.Invoke();
            Destroy(this);
        }
    }
}
