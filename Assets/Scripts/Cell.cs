using System;
using UnityEngine;

namespace Common {
	[Serializable]
	public class Cell
	{
		public bool blocked = false;
		public bool seen = false;
		public bool safe = false;
		public bool noisy = false;
		public bool waypoint = false;
		public bool goal = false;
		public short cluster = 0;
		
		public Boolean IsWalkable() {
			return safe || (!(blocked || seen));
		}
		
		public Cell Copy() {
			Cell copy = new Cell();
			copy.blocked = this.blocked;
			copy.seen = this.seen;
			copy.safe = this.safe;
			copy.noisy = this.noisy;
			copy.waypoint = this.waypoint;
			copy.goal = this.goal;
			copy.cluster = this.cluster;
			return copy;
		}
	}
}