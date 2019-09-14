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
}
