using System.Collections;
using System.Collections.Generic;
using System.IO;

using System.Reflection;

using UnityEngine;
using System.Text.RegularExpressions;

using EDTracking; 


public class routeData
{
	public EDRoute readRoute(TextAsset asset) {
		MonoBehaviour.print("readRoute:"+asset);
		EDRoute r = EDRoute.FromString(asset.text);
		return r;
	}
}

