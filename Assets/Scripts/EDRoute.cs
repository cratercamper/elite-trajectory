﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

using UnityEngine;

namespace EDTracking
{
	[Serializable]
    public class EDRoute
    {
        public List<EDWaypoint> Waypoints;// { get; set; } = null;
        public string Name;// { get; set; } = null;
        private double _totalWaypointDistance = 0;
        private List<double> _waypointDistances = new List<double>();
        private List<double> _distanceLeftAtWaypoint = new List<double>();
        private int _lastWaypointCount = 0;
		private string _saveFilename = "";

        public EDRoute()
        {
            Waypoints = new List<EDWaypoint>();
            Name = "";
        }

        public EDRoute(string name)
        {
            Name = name;
            Waypoints = new List<EDWaypoint>();
        }

        public EDRoute(string name, List<EDWaypoint> waypoints)
        {
            Name = name;
            Waypoints = waypoints;
        }

        private void CalculateDistances()
        {
            if (Waypoints.Count == _lastWaypointCount)
                return;

            _distanceLeftAtWaypoint = new List<double>();
            _waypointDistances = new List<double>();
            _totalWaypointDistance = 0;
            if (Waypoints.Count < 2)
                return;

            for (int i=0; i<Waypoints.Count-1; i++)
            {
                _waypointDistances.Add(EDLocation.DistanceBetween(Waypoints[i].Location, Waypoints[i + 1].Location));
                _totalWaypointDistance += _waypointDistances[i];
            }

            _distanceLeftAtWaypoint.Add(_totalWaypointDistance);
            for (int i = 0; i < Waypoints.Count - 1; i++)
                _distanceLeftAtWaypoint.Add(_distanceLeftAtWaypoint[i] - _waypointDistances[i]);
        }

        public double TotalDistanceLeftAtWaypoint(int WaypointIndex)
        {
            CalculateDistances();
            if (WaypointIndex < _distanceLeftAtWaypoint.Count)
                return _distanceLeftAtWaypoint[WaypointIndex];
            return 0;
        }

        public override string ToString()
        {
			return JsonUtility.ToJson(this); 
//            return JsonSerializer.Serialize(this);
        }


        public static EDRoute FromString(string s)
        {
			return JsonUtility.FromJson<EDRoute>(s);
        }


        public static EDRoute LoadFromFile(string filename)
        {
            // Attempt to load the route from the file
//            try
 //           {
                return FromString(File.ReadAllText(filename));
  //          }
   //         catch { }
    //        return null;
        }

        public void SaveToFile(string filename)
        {
            try
            {
                File.WriteAllText(filename, this.ToString());
                _saveFilename = filename;
            }
            catch { }
        }


    }
}
