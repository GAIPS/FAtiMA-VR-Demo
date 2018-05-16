using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SheetManager : MonoBehaviour {


    GameObject[] _sheets;
    int movedSheets = 0;
	// Use this for initialization
	void Start () {

        _sheets = GameObject.FindGameObjectsWithTag("DialogHistory");
	}
	
	// Update is called once per frame
	void Update () {
		
        foreach (var s in _sheets)
        {
            if (s.GetComponent<PaperScript>().full)
                Full(s);
        }
	}

    void Full(GameObject sheet)
    {
        Debug.Log("Full");
        movedSheets += 1;
        sheet.transform.parent.transform.parent.transform.Translate(0.0f, 0.0f, movedSheets);

        
    }
}
