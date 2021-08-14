namespace GameOfLifeLib {

	/* Compatibility shim since System.Windows isn't a thing here */
	public readonly struct Point {

		public readonly int X;
		public readonly int Y;

		public Point(int x, int y) {
			this.X = x;
			this.Y = y;
		}

	}

}
