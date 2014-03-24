using UnityEngine;
using System.Collections;

public class Attack : ScriptableObject
{
	public string attackName = "NewAttack";
	public Element type;
	public float hitChance = 1;
	public int damage;
	public int range;
	public Vector[] area;
}
