using UnityEngine;
using System.Collections;

public class BUnit : MonoBehaviour {

	public GameObject renderObject;
	public GameObject meshContainer;
	public GameObject effectAnchor;
	public Animator animator;
	public BUnitUI unitUI;
	public float movementSpeed = 4;

	public AudioSource deathSound;
	public AudioSource attackSound;
	public AudioSource selectionSound;

	BView context;
	Action action;
	public Action CurrentAction {get{return action;}}

	public enum Action{
		MOVE,
		CONFIRMMOVE,
		ATTACK,
		CONFIRMATTACK,
		IDLE
	}

	public Unit unit;
	public BCombatMenu bCombatMenu;

	Attack defaultAttack;
	Attack selectedAttack;
	Color flashColor;
	Color defaultColor;

	BMapTile target;

	public void Init(BView context, Unit unit, BCombatMenu bCombatMenu) {
		this.context = context;
		this.unit = unit;
		this.bCombatMenu = bCombatMenu;

		defaultAttack = unit.attacks[unit.defaultAttack];

		if(unit.team == Unit.Team.PLAYER) {
			renderObject.renderer.material.color = new Color(0.25490f, 0.85882f, 0.23529f);
			meshContainer.transform.rotation = Quaternion.AngleAxis(90f,Vector3.up);
		} else {
			renderObject.renderer.material.color = new Color(0.77255f, 0.21961f, 0.21961f);
			meshContainer.transform.rotation = Quaternion.AngleAxis(-90f,Vector3.up);
		}

		defaultColor = renderObject.renderer.material.color;
		flashColor = Color.red;

		unitUI.Init(this);
	}

	public void Activate()
	{
		selectionSound.Play();
		PopupCombatMenu();
		ClearDisplayRange();
		DisplayMovementRange();
	}

	public void PopupCombatMenu() 
	{
		bCombatMenu.OpenForBUnit(this);
	}

	public void DisplayMovementRange()
	{
		action = Action.MOVE;
		context.DisplayRange(this, unit.MovePoints, DisplayRangeMode.ALL_CLICKABLE);
	}

	public void SelectMovementTarget(BMapTile bMapTile)
	{
		ClearDisplayRange();
		DisplayMovementRange();
		// display calculated path
		Path path = context.HighlightMovementPath(this, bMapTile);
		context.SetMovementMarker(bMapTile);

		// save selected target
		target = bMapTile;
		action = Action.CONFIRMMOVE;
	}

	public void SelectAttackTarget(BMapTile bMapTile)
	{
		Debug.Log("selectAttackTarget");
		ClearDisplayRange();
		DisplayAttackRange(selectedAttack);
		context.DisplayArea(bMapTile, selectedAttack.area);

		// save selected target
		target = bMapTile;
		action = Action.CONFIRMATTACK;
	}

	public void DisplayAttackRange(Attack attack)
	{
		if (attack == null) {
			selectedAttack = defaultAttack;
		} else {
			selectedAttack = attack;
		}
		action = Action.ATTACK;

		// set display mode depending on unit team
		// only units of the enemy team will be marked as clickable
		int mode = (unit.team == Unit.Team.PLAYER) ? DisplayRangeMode.TEAM_0_CLICKABLE : DisplayRangeMode.TEAM_1_CLICKABLE;

		context.DisplayRange(this, selectedAttack.range, DisplayRangeMode.ALL_CLICKABLE);
	}

	public void ClearDisplayRange ()
	{
		action = Action.IDLE;
		// reset map marker
		context.CleanMap();
	}

	public void SetMoveTarget(BMapTile bMapTile)
	{
		Debug.Log("SetMoveTarget action="+action);
		switch (action) {
		case Action.MOVE:
			SelectMovementTarget(bMapTile);
			break;
		case Action.CONFIRMMOVE:
			if (bMapTile == target) {
				// use target for move
				context.controller.MoveUnit (unit, bMapTile.mapTile);
				action = Action.IDLE;
			} else {
				SelectMovementTarget (bMapTile);
			}
			break;
		}
	}

	public void SetAttackTarget(BMapTile bMapTile)
	{
		Debug.Log("SetAttackTarget action="+action);
		switch(action) {
		case Action.ATTACK:
			SelectAttackTarget(bMapTile);
			break;
		case Action.CONFIRMATTACK:
			if (bMapTile == target) {
				context.controller.AttackMapTile(this.unit, bMapTile.mapTile, selectedAttack);
				action = Action.IDLE;
			} else {
				SelectAttackTarget(bMapTile);
			}
			break;
		}
	}

	public void EndTurn()
	{
		context.EndTurn();
	}

	public void MoveAlongPath(BMapTile[] path)
	{
		bCombatMenu.ActionCompleted();
		StartCoroutine(MoveRoutine(path));
	}

