using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
    using UnityEditor;
#endif
using UnityEngine.UI;


using EDTracking;



public class trajectory : MonoBehaviour
{

	//------------------------------------------------------------------------------------------------------------------------------------------------//
	//--- indicator class ----------------------------------------------------------------------------------------------------------------------------//
	//------------------------------------------------------------------------------------------------------------------------------------------------//
	public class indicator {
		//class with datapoints from trajectoryData - dataPoint struct 
		// - rendering info sugar
		// - helper functions called from trajectory class (advance in time, etc.)

		trajectoryData.dataPoint dp;

		static float timeEnd;
		static indicator indicatorNow;


		//rendering & parameters - line connecting 2 datapoints
		GameObject line;
		LineRenderer lr;
		Color lineStartColorOrig;
		Color lineEndColorOrig;
		float lineBrightness = 1.0f;

		//rendering & parameters - point showing every datapoint
		GameObject point;
		Vector3 pointScaleOrig;

		public indicator (
						trajectoryData.dataPoint dp, Vector3 pos, Vector3 previous,
						Color color, GameObject indicatorPointObject,
						GameObject pointContainer, GameObject lineContainer) {

			if (previous == new Vector3(0,0,0)) previous = pos;
			GameObject point = Instantiate(indicatorPointObject, pos, Quaternion.Euler(0, dp.Heading, 0));
			point.transform.SetParent(pointContainer.transform);

			this.dp = dp;
			this.point = point;
			this.line = makeLine(previous, pos, color, lineContainer);

            lr = line.GetComponent<LineRenderer>();
			lineStartColorOrig = lr.startColor;
			lineEndColorOrig   = lr.endColor;

			pointScaleOrig = point.transform.localScale;
			point.SetActive(false);

			if (timeEnd < dp.TimeSinceStart) {
				timeEnd = dp.TimeSinceStart;
			}

		}

		public void SetActive(bool val) {
			line.SetActive(val);
		}

		static public string spanToMinSec(System.TimeSpan time) {
			return time.ToString(@"hh\:mm\:ss");
		}

		public float getTimeRelativeMin(indicator fromIndicator=null) {
			float start = 0.0f;
			if (fromIndicator != null) {start = fromIndicator.dp.TimeSinceStart;}

			System.TimeSpan time = System.TimeSpan.FromSeconds(this.dp.TimeSinceStart - start);
			return (float) time.TotalMinutes;
		}

		public string getTimeRelativeMinSec(indicator fromIndicator=null) {
			float start = 0.0f;
			if (fromIndicator != null) {start = fromIndicator.dp.TimeSinceStart;}

			System.TimeSpan time = System.TimeSpan.FromSeconds(this.dp.TimeSinceStart - start);
			return indicator.spanToMinSec(time);
		}

		static public EDLocation locationFromIndicator(indicator ind, double planetRadius) { //TODO: make non-static
			return new EDLocation(ind.dp.Latitude, ind.dp.Longitude, ind.dp.Altitude, planetRadius);
		}

		static public Vector3 positionNow() {
			//FIXME: iterate instead remember & require bump (+ cache with set time)
			return new Vector3(indicatorNow.dp.Latitude, indicatorNow.dp.Altitude, indicatorNow.dp.Longitude);
		}

		static public int headingNow() {
			//FIXME: iterate instead remember & require bump (+ cache with set time)
			return indicatorNow.dp.Heading;
		}

		void setInPast() {
			lineBrightness = 1.0f;
			setLineBrightness();
			point.transform.localScale = pointScaleOrig;

			indicatorNow = this;
		}

		void setInFuture(bool bright=false) {
			if (bright) {
				lineBrightness = 1.0f;
			} else {
				lineBrightness = 0.4f;
			}
			setLineBrightness();
			point.transform.localScale = pointScaleOrig / 4f;
		}

		void setLineBrightness() {
			lr.startColor = lineStartColorOrig * lineBrightness;
			lr.endColor =   lineEndColorOrig * lineBrightness;
		}

