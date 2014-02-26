using System;

namespace Extensions {

	public static class RealExtensions
	{
		public struct Epsilon
		{
			public Epsilon(double value) { _value = value; }
			private double _value;
			internal bool IsEqual   (double a, double b) { return (a == b) ||  (Math.Abs(a - b) < _value); }
			internal bool IsNotEqual(double a, double b) { return (a != b) && !(Math.Abs(a - b) < _value); }
		}
		public static bool EQ(this double a, double b, Epsilon e) { return e.IsEqual   (a, b); }
		public static bool LE(this double a, double b, Epsilon e) { return e.IsEqual   (a, b) || (a < b); }
		public static bool GE(this double a, double b, Epsilon e) { return e.IsEqual   (a, b) || (a > b); }
		
		public static bool NE(this double a, double b, Epsilon e) { return e.IsNotEqual(a, b); }
		public static bool LT(this double a, double b, Epsilon e) { return e.IsNotEqual(a, b) && (a < b); }
		public static bool GT(this double a, double b, Epsilon e) { return e.IsNotEqual(a, b) && (a > b); }
	}
}

