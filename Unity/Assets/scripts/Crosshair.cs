﻿using UnityEngine;
using System.Collections;

public class Crosshair : MonoBehaviour {
	public bool keyboardMode;
	public float arrowKeySensitivity;
	public Camera cam;
	public GameObject viewPortObject;
	
	public enum State { IDLE, LOCKING }
	public State currentState;
	public int maxLockCount;
	public float breakThreshold;

	// Force push controllers
	public float power;
	public float radius;

	// Private state
	private Transform target;
	private int lockCount;
	private float x, y; // (0, 0) is top left of Viewport
	private Collider viewPort;
	private GameStateController gameMaster;

	void Awake() {
		currentState = State.IDLE;
		viewPort = (Collider)viewPortObject.GetComponents(typeof(Collider))[0];
		gameMaster = (GameStateController)GameObject.FindObjectOfType(typeof(GameStateController));
	}

	void Update () {
		if (keyboardMode) processInput();
		else updatePos();
		if (gameMaster.fireEnabled) {
			transform.renderer.enabled = true;
			checkLockOn();
		}
		else transform.renderer.enabled = false;
	}

	public void resetCursor() {
		transform.position = viewPort.transform.position + 
			(viewPort.transform.position - cam.transform.position);
	}

	// KEYBOARD MODE
	private void processInput() {
		// Receive Input
		if (Input.GetKey(KeyCode.LeftArrow)) {
			transform.position += new Vector3(-1 * arrowKeySensitivity * Time.deltaTime, 0, 0);
		}
		if (Input.GetKey(KeyCode.RightArrow)) {
			transform.position += new Vector3(arrowKeySensitivity * Time.deltaTime, 0, 0);
		}
		if (Input.GetKey(KeyCode.UpArrow)) {
			transform.position += new Vector3(0, arrowKeySensitivity * Time.deltaTime, 0);
		}
		if (Input.GetKey(KeyCode.DownArrow)) {
			transform.position += new Vector3(0, -1 * arrowKeySensitivity * Time.deltaTime, 0);
		}
		if (Input.GetKeyDown(KeyCode.F)) {
			fire();
		}
	}

	private void updatePos() {
		Vector3 e = viewPort.bounds.extents;
		Vector3 t = viewPortObject.transform.position;

		transform.position = t + new Vector3(e.x * x, e.y * y, 0);
	}

	public void setPos(float newx, float newy) {
		x = newx;
		y = newy;
	}
	
	private void reset() {
		lockCount = 0;
		currentState = State.IDLE;
		target = null;
	}

	// CHECK IF A TARGET IS LOCKABLE AND ALIVE
	private bool checkLockable(RaycastHit hit) {
		Component[] ls = hit.transform.GetComponents(typeof(Lockable));
		if (ls.Length > 0 && ((Lockable)ls[0]).isItAlive()) {
			target = hit.transform;
			return true;
		}

		ls = hit.transform.parent.GetComponents(typeof(Lockable));
		if (ls.Length > 0 && ((Lockable)ls[0]).isItAlive()) {
			target = hit.transform.parent;
			return true;
		}

		return false;
	}

	// RAYCAST TO SEE IF WE ARE LOCKING ON TO ANYTHING
	private void checkLockOn() {
		// initialize RaycastHits
		RaycastHit hitFar = new RaycastHit();

		// Vector shit
		Vector3 aim = transform.position - cam.transform.position;
		Vector3 offset = Vector3.Normalize(aim) * .009f;

		Debug.DrawRay(transform.position + offset, Vector3.Normalize(aim)*200, Color.blue);

		bool f = Physics.Raycast (transform.position + offset, aim, out hitFar, 200);

		// lock on to a far target
		if (f && checkLockable(hitFar)) {
			updateState(hitFar);
		}

		// Or else reset
		else if (currentState != State.IDLE) {
			if (target) {
				Lockable l = (Lockable)target.GetComponents(typeof(Lockable))[0];
				l.onRelease();
			}
			reset ();
		}
	}

	// UPDATE LOCKON STATE
	// guaranteed either target or target.parent is Lockable
	private void updateState(RaycastHit hit) {
		// If IDLE, set target and set state to LOCKING
		if (currentState == State.IDLE) {
			currentState = State.LOCKING;
			Component[] cs = target.GetComponents(typeof(Lockable));
			if (cs.Length > 0) {
				((Lockable)cs[0]).onLock();
			}
		}
		
		// If LOCKING and SAME TARGET, increment count or FIRE!!
		if (currentState == State.LOCKING && hit.transform == target) {
			if (lockCount < maxLockCount) {
				Debug.Log("Locking: " + hit.transform.name);
				lockCount = lockCount + 1;
			}
		}
		
		// If LOCKING and NEW TARGET, reset
		else if (currentState == State.LOCKING) {
			lockCount = 0;
		}

		else {

		}
	}

	public void fire() {
		if (target) fire(target);
	}
	
	private void fire(RaycastHit hit) {
		// If we have a Rigidbody, apply an explosive force to it
		if (hit.rigidbody) {
			hit.rigidbody.AddExplosionForce(power, transform.position, radius);
		}

		// Call the target's onFire function
		Lockable l = (Lockable)hit.transform.GetComponents(typeof(Lockable))[0];
		l.onFire();

		// Sounds
		gameMaster.onFire();
	}

	private void fire(Transform t) {
		// If we have a Rigidbody, apply an explosive force to it
		if (t.rigidbody) {
			t.rigidbody.AddExplosionForce(power, transform.position-new Vector3(0,1,0), radius);
		}
		
		// Call the target's onFire function
		Lockable l = (Lockable)t.GetComponents(typeof(Lockable))[0];
		l.onFire();

		// Sounds
		gameMaster.onFire();
	}
}