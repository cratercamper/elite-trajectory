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
        public override string ToString()
        {
			return JsonUtility.ToJson(this); 
//            return JsonSerializer.Serialize(this);
        }
/*

        public static EDRoute FromString(string location)
        {
            return (EDRoute)JsonSerializer.Deserialize(location, typeof(EDRoute));
        }

        public static EDRoute LoadFromFile(string filename)
        {
            // Attempt to load the route from the file
            try
            {
                return FromString(File.ReadAllText(filename));
            }
            catch { }
            return null;
        }
*/

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