		GameObject makeLine(Vector3 start, Vector3 end, Color color, GameObject lineContainer)
			{
				GameObject myLine = new GameObject();
				myLine.name = "Line";
				myLine.transform.SetParent(lineContainer.transform);
				myLine.transform.position = start;
				myLine.AddComponent<LineRenderer>();
				LineRenderer lr = myLine.GetComponent<LineRenderer>();
				lr.sortingOrder = 1;
				lr.material = new Material(Shader.Find("Sprites/Default"));
	//             lr.material = new Material(Shader.Find("Particles/Alpha Blended Premultiply"));
				lr.startColor=color;

				lr.endColor=color;

				lr.startWidth = lineWidth;
				lr.endWidth = lineWidth;

				lr.SetPosition(0, start);
				lr.SetPosition(1, end);
			return myLine;
		}


		public bool isInFuture(float timeNow) {
			//returns whether given point is in future;
			float tDp = dp.TimeSinceStart / timeEnd;

			if (Input.GetKeyDown(KeyCode.F8)) { MonoBehaviour.print("tNow: "+tDp+" timeSinceStart: " + dp.TimeSinceStart + " timeEnd: "+timeEnd);}

			if (tDp <= timeNow) {
				return false;
			} else {
				return true;
			}
		}

		public void bump(float timeNow, bool isRelativeWaypointMode=false) {
			//bump all lines & points to be correctly shown according to time & camera distance

			if (!isInFuture(timeNow)) {
				setInPast();
			} else {
				setInFuture(isRelativeWaypointMode); //relative mode sets 1.0 brightness also in future
			}

			if (isRelativeWaypointMode) {
				line.SetActive(false); //in case of relative timing, we switch lines active in LateUpdate() - only for trajectories that are winners between waypoints
			} else {
				line.SetActive(true); //otherwise (no wp mode, wp mode with absolute times) we want all lines active
			}

			//set width of line between points according to distance (we want to see line that is far away)
			float lineCamDist = (line.transform.position - Camera.main.transform.position).magnitude;
			lr.startWidth = 0.1f * trajectory.lineWidth * lineCamDist;
			lr.endWidth =  0.1f * trajectory.lineWidth * lineCamDist;
		}

	}



//====================================================================================================================================================//
//=== trajectory class ===============================================================================================================================//
//====================================================================================================================================================//

	public Text textPrefab;
	private Text textField;
	static private Text textFieldTime;

	//parsed from status.json (tracking.log)
	[ReadOnly] public string planet;
	double planetRadius;

	bool timeFlies = true;

	//Vector3 displayScale = new Vector3(100.0f, 0.01f, -50.0f);
//	Vector3 displayShift = new Vector3(-71.35045f, 0f, -17.93333f);
//Vector3 displayShift = new Vector3(47.341911f,0,133.281219f); //set from the first entry

	Vector3 displayScale = new Vector3(100.0f, 0.0017f, -50.0f); // latitude is 180 (-90 to 90), longitude is 360 (-180 to 0 to 180), middle coord is height of jumps
	static Vector3 displayShift = new Vector3(0,0,0); //automatically will be set from the first dataPoint of the first trajectory

	public trajectoryData td;
	[ReadOnly] public int dataPointsRead = 0;

	public GameObject indicatorPointObject;
	public GameObject srvObject;
	GameObject srv; //TODO: change to ship if attitude high enough

	//enable real-time (re)reading of asset file //TODO: when in real time mode, read directly Status.json file
	public bool isRealTime;
	float timeNextReread = 10f;
	float timeNextRereadPeriod = 5f;

	static public float lineWidth=0.05f;

//	public bool isLinesShowingDirection;

	//index of dataPoint (indicator) at timeNow for this trajectory
	[ReadOnly] public int indexNow;
	//global time -  shared among all trajectories, max value automatically set from the longest trajectory
	[Range(0.0f, 1.0f)]
	public float timeNowGlobal=0.0f;
	static public float timeNow=0.0f;

	//time in trajectory (automatically shared among all trajectories)
	static public float timeIncrement = 0.002f;
	public float timeIncrementGlobal = 0.002f;

	public string cmdr;
	public Color trajectoryColor = new Color(0.1f, 1.0f, 0.1f);
	//.json file in SRVTracker-tracking.log file format
	public TextAsset jsonTrackingLogFile;

	GameObject lineContainer;
	GameObject pointContainer;

	List<indicator>indicators;

	static int sernoMax = 0;
	int serno;
	int drawOrder=0;

