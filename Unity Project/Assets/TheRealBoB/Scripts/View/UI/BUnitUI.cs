using UnityEngine;
using System.Collections;

public class BUnitUI : MonoBehaviour {

	public UISprite typeSprite;
	public UISlider lifebar;
	public Color playerColor;
	public Color aiColor;
	public UILabel nameLabel;
	public BapBar apbar;
	public BDamageNumber damageNumber;
	BUnit parent;

	bool initalized = false;

	public void Init(BUnit parent) 
	{
		this.parent = parent;

		// change color of life bar depending on team
		if(parent.unit.team == Unit.Team.PLAYER) {
			UISprite sprite = (UISprite) lifebar.foregroundWidget;
			sprite.color = playerColor;
			//lifebar.foreground.GetComponent<UISprite>().color = playerColor;
		} else {
			lifebar.foregroundWidget.color = aiColor;
			//lifebar.foreground.GetComponent<UISprite>().color = aiColor;
		}

		// set name label
		nameLabel.text = parent.unit.UnitName;
		typeSprite.spriteName = parent.unit.Element.elementName;

		initalized = true;
	}

	public void UpdateLivebar() 
	{
		if(!initalized) return;

		lifebar.value = parent.unit.HealthPoints / (float) parent.unit.MaxHealthPoints;
	}

	public void ShowDamage(int damage)
	{
		damageNumber.Display(damage.ToString());
	}
}
