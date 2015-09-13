﻿using UnityEngine;
using System.Collections;

public class Character : Entity 
{
	public int treasures=0;
	//Used in healthController()
	public int inLitArea=0;
	bool dying=false;
	//Used for MoveController()
	float jumpPower, jumpCounter = 0.0F;
	//Used in CameraController()
	float hRotation = 0F, vRotation = 0F;
	//Used in DeathController()
	int deathCounter = 0;
	float respawnTimer = -99;
	Vector3 respawnPoint;
	//Used in WeaponController()
	public int weaponChoice = 0;
	public GameObject[] weapons;
	public Vector3 target;

	protected void Start()
	{
		EntityStart();
		// Spawn point of the character.
		respawnPoint = new Vector3(
			GameObject.FindGameObjectWithTag("Respawn").transform.position.x+Random.Range(-1,1)*5,
			GameObject.FindGameObjectWithTag("Respawn").transform.position.y,
			GameObject.FindGameObjectWithTag("Respawn").transform.position.z+Random.Range(-1,1)*5);
		InvokeRepeating("healthRegenController",1,1);
		print (GetComponent<Collider> ());

	}

	// Called every frame.
	protected void Update() 
	{
		EntityUpdate();
		CameraController();
		MoveController();



		// Runs WeaponController() if character is still alive. Else, it runs DeathController().
		if(health>0)
		{ 
			dying = false;
			aggroValue = baseAggroValue + treasures;
			WeaponController();
		}
		else if(!dying)
		{
			dying=true;
			if(GetComponent<Animator>()) GetComponent<Animator>().enabled = false;
			if(GetComponent<CharacterAnimations>())
			{
				foreach (Rigidbody bone in GetComponent<CharacterAnimations>().Bones)
				{
					bone.isKinematic = false;
					Physics.IgnoreCollision(bone.GetComponent<Collider>(), GetComponent<Collider>());
				}
				GetComponent<CharacterAnimations>().IKActive = false;
				GetComponent<CharacterAnimations>().headIK = false;
			}
			aggroValue = 0;
			weapons[weaponChoice].SendMessage("MainActionController", false);
			weapons[weaponChoice].SendMessage("SecondaryActionController", false);
			CancelInvoke("healthRegenController");
			InvokeRepeating("DeathController",0,1);
		}
	}

	// Controls Movement(old)
	/*
	void MoveController()
	{
		float jumpSpeed = 20.0F;
		float jumpPower = .5F;

		Vector3 moveDirection = Vector3.zero;

		CharacterController controller = GetComponent<CharacterController>();
		if(health > 0)
		{
			moveDirection = new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical"));
			moveDirection = transform.TransformDirection(moveDirection);// makes input directions camera relative
			moveDirection *= baseSpeed * speedMod;

			if (controller.isGrounded) 
			{
				jumpCounter = jumpPower;
				moveDirection *= 2;
			}
			else if(!controller.isGrounded && !Input.GetButton("Jump")) jumpCounter = 0;
			else if(jumpCounter > 0) jumpCounter -= 1*Time.deltaTime;

			if (Input.GetButton("Jump") && jumpCounter > 0) moveDirection.y = jumpSpeed;
		}
		moveDirection.y += Physics.gravity.y;
		controller.Move(moveDirection * Time.deltaTime);
		
	}
*/
	void MoveController()
	{	
		Vector3 moveDirection = Vector3.zero;
		
		CharacterController controller = GetComponent<CharacterController>();
		if(health > 0)
		{
			moveDirection = new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical"));
			moveDirection = transform.TransformDirection(moveDirection);// makes input directions camera relative
			moveDirection *= baseSpeed * speedMod;
			
			if (controller.isGrounded) 
			{
				if (Input.GetButton("Jump"))
				{
					if(jumpCounter == 0)
						jumpCounter = 1.5f;
					else
					jumpCounter+=Time.deltaTime; 
					if(jumpCounter > 2) jumpCounter=2;
				}
				else
				{
					jumpPower = jumpCounter*10;
					jumpCounter=0;
				}
			}
			else
			{
				jumpPower-=Time.deltaTime*5;
				moveDirection /=2;
			}
		}
		moveDirection.y = jumpPower + Physics.gravity.y;
		controller.Move(moveDirection * Time.deltaTime);
		
	}

	void CameraController()
	{
		float horizontalSpeed = 7.0F;
		float verticalSpeed = 7.0F;

		//Rotates Player on "X" Axis Acording to Mouse Input
		hRotation = (hRotation + horizontalSpeed * Input.GetAxis("Mouse X"))%360;
		transform.localEulerAngles = new Vector3(0, hRotation, 0);
		
		//Rotates Player on "Y" Axis Acording to Mouse Input
		vRotation = Mathf.Clamp(vRotation - verticalSpeed * Input.GetAxis("Mouse Y"), -90,90);
		Camera.main.transform.localEulerAngles = new Vector3(vRotation, 0, 0);

		RaycastHit hit;

		if(Physics.Raycast(GetComponentInChildren<Camera>().transform.position, 
		                   GetComponentInChildren<Camera>().transform.forward, out hit))
			target = hit.point;
		else target = Vector3.zero;
		Debug.DrawLine(transform.position, Vector3.zero, Color.cyan);

	}

