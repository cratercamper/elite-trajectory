using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
#if UNITY_EDITOR
    using UnityEditor;
#endif
using UnityEngine.UI;


using EDTracking;




public class Trajectory : MonoBehaviour {

	//------------------------------------------------------------------------------------------------------------------------------------------------//

//====================================================================================================================================================//
//=== trajectory class ===============================================================================================================================//
//====================================================================================================================================================//

	public bool isTrajectoryHacked=false;

	public Text textPrefab;
	private Text textField;
	static private Text textFieldTime;


	//parsed from status.json (tracking.log)
	[ReadOnly] public string planet;
	double planetRadius;

	public TrajectoryData td;
	[ReadOnly] public int dataPointsRead = 0;

	public GameObject indicatorPointObject;
	public GameObject srvPrefab;
	[ReadOnly]
	public GameObject srv; //TODO: change to ship if attitude high enough

	//enable real-time (re)reading of asset file //TODO: when in real time mode, read directly Status.json file
	public bool isRereadingFromDisk;
	float timeNextReread = 10f;
	float timeNextRereadPeriod = 5f;

	public float lineWidth=0.05f;
	//global time -  shared among all trajectories, max value automatically set from the longest trajectory

//	public bool isLinesShowingDirection;

	[ReadOnly] public float distanceTotalNow=float.MaxValue;
	[ReadOnly] float timeFinished = float.MaxValue;
	//index of dataPoint (trajectoryIndicator) at timeNow for this trajectory
	[ReadOnly] public int indexNow;
	[ReadOnly] public int waypointNow;
	//global time -  shared among all trajectories, max value automatically set from the longest trajectory
	[ReadOnly]
	public float timeNow;
//	static public float timeNow=0.0f;

	//time in trajectory (automatically shared among all trajectories)
//	static public float timeIncrement = 0.002f;
//	public float timeIncrementGlobal = 0.002f;

	public string cmdr;
	public Color trajectoryColor = new Color(0.1f, 1.0f, 0.1f);
	//.json file in SRVTracker-tracking.log file format
	public TextAsset jsonTrackingLogFile;

	GameObject lineContainer;
	GameObject pointContainer;

	List<trajectoryIndicator>indicators;

	static int sernoMax = 0;
	int serno;
	int drawOrder=0;

//	EDLocation locationNow;

	private Route myRoute;


	static public GameObject followLeaderObject;

	public trajectoryIndicator getIndicator(int index) {
		if (index < 0) return null;
		return indicators[index];
	}

	public int getIndicatorCount() {
		if (indicators!=null)
			return indicators.Count;
		return 0;
	}

	public float getTimeNowSec() {
		return indicators[indexNow].getTimeRelativeSec();
	}

