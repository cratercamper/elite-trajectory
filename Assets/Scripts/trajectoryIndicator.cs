using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using EDTracking;

//--- trajectoryIndicator class ----------------------------------------------------------------------------------------------------------------------------//
//------------------------------------------------------------------------------------------------------------------------------------------------//
//public class trajectoryIndicator : MonoBehaviour {
public class trajectoryIndicator {
	//class with datapoints from trajectoryData - dataPoint struct 
	// - rendering info sugar
	// - helper functions called from trajectory class (advance in time, etc.)

	TrajectoryData.dataPoint dp;

	static float timeEnd;

	//rendering & parameters - line connecting 2 datapoints
	GameObject line;
	LineRenderer lr;
	Color lineStartColorOrig;
	Color lineEndColorOrig;
	float lineBrightness = 1.0f;

	float brightnessDim = 0.2f;
	float brightnessBright = 1.0f;
	float dimFactor = 1.0f; //used to completely fade out the line (e.g. after death)

	public int nextWaypoint=-1;

	//rendering & parameters - point showing every datapoint
	GameObject point;
	Vector3 pointScaleOrig;

	public trajectoryIndicator (
					TrajectoryData.dataPoint dp, Vector3 pos, Vector3 previous,
					Color color, float lineWidth, GameObject indicatorPointObject,
					GameObject pointContainer, GameObject lineContainer) {

		if (previous == new Vector3(0,0,0)) previous = pos;
		GameObject point = Object.Instantiate(indicatorPointObject, pos, Quaternion.Euler(0, dp.Heading, 0));
		point.transform.SetParent(pointContainer.transform);

		this.dp = dp;
		this.point = point;
		this.line = makeLine(previous, pos, color, lineContainer, lineWidth);

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

	public bool isHigh() {
		if (dp.Altitude > 1000.0f) return true;
		return false;
	}

	static public string spanToMinSec(System.TimeSpan time) {
		return time.ToString(@"hh\:mm\:ss");
	}

	public float getTimeRelativeSec(Trajectory t, int index) {
		return getTimeRelativeSec(t.getIndicator(index));
	}

	public float getTimeRelativeSec(trajectoryIndicator fromIndicator=null) {
		float start = 0.0f;
		if (fromIndicator != null) {start = fromIndicator.dp.TimeSinceStart;}

		System.TimeSpan time = System.TimeSpan.FromSeconds(this.dp.TimeSinceStart - start);
		return (float) time.TotalSeconds;
	}

	public float getTimeRelativeMin(trajectoryIndicator fromIndicator=null) {
		float start = 0.0f;
		if (fromIndicator != null) {start = fromIndicator.dp.TimeSinceStart;}

		System.TimeSpan time = System.TimeSpan.FromSeconds(this.dp.TimeSinceStart - start);
		return (float) time.TotalMinutes;
	}

	public string getTimeRelativeMinSec(trajectoryIndicator fromIndicator=null) {
		float start = 0.0f;
		if (fromIndicator != null) {start = fromIndicator.dp.TimeSinceStart;}

		System.TimeSpan time = System.TimeSpan.FromSeconds(137 + this.dp.TimeSinceStart - start); //FIXME: HACK: 137 shouldn't be there
		return trajectoryIndicator.spanToMinSec(time);
	}

	static public EDLocation locationFromIndicator(trajectoryIndicator ind, double planetRadius) { //TODO: make non-static
		return new EDLocation(ind.dp.Latitude, ind.dp.Longitude, ind.dp.Altitude, planetRadius);
	}

	static public Vector3 positionNow(trajectoryIndicator indicator) {
		//FIXME: iterate instead remember & require bump (+ cache with set time)
		return new Vector3(indicator.dp.Latitude, indicator.dp.Altitude, indicator.dp.Longitude);
	}

	static public int headingNow(trajectoryIndicator indicator) {
		//FIXME: iterate instead remember & require bump (+ cache with set time)
		return indicator.dp.Heading;
	}

	void setInPast() {
		lineBrightness = brightnessBright;
		setLineBrightness();
		point.transform.localScale = pointScaleOrig;
	}

	void setInFuture(bool bright=false) {
		if (bright) {
			lineBrightness = brightnessBright;
		} else {
			lineBrightness = brightnessDim;
		}
		setLineBrightness();
		point.transform.localScale = pointScaleOrig / 4f;
	}

	void setLineBrightness() {
		lr.startColor = dimFactor * lineStartColorOrig * lineBrightness;
		lr.endColor =   dimFactor * lineEndColorOrig * lineBrightness;
	}

	GameObject makeLine(Vector3 start, Vector3 end, Color color, GameObject lineContainer, float lineWidth)
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

	public void dim(float dimFactorNew) {
		dimFactor = dimFactorNew;
	}

	public void bump(Trajectory t, float timeNow, float lineWidth, int lineTailLength, bool isTailSeconds, int myIndex, int indexNow, bool isRelativeWaypointMode=false) {
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

		if (lineCamDist > 500.0f) { //avoid lines that are too far to be too biglh
			lineCamDist = 500.0f;
		}

		lr.startWidth = 0.1f * lineWidth * lineCamDist;
		lr.endWidth =  0.1f *  lineWidth * lineCamDist;

		//draw only lines given by lineTailLength (seconds back or indices back)
		if ( (!isRelativeWaypointMode) && (indexNow>-1) ) {
			if (isTailSeconds) {

				float dT = - getTimeRelativeSec(t.getIndicator(indexNow));

				if (dT > lineTailLength/2) { //brightness not decreased on 1st half of the tail
					if (dT > lineTailLength) {
						lineBrightness=brightnessDim;
					} else {
						int lenPastHalf = (indexNow - myIndex) - lineTailLength/2;
						lineBrightness= brightnessBright - ((brightnessBright - brightnessDim) * ((float)lenPastHalf / ((float)lineTailLength/2.0f)));
					}
					setLineBrightness();
				}

			} else {
				if (indexNow - myIndex > lineTailLength/2) {
					if (indexNow - myIndex > lineTailLength) {
						lineBrightness=brightnessDim;
					} else {
						int lenPastHalf = (indexNow - myIndex) - lineTailLength/2;
						lineBrightness= brightnessBright - ((brightnessBright - brightnessDim) * ((float)lenPastHalf / ((float)lineTailLength/2.0f)));
		//				MonoBehaviour.print("BRIGHT: lph:"+lenPastHalf+" lb:"+lineBrightness+" ratio:"+((float)lenPastHalf / ((float)lineTailLength/2.0f)));
					}
					setLineBrightness();
				}
			}
		}
	}

}