	void WeaponController()
	{
		//Weapon chooser
		if(Input.GetKeyDown(KeyCode.Alpha1)) 
		{
			weapons[weaponChoice].SetActive(false);
			weaponChoice=0;
			weapons[weaponChoice].SetActive(true);
		}
		else if(Input.GetKeyDown(KeyCode.Alpha2)) 
		{
			weapons[weaponChoice].SetActive(false);
			weaponChoice=1;
			weapons[weaponChoice].SetActive(true);
		}
		else if(Input.GetKeyDown(KeyCode.Alpha3)) 
		{
			weapons[weaponChoice].SetActive(false);
			weaponChoice=2;
			weapons[weaponChoice].SetActive(true);

		}
		else if(Input.GetKeyDown(KeyCode.Alpha4))
		{
			weapons[weaponChoice].SetActive(false);
			weaponChoice=3;
			weapons[weaponChoice].SetActive(true);
		}

		//Grid controller
		if(weaponChoice == 3) gameObject.GetComponentInChildren<Camera>().cullingMask |= 1 << LayerMask.NameToLayer("GridLines");
		else gameObject.GetComponentInChildren<Camera>().cullingMask &=  ~(1 << LayerMask.NameToLayer("GridLines"));

		//Attack controller
		if(Input.GetButton("Fire1")) weapons[weaponChoice].SendMessage("MainActionController", true);
		else weapons[weaponChoice].SendMessage("MainActionController", false);
		
		if(Input.GetButton("Fire2")) weapons[weaponChoice].SendMessage("SecondaryActionController", true);
		else weapons[weaponChoice].SendMessage("SecondaryActionController", false);
	}

	// Regenerates health based on distance from crystal. Separate from and stacks with an Entity's regen float.
	void healthRegenController()
	{
		float counter = (GameObject.Find("Game Controller").GetComponent<GameController>().sphereScale/2)-
					Vector3.Distance(gameObject.transform.position, 
			                 GameObject.Find("Game Controller").GetComponentInChildren<Crystal>().transform.position);

		if(inLitArea >= 1 && health < maxHealth)
			health += counter / 1000;
		else if (inLitArea < 1)
			health += counter / 100;
	}

	//Controls respawn timer and respawn position.
	void DeathController()
	{
		if(respawnTimer == -99) 
		{
			Debug.Log ("you are dying");
			respawnTimer = deathCounter+1 * 10f;
		}
		else if(health > 0)
		{
			respawnTimer = -99;
			dying=false;
			if(GetComponent<Animator>()) GetComponent<Animator>().enabled = true;
			if(GetComponent<CharacterAnimations>())
			{
				foreach (Rigidbody bone in GetComponent<CharacterAnimations>().Bones)
				{
					bone.isKinematic = true;
					Physics.IgnoreCollision(bone.GetComponent<Collider>(), GetComponent<Collider>());
				}
				GetComponent<CharacterAnimations>().IKActive = true;
				GetComponent<CharacterAnimations>().headIK = true;
			}
			Debug.Log("someone helped you up");
			InvokeRepeating("healthRegenController",1,1);
			CancelInvoke("DeathController");
		}
		else if( respawnTimer > 0) respawnTimer--;
		else
		{
			respawnTimer = -99;
			deathCounter++;
			this.transform.position = respawnPoint;
			treasures = 0;
			health = maxHealth;
			dying=false;
			if(GetComponent<Animator>()) GetComponent<Animator>().enabled = true;
			if(GetComponent<CharacterAnimations>())
			{
				foreach (Rigidbody bone in GetComponent<CharacterAnimations>().Bones)
					bone.isKinematic = true;
				GetComponent<CharacterAnimations>().IKActive = true;
				GetComponent<CharacterAnimations>().headIK = true;
			}
			Debug.Log("you got better");
			InvokeRepeating("healthRegenController",1,1);
			CancelInvoke("DeathController");
		}
	}

	public Vector3 Target 
	{
		get 
		{
			return target;
		}
		set 
		{
			target = value;
		}
	}

	// OnTriggerEnter and Exit are called when entering and leaving triggers.

	void OnTriggerEnter(Collider col)
	{
		if(col.gameObject.tag == "LitArea")
		{
			inLitArea++;
		}
		if(col.gameObject.tag == "Treasure")
		{
			treasures++;
			Destroy(col.gameObject);
		}
	}
	void OnTriggerExit(Collider col)
	{
		if(col.gameObject.tag == "LitArea")
		{
			inLitArea--;
		}
	}

	public bool Dying {
		get {
			return dying;
		}
		set {
			dying = value;
		}
	}

	public float JumpCounter {
		get {
			return jumpCounter;
		}
		set {
			jumpCounter = value;
		}
	}
}