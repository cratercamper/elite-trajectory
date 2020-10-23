using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class trajectoryTerrainRaycast {
	static public double raycastToTerrain(GameObject o) {
		return raycastToTerrain(o.transform.position);
    }

	static public double raycastToTerrain(Vector3 pos) {
        // Bit shift the index of the layer (8) to get a bit mask for layer 8:plane
        int layerMask = 1 << 8;

		RaycastHit hit;
        // Does the ray intersect any objects excluding the player layer
        if (Physics.Raycast(pos, Vector3.down, out hit, Mathf.Infinity, layerMask)) {
            Debug.DrawRay(pos,   Vector3.down * hit.distance, Color.grey);
//            Debug.Log("Did Hit dist:" + hit.distance);
			return hit.distance;
        }
        else {
            Debug.DrawRay(pos, Vector3.down * 1000, Color.red);
//            Debug.Log("Did not Hit");
			return System.Double.MaxValue;
        }
    }
}

