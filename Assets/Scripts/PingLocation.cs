using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PingLocation : MonoBehaviour {

    public string ipAddress;

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
        if (Input.GetKeyDown(KeyCode.A))
        {
            float wei = 237.25f; //Random.Range(-10f, 10f);
            float wei2 = 1027.59f; //Random.Range(-10f, 10f);
            StartCoroutine(setLocation(wei, wei2));
        }
    }

    IEnumerator setLocation(float x, float y)
    {
        WWWForm form = new WWWForm();
        form.AddField("x", x.ToString());
        form.AddField("y", y.ToString());

        string url = ipAddress + ":3000/set_holo_pos";

        // Post a request to an URL with our custom headers
        WWW www = new WWW(url, form);
        yield return www;

        Debug.Log(www.text);
    }
}
