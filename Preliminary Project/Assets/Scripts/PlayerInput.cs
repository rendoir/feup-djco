﻿// This script handles inputs for the player. 
// It keeping inputs from Update() in sync with FixedUpdate()

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;

//Ensure this script runs before all other scripts to prevent laggy inputs
[DefaultExecutionOrder(-100)]
public class PlayerInput : MonoBehaviour
{
	[HideInInspector] public float horizontal;		//Float that stores horizontal input
	[HideInInspector] public float vertical;		//Float that stores vertical input
	[HideInInspector] public bool jumpHeld;			//Bool that stores jump pressed
	[HideInInspector] public bool jumpPressed;		//Bool that stores jump held
	[HideInInspector] public bool crouchHeld;		//Bool that stores crouch pressed
	[HideInInspector] public bool crouchPressed;	//Bool that stores crouch held
	[HideInInspector] public bool shootPressed;		//Bool that stores shoot input
	[HideInInspector] public bool shootHeld;		//Bool that stores shoot held
	[HideInInspector] public Vector3 mousePosition;	//Vector that store mouse position

	//UI Checks
	GraphicRaycaster m_Raycaster;
    PointerEventData m_PointerEventData;
    EventSystem m_EventSystem;

	bool readyToClear;								//Bool used to keep input in sync

	void Start()
	{
		GameObject canvas = HUD.canvas;
		m_Raycaster = canvas.GetComponent<GraphicRaycaster>();
		m_EventSystem = canvas.GetComponent<EventSystem>();
	}

	void Update()
	{
		//Clear out existing input values
		ClearInput();

		//If the Game Manager says the game is over, exit
		//if (GameManager.IsGameOver())
			//return;

		//Process keyboard, mouse, gamepad (etc) inputs
		ProcessInputs();

		//Clamp the horizontal input to be between -1 and 1
		horizontal = Mathf.Clamp(horizontal, -1f, 1f);
	}

	void FixedUpdate()
	{
		//In FixedUpdate() we set a flag that lets inputs to be cleared out during the 
		//next Update(). This ensures that all code gets to use the current inputs
		readyToClear = true;
	}

	void ClearInput()
	{
		//If we're not ready to clear input, exit
		if (!readyToClear)
			return;

		//Reset all inputs
		horizontal		= 0f;
		vertical		= 0f;
		jumpPressed		= false;
		jumpHeld		= false;
		crouchPressed	= false;
		crouchHeld		= false;
		shootPressed	= false;
		shootHeld		= false;

		readyToClear	= false;
	}

	void ProcessInputs()
	{
		//Accumulate horizontal axis input
		horizontal		+= Input.GetAxis("Horizontal");
		vertical		+= Input.GetAxis("Vertical");

		//Accumulate button inputs
		jumpPressed		= jumpPressed || Input.GetButtonDown("Jump");
		jumpHeld		= jumpHeld || Input.GetButton("Jump");

		crouchPressed	= crouchPressed || Input.GetButtonDown("Crouch");
		crouchHeld		= crouchHeld || Input.GetButton("Crouch");

		shootPressed	= shootPressed || Input.GetButtonDown("Shoot");
		shootHeld		= shootHeld || Input.GetButton("Shoot");

		//Mouse inputs
		mousePosition   = Input.mousePosition;

		CheckUI();	
	}

	void CheckUI()
	{
		m_PointerEventData = new PointerEventData(m_EventSystem);
		m_PointerEventData.position = Input.mousePosition;

		List<RaycastResult> results = new List<RaycastResult>();

		m_Raycaster.Raycast(m_PointerEventData, results);

		foreach (RaycastResult result in results)
		{
			if(result.gameObject.name != "Canvas") {
				shootPressed = false;
				shootHeld = false;
			}
		}
	}
}
