﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Globalization;

namespace EDTracking
{

	[Serializable]
    public class EDWaypoint
    {
/*
        public EDLocation Location { get; set; } = null;
        public double Radius { get; set; } = 5000;
        public double Altitude { get; set; } = 0;
        public sbyte AltitudeTest { get; set; } = 0; // -1, must be below, +1 must be above, 0 not checked
        public int Direction { get; set; } = -1;
        public DateTime TimeTracked { get; internal set; }  // To store the time the location was recorded when route recording
        private static int _nextWaypointNumber = 1;
*/

        public EDLocation Location = null;
        public double Radius = 5000;
        public double Altitude = 0;
        public sbyte AltitudeTest = 0; // -1, must be below, +1 must be above, 0 not checked
        public int Direction = -1;
        public DateTime TimeTracked;  // To store the time the location was recorded when route recording
        private static int _nextWaypointNumber = 1;

        public EDWaypoint()
        { }

        public EDWaypoint(EDLocation location)
        {
            Location = location;
        }

        public EDWaypoint(EDLocation location, double hitRadius, int hitDirection): this(location)
        {
            Radius = hitRadius;
            Direction = hitDirection;
        }

        public EDWaypoint(EDLocation location, DateTime timeTracked, double radius): this(location)
        {
            TimeTracked = timeTracked;
            Radius = radius;
        }

        public string Name
        {
            get {
                if (!String.IsNullOrEmpty(Location.Name))
                    return Location.Name;
                Location.Name = $"Waypoint {_nextWaypointNumber:000}";
                _nextWaypointNumber++;
                return Location.Name;
            }
            set { Location.Name = value; }
        }

        public bool LocationIsWithinWaypoint(EDLocation location)
        {
            return (EDLocation.DistanceBetween(Location, location) < Radius);
        }
/*
        public override string ToString()
        {
            return JsonSerializer.Serialize(this);
        }

        public static EDWaypoint FromString(string location)
        {
            return (EDWaypoint)JsonSerializer.Deserialize(location, typeof(EDWaypoint));
        }
*/
    }
}