	public void PlayAttack(BUnit[] targets, Attack attack, byte efficeny, int damage)
	{
		// calculate the center point of all targets
		Vector3 lookAt = Vector3.zero;
		foreach(BUnit bUnit in targets) {
			lookAt += bUnit.transform.position;
		}
		lookAt /= targets.Length;

		meshContainer.transform.LookAt(lookAt);
		StartCoroutine(AttackRoutine(targets,attack, efficeny, damage));
	}

	/// <summary>
	/// Routine to perfome a attack animation
	/// </summary>
	/// <returns>IEnumerator is needed for co-routines.<</returns>
	/// <param name="target">The BUnit whom is the attack target.</param>
	/// <param name="attack">The attack which will be performed.</param>
	/// <param name="efficeny">0 = not effectiv, 1 = normal efficeny, 2 = very effectiv</param>
	/// <param name="damage">The amount of damage dealt by this attack.</param>
	IEnumerator AttackRoutine(BUnit[] targets, Attack attack, byte efficeny, int damage)
	{
		bCombatMenu.Hide();
		// sound effect
		attackSound.Play();
		// animation
		animator.SetTrigger("AttackTrigger");
		yield return new WaitForSeconds(0.6f);
		foreach(BUnit bUnit in targets) {
			bUnit.PlayHitAnimation(efficeny, damage);
			bUnit.unitUI.ShowDamage(damage);
		}
		yield return new WaitForSeconds(0.6f);
		EventProxyManager.FireEvent(this, new EventDoneEvent());
	}

	/// <summary>
	/// Plaies the hit animation.
	/// </summary>
	/// <param name="efficeny">0 = not effectiv, 1 = normal efficeny, 2 = very effectiv</param>
	public void PlayHitAnimation(byte efficeny, int damage)
	{
		if(damage > 0) {
			unitUI.UpdateLivebar();
			StartCoroutine(DamageFlashRoutine());
			StartCoroutine(ShakeRoutine(0.25f * efficeny,0.1f * efficeny));
		} else {
			EventProxyManager.FireEvent(this, new EventDoneEvent());
		}
	}

	/// <summary>
	/// Routine to performe a camera shake effect.
	/// </summary>
	/// <returns>IEnumerator is needed for co-routines.</returns>
	/// <param name="magnitude">The maginitude of the shake.</param>
	/// <param name="duration">The duration of the shake.</param>
	private IEnumerator ShakeRoutine(float magnitude, float duration) {
		
		float elapsed = 0.0f;
		Vector3 originalCamPos = Camera.main.transform.position;
		
		while (elapsed < duration) {
			
			elapsed += Time.deltaTime;          
			// damper is used to reduce shake over time lineary
			float percentComplete = elapsed / duration;         
			float damper = 1.0f - Mathf.Clamp(4.0f * percentComplete - 3.0f, 0.0f, 1.0f);
			
			// map value to [-1, 1]
			float x = Random.value * 2.0f - 1.0f;
			float y = Random.value * 2.0f - 1.0f;
			x *= magnitude * damper;
			y *= magnitude * damper;

			// perfomce the actual shake
			Camera.main.transform.position = new Vector3(originalCamPos.x + x, originalCamPos.y + y, originalCamPos.z);
			
			yield return null;
		}
		
		Camera.main.transform.position = originalCamPos;
	}


	public void Died()
	{
		StartCoroutine(DeathRoutine());
	}

	private IEnumerator DeathRoutine()
	{
		yield return new WaitForSeconds(1f);
		deathSound.Play();
		animator.SetTrigger("DeathTrigger");
		yield return new WaitForSeconds(2f);
		renderObject.SetActive(false);
		unitUI.gameObject.SetActive(false);
		EventProxyManager.FireEvent(this, new EventDoneEvent());
	}

	private IEnumerator DamageFlashRoutine() 
	{
		BParticleManager.PlayEffect("HitEffect", effectAnchor.transform.position);
		animator.SetTrigger("DamagedTrigger");
//		renderObject.renderer.material.color = flashColor;
		yield return new WaitForSeconds(0.5f);
//		renderObject.renderer.material.color = defaultColor;

	}

	/// <summary>
	/// This routine does the movement animation.
	/// </summary>
	/// <returns>Nothing; IEnumerator is just for coroutines</returns>
	/// <param name="path">The pathe we want to move along</param>
	private IEnumerator MoveRoutine(BMapTile[] path)
	{
		bCombatMenu.Hide();
		for (int i = 1; i < path.Length; i++) {
			Vector3 nextWp = path[i].transform.position;
			Vector3 lookPoint = nextWp;
			lookPoint.y = 0;
			meshContainer.transform.LookAt(lookPoint);
			do {
				Vector3 translation = nextWp - transform.position;
				float distance = translation.magnitude;
				translation = translation.normalized * Time.deltaTime * movementSpeed;
				if(distance < translation.magnitude) {
					transform.position = nextWp;
					break;
				} else {
					transform.Translate( transform.InverseTransformDirection(translation) );
				}
				yield return 0;
			} while(transform.position != nextWp);
		}
		bCombatMenu.OpenForBUnit(this);
		EventProxyManager.FireEvent(this, new EventDoneEvent());
	}
}
