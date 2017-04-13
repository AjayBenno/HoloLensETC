using UnityEngine;
using System.Collections;
using System.Text;
using HoloToolkit.Unity;
using UnityEngine.VR.WSA.Persistence;
using UnityEngine.SceneManagement;
using UnityEngine.Networking;
using System.Collections.Generic;

public class AnchorControl : MonoBehaviour
{
    public string ipAddress;

    public float deltaAnchorPos;
    public float anchorRotationSpeed;

    private readonly float A = 7.40725f;
    private readonly float B = 13.333f;
    private readonly float C = 235.4985f;
    private readonly float D = 1025.535f;


    public GameObject PlacementObject;
    public GameObject AnchorAxis;
    public GameObject Console;

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
        ShowCoordinates,
        Nudge,
        Stop
    }
    private enum NudgeState
    {
       Forward,
       Up,
       Left,
       Right,
       Back,
       Down,
       RotateRight,
       RotateLeft
    }

    private ControlState currentState;
    private NudgeState nudgeState;
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
                    ttsMgr.SpeakText("Anchor Loaded, Connecting to Web Interface");
                    DisplayUI.Instance.AppendText(sb.ToString());

                    //TODO: ControlState.ConnectToWebInterface
                }
                else
                {
                    ttsMgr.SpeakText("No Anchors Found, Creating Anchor");
                    Debug.Log("No Anchors Found, Creating Anchor");
                }                
                anchorManager.AttachAnchor(AnchorAxis, SavedAnchorFriendlyName);
                currentState = ControlState.Ready;
                break;
            case ControlState.Ready:
                break;
            case ControlState.Stop:
                break;
            case ControlState.PlaceAnchor:
                // TODO: Use GazeManager + Cursor Tracking instead of another Raycast
                var headPosition = Camera.main.transform.position;
                var gazeDirection = Camera.main.transform.forward;
                RaycastHit hitInfo;
                if (Physics.Raycast(headPosition, gazeDirection, out hitInfo,
                    30.0f, spatialMappingManager.LayerMask))
                {                    
                    AnchorAxis.transform.position = hitInfo.point;

                    // Rotate this object to face the user.
                    Quaternion toQuat = Camera.main.transform.localRotation;
                    toQuat.x = 0;
                    toQuat.z = 0;
                    //changed to local rotation, see what this will do:
                    AnchorAxis.transform.localRotation = toQuat;
                }
                break;
            case ControlState.ShowCoordinates:
                //Display Coordinates:
                Vector3 anchorPos = AnchorAxis.transform.position;
                Vector3 cameraPos = Camera.main.transform.position;
                Vector3 worldPos = cameraPos - anchorPos;
                //BEGIN CONVERSION BETWEEN WORLD AND ROS
                float oldX = worldPos.x;
                float oldZ = worldPos.z;
                float newX = A * oldX - B * oldZ + C;
                float newZ = B * oldX + A * oldZ + D;

                StartCoroutine(setLocation(newX, newZ));

                //StartCoroutine(setLocation(237.25f, 1027.59f));

                string placeCoords = getCoords(worldPos);
                //string placeCoordsWithRos = placeCoords + "\n ROSx: " + newX.ToString() + " RosZ: " + newZ.ToString();
                string placeCoordsWithRos = placeCoords + "\n ROSx: " + newX.ToString() + " RosZ: " + newZ.ToString();
                //string vectorAngleInfo = getVectorAngle();
                //DisplayUI.Instance.SetText(placeCoordsWithRos);
                break;
            case ControlState.Nudge:  
                Vector3 newPosition = AnchorAxis.transform.position;
                switch (nudgeState)
                {
                    case NudgeState.Back:
                        newPosition.z -= deltaAnchorPos;
                        AnchorAxis.transform.position = newPosition;
                        break;
                    case NudgeState.Forward:
                        newPosition.z += deltaAnchorPos;
                        AnchorAxis.transform.position = newPosition;
                        break;
                    case NudgeState.Left:
                        newPosition.x -= deltaAnchorPos;
                        AnchorAxis.transform.position = newPosition;
                        break;
                    case NudgeState.Right:
                        newPosition.x += deltaAnchorPos;
                        AnchorAxis.transform.position = newPosition;
                        break;
                    case NudgeState.Down:
                        newPosition.y -= deltaAnchorPos;
                        AnchorAxis.transform.position = newPosition;
                        break;
                    case NudgeState.Up:
                        newPosition.y += deltaAnchorPos;
                        AnchorAxis.transform.position = newPosition;
                        break;
                    case NudgeState.RotateRight:
                        //AnchorAxis.transform.Rotate(anchorRotationSpeed * Vector3.up * Time.deltaTime, Space.Self);
                        break;
                    case NudgeState.RotateLeft:
                        //AnchorAxis.transform.Rotate(-1 * anchorRotationSpeed * Vector3.up * Time.deltaTime, Space.Self);
                        break;
                }
                break;
        }
    }

    public void PlaceAnchor()
    {
        //possibly refactor this if we have too many states?
        if (currentState != ControlState.Ready && currentState != ControlState.Stop
            && currentState != ControlState.ShowCoordinates)
        {
            ttsMgr.SpeakText("AnchorStore Not Ready");
            return;
        }
        //else if (currentState == ControlState.ShowCoordinates)
        //{
        //   currentState = ControlState.Ready;
        //}
        
        anchorManager.RemoveAnchor(AnchorAxis);
        currentState = ControlState.PlaceAnchor;
    }

    public void ClearText()
    {
        ttsMgr.SpeakText("Clearing Text");
        DisplayUI.Instance.ClearText();
    }

    public void Restart()
    {
        //ttsMgr.SpeakText("Restarting World");

        //while (ttsMgr.audioSource.isPlaying)
        //{
        //    Debug.Log("waiting for text to finish");
        //}

        SceneManager.LoadScene("AnchorSharing");
    }

    public void LockAnchor()
    {
        if (currentState != ControlState.PlaceAnchor && 
            currentState != ControlState.Stop &&
            currentState != ControlState.ShowCoordinates)
        {
            ttsMgr.SpeakText("Not in Anchor Placement State");
            return;
        }
        // Add world anchor when object placement is done.
        anchorManager.AttachAnchor(AnchorAxis, SavedAnchorFriendlyName);
        currentState = ControlState.Ready;

        //ttsMgr.SpeakText("Anchor Placed");
        SceneManager.LoadScene("AnchorSharing");
    }

    public void ShowCoordinates()
    {
        ttsMgr.SpeakText("haha relaaaax");
        currentState = ControlState.ShowCoordinates;
    }

    string getCoords(Vector3 position)
    {
        string newString = "x: " + position.x.ToString() + " y: " + position.y.ToString() + " z: " + position.z.ToString();
        return newString;
    }
    string getVectorAngle()
    {
       float d= getDistance(PlacementObject.transform.position, Camera.main.transform.position);
       float angle= getAngle(PlacementObject.transform.position, Camera.main.transform.position);

       return "Distance From Anchor " + d.ToString() + "Angle From anchor" + angle.ToString();

    }
    public void NudgeForward()
    {
        nudgeController("forward");
    }
    public void NudgeBack()
    {
        nudgeController("back");
    }
    public void NudgeLeft()
    {
        nudgeController("left");
    }
    public void NudgeRight()
    {
        nudgeController("right");
    }
    public void NudgeUp()
    {
        nudgeController("up");
    }
    public void NudgeDown()
    {
        nudgeController("down");
    }
    public void RotateRight()
    {
        nudgeController("rotate right");
    }
    public void RotateLeft()
    {
        nudgeController("rotate left");
    }
    public void Stop()
    {
        if(currentState != ControlState.PlaceAnchor && 
           currentState != ControlState.Nudge)
        {
            ttsMgr.SpeakText("Nothing to stop");
            return;
        }
        currentState = ControlState.Stop;
    }
    public void nudgeController(string command)
    {
        if (currentState != ControlState.Stop)
        {
            ttsMgr.SpeakText("Say Stop first");
            return;
        }
        currentState = ControlState.Nudge;
        switch (command)
        {
            case "left":
                nudgeState = NudgeState.Left;
                break;
            case "right":
                nudgeState = NudgeState.Right;
                break;
            case "up":
                nudgeState = NudgeState.Up;
                break;
            case "down":
                nudgeState = NudgeState.Down;
                break;
            case "forward":
                nudgeState = NudgeState.Forward;
                break;
            case "back":
                nudgeState = NudgeState.Back;
                break;
            case "rotate right":
                nudgeState = NudgeState.RotateRight;
                break;
            case "rotate left":
                nudgeState = NudgeState.RotateLeft;
                break;
        }
    }

    public void HideConsole()
    {
        Console.SetActive(false);
    }

    public void ShowConsole()
    {
        Console.SetActive(true);
    }

    private float getDistance(Vector3 anchorPosition,Vector3 currentPosition)
    {
        return Vector3.Distance(anchorPosition, currentPosition);
    }
    private float getAngle(Vector3 anchorPosition, Vector3 currentPosition)
    {
        return Vector3.Angle(anchorPosition, currentPosition);
    }

    IEnumerator setLocation(float x, float y)
    {
        WWWForm form = new WWWForm();
        form.AddField("x", x.ToString());
        form.AddField("y", y.ToString());

        List<IMultipartFormSection> formData = new List<IMultipartFormSection>();
        formData.Add(new MultipartFormDataSection("x=123&y=456"));
        //formData.Add(new MultipartFormFileSection("my file data", "myfile.txt"));


        string url = "http://" + ipAddress + ":3000/set_holo_pos";
          
        UnityWebRequest www = UnityWebRequest.Post(url, form);
        yield return www.Send();

        Debug.Log(url);

        DisplayUI.Instance.SetText(url);



        // Post a request to an URL with our custom headers
       // WWW www = new WWW(url, form);
        //yield return www;

        //Debug.Log(www.text);

        //DisplayUI.Instance.SetText(www.text);
    }
}
