using UnityEngine;
using System.Collections;
using System.Text;
using HoloToolkit.Unity;
using UnityEngine.VR.WSA.Persistence;
using UnityEngine.SceneManagement;

public class AnchorControl : MonoBehaviour
{
    public GameObject PlacementObject;
    public string SavedAnchorFriendlyName;
    private WorldAnchorManager anchorManager;
    private WorldAnchorStore anchorStore;
    private SpatialMappingManager spatialMappingManager;
    private TextToSpeechManager ttsMgr;
    
    private enum ControlState
    {
        WaitingForAnchorStore,
        CheckAnchorStatus,
        Ready,
        PlaceAnchor,
        ShowCoordinates
    }

    private ControlState currentState;

    // Use this for initialization
    void Start()
    {
        currentState = ControlState.WaitingForAnchorStore;

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

        WorldAnchorStore.GetAsync(AnchorStoreReady);
    }

    void AnchorStoreReady(WorldAnchorStore store)
    {
        anchorStore = store;
        currentState = ControlState.CheckAnchorStatus;
        Debug.Log("Anchor Store Ready");        
    }
    
    void Update()
    {
        switch (currentState)
        {            
            case ControlState.CheckAnchorStatus: //Checking anchor status?
                var cnt = anchorStore.anchorCount;                
                if (cnt > 0)
                {
                    var sb = new StringBuilder("Found Anchor" + (cnt == 1 ? " " : "s "));
                    foreach (var ids in anchorStore.GetAllIds())
                    {
                        sb.Append(ids);
                    }
                    Debug.Log(sb.ToString());
                    ttsMgr.SpeakText(sb.ToString());
                    DisplayUI.Instance.AppendText(sb.ToString());
                }
                else
                {
                    ttsMgr.SpeakText("No Anchors Found, Creating Anchor");
                    Debug.Log("No Anchors Found, Creating Anchor");
                }                
                anchorManager.AttachAnchor(PlacementObject, SavedAnchorFriendlyName);
                currentState = ControlState.Ready;
                break;
            case ControlState.Ready:
                break;
            case ControlState.PlaceAnchor:
                // TODO: Use GazeManager + Cursor Tracking instead of another Raycast
                var headPosition = Camera.main.transform.position;
                var gazeDirection = Camera.main.transform.forward;
                RaycastHit hitInfo;
                if (Physics.Raycast(headPosition, gazeDirection, out hitInfo,
                    30.0f, spatialMappingManager.LayerMask))
                {                    
                    PlacementObject.transform.position = hitInfo.point;

                    // Rotate this object to face the user.
                    //Quaternion toQuat = Camera.main.transform.localRotation;
                    //toQuat.x = 0;
                    //toQuat.z = 0;
                    //this.transform.rotation = toQuat;
                }
                break;
            case ControlState.ShowCoordinates:
                //Display Coordinates:
                Vector3 anchorPos = PlacementObject.transform.position;
                Vector3 cameraPos = Camera.main.transform.position;
                Vector3 worldPos = cameraPos - anchorPos;

                string placeCoords = getCoords(worldPos);

                DisplayUI.Instance.SetText(placeCoords);
                break;
        }
    }

    public void PlaceAnchor()
    {
        //possibly refactor this if we have too many states?
        if (currentState != ControlState.Ready)
        {
            ttsMgr.SpeakText("AnchorStore Not Ready");
            return;
        }
        //else if (currentState == ControlState.ShowCoordinates)
        //{
        //   currentState = ControlState.Ready;
        //}
        
        anchorManager.RemoveAnchor(PlacementObject);
        currentState = ControlState.PlaceAnchor;
    }

    public void ClearText()
    {
        ttsMgr.SpeakText("Clearing Text");
        DisplayUI.Instance.ClearText();
    }

    public void Restart()
    {
        SceneManager.LoadScene("AnchorSharing");
    }

    public void LockAnchor()
    {
        if (currentState != ControlState.PlaceAnchor)
        {
            ttsMgr.SpeakText("Not in Anchor Placement State");
            return;
        }
        // Add world anchor when object placement is done.
        anchorManager.AttachAnchor(PlacementObject, SavedAnchorFriendlyName);
        currentState = ControlState.Ready;
        ttsMgr.SpeakText("Anchor Placed");
    }

    public void ShowCoordinates()
    {
        ttsMgr.SpeakText("Displaying Coordinates");
        currentState = ControlState.ShowCoordinates;
    }

    string getCoords(Vector3 position)
    {
        //position.x;
        string newString = "x: " + position.x.ToString() + " y: " + position.y.ToString() + " z: " + position.z.ToString();
        return newString;
    }
    
}
