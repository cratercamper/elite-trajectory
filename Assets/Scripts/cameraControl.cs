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
		MonoBehaviour.print("cam far clip: "+ cam.farClipPlane);
		cam.farClipPlane=10000;
    }

    // Update is called once per frame
    void Update()
    {
		if (Input.GetKeyDown(KeyCode.LeftShift)) { speed *= 5f; }
		if (Input.GetKeyUp(KeyCode.LeftShift)) { speed /= 5f; }


		if (!isAxesInputDisabled) {
			cam.transform.Translate (Vector3.right * Input.GetAxisRaw("Horizontal") * speed * Time.deltaTime);
			cam.transform.Translate (Vector3.up * Input.GetAxisRaw("ForwardBackward") * speed * Time.deltaTime);
			cam.transform.Translate (Vector3.forward * Input.GetAxisRaw("Vertical") * speed * Time.deltaTime);
			cam.transform.Rotate (Input.GetAxisRaw("Pitch") * speedRot * Time.deltaTime, 0.0f, 0.0f);
			cam.transform.Rotate (0.0f, Input.GetAxisRaw("Yaw") * speedRot * Time.deltaTime, 0.0f, Space.World);

			if (cam.transform.position.y < 0.0f) cam.transform.position = new Vector3(cam.transform.position.x, 0.0f, cam.transform.position.z);
		}






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

