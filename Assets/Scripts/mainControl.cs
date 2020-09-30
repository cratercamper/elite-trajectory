using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using EDTracking;

// drawing rectangles
public static class EditorGUITools {

    private static readonly Texture2D backgroundTexture = Texture2D.whiteTexture;
    private static readonly GUIStyle textureStyle = new GUIStyle {normal = new GUIStyleState { background = backgroundTexture } };

    public static void DrawRect(Rect position, Color color, GUIContent content = null) {
        var backgroundColor = GUI.backgroundColor;
        GUI.backgroundColor = color;
        GUI.Box(position, content ?? GUIContent.none, textureStyle);
        GUI.backgroundColor = backgroundColor;
    }

    public static void LayoutBox(Color color, GUIContent content = null) {
        var backgroundColor = GUI.backgroundColor;
        GUI.backgroundColor = color;
        GUILayout.Box(content ?? GUIContent.none, textureStyle);
        GUI.backgroundColor = backgroundColor;
    }
}

public class mainControl : MonoBehaviour {

	Screenshot ss;
	bool timeFlies = true;

	public GameObject trajectoryPrefabObject;

	[Range(1, 10000)]
	public int lineTailLength=200;
	public bool isTailSeconds=true;

	//global time -  shared among all trajectories, max value automatically set from the longest trajectory
	float timeNow=0.0f;
	[Range(0.0f, 1.0f)] public float timeNowGlobal=0.0f;
	[ReadOnly] public string timeNowHuman="--:--:--";
	[ReadOnly] public float timeNowSeconds=0.0f;

	[ReadOnly] public float nextCapture=0.0f;
	public float nextCapturePeriodSimSeconds=10.0f;
	public bool isScreenshot = false;
	public bool isScreenshotNoGui = false;
	public string midFixOfScreenshotName = "";

	//time in trajectory (automatically shared among all trajectories)
	static public float timeIncrement = 0.002f;
	public float timeIncrementGlobal = 0.002f;
	public bool isPlayingRealTime = true;
	public bool isReversedTime = false;

	public static mainControl instance {get ; private set;} = null;

	static int backgroundPlaneIndex=1;

	EDRoute routeAuto;

	bool isHelpDisplayed = false;
	public bool isFollow = false;
	public bool isFollowWithInertia = false;
	public bool isFollowCurrent = false;
	GameObject followObject;
	Vector3 lastFollowPosition;
	Vector3 lastFollowVelocity;
	Vector3 lastFollowWantedPosition;
	Vector3 lastFollowCameraPosition;
//	float followVelocityChange = 0.1f;
	float lastDistanceToFollowWantedPosition = 1.0f;


	public bool isStatusFileEnabled = true;
//	public bool isStatusFileUseEvents = true;
	public string statusFile = "";
	public bool isSaveToFile = true;
	public string statusFileMyCopy="";

	StatusFileReader statusFileReader;
	private bool isStatusFileReadingRunning = false;

	[ReadOnly] public Route activeRoute;




	public mainControl() {
		if (instance != null) return;
		instance = this;
	}

	void Awake() {
		ss = (Screenshot) FindObjectOfType(typeof(Screenshot));
	}

	Trajectory statusTrajectory;
	void Start() {
		statusFileReader = new StatusFileReader();

		if (isStatusFileEnabled) {
			if (statusTrajectory == null) {
				GameObject o;
				o = Instantiate(trajectoryPrefabObject, new Vector3(0,0,0), Quaternion.Euler(0, 0, 0)); 
				statusTrajectory = (Trajectory) o.GetComponent<Trajectory>();
				statusTrajectory.transform.parent = activeRoute.transform;

				Debug.Log("created trajectory:"+statusTrajectory);

			}
		}
	}


	public void addDataToStatusTrajectory(string json) {
		statusTrajectory.addData(json);
	}


