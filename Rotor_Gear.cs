
public Program() {
	// programCounter = 0;
	Runtime.UpdateFrequency = UpdateFrequency.Update100;

	setup();
}

public List<Rotor_Landing_Gear> rotor_gears = new List<Rotor_Landing_Gear>();

public void Main(string argument, UpdateType runType) {
	Echo($"rotor gears: {rotor_gears.Count}");

	foreach(Rotor_Landing_Gear rlg in rotor_gears) {
		if(!rlg.check()) {
			// some broken blocks, re-do setup, then update immediately, then continue as normal
			setup();
			Runtime.UpdateFrequency = Runtime.UpdateFrequency | UpdateFrequency.Once;
		}
	}
}

public void setup() {
	// get all blocks of the types we need
	var blocks = new List<IMyTerminalBlock>();
	GridTerminalSystem.GetBlocksOfType<IMyTerminalBlock>(blocks, block => (block is IMyMotorStator || block is IMyLandingGear || block is IMyTimerBlock));

	// put them in groups respective of their type of block
	var rotors = new List<IMyMotorStator>();
	var gear = new List<IMyLandingGear>();
	var timers = new List<IMyTimerBlock>();
	foreach(IMyTerminalBlock block in blocks) {
		if(!block.IsFunctional) continue;

		if(block is IMyMotorStator) {
			rotors.Add((IMyMotorStator)block);
		}
		if(block is IMyLandingGear) {
			gear.Add((IMyLandingGear)block);
		}
		if(block is IMyTimerBlock) {
			timers.Add((IMyTimerBlock)block);
		}
	}

	// setup our data depending on how the blocks relate to each-other
	foreach(IMyMotorStator rotor in rotors) {
		var gear_for_this_rotor = new List<IMyLandingGear>();

		for(int i = gear.Count - 1; i >= 0; i--) {
			if(rotor.TopGrid == gear[i].CubeGrid) {
				gear_for_this_rotor.Add(gear[i]);
				gear.RemoveAt(i);
			}
		}

		var timers_for_this_rotor = new List<IMyTimerBlock>();
		for(int i = timers.Count - 1; i >= 0; i--) {
			if(rotor.TopGrid == timers[i].CubeGrid) {
				timers_for_this_rotor.Add(timers[i]);
				timers.RemoveAt(i);
			}
		}

		if(gear_for_this_rotor.Count == 0) continue;

		rotor_gears.Add(new Rotor_Landing_Gear(rotor, gear_for_this_rotor, timers_for_this_rotor));
	}
}

public class Rotor_Landing_Gear {
	public IMyMotorStator rotor;
	public List<IMyLandingGear> gear;
	public List<IMyTimerBlock> timers;
	public bool already_locked = false;

	public Rotor_Landing_Gear(IMyMotorStator rotor, List<IMyLandingGear> gear, List<IMyTimerBlock> timers) {
		this.rotor = rotor;
		this.gear = gear;
		this.timers = timers;
	}

	public bool check() {
		if(!rotor.IsFunctional) return false;

		bool any_locked = false;
		foreach(IMyLandingGear landing_gear in gear) {
			if(!landing_gear.IsFunctional) return false;
			if(landing_gear.IsLocked) {
				any_locked = true;
			}
		}

		bool timers_are_functional = true;
		foreach(IMyTimerBlock timer in timers) {
			if(!timer.IsFunctional) {
				timers_are_functional = false;
				continue;
			}

			if(any_locked) {
				if(timer.CustomName.ToLower().Contains("lock:start") && !timer.CustomName.ToLower().Contains("unlock:start")) {
					timer.StartCountdown();
				}
				if(timer.CustomName.ToLower().Contains("lock:trigger") && !timer.CustomName.ToLower().Contains("unlock:trigger")) {
					timer.Trigger();
				}
			} else {
				if(!already_locked) continue;

				if(timer.CustomName.ToLower().Contains("unlock:start")) {
					timer.StartCountdown();
				}
				if(timer.CustomName.ToLower().Contains("unlock:trigger")) {
					timer.Trigger();
				}
			}
		}

		rotor.Enabled = !any_locked;
		already_locked = any_locked;

		return timers_are_functional;
	}
}