	public float getTimeEndSec() {
		return indicators[indicators.Count-1].getTimeRelativeSec();
	}

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
//		MonoBehaviour.print("t:"+this+" drawOrder"+drawOrder+" maxY:"+maxY);

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
	}

    // Start is called before the first frame update
	bool wasStartCalledSoTheBugDidntManifestNow = false;
    void Start() {
		wasStartCalledSoTheBugDidntManifestNow=true;
		myRoute = (Route) transform.parent.gameObject.GetComponent("Route");

		srv = Instantiate(srvPrefab, new Vector3(0,0,0), Quaternion.Euler(0, 0, 0)); srv.transform.parent = this.transform;
		(lineContainer = new GameObject()).name = "Lines";                           lineContainer.transform.parent = this.transform;
		(pointContainer = new GameObject()).name = "Points";                         pointContainer.transform.parent = this.transform;

		Debug.Log("init() t:"+this);
		init();

		serno = Trajectory.sernoMax;
		Trajectory.sernoMax = Trajectory.sernoMax+1;
		prepareTextField();
		if (textFieldTime == null) prepareTextFieldTime();
	}

	void init() {

#if UNITY_EDITOR
		if (isRereadingFromDisk) { AssetDatabase.Refresh(); } //TODO: base realtime reading not on assets
#endif

		td = new TrajectoryData(jsonTrackingLogFile);


		if (cmdr.Equals("")) { // if not set in editor
			cmdr = td.cmdr.Substring(0,System.Math.Min(20, td.cmdr.Length));
		}
		for (int i=cmdr.Length; i< 20; i++) cmdr=cmdr+" ";
		planet = td.planet;
		planetRadius=td.planetRadius;

		indicators = new List<trajectoryIndicator>();
		makeIndicators();
//		MonoBehaviour.print("init() of '" + this.name + "' done, points read: "+dataPointsRead);
	}

	public void addData(string json) {
		td.parseFromJson(json);
		makeIndicators();
		MonoBehaviour.print("addData() of '" + this.name + "' done, points read: "+dataPointsRead);
	}


	Vector3 applyDisplayShiftScale(Route r, Vector3 v) {
		return Vector3.Scale(r.displayScale,(r.displayShift + v));
	}


	void makeIndicators() {
		foreach (Transform child in lineContainer.transform) { GameObject.Destroy(child.gameObject); }
		foreach (Transform child in pointContainer.transform) { GameObject.Destroy(child.gameObject); }

		Debug.Log("HEER");

		indicators = new List<trajectoryIndicator>();
		Vector3 prev = new Vector3(0,0,0);


		int i=0;
		foreach(TrajectoryData.dataPoint dp in td.data) {
			i++;

			if ( (dp.TerrainHeight > 1000000.0) || (dp.TerrainHeight < -1000000.0) ) {
				Debug.Log("ERROR: dp.TerrainHeight out of bounds! index:"+i+" dp.TerrainHeight "+dp.TerrainHeight);
				continue;
			}
			Vector3 pos = applyDisplayShiftScale(myRoute, new Vector3(dp.Latitude, (float) (dp.Altitude +(dp.TerrainHeight)), dp.Longitude));
			indicators.Add(new trajectoryIndicator(dp, pos, prev, trajectoryColor, lineWidth, indicatorPointObject, pointContainer, lineContainer));
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


		if (Trajectory.textFieldsWaypointTimesLegend == null) {
			Trajectory.textFieldsWaypointTimesLegend = new List<Text>();
			// create legend (all waypoints have the same location (end of the race)
			// ...waypoint radiuses mean how much kilometers are remaining)

			//FIXME: following calculation done for all waypoints concentric with same centre & diferent radii
			for (int i=0; i<no; i++) {
				Text text= Instantiate (textPrefab, new Vector3(300+90*i,0,0), Quaternion.identity) as Text;
				GameObject canvas = GameObject.Find("Canvas");
				text.transform.SetParent(canvas.transform, false);
				text.text = " "+myRoute.getRoute().Waypoints[i].Radius/1000.0+" km";
				text.color = new Color(0.8f, 0.8f, 0.8f);

				Trajectory.textFieldsWaypointTimesLegend.Add(text);
			}
		}


/*
		Trajectory.textFieldsWaypointTimesLegend[0].text = "Start";
		Trajectory.textFieldsWaypointTimesLegend[1].text = "70 km";
		Trajectory.textFieldsWaypointTimesLegend[2].text = "60 km";
		Trajectory.textFieldsWaypointTimesLegend[3].text = "50 km";
		Trajectory.textFieldsWaypointTimesLegend[4].text = "40 km";
		Trajectory.textFieldsWaypointTimesLegend[5].text = "30 km";
		Trajectory.textFieldsWaypointTimesLegend[6].text = "20 km";
		Trajectory.textFieldsWaypointTimesLegend[7].text = "10 km";
		Trajectory.textFieldsWaypointTimesLegend[8].text = "Finish";
*/
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

		if (Trajectory.textFieldsWaypointTimesLegend != null) {
			foreach(Text t in textFieldsWaypointTimesLegend) {
				t.gameObject.SetActive(show);
			}
		}
	}

	bool isWaypointInfoAbsoluteTiming = true;

	struct trajectoryWithTwoIndicesAndField {
		public Trajectory t    {get; private set;}
		public int startIndex  {get; private set;}
		public int stopIndex   {get; private set;}
		public Text field      {get; private set;}

		public trajectoryWithTwoIndicesAndField(Trajectory t, int startIndex, int stopIndex, Text field) {
			this.t = t;
			this.startIndex = startIndex;
			this.stopIndex = stopIndex;
			this.field = field;
		}

	}

	static List<SortedDictionary<float, trajectoryWithTwoIndicesAndField>> waypointRelativeTimes=null;

	void addToWaypointRelativeTimes(int wpIndex, float relativeTime, Trajectory t, int startIndex, int stopIndex, Text field) {
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
		EDRoute r = myRoute.getRoute();

		if (r.Waypoints.Count < 1) {
			MonoBehaviour.print("ERROR: no waypoints in route!");
			return;
		}

		if (textFieldsWaypointTimes == null) {
			textFieldsWaypointTimes = prepareTextFieldsWaypointTimes(r.Waypoints.Count);
		}

		toggleWaypointInfo(true);

		int indexPoint=-1;
		int indexWaypointToCheck=0;
		bool done=false;

		trajectoryIndicator relativeFromIndicator=null;
		int relativeFromIndicatorIndex=0;

		while ((!done) && (indexPoint<indicators.Count-1) && (indexWaypointToCheck<r.Waypoints.Count)) { //TODO: test
			indexPoint++; //iterate points of this trajectory

			EDLocation loc = trajectoryIndicator.locationFromIndicator(indicators[indexPoint], planetRadius);

			if (r.Waypoints[indexWaypointToCheck].LocationIsWithinWaypoint(loc)) {
				//trajectoryIndicator point location is within next (wanted) waypoint

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
//				Debug.Log("GWI finished:i"+indexPoint+" t:"+this);
			}


			if (isTrajectoryHacked) {
				for (int i=1; i<r.Waypoints.Count-1; i++)
					textFieldsWaypointTimes[i].text="?";
			}
		}
	}


	public void calculateNextWaypoint() {
		EDRoute r = myRoute.getRoute();

		if (r.Waypoints.Count < 1) {
			MonoBehaviour.print("ERROR: no waypoints in route!");
			return;
		}

		int indexPoint=-1;
		int indexWaypointToCheck=0;

		while (indexPoint<indicators.Count-1) { //TODO: test
			indexPoint++; //iterate all points of this trajectory

			EDLocation loc = trajectoryIndicator.locationFromIndicator(indicators[indexPoint], planetRadius);

			if (indexWaypointToCheck < r.Waypoints.Count) {
				if (r.Waypoints[indexWaypointToCheck].LocationIsWithinWaypoint(loc)) {
					//trajectoryIndicator point location is within next (wanted) waypoint
					indexWaypointToCheck++;
				}
			}

//			MonoBehaviour.print("WP t:"+this+ " Setting no.:"+indexPoint+" out of:"+indicators.Count);
			if (indexWaypointToCheck < r.Waypoints.Count) {
				indicators[indexPoint].nextWaypoint = indexWaypointToCheck;
			} else {
				indicators[indexPoint].nextWaypoint = r.Waypoints.Count-1;
				if (timeFinished == float.MaxValue) {
					timeFinished = indicators[indexPoint].getTimeRelativeSec();
//					Debug.Log("FIN-SET t:"+this+" T:"+timeFinished);
				}
			}
		}
	}

	string textFieldPrepare(Trajectory t, bool alive, bool finished, string s="") {
		string ret = " █ "; 
		if (!alive) ret = " ☠ ";
		if (finished) ret = " 🏁 ";
		ret += t.cmdr + "  " + s;
		return ret;
	}

	public void updateMeForTheSakeOfGlobalTime() {
		Update();
	}


	float calculateDimFactor() {
		float ret = 1.0f;

		//Debug.Log("indicators cnt:"+indicators.Count);
//		try {
		if (getTimeNowSec() < (- 5.0f + mainControl.instance.getTimeNowSec()) ) {
			ret = 10.0f / (- 5.0f +  mainControl.instance.getTimeNowSec() - getTimeNowSec() );
			if (ret>1.0f) ret = 1.0f;
			if (ret<0.0f) ret = 0.0f;
//			Debug.Log("ret:"+ret);

//			Debug.Log("T:"+getTimeNowSec()+" mainT:"+mainControl.instance.getTimeNowSec() + " t:"+this);
		}
//		} catch (Exception e) {
//			Debug.Log("ERROR: Exception! t:"+this+" indexNow:"+indexNow);
//		}
		return ret;
	}

/*-----------------------------------------------------------------------------------------------------------*/
/*-----------------------------------------------------------------------------------------------------------*/
/*-----------------------------------------------------------------------------------------------------------*/
/*-----------------------------------------------------------------------------------------------------------*/
/*-----------------------------------------------------------------------------------------------------------*/
/*-----------------------------------------------------------------------------------------------------------*/
    // Update is called once per frame
    void Update() {
		//handle bug
		if (!wasStartCalledSoTheBugDidntManifestNow) {
			MonoBehaviour.print("ERROR: Unity didn't call Start()! Calling now. You should restart Unity. Trajectory:"+this); // happened in Unity 2019.4.1f1
			Start();
		}


		if (indicators.Count == 0) {
			Debug.Log("WARN: Skipping update! No indicators in t:"+this);
			return;
		}

		float dimFactor=calculateDimFactor();


		timeNow = mainControl.instance.getTimeNow();

		//calculate next waypoint
		if (indicators != null) {if (indicators.Count > 0) { if (indicators[indicators.Count-1].nextWaypoint == -1) { calculateNextWaypoint();}}} //once run through all points & set nextWaypoint

//		Debug.Log("HHHH 1");
		//update indicators
		int i=0;
		indexNow = -1;
//		foreach(trajectoryIndicator ind in indicators) {
		if (indicators != null) {
			for (i=indicators.Count-1; i>-1; i--) {
	//		for (i=0; i < indicators.Count - 1; i++) {
	//			i++;
				trajectoryIndicator ind = indicators[i];
				ind.bump(this, timeNow, lineWidth, mainControl.instance.lineTailLength, mainControl.instance.isTailSeconds, i, indexNow, !isWaypointInfoAbsoluteTiming); //update indicators (bright/dim according to new position, ...)
				ind.dim(dimFactor);
				if (!ind.isInFuture(timeNow) && indexNow < 0) indexNow = i;
	//			Debug.Log("i now:"+indexNow);
			}

			waypointNow = indicators[indexNow].nextWaypoint;
		}

//		Debug.Log("HHHH 2");

		//move & scale SRV - TODO: check flying & change to ship
		if (indexNow > -1) {
			srv.transform.position = applyDisplayShiftScale(myRoute, trajectoryIndicator.positionNow(indicators[indexNow]));
			srv.transform.eulerAngles = new Vector3(0,  trajectoryIndicator.headingNow(indicators[indexNow]), 0);
		}
		float srvCamDist = (srv.transform.position - Camera.main.transform.position).magnitude;
		float srvScaleOrig = 0.004f;
		srv.transform.localScale = new Vector3(srvScaleOrig*srvCamDist, srvScaleOrig*srvCamDist, srvScaleOrig*srvCamDist);


		Renderer[] srvParts = srv.GetComponentsInChildren<Renderer>();
		foreach (var part in srvParts) {
			if (indicators[indexNow].isHigh()) {
				part.enabled = false;
			} else {
				part.enabled = true;
			}
		}

//		Debug.Log("HHHH 3 indexNow:"+indexNow + " indicators.Count:"+indicators.Count);
		//get new data if we are in real-time mode
		if (isRereadingFromDisk) {
			if (timeNextReread < Time.realtimeSinceStartup) {
				timeNextReread = Time.realtimeSinceStartup + timeNextRereadPeriod;
				init(); //reread whole file (which may contain new entries), recreate indicators (TODO: recreate to read only new entries, read file, not asset)
				Debug.Log("Reread finished for t:"+this);
			}
		}


		//calculate distance to next waypoint & to finish, write to player list
		if ((indexNow > -1) && (indicators[indexNow].nextWaypoint > 0)) {
			EDLocation loc0 = new EDLocation(
				trajectoryIndicator.positionNow(indicators[indexNow]).x, 
				trajectoryIndicator.positionNow(indicators[indexNow]).z, 
				trajectoryIndicator.positionNow(indicators[indexNow]).y,  planetRadius);

			int nextWaypoint = indicators[indexNow].nextWaypoint;
			EDRoute r = myRoute.getRoute();
//			Debug.Log("HHHH 3b nextWaypoint:"+nextWaypoint + " r.Waypoints.Count:"+r.Waypoints.Count);
			EDLocation loc1 = r.Waypoints[nextWaypoint].Location;

			double remainingWaypointDist =  r.TotalDistanceLeftAtWaypoint(nextWaypoint);
			double distToWaypoint = EDLocation.DistanceBetween(loc0, loc1);

			distanceTotalNow = ((float) remainingWaypointDist + (float) distToWaypoint)/1000.0f;

		}

//		Debug.Log("HHHH 4");

		if (textField != null) {
			string dist = (distanceTotalNow).ToString("0.00") + " km";
			if (distanceTotalNow == float.MaxValue ) {
				dist = "??.??";
			}

			textField.text = textFieldPrepare(this, true, false, ""+dist);
			textField.color = trajectoryColor;
		} else {
			MonoBehaviour.print("ERROR: text field not instantiated!");
		}

//		Debug.Log("HHHH 5");

		//waypoint toggle
		if (Input.GetKeyDown("f2")) { // cycle: off -> absolute -> relative
			if (!textFieldsWaypointTimesEnabled) { //off -> absolute
				textFieldsWaypointTimesEnabled = true;
				isWaypointInfoAbsoluteTiming = true;
			} else {
				if (isWaypointInfoAbsoluteTiming) { //absolute -> relative
					isWaypointInfoAbsoluteTiming = false;
				} else { //relative -> off
					textFieldsWaypointTimesEnabled = false;
					isWaypointInfoAbsoluteTiming = true;
				}
			}
		}

		if (textFieldsWaypointTimesEnabled) {
			getWaypointInfo(isWaypointInfoAbsoluteTiming);
			myRoute.toggleWaypoints(true);
		} else {
			eraseWaypointInfo();
			toggleWaypointInfo(false);
			myRoute.toggleWaypoints(false);
		}



//		Debug.Log("HHHH 6");
/*
		if (indexNow > -1) {
			locationNow = new EDLocation(
				trajectoryIndicator.positionNow(indicators[indexNow]).x,
				trajectoryIndicator.positionNow(indicators[indexNow]).z,
				trajectoryIndicator.positionNow(indicators[indexNow]).y,  planetRadius);
		}
*/
		isLateUpdateDone=false;
    }


	static bool isLateUpdateDone = false;

	void LateUpdate() {
		if (isLateUpdateDone) return; //do once for all trajectories
		isLateUpdateDone = true;
		// sort trajectories (in the printout on screen) by distance remaining

		Trajectory[] trajectories = (Trajectory[]) GameObject.FindObjectsOfType (typeof(Trajectory));
		Trajectory[] trajectoriesFin = (Trajectory[]) GameObject.FindObjectsOfType (typeof(Trajectory));


		var finishTimesTrajectories = new SortedDictionary<double, Trajectory>();
		foreach(Trajectory t in trajectoriesFin) {
//			Debug.Log("FIN t:"+t+" Tnow:"+mainControl.instance.getTimeNowSec()+" T:"+t.timeFinished);

			if (!(t.timeFinished < mainControl.instance.getTimeNowSec())) {
				continue;
			}


//			Debug.Log("FIN traj Length:"+trajectories.Length+" T:"+t.timeFinished);
			trajectories = trajectories.Where(val => val != t).ToArray();
//			Debug.Log("FIN traj Length:"+trajectories.Length+" T:"+t.timeFinished);

			finishTimesTrajectories[t.timeFinished] = t;
		}

		int drawOrder = 0;

		foreach (var item in finishTimesTrajectories) {
				Trajectory t = item.Value;
				t.drawOrder = drawOrder;
				t.moveTextFieldToPlace();

				t.textField.text = textFieldPrepare(t,true,true,trajectoryIndicator.spanToMinSec(System.TimeSpan.FromSeconds(t.timeFinished)));

				drawOrder++;
			}


//		MonoBehaviour.print("found t:"+trajectories.Length);
		var distancesTrajectories = new SortedDictionary<double, Trajectory>();

		//order list of players according to distance to finish
		foreach(Trajectory t in trajectories) { 
//			Debug.Log("trajectory::"+t+" dist:"+t.distanceTotalNow);
			distancesTrajectories[t.distanceTotalNow+t.serno/100000.0f] = t; 
		}


		foreach (var item in distancesTrajectories) {
			Trajectory t = item.Value;
			if (t.indicators == null) continue;
			if (t.indicators.Count == 0) continue;

			t.drawOrder = drawOrder;
			t.moveTextFieldToPlace();

			if (t.indicators[t.indexNow].isHigh()) { //FIXME: HACK to recognize death
				t.textField.text = textFieldPrepare(t,false,false,"DNF");
			}

			if (drawOrder == 0) {
				Trajectory.followLeaderObject = t.srv; //leader of remaining racers
			}

			drawOrder++;
		}

		textFieldTime.text = "T+"+mainControl.instance.getTimeHuman(mainControl.instance.getTimeNowSec()); // leader sets time trajectoryIndicator on screen


		// in waypoint-relative-time mode, find out who was fastest for every waypoint, set background of textField
		// ...& display only that part of the trajectories where these were fastest in one of the waypoints

		if (!isWaypointInfoAbsoluteTiming) {

			int index=-1;
			if (waypointRelativeTimes!=null) {

				foreach(SortedDictionary<float, Trajectory.trajectoryWithTwoIndicesAndField> dict in waypointRelativeTimes) {
					index++;

					bool first=true;
					foreach(KeyValuePair<float, Trajectory.trajectoryWithTwoIndicesAndField> kvp in dict) {
						Trajectory.trajectoryWithTwoIndicesAndField tWTI = kvp.Value;
						Trajectory t = tWTI.t;
						if (first) {
	//						MonoBehaviour.print("dict:"+" k:"+kvp.Key+" v:"+kvp.Value);
							float timeMin = kvp.Key;
//							MonoBehaviour.print("index:"+index+" best min:"+timeMin+" traj:"+tWTI.t.name+" startI"+tWTI.startIndex+" stopI:"+tWTI.stopIndex);

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
				}
			}
		}
	}


}



