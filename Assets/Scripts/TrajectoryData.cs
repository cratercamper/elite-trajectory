using System.Collections;
using System.Collections.Generic;
using System.IO;

using System.Reflection;

using UnityEngine;
using System.Text.RegularExpressions;


public class TrajectoryData {
	const float DEFAULT_PLANET_RADIUS=2161194.25f; //DJAMBE

	TextAsset json;

	public string cmdr="default cmdr name";
	public string planet;
	public float planetRadius=0;
	System.DateTime timeItStarted=System.DateTime.MaxValue;
	
/* from this we parse dataPoint (structure was weird to deserialize it simply):

{"Latitude":72.475303999999994,
"Longitude":17.665012000000001,
"Heading":108,
"Flags":18939918,
"TimeStamp":"2020-08-29T21:53:05Z",
"BodyName":"Brokpoi 1 a",
"PlanetRadius":2320216,
"Altitude":0,
"Commandedr":"1ba3db1d-265b-4097-acff-8edcb3bba4fa"}
{ "timestamp":"2020-08-29T21:53:05Z",
 "event":"Status", "Flags":18939918, "Pips":[4,8,0], "FireGroup":1, "GuiFocus":0, "Fuel":{ "FuelMain":40.000000, "FuelReservoir":0.023712 },
 "Cargo":0.000000, "LegalState":"Clean",
 "Latitude":72.475304,
 "Longitude":17.665012,
 "Heading":108,
 "Altitude":0,
 "BodyName":"Brokpoi 1 a",
 "PlanetRadius":2320216.000000 }
*/

	public struct dataPoint {
/*
		static public void resetReader() {
			timeItStarted=System.DateTime.MaxValue;
			dataPoint.cmdr=null;
			cmdr=null;
			planet=null;
			planetRadius=-1;
		}
*/

		public dataPoint(System.DateTime timeItStarted, string[] vars) {
			if (timeItStarted == System.DateTime.MaxValue) {
					MonoBehaviour.print("ERROR: timeItStarted was not set in TrajectoryData parsing! Times will be wrong.");
			}

			Latitude = float.Parse(vars[0]);
			Longitude = float.Parse(vars[1]);
			Altitude = int.Parse(vars[2]);
			Heading = int.Parse(vars[3]);
			TimeStamp = System.DateTime.Parse(vars[4].Replace("\"", ""), null, System.Globalization.DateTimeStyles.RoundtripKind);
			// TimeSinceStart // - not parsed

			if (timeItStarted <= TimeStamp) {
			}
			else {
				if (timeItStarted != System.DateTime.MaxValue) {
					MonoBehaviour.print("ERROR: TimeStamp '"+TimeStamp.ToString("o")+"' lower than encountered before. dataPoint.TimeSinceStart will be incorrect for some entries!");
				}
				timeItStarted = TimeStamp;
			}

			TimeSinceStart = (float) (TimeStamp - timeItStarted).TotalSeconds; //time in seconds since start (start should be (is) synchronized for all trajectories) 


			
		}

		public float Latitude { get; }
		public float Longitude { get; }
		public int Altitude { get; }
		public int Heading { get; }
		public System.DateTime TimeStamp { get; }
		public float TimeSinceStart { get; }

		public override string ToString() {
			return "" + TimeSinceStart + " s:  " + " lat:" + Latitude + " lon:" + Longitude + "alt:" + Altitude + " head:" + Heading + "  (T iso: " + TimeStamp.ToString("o") + ")";
		}
	}


	public List<TrajectoryData.dataPoint> data;

	public TrajectoryData(TextAsset json) {
		this.json = json;
		this.data = new List<TrajectoryData.dataPoint>();
		parseFromJson(json.text);
	}

    public void parseFromJson(string s) {
//		Debug.Log("parsing... len:"+s.Length);

		StringReader reader = new StringReader(s);
		string line;
		do {
			line = reader.ReadLine();
			parseLine(line);
		}	while (line!=null);


//		int i=0;
		foreach (dataPoint dp in data) {
//			MonoBehaviour.print(""+(i++)+" | "+dp);
		}

//		Debug.Log("parsed (B):"+" data.Count:"+data.Count);
    }

	string parse(string propName, string line, bool noWarning=false) {
		string pattern;
		string found;

			pattern = propName;
			if (pattern.Equals("TimeStamp")) {
				pattern = "[tT]ime[sS]tamp";
			}
			pattern = pattern  + "\":[^,]*(,| *})";
//			MonoBehaviour.print("--search:"+pattern);
			Match m = Regex.Match(line, pattern, RegexOptions.None);
			if (m.Success) {
				found=m.Value;
				found=found.Remove(m.Value.Length - 1);         //remove trailing ','
				found=found.Substring(found.IndexOf(":")+1); //remove ':' & chars in front of it
			}
			else { 
				if (!noWarning) {MonoBehaviour.print("WARN: Failed to parse '"+propName+"' from: "+line);}
				return null;
			}

		return found.Replace("\"", "");
	}

	void parseLine(string line) {
		if (line == null) return;

		System.Reflection.PropertyInfo[] props = typeof(TrajectoryData.dataPoint).GetProperties(BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);

		//parse static varibles in pathData struct
		if (cmdr.Equals("default cmdr name")) { 
			string parsed = parse("Commandedr",line, true); 
			if (parsed != null) cmdr = parsed;
		}  //typo in tracking.log creation

		if (cmdr.Equals("default cmdr name")) { 
			string parsed = parse("Commander",line, true); 
			if (parsed != null) cmdr = parsed;
		}   //status.json doesn't contain it

		if (planet == null) { planet = parse("BodyName",line); }
		if (planetRadius == 0.0f) { 
			string parsed = parse("PlanetRadius",line);
			if (parsed != null)	{
				planetRadius = float.Parse(parsed, System.Globalization.CultureInfo.InvariantCulture);
			} else {
				MonoBehaviour.print("WARN: PlanetRadius cannot be parsed from first line, using default!");
				planetRadius = DEFAULT_PLANET_RADIUS;
			}
		}

		//parse timeItStarted from the first line (or one of the first lines), we expect timebased ascending order of entries
		if (timeItStarted == System.DateTime.MaxValue) {
			
			timeItStarted = System.DateTime.Parse((parse("TimeStamp", line)).Replace("\"", ""), null, System.Globalization.DateTimeStyles.RoundtripKind);
		}


		string[] found = new string[props.Length];

		//parse all non-static varibles in pathData struct using reflection
		for(int i = 0; i < props.Length; i++) {
			if (props[i].Name == "TimeSinceStart") continue; // FloatTime is calculated inside dataPoint struct

			string ret = parse(props[i].Name, line);
			if (ret == null) return; //failed to parse something, datapoint will not be created
			found[i]=ret;
		}

		dataPoint newdp = new dataPoint(timeItStarted, found);


		if (newdp.Latitude != 0.000f && newdp.Longitude != 0.000f && newdp.Heading != -1) { //TODO: change this to Flags != 0 (+ parsing flags)
			data.Add(newdp);
		} else {
			//line is Health entry instead of position entry
			//MonoBehaviour.print("WARN: skipping line with Latitude 0.0 & Longitude 0");
		}
	}


}