	EDLocation locationNow;

	void prepareTextField() {
		GameObject canvas = GameObject.Find("Canvas");
		textField = Instantiate (textPrefab, new Vector3(0,-30-30*serno,0), Quaternion.identity) as Text;
		textField.transform.SetParent(canvas.transform, false);
	}

	void prepareTextFieldTime() {
		GameObject canvas = GameObject.Find("Canvas");
		textFieldTime = Instantiate (textPrefab, new Vector3(0, -Screen.height +30,0), Quaternion.identity) as Text;
		textFieldTime.transform.SetParent(canvas.transform, false);
		textFieldTime.color = new Color(0.8f, 0.8f, 0.8f);
	}

	void moveTextFieldToPlace() {

		GameObject canvas = GameObject.Find("Canvas");
		float minX = canvas.GetComponent<RectTransform>().position.x + canvas.GetComponent<RectTransform>().rect.xMin;
		float maxY = canvas.GetComponent<RectTransform>().position.y + canvas.GetComponent<RectTransform>().rect.yMax;
		float z = canvas.GetComponent<RectTransform>().position.z;
//		MonoBehaviour.print("maxY:"+maxY);

		// we need to shift (up) by maxY/2 - weird that Instantiate above doesn't need this
		textField.GetComponent<RectTransform>().localPosition = new Vector3(textField.transform.localPosition.x, maxY/2 -30 -30*drawOrder, 0);

		if (textFieldsWaypointTimes != null) {
			foreach(Text t in textFieldsWaypointTimes) {
//				t.transform.Translate(new Vector3(0, -30*drawOrder, 0));
				t.transform.localPosition = new Vector3(t.transform.localPosition.x, maxY/2 -30 -30*drawOrder, 0);
			}
		}
	}



	void OnValidate() {
		// change static trajectory variables for timeNow & timeIncrement (change in any of the trajectories in editor changes it for all)
		if (timeNow != timeNowGlobal) timeNow = timeNowGlobal;
		if (timeIncrement != timeIncrementGlobal) timeIncrement = timeIncrementGlobal;
	}

    // Start is called before the first frame update
    void Start()
    {
		srv = Instantiate(srvObject, new Vector3(0,0,0), Quaternion.Euler(0, 0, 0));
		(lineContainer = new GameObject()).name = "Lines";
		(pointContainer = new GameObject()).name = "Points";
		init();
		serno = trajectory.sernoMax;
		trajectory.sernoMax = trajectory.sernoMax+1;
		prepareTextField();
		if (textFieldTime == null) prepareTextFieldTime();
	}

	void init() {

#if UNITY_EDITOR
		if (isRealTime) { AssetDatabase.Refresh(); } //TODO: base realtime reading not on assets
#endif
		td = new trajectoryData(jsonTrackingLogFile);


		if (cmdr.Equals("")) { // if not set in editor
			cmdr = td.cmdr.Substring(0,System.Math.Min(20, td.cmdr.Length));
		}
		for (int i=cmdr.Length; i< 20; i++) cmdr=cmdr+" ";
		planet = td.planet;
		planetRadius=td.planetRadius;

		setDisplayShift();
		indicators = new List<indicator>();
		makeIndicators();
		MonoBehaviour.print("init() of '" + this.name + "' done, points read: "+dataPointsRead);
	}

	void setDisplayShift(){
		if (trajectory.displayShift == new Vector3(0,0,0)) { //set from the first dataPoint of ther first trajectory
			displayShift = new Vector3(-td.data[0].Latitude, 0, -td.data[0].Longitude);
			MonoBehaviour.print("displayShift:"+displayShift);
		}
	}

	Vector3 applyDisplayShiftScale(Vector3 v) {
		return Vector3.Scale(displayScale,(displayShift + v));
	}

	void makeIndicators() {

		foreach (Transform child in lineContainer.transform) { GameObject.Destroy(child.gameObject); }
		foreach (Transform child in pointContainer.transform) { GameObject.Destroy(child.gameObject); }

		indicators = new List<indicator>();

		Vector3 prev = new Vector3(0,0,0);

		int i=0;
		foreach(trajectoryData.dataPoint dp in td.data) {
			i++;

			Vector3 pos = applyDisplayShiftScale(new Vector3(dp.Latitude, dp.Altitude, dp.Longitude));

			indicators.Add(new indicator(dp, pos, prev, trajectoryColor, indicatorPointObject, pointContainer, lineContainer));

			prev = pos;
		}

		dataPointsRead = i;
    }


