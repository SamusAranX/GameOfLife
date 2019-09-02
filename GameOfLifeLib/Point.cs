using System;

namespace GameOfLifeLib
{
	/* Compatibility shim since System.Windows isn't a thing here */
	public struct Point
	{
		public double X;
		public double Y;

		public Point(double x, double y)
		{
			this.X = x;
			this.Y = y;
		}
	}
}
