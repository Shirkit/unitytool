using System;
using UnityEngine;

[System.Serializable]
public class Cell
{
	public bool blocked = false;
	public bool seen = false;
	public bool safe = false;
	public bool noisy = false;
	public bool waypoint = false;
	
	public Boolean IsWalkable() {
		return safe || (!(blocked || seen));
	}
	
	public Cell Copy() {
		Cell copy = new Cell();
		copy.blocked = this.blocked;
		copy.seen = this.seen;
		copy.safe = this.safe;
		copy.safe = this.safe;
		copy.waypoint = this.waypoint;
		return copy;
	}
}
