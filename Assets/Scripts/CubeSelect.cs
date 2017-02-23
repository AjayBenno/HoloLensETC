using UnityEngine;
using System.Collections;
using HoloToolkit.Unity;
using UnityEngine.VR.WSA.Input;

public class CubeSelect : MonoBehaviour
{
    private Color cubeColor;
    private Renderer rend;

    void Start()
    {
        rend = GetComponent<Renderer>();
        cubeColor = rend.material.color;
    }

    public void ResetColor()
    {
        rend.material.color = cubeColor;
    }

    public void Changecolor()
    {
        rend.material.color = Color.red;
    }

    public void Select()
    {
        var tts = GetComponent <TextToSpeechManager>();
        if(tts!=null)
        {
            tts.SpeakText(transform.name + " Selected");
        }

        if (GestureManager.Instance.FocusedObject != transform.gameObject)
        {
            Debug.LogFormat("Selected: {0}", transform.name);
            GestureManager.Instance.OverrideFocusedObject = transform.gameObject;            
        }
    }
}
