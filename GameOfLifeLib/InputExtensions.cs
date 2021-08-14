using System;
using System.Text.RegularExpressions;

namespace GameOfLifeLib {

	public static class InputExtensions {

		public static int InputInt32(string prompt = "", int min = int.MinValue, int max = int.MaxValue) {
			if (prompt.Trim() != "") {
				if (min != int.MinValue && max != int.MaxValue)
					prompt += $" ({min}-{max})";

				Console.WriteLine(prompt);
			}

			bool parsed;
			int inputInt;
			do {
				var input = Console.ReadLine();
				parsed = int.TryParse(input, out inputInt);
				if (!parsed)
					Console.WriteLine("That was not an integer.");
				else if (inputInt < min || inputInt > max)
					Console.WriteLine("The entered value was outside of the expected range.");
			} while (!parsed || inputInt < min || inputInt > max);

			return inputInt;
		}

		public static Point InputPoint(string prompt = "") {
			if (prompt.Trim() != "")
				Console.WriteLine(prompt);

			var valid = false;
			var inputPoint = new Point(0, 0);
			do {
				var input = Console.ReadLine();
				if (input != null) {
					var match = Regex.Match(input, "(\\d+)\\D+(\\d+)");
					if (match.Success) {
						var x = int.Parse(match.Groups[1].Value);
						var y = int.Parse(match.Groups[2].Value);

						inputPoint = new Point(x, y);

						valid = true;
					}
				}

				if (!valid)
					Console.WriteLine("That was not a valid value.");
			} while (!valid);

			return inputPoint;
		}

	}

}