	void resetTimeField(Text text) {
		text.text = "--:--:--";
		text.color = new Color(0.2f, 0.2f, 0.2f);
	}

	List<Text> prepareTextFieldsWaypointTimes(int no) {
		List<Text> ret = new List<Text>();

		for (int i=0; i<no; i++) {
			Text text= Instantiate (textPrefab, new Vector3(300+90*i,-30-30*serno,0), Quaternion.identity) as Text;
			GameObject canvas = GameObject.Find("Canvas");
			text.transform.SetParent(canvas.transform, false);
			resetTimeField(text);

			ret.Add(text);
		}


		if (trajectory.textFieldsWaypointTimesLegend == null) {
			trajectory.textFieldsWaypointTimesLegend = new List<Text>();
			// create legend (all waypoints have the same location (end of the race)
			// ...waypoint radiuses mean how much kilometers are remaining)
			for (int i=0; i<no; i++) {
				Text text= Instantiate (textPrefab, new Vector3(300+90*i,0,0), Quaternion.identity) as Text;
				GameObject canvas = GameObject.Find("Canvas");
				text.transform.SetParent(canvas.transform, false);
				text.text = " "+mainControl.route.Waypoints[i].Radius/1000.0+" km";
				text.color = new Color(0.8f, 0.8f, 0.8f);

				trajectory.textFieldsWaypointTimesLegend.Add(text);
			}
		}

		return ret;
	}

	bool textFieldsWaypointTimesEnabled=false;
	List<Text> textFieldsWaypointTimes=null;

	static List<Text> textFieldsWaypointTimesLegend=null;

	void eraseWaypointInfo() {
		if (textFieldsWaypointTimes != null) {
			foreach(Text t in textFieldsWaypointTimes) {
				resetTimeField(t);
			}
		}
	}

	void toggleWaypointInfo(bool show) {
		if (textFieldsWaypointTimes != null) {
			foreach(Text t in textFieldsWaypointTimes) {
				t.gameObject.SetActive(show);
			}
		}

		if (trajectory.textFieldsWaypointTimesLegend != null) {
			foreach(Text t in textFieldsWaypointTimesLegend) {
				t.gameObject.SetActive(show);
			}
		}
	}

	bool isWaypointInfoAbsoluteTiming = true;

	struct trajectoryWithTwoIndicesAndField {
		public trajectory t    {get; private set;}
		public int startIndex  {get; private set;}
		public int stopIndex   {get; private set;}
		public Text field      {get; private set;}

		public trajectoryWithTwoIndicesAndField(trajectory t, int startIndex, int stopIndex, Text field) {
			this.t = t;
			this.startIndex = startIndex;
			this.stopIndex = stopIndex;
			this.field = field;
		}

	}

	static List<SortedDictionary<float, trajectoryWithTwoIndicesAndField>> waypointRelativeTimes=null;

	void addToWaypointRelativeTimes(int wpIndex, float relativeTime, trajectory t, int startIndex, int stopIndex, Text field) {
		if (waypointRelativeTimes==null) waypointRelativeTimes = new List<SortedDictionary<float, trajectoryWithTwoIndicesAndField>>();

		trajectoryWithTwoIndicesAndField tWTI = new trajectoryWithTwoIndicesAndField(t, startIndex, stopIndex, field);

		SortedDictionary<float, trajectoryWithTwoIndicesAndField> dict;
		if (waypointRelativeTimes.Count-1 < wpIndex) {
			dict = new SortedDictionary<float, trajectoryWithTwoIndicesAndField>();
			dict[relativeTime]=tWTI;
			waypointRelativeTimes.Add(dict);
		} else {
			dict = waypointRelativeTimes[wpIndex];
			dict[relativeTime]=tWTI;
		}
	}


