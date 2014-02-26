using System;
using UnityEngine;

namespace Objects {

	public class HealthPack : MonoBehaviour {

		public int healthHealed = 100;
		[HideInInspector]
		public int posX, posZ;

	}
}

