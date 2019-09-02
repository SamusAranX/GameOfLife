using System;
using System.Windows;

namespace GameOfLifeLib
{
	public static class StringExtensions
	{
		public static string PadBoth(this string str, int length, char padChar = ' ')
		{
			var spaces = length - str.Length;
			var padLeft = spaces / 2 + str.Length;
			return str.PadLeft(padLeft, padChar).PadRight(length, padChar);
		}
	}

	public static class PointExtensions
	{

		public static Point DivideBy(this Point p, double divisor)
		{
			return new Point { X = p.X / divisor, Y = p.Y / divisor };
		}

		public static Point MultiplyBy(this Point p, double factor)
		{
			return new Point { X = p.X * factor, Y = p.Y * factor };
		}

		public static Point Round(this Point p)
		{
			return new Point { X = Math.Round(p.X), Y = Math.Round(p.Y) };
		}

		public static Point Floor(this Point p)
		{
			return new Point { X = Math.Floor(p.X), Y = Math.Floor(p.Y) };
		}

		public static Point Ceiling(this Point p)
		{
			return new Point { X = Math.Ceiling(p.X), Y = Math.Ceiling(p.Y) };
		}

		public static Point Limit(this Point p, double limitExclusive)
		{
			return new Point { X = Math.Min(p.X, limitExclusive), Y = Math.Min(p.Y, limitExclusive) };
		}

		public static Point Clamp(this Point p, double min, double max)
		{
			var clampedX = p.X.Clamp(min, max);
			var clampedY = p.Y.Clamp(min, max);

			return new Point { X = clampedX, Y = clampedY };
		}

	}

	public static class DoubleExtensions
	{

		public static int Round(this double d)
		{
			return (int)Math.Round(d);
		}

		public static T Clamp<T>(this T val, T min, T max) where T : IComparable<T>
		{
			if (val.CompareTo(min) < 0)
				return min;

			if (val.CompareTo(max) > 0)
				return max;

			return val;
		}

	}
}
