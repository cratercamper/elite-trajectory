using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class cameraControl : MonoBehaviour
{
	Camera cam;

	public bool isAxesInputDisabled;
	public float speed = 7.0f;
	public float speedRot = 30f;

    // Start is called before the first frame update
    void Start()
    {
		cam = Camera.main;
//		MonoBehaviour.print("cam far clip: "+ cam.farClipPlane);
		cam.nearClipPlane=0.01f;
		cam.farClipPlane=10000;
    }

    // Update is called once per frame
    void Update() {
		if (Input.GetKeyDown(KeyCode.LeftShift)) { speed *= 5f; }
		if (Input.GetKeyUp(KeyCode.LeftShift)) { speed /= 5f; }



/* BUG: Input manager doesn't work properly - joystick (wheel) interferes even when the following code is disabled via variable
		...Unity just detects GetAxisRaw + Vector something & disabling via speed or via isAxesInputDisabled simply doesn't work!

		if (!isAxesInputDisabled) {
//DO NOT ENABLE - joystics/gamepads will interfere even with 'isAxesInputDisabled==true'!
			cam.transform.Translate (Vector3.right * Input.GetAxisRaw("Horizontal") * speed * Time.deltaTime);
			cam.transform.Translate (Vector3.left * Input.GetAxisRaw("Horizontal") * speed * Time.deltaTime);
			cam.transform.Translate (Vector3.up * Input.GetAxisRaw("ForwardBackward") * speed * Time.deltaTime);
			cam.transform.Translate (Vector3.forward * Input.GetAxisRaw("Vertical") * speed * Time.deltaTime);
			cam.transform.Rotate (Input.GetAxisRaw("Pitch") * speedRot * Time.deltaTime, 0.0f, 0.0f);
			cam.transform.Rotate (0.0f, Input.GetAxisRaw("Yaw") * speedRot * Time.deltaTime, 0.0f, Space.World);
		}
*/

		if (Input.GetKey(KeyCode.A)) cam.transform.Translate (Vector3.left * speed * Time.deltaTime);
		if (Input.GetKey(KeyCode.D)) cam.transform.Translate (Vector3.right * speed * Time.deltaTime);
		if (Input.GetKey(KeyCode.R)) cam.transform.Translate (Vector3.up * speed * Time.deltaTime);
		if (Input.GetKey(KeyCode.F)) cam.transform.Translate (Vector3.down * speed * Time.deltaTime);
		if (Input.GetKey(KeyCode.W)) cam.transform.Translate (Vector3.forward * speed * Time.deltaTime);
		if (Input.GetKey(KeyCode.S)) cam.transform.Translate (Vector3.back * speed * Time.deltaTime);
		if (Input.GetKey(KeyCode.X)) cam.transform.Translate (Vector3.back * speed * Time.deltaTime);

		if (Input.GetKey(KeyCode.UpArrow)) cam.transform.Rotate (-speedRot * Time.deltaTime, 0.0f, 0.0f);
		if (Input.GetKey(KeyCode.DownArrow)) cam.transform.Rotate (speedRot * Time.deltaTime, 0.0f, 0.0f);
		if (Input.GetKey(KeyCode.LeftArrow)) cam.transform.Rotate (0.0f, -speedRot * Time.deltaTime, 0.0f, Space.World);
		if (Input.GetKey(KeyCode.RightArrow)) cam.transform.Rotate (0.0f, speedRot * Time.deltaTime, 0.0f, Space.World);



		float terrainDist;
		terrainDist  = (float) - trajectoryTerrainRaycast.raycastToTerrain(new Vector3(cam.transform.position.x,cam.transform.position.y+100.0f,cam.transform.position.z));

		if (terrainDist > -101.0f) {
			Debug.Log("camshift: terrainDist:"+terrainDist);
			cam.transform.Translate (Vector3.up * (terrainDist +101.0f));
		}

//		if (cam.transform.position.y < -5.0f) cam.transform.position = new Vector3(cam.transform.position.x, -5.0f, cam.transform.position.z);



		if (Input.GetKeyDown(KeyCode.F5) || Input.GetKeyDown("q")) {
#if UNITY_EDITOR
			UnityEditor.EditorApplication.isPlaying = false;
#else
			Application.Quit();
#endif
		}

		
    }

}




/*

    Normal keys: “a”, “b”, “c” …
    Number keys: “1”, “2”, “3”, …
    Arrow keys: “up”, “down”, “left”, “right”
    Keypad keys: “[1]”, “[2]”, “[3]”, “[+]”, “[equals]”
    Modifier keys: “right shift”, “left shift”, “right ctrl”, “left ctrl”, “right alt”, “left alt”, “right cmd”, “left cmd”
    Mouse Buttons: “mouse 0”, “mouse 1”, “mouse 2”, …
    Joystick Buttons (from any joystick): “joystick button 0”, “joystick button 1”, “joystick button 2”, …
    Joystick Buttons (from a specific joystick): “joystick 1 button 0”, “joystick 1 button 1”, “joystick 2 button 0”, …
    Special keys: “backspace”, “tab”, “return”, “escape”, “space”, “delete”, “enter”, “insert”, “home”, “end”, “page up”, “page down”
    Function keys: “f1”, “f2”, “f3”, …

*/