	void getWaypointInfo(bool isWaypointInfoAbsoluteTiming=true) {
		EDRoute r = mainControl.route;

		if (r.Waypoints.Count < 1) {
			MonoBehaviour.print("ERROR: no waypoints in mainControl.route!");
			return;
		}

		if (textFieldsWaypointTimes == null) {
			textFieldsWaypointTimes = prepareTextFieldsWaypointTimes(r.Waypoints.Count);
		}

		toggleWaypointInfo(true);

		int indexPoint=-1;
		int indexWaypointToCheck=0;
		bool done=false;

		indicator relativeFromIndicator=null;
		int relativeFromIndicatorIndex=0;

		while ((!done) && (indexPoint<indicators.Count-1) && (indexWaypointToCheck<r.Waypoints.Count)) { //TODO: test
			indexPoint++;

			EDLocation loc = indicator.locationFromIndicator(indicators[indexPoint], planetRadius);

			if (r.Waypoints[indexWaypointToCheck].LocationIsWithinWaypoint(loc)) {

				if (isWaypointInfoAbsoluteTiming) {
					textFieldsWaypointTimes[indexWaypointToCheck].text = indicators[indexPoint].getTimeRelativeMinSec();
				} else {
					textFieldsWaypointTimes[indexWaypointToCheck].text = "+" + indicators[indexPoint].getTimeRelativeMinSec(relativeFromIndicator).Substring(1);
					addToWaypointRelativeTimes(
						indexWaypointToCheck, 
						indicators[indexPoint].getTimeRelativeMin(relativeFromIndicator), 
						this, 
						relativeFromIndicatorIndex, 
						indexPoint,
						textFieldsWaypointTimes[indexWaypointToCheck]);

					relativeFromIndicator=indicators[indexPoint];
					relativeFromIndicatorIndex = indexPoint;
				}

				textFieldsWaypointTimes[indexWaypointToCheck].color = Color.grey;
				indexWaypointToCheck++;
			}

			if (indicators[indexPoint].isInFuture(timeNow)) { //check wp-s only up to timeNow (current time in the replay)
				done = true;
			}
		}

	}


    // Update is called once per frame
    void Update()
    {

		if (Input.GetKeyDown("space")) { timeFlies = !timeFlies; }
		if (Input.GetKeyDown("[+]")) { timeIncrement *= 1.2f; timeIncrementGlobal = timeIncrement;}
		if (Input.GetKeyDown("[-]")) { timeIncrement *= 1/1.2f; timeIncrementGlobal = timeIncrement;}


		if (timeFlies) {
			timeNow += timeIncrement * Time.deltaTime;
			if (timeNow > 1.0f) timeNow = 0f;
			timeNowGlobal = timeNow;

			finalRunActions();
		}

		//foreach (Transform child in lineContainer.transform) { GameObject.Destroy(child.gameObject); }

		
		float timeEnd = td.data[td.data.Count-1].TimeSinceStart;
		int i=0;
		foreach(indicator ind in indicators) {
			i++;

			ind.bump(timeNow, !isWaypointInfoAbsoluteTiming); //if not showing absolute timing, we want to set lines back to active
			if (!ind.isInFuture(timeNow)) indexNow = i;
		}

		srv.transform.position = applyDisplayShiftScale(indicator.positionNow());
		srv.transform.eulerAngles = new Vector3(0, -indicator.headingNow(), 0);

		float srvCamDist = (srv.transform.position - Camera.main.transform.position).magnitude;
		float srvScaleOrig = 0.003f;
		srv.transform.localScale = new Vector3(srvScaleOrig*srvCamDist, srvScaleOrig*srvCamDist, srvScaleOrig*srvCamDist);


		if (isRealTime) {
			if (timeNextReread < Time.realtimeSinceStartup) {
				timeNextReread = Time.realtimeSinceStartup + timeNextRereadPeriod;
//				MonoBehaviour.print("Time.realtimeSinceStartup:"+Time.realtimeSinceStartup+" realtimeSinceStartup:"+timeNextReread);
				init();
			}

		}

		EDLocation loc0 = new EDLocation(indicator.positionNow().x, indicator.positionNow().z, indicator.positionNow().y,  planetRadius);
		EDLocation loc1 = mainControl.getTargetLocation();

		if (textField != null) {
			textField.text = " █ "  + cmdr + "  " + (EDLocation.DistanceBetween(loc0, loc1)/1000.0f).ToString("0.00") + " km";
			textField.color = trajectoryColor;
		} else {
			MonoBehaviour.print("ERROR: text field not instantiated!");
		}

		if (Input.GetKeyDown("f2")) {
			// cycle: off -> absolute -> relative
			if (!textFieldsWaypointTimesEnabled) {
				textFieldsWaypointTimesEnabled = true;
				isWaypointInfoAbsoluteTiming = true;
			} else {
				if (isWaypointInfoAbsoluteTiming) {
					isWaypointInfoAbsoluteTiming = false;
				} else {
					textFieldsWaypointTimesEnabled = false;
					isWaypointInfoAbsoluteTiming = true;
				}
			}
		}

		if (textFieldsWaypointTimesEnabled) {
			getWaypointInfo(isWaypointInfoAbsoluteTiming);
		} else {
			eraseWaypointInfo();
			toggleWaypointInfo(false);
		}


		locationNow = new EDLocation(indicator.positionNow().x, indicator.positionNow().z, indicator.positionNow().y,  planetRadius);
		isLateUpdateDone=false;
    }

