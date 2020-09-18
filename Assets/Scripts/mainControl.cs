using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using EDTracking;

public static class EditorGUITools
{

    private static readonly Texture2D backgroundTexture = Texture2D.whiteTexture;
    private static readonly GUIStyle textureStyle = new GUIStyle {normal = new GUIStyleState { background = backgroundTexture } };

    public static void DrawRect(Rect position, Color color, GUIContent content = null)
    {
        var backgroundColor = GUI.backgroundColor;
        GUI.backgroundColor = color;
        GUI.Box(position, content ?? GUIContent.none, textureStyle);
        GUI.backgroundColor = backgroundColor;
    }

    public static void LayoutBox(Color color, GUIContent content = null)
    {
        var backgroundColor = GUI.backgroundColor;
        GUI.backgroundColor = color;
        GUILayout.Box(content ?? GUIContent.none, textureStyle);
        GUI.backgroundColor = backgroundColor;
    }
}

public class mainControl : MonoBehaviour
{

	static public bool isHelpDisplayed = false;
	static public EDLocation targetLocation=null;
	static public EDLocation startLocation =null;
	static int backgroundPlaneIndex=1;

	public int kmPerWaypoint = 20;
	[ReadOnly] public static EDRoute route;

	[ReadOnly] static public double planetRadius=2161194.25;
	
    // Start is called before the first frame update
    void Start()
    {
		//EDLocation loc = new EDLocation (72.475555f,17.662354f,0.0f, 2320216.0f);
		targetLocation = new EDLocation (-48.947948, -144.128311, 0.0, 2161194.25); // Djambe ABC 1 - Crown Depot
		startLocation =  new EDLocation (-47.342247, -133.257202, 0.0, 2161194.25); // Djambe ABC 1 - Plexico Colony

		double distanceKm = 0.001 * EDLocation.DistanceBetween(targetLocation, startLocation);

		int howManyWaypoints = (int) (distanceKm / (int) kmPerWaypoint);


		List<EDWaypoint> listRouteWaypoints = new List<EDWaypoint>();
		//create waypoints - all located in target, with decreasing radii (we will track who first enters 250 km radius from target, 225 km, 200 km, ...)
		for (int i = 0; i <= howManyWaypoints; i++) {
			double distanceToTarget = ((howManyWaypoints-i)*kmPerWaypoint);//-1 ...we don't want to have first short wp & we want the last wp to be 0 km radius
			if (distanceToTarget < 0.1) {distanceToTarget = 0.1;} //last waypoint will be triggered when 100 m from target
			EDWaypoint wp = new EDWaypoint(targetLocation);
			wp.Radius = 1000.0*distanceToTarget;
//			MonoBehaviour.print("Waypoint no. "+i+" dist:"+distanceToTarget+" km"); 

			listRouteWaypoints.Add(wp);
		}

		
	//	EDRoute route = new EDRoute("race3");
		route = new EDRoute("race3", listRouteWaypoints);
		//MonoBehaviour.print("route:"+route.ToString() + " wp count:"+listRouteWaypoints.Count);
		MonoBehaviour.print("route:"+route.ToString() + " wp count:"+listRouteWaypoints.Count);

    }

	void OnGUI() {
		if (canvasHelp.activeSelf) {
			MonoBehaviour.print("HELP:"+mainControl.isHelpDisplayed);

			GUI.skin.font = (Font) Resources.Load("monoMMM_5");

			EditorGUITools.DrawRect(new Rect (20, 20, Screen.width -40, Screen.height -40), Color.grey);
			EditorGUITools.DrawRect(new Rect (22, 22, Screen.width -44, Screen.height -44), new Color(0.1f, 0.1f, 0.1f));

			GUI.Label(new Rect(30, 30, Screen.width-30, Screen.height-30), @"
HELP:

 program:
   F5 q ... quit, quit

 display:
   F1 ... show this help
   F2 ... cycle waypoint views (no waypoints, waypoints - absolute timing, waypoints - relative timing)
   F3 ... cycle underlying terrain texture (bug: when shown, some parts of trajectories fail to display)

 replay:
   + - ... speed up, slow down ('time Increment Global' variable in editor - trajectory object)
   space ... (un)freeze time

 camera control:
   W A S D ... forward, left, backward, right
   R F ... up, down
   Left Right ... yaw left/right (rotate camera around world y axis)
   Up Down ... pitch up/down (rotate camera around local x axis)

 input data:
   - Status.json (obtain using 'tail -F Status.json | tee trajectory00.json') or tracking.log from SRVTracker app
   - input data must be manually assigned in Unity to existing trajectory (duplicate existing with CTRL-D, drop 
     .json data file from Assets into 'json tracking log file' variable in editor)
   - TODO: load data & create trajectories automatically (reading all .json files from input directory)

 TODO: improve real-time mode (reading & displaying data on-fly from Status.json)
 TODO: creation of route (coords entered, get points from existing trajectory), navigation in real-time mode using this
 TODO: allow modification of waypoint distances ('i', 'o' keys)
 TODO: allow interaction with trajectory points (mouse click -> position of point in E:D world, no. of line in file)
 TODO: optimize - all trajectory points now iterated in every frame; some points can be deleted (interpolate instead)
");

			EditorGUITools.DrawRect(new Rect (370, Screen.height -34, Screen.width -240-190, 26), Color.grey);
			EditorGUITools.DrawRect(new Rect (372, Screen.height -32, Screen.width -244-190, 22), new Color(0.1f, 0.1f, 0.1f));
			GUI.Label(new Rect(384, Screen.height-54, Screen.width-30, Screen.height-20), @"
 EliteStatusVisualizer v1.0 (https://github.com/cratercamper/elitestatusvisualizer)
");

		}
	}

	static GameObject canvasHelp=null; 
	void showHelp(bool show=true) {
		if (canvasHelp == null) canvasHelp = GameObject.Find("Canvas - help");
		canvasHelp.SetActive(show);
	}

    // Update is called once per frame
    void Update()
    {
		if (Input.GetKey(KeyCode.F1)) {
			mainControl.isHelpDisplayed = true;
		} else {
			mainControl.isHelpDisplayed = false;
		}

		if (Input.GetKey(KeyCode.F3)) {
			GameObject backgrounds = GameObject.Find("Backgrounds");
			int index=1;
			foreach (Transform child in backgrounds.transform)
			{
				//backgroundPlaneIndex 0 means all background objects will be switched off
				if (index == mainControl.backgroundPlaneIndex) {
					child.gameObject.SetActive(true);
				} else {
					child.gameObject.SetActive(false);
				}

				index++;
			}

			mainControl.backgroundPlaneIndex = (mainControl.backgroundPlaneIndex+1) % index;
			MonoBehaviour.print("backgrounds count: "+(index-1)+" next:"+mainControl.backgroundPlaneIndex);
		}

		showHelp(mainControl.isHelpDisplayed);
		
    }


	static public EDLocation getTargetLocation() {
		return targetLocation;
	}
}
