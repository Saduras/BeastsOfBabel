using System;
using System.Collections.Generic;

public class Combat
{
	public int round = 0;
	Queue<Unit> unitQueue;

    public void SetupRound(List<Unit> unitList)
    {
		// count rounds
        round++;
		// reset units abilitys
		foreach (Unit unit in unitList) {
			unit.ResetTurn();	
		}

        FillUnitQueue(unitList);
        EventProxyManager.FireEvent(EventName.RoundSetup, this, null);
    }

    public Unit GetNextUnit()
    {
		return unitQueue.Dequeue();
    }

	public int TurnsLeft()
	{
		return unitQueue.Count;
	}

	void FillUnitQueue(List<Unit> unitList) 
	{
		// sort unit list
		unitList.Sort();
		// clear queue and refill with sorted list
		if(unitQueue != null)
			unitQueue.Clear();
		unitQueue = new Queue<Unit>();

		foreach(Unit unit in unitList)
		{
			unitQueue.Enqueue(unit);
		}
	}
}

