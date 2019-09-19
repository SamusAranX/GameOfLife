namespace GameOfLifeUWP.GameOfLifeLib
{
	/* Compatibility shim since System.Windows isn't a thing here */
	public struct Point
	{
		public int X;
		public int Y;

		public Point(int x, int y)
		{
			this.X = x;
			this.Y = y;
		}
	}
}
