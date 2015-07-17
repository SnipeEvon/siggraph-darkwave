using UnityEngine;
using System.Collections;

public class MeleeWeapon : Weapon 
{

	// Use this for initialization
	void Start () 
	{
		WeaponStart();
	}
	
	// Update is called once per frame
	void Update () 
	{
		MainAction();
		SecondaryAction();
		WeaponTime();
	}
	public void MainAction()
	{
		if(mainActionFlag)
		{
			AttackAnimation();
			Ready=false;
			if(currentCooldown == 0)
			{
				currentCooldown=cooldown;
			}
		}
	}
	
	public void SecondaryAction()
	{
		if(secondaryActionFlag)
		{
			if(!mainActionFlag)
			{

				if(currentCooldown == 0)
				{
					AttackAnimation();
					//Weapon swing stub

					currentCooldown = cooldown;
					energy -= energyDrain;
				}
			}
		}
	}
}
