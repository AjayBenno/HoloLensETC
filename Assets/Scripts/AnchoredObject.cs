using UnityEngine;
using System.Collections;
using HoloToolkit.Unity;

public class AnchoredObject : MonoBehaviour
{
    public string SavedAnchorFriendlyName;
    private WorldAnchorManager anchorManager;
    private SpatialMappingManager spatialMappingManager;
    private TextToSpeechManager ttsMgr;

    // Use this for initialization
    void Start()
    {
        ttsMgr = GetComponent<TextToSpeechManager>();
        if (ttsMgr == null)
        {
            Debug.LogError("TextToSpeechManager Required");
        }

        anchorManager = WorldAnchorManager.Instance;
        if (anchorManager == null)
        {
            Debug.LogError("This script expects that you have a WorldAnchorManager component in your scene.");
        }

        spatialMappingManager = SpatialMappingManager.Instance;
        if (spatialMappingManager == null)
        {
            Debug.LogError("This script expects that you have a SpatialMappingManager component in your scene.");
        }

        if (anchorManager != null && spatialMappingManager != null)
        {
            anchorManager.AttachAnchor(this.gameObject, SavedAnchorFriendlyName);
            ttsMgr.SpeakText("Anchor Locked");
        }
        else
        {
            ttsMgr.SpeakText("Cannot Lock Anchor");
        }
    }

    // Update is called once per frame
    void Update()
    {

    }
}