	static bool isLateUpdateDone = false;

	void LateUpdate() {
		if (isLateUpdateDone) return;
		isLateUpdateDone = true;
		// sort trajectories (in the printout on screen) by distance remaining 
		trajectory[] trajectories = (trajectory[]) GameObject.FindObjectsOfType (typeof(trajectory));
		var distancesTrajectories = new SortedDictionary<double, trajectory>();

		foreach(trajectory t in trajectories) {
			EDLocation loc0 = t.locationNow;
			EDLocation loc1 = mainControl.getTargetLocation();
			double dist = EDLocation.DistanceBetween(loc0, loc1)/1000.0;
			distancesTrajectories[dist] = t;
		}

		int drawOrder = 0;
		foreach (var item in distancesTrajectories) {
			trajectory t = item.Value;
			t.drawOrder = drawOrder;
			t.moveTextFieldToPlace();

			if (drawOrder == 0) {
//				MonoBehaviour.print("cnt:"+t.indicators.Count+" index:"+t.indexNow);
				if (t.indicators.Count > t.indexNow) {
					textFieldTime.text = "T+"+t.indicators[t.indexNow].getTimeRelativeMinSec(); // leader sets time indicator on screen
				}
			}

			drawOrder++;
		}


		// in waypoint-relative-time mode, find out who was fastest for every waypoint, set background of textField 
		// ...& display only that part of the trajectories where these were fastest in one of the waypoints

		if (!isWaypointInfoAbsoluteTiming) {

			int index=-1;
			if (waypointRelativeTimes!=null) {
	//			foreach(var dict in waypointRelativeTimes) {
	//				MonoBehaviour.print("dict:"+dict);


				foreach(SortedDictionary<float, trajectory.trajectoryWithTwoIndicesAndField> dict in waypointRelativeTimes) {
					index++;

					bool first=true;
					foreach(KeyValuePair<float, trajectory.trajectoryWithTwoIndicesAndField> kvp in dict) {
						trajectory.trajectoryWithTwoIndicesAndField tWTI = kvp.Value;
						trajectory t = tWTI.t;
						if (first) {
	//						MonoBehaviour.print("dict:"+" k:"+kvp.Key+" v:"+kvp.Value);
							float timeMin = kvp.Key;
							MonoBehaviour.print("index:"+index+" best min:"+timeMin+" traj:"+tWTI.t.name+" startI"+tWTI.startIndex+" stopI:"+tWTI.stopIndex);

							tWTI.field.color = new Color(255, 160, 0);
							first = false;

							for (int i = tWTI.startIndex; i<tWTI.stopIndex; i++) {
								t.indicators[i].SetActive(true);
							}
						} else {
							for (int i = tWTI.startIndex; i<tWTI.stopIndex; i++) {
								t.indicators[i].SetActive(false);
							}
						}

					}
	//				float timeMin = dict[dict.First()].Key;
	//				trajectoryWithTwoIndicesAndField tWTI = dict[dict.First()].Value;
	//
				}
			}
		}
	}


	void finalRunActions() {
		//TODO: when not isWaypointInfoAbsoluteTiming, remove parts of trajectories that didn't make it to their next wp
	}
}



