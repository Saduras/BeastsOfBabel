using System;
using System.Collections.Generic;

/// <summary>
/// This event proxy allows to registert and fire events without any inital reference
/// </summary>
public class EventProxyManager
{
	#region singelton
	static EventProxyManager instance;
	static EventProxyManager Instance {
		get {
			if(instance == null)
				instance = new EventProxyManager();
			return instance;
		}
	}
	private EventProxyManager() {}
	#endregion

	Dictionary<EventName, EventProxy> proxyDict = new Dictionary<EventName, EventProxy>();

	// static methods to fire/register events without need of any reference
    #region external
	public static void FireEvent(object sender, EventProxyArgs args)
    {
        Instance._FireEvent(sender, args);
    }

    public static void RegisterForEvent(EventName name, EventProxy.EventProxyHandler handler)
    {
        Instance._RegisterForEvent(name, handler);
    }

    public static void Clear()
    {
        Instance._Clear();
    }
    #endregion

    #region internal
	void _FireEvent(object sender, EventProxyArgs args)
	{
		if (proxyDict.ContainsKey(args.name))
			proxyDict[args.name].FireEvent(sender, args);
		// else
		// fire error event
	}

	void _RegisterForEvent(EventName name, EventProxy.EventProxyHandler handler) 
	{
		if (!proxyDict.ContainsKey(name))
			proxyDict.Add(name, new EventProxy());

		proxyDict[name].EventFired += handler;
    }

    private void _Clear()
    {
        proxyDict.Clear();
    }
    #endregion
}

/// <summary>
/// Enum of all valid event types
/// </summary>
public enum EventName {
	DefaultEvent, // just a placehoder for EventProxyArgs
	Initialized,
	UnitSpawned,
	UnitActivated,
	UnitMoved,
	UnitAttacked,
	UnitDied,
	BMapTileTapped,
	BUnitTapped,
    RoundSetup,
	TurnStarted,
	Gameover,
	EventDone,
	DebugLog,
	CombatLogInitialized
}

public class DebugLogEvent : EventProxyArgs
{
	public string debugLogString;

	public DebugLogEvent (string debugLogString)
	{
		this.debugLogString = debugLogString;
		this.name = EventName.DebugLog;
	}
}