	void OnGUI() {
		if (isScreenshot) {
			if (getTimeNowSec() > nextCapture) {
				nextCapture += nextCapturePeriodSimSeconds;
				if (nextCapture < getTimeNowSec()) nextCapture = getTimeNowSec();

				if (isScreenshotNoGui) {
					ss.captureScreenshot((""+System.Math.Round(getTimeNowSec(),0)+"_").PadLeft(6,'0')+ midFixOfScreenshotName);
				} else {
					ScreenCapture.CaptureScreenshot("screenshots/"+( (System.Math.Round(getTimeNowSec(),0)+"_").PadLeft(6,'0') )+ midFixOfScreenshotName +".png");
				}
//				Debug.Log("ss T:"+getTimeNowSec());
			}
		} else {
			nextCapture = 0.0f; // allow going back in time & capturing the same time again
		}


		if (isStatusFileEnabled) {
			if (!isStatusFileReadingRunning) {
//				startTracking();
				isStatusFileReadingRunning = true;
			}
		} else {
			if (isStatusFileReadingRunning) {
//				stopTracking();
				isStatusFileReadingRunning = false;
			}
		}

		if (canvasHelp.activeSelf) {
			//MonoBehaviour.print("HELP:"+mainControl.isHelpDisplayed);

			GUI.skin.font = (Font) Resources.Load("monoMMM_5");

			EditorGUITools.DrawRect(new Rect (20, 20, Screen.width -40, Screen.height -40), Color.grey);
			EditorGUITools.DrawRect(new Rect (22, 22, Screen.width -44, Screen.height -44), new Color(0.1f, 0.1f, 0.1f));

			GUI.Label(new Rect(30, 30, Screen.width-30, Screen.height-30), @"
HELP:

 program:
   F5 ... quit; also 'Q'

 display:
   F1 ... show this help
   F2 ... cycle waypoint views (no waypoints, waypoints - absolute timing, waypoints - relative timing)
   F3 ... cycle underlying terrain texture (bug: when shown, some parts of trajectories fail to display)

 replay:
   + - ... speed up, slow down ('time Increment Global' variable in editor - trajectory object)
   Space ... (un)freeze time
   B ... reverse time
   K ... screenshot 1920x1080; V (hold) ...multiple screenshots (same as 'K') until key release

 camera control:
   W A S D ... forward, left, backward, right
   R F ... up, down
   Left Right ... yaw left/right (rotate camera around world y axis)
   Up Down ... pitch up/down (rotate camera around local x axis)
   Shift ... faster camera movement
   G ... toggle follow leader

 input data:
   - Status.json (obtain using 'tail -F Status.json | tee -a trajectory00.json') or tracking.log from SRVTracker app
   - input data must be manually assigned in Unity to existing trajectory (duplicate existing with CTRL-D, drop 
     .json data file from Assets into 'json tracking log file' variable in editor)

 TODO: load data & create trajectories automatically (reading all .json files from input directory)
 TODO: improve real-time mode (reading & displaying data on-fly from Status.json)
 TODO: creation of route (coords entered, get points from existing trajectory), navigation in real-time mode using this
 TODO: allow modification of waypoint distances ('i', 'o' keys)
 TODO: allow interaction with trajectory points (mouse click -> position of point in E:D world, no. of line in file)
 TODO: optimize - all trajectory points now iterated in every frame; some points can be deleted (interpolate instead)
");

			EditorGUITools.DrawRect(new Rect (370, Screen.height -34, Screen.width -240-190, 26), Color.grey);
			EditorGUITools.DrawRect(new Rect (372, Screen.height -32, Screen.width -244-190, 22), new Color(0.1f, 0.1f, 0.1f));
			GUI.Label(new Rect(384, Screen.height-54, Screen.width-30, Screen.height-20), @"
 Elite-Trajectory      v1.0 (https://github.com/cratercamper/elite-trajectory)
");

		}
	}

	static GameObject canvasHelp=null; 
	void showHelp(bool show=true) {
		if (canvasHelp == null) canvasHelp = GameObject.Find("Canvas - help");
		canvasHelp.SetActive(show);
	}


	public float getTimeNow() {
		return timeNow;
	}

	public float getTimeNowSec() {
		return timeNowSeconds;
	}

	public string getTimeHuman(float timeNowSeconds) {
		System.TimeSpan time = System.TimeSpan.FromSeconds(timeNowSeconds);
		return time.ToString(@"hh\:mm\:ss");
	}


	void OnValidate() {
		// change static trajectory variables for timeNow & timeIncrement (change in any of the trajectories in editor changes it for all)
		if (timeNow != timeNowGlobal) {
			timeNow = timeNowGlobal;

			trajectoryLongest.updateMeForTheSakeOfGlobalTime();
			timeNowSeconds = trajectoryLongest.getTimeNowSec();
			timeNowHuman = getTimeHuman(timeNowSeconds);
		}

		if (timeIncrement != timeIncrementGlobal) {
			timeIncrement = timeIncrementGlobal;
			isPlayingRealTime = false;
		}

		if (isFollow && isFollowCurrent) isStatusFileEnabled = true;
	}

	Trajectory trajectoryLongest = null;
	float timeNextStatusFilePoll = 0.0f;
	float timeNextStatusFilePollDelta = 0.7f;

	void pollStatusFile() {
		if (isFollowCurrent) {
			if (Time.time > timeNextStatusFilePoll) {
				timeNextStatusFilePoll = Time.time + timeNextStatusFilePollDelta;

				string newData = statusFileReader.ProcessStatusFileUpdate(statusFile);
//				Debug.Log("newData:"+newData);
				if (!System.String.IsNullOrEmpty(newData)) {
					addDataToStatusTrajectory(newData);
				}
			}
		}
	}

    // Update is called once per frame
    void Update() {

		pollStatusFile();

		if (trajectoryLongest == null) {
			Trajectory[] tt = activeRoute.GetComponentsInChildren<Trajectory>();
			foreach (Trajectory t in tt) {
				if ( (trajectoryLongest == null) || (t.getIndicatorCount() > trajectoryLongest.getIndicatorCount()) ) {
					trajectoryLongest = t;
				}
			}
		}

		//check input
		if (Input.GetKeyDown("space")) { timeFlies = !timeFlies; }
		if (Input.GetKeyDown("[+]")) { timeIncrement *= 1.2f; timeIncrementGlobal = timeIncrement;    isPlayingRealTime = false;}
		if (Input.GetKeyDown("[-]")) { timeIncrement *= 1/1.2f; timeIncrementGlobal = timeIncrement;  isPlayingRealTime = false;}

		//update time
		if (timeFlies) {
			if (isPlayingRealTime) {
				int indexNow = trajectoryLongest.indexNow;
				if (indexNow > 0) {
					timeIncrement = (indexNow / trajectoryLongest.getTimeNowSec() ) / trajectoryLongest.getTimeEndSec();
					timeIncrementGlobal = timeIncrement;
					//Debug.Log("time relative s:"+ trajectoryLongest.getTimeNowSec() +" indexNow:"+indexNow +" timeIncrement"+timeIncrement + " Time.deltaTime"+Time.deltaTime);
				}
			}

			if (!isReversedTime) {
				timeNow += timeIncrement * Time.deltaTime;
			} else {
				timeNow -= timeIncrement * Time.deltaTime;
			}
			if (timeNow > 1.0f) timeNow = 0f;
			if (timeNow < 0.0f) timeNow = 0f;

			timeNowGlobal = timeNow;
			timeNowSeconds = trajectoryLongest.getTimeNowSec();
			timeNowHuman = getTimeHuman(timeNowSeconds);
		}


		if (Input.GetKey(KeyCode.F1)) {
			mainControl.instance.isHelpDisplayed = true;
		} else {
			mainControl.instance.isHelpDisplayed = false;
		}

		if (Input.GetKeyDown(KeyCode.F3)) {
			//cycle backgrounds - children of Backgrounds object - which is child of activeRoute
			Transform backgroundContainerT = activeRoute.transform.Find("Backgrounds");
			int index=1;
			if (backgroundContainerT != null) {
				GameObject backgroundContainer = backgroundContainerT.gameObject;
				foreach (Transform child in backgroundContainer.transform) {
					//backgroundPlaneIndex 0 means all background objects will be switched off
					if (index == mainControl.backgroundPlaneIndex) {
						child.gameObject.SetActive(true);
					} else {
						child.gameObject.SetActive(false);
					}
					index++;
				}
			}

			mainControl.backgroundPlaneIndex = (mainControl.backgroundPlaneIndex+1) % index;
			MonoBehaviour.print("backgrounds count: "+(index-1)+" next:"+mainControl.backgroundPlaneIndex); //TODO: status bar info
		}

		showHelp(mainControl.instance.isHelpDisplayed);
    }


//	static public EDLocation getTargetLocation() {
//		return instance.targetLocation;
//	}

//	public static EDRoute getRoute() {
//		return instance.route;
//	}


	void LateUpdate() {

		if (Input.GetKey("b")) {
			isReversedTime=true;
		} else { 
			isReversedTime=false;
		}

		if (Input.GetKeyDown("g")) {
			isFollow = !isFollow;
			if (followObject == null) {
				Debug.Log("WARN: followObject == null. Nothing to follow");
				isFollow = false;
			}

			// set initial position
			// TODO: set initial rotation to see 2nd contender? (or other key to switch views?)
			if (isFollow) {
				//Camera.main.transform.position =followObject.transform.localPosition+ followObject.transform.position - (Camera.main.transform.forward * 10.0f);
				Camera.main.transform.position = followObject.transform.position - (Camera.main.transform.forward * 5.0f);
				lastFollowWantedPosition = Camera.main.transform.position;
				lastFollowCameraPosition = Camera.main.transform.position;
				lastFollowPosition = followObject.transform.position;
			}
		}

		if (isFollow) {

			if (isFollowCurrent) {
				followObject = statusTrajectory.srv;
			} else {
				followObject = Trajectory.followLeaderObject;
			}

			if (lastFollowPosition == null) lastFollowPosition = followObject.transform.position;
			if (followObject != null) {

			//new wanted position of camera is last wanted position plus shift of the followed object
			Vector3 objectDeltaPosition = ( followObject.transform.position - lastFollowPosition );
			Vector3 cameraDeltaPosition = ( Camera.main.transform.position - lastFollowCameraPosition );
			Vector3 wantedPosition = lastFollowWantedPosition +  objectDeltaPosition + cameraDeltaPosition;
			lastFollowPosition = followObject.transform.position;

			float coef;

			if (10.0f *lastFollowVelocity.magnitude > 1.0f) {
				coef = Time.deltaTime * 3.0f * System.Math.Min(100.0f, System.Math.Max(0.0000001f, 100.0f * lastFollowVelocity.magnitude * lastFollowVelocity.magnitude) );
			} else {
				coef = Time.deltaTime * 3.0f * System.Math.Min(100.0f, System.Math.Max(0.0000001f, 10.0f * lastFollowVelocity.magnitude) );
			}


			float distanceToFollowWantedPosition = (lastFollowWantedPosition - Camera.main.transform.position).magnitude;
			if (distanceToFollowWantedPosition < 1.0f * lastDistanceToFollowWantedPosition) {
				coef = coef / 100.0f;
//				Debug.Log("SLOW  dist:"+distanceToFollowWantedPosition + " last dist:"+lastDistanceToFollowWantedPosition);
			}

			if (!isFollowWithInertia) {
				coef = 1.0f;
			}
			lastDistanceToFollowWantedPosition = distanceToFollowWantedPosition;

			coef = System.Math.Min(1.0f, coef);
//			if ((lastFollowVelocity.magnitude > 0.0001f) && (lastFollowVelocity.magnitude < 2.0f)) {
//				coef = coef * lastFollowVelocity.magnitude;
//			};
//			coef = coef * coef;
			lastFollowVelocity = (1.0f - coef) * lastFollowVelocity + coef * (lastFollowWantedPosition - Camera.main.transform.position);

//			Debug.Log("------------------------------------------------");
//			Debug.Log("coef:"+coef);
//			Debug.Log("lastFollowVelocity.magnitude:"+lastFollowVelocity.magnitude);
//			Debug.Log("lastFollowVelocity.magnitude ^2:"+lastFollowVelocity.magnitude*lastFollowVelocity.magnitude);
//			Debug.Log("objectDeltaPosition:"+objectDeltaPosition);
//			Debug.Log("cameraDeltaPosition:"+cameraDeltaPosition);
//			Debug.Log("wantedPosition:"+wantedPosition);
//			Debug.Log("lastFollowPosition:"+lastFollowPosition);
//			Debug.Log("lastFollowVelocity:"+lastFollowVelocity);
//			Debug.Log("cam pos 0:"+Camera.main.transform.position );
			Camera.main.transform.position = Camera.main.transform.position + 3.0f * Time.deltaTime * lastFollowVelocity;
//			Debug.Log("cam pos 1:"+Camera.main.transform.position );

			lastFollowWantedPosition = wantedPosition;
			lastFollowCameraPosition = Camera.main.transform.position;
			}
		}
	}

	void OnApplicationQuit() {
		timeIncrementGlobal = 0.002f; 
		timeIncrement = 0.002f; //avoids switching off 'isPlayingRealTime' bool on app quit (OnValidate() is executed when going back to editor)
	}
}
