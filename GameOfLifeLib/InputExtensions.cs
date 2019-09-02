using System;
using System.Collections.Generic;
using System.Net;
using System.Text.RegularExpressions;

namespace GameOfLifeLib
{
	public static class InputExtensions
	{
		public static char InputChar(string prompt = "", char[] allowedChars = null)
		{
			if (prompt.Trim() != "")
				Console.WriteLine(prompt);

			if (allowedChars == null)
				allowedChars = Array.Empty<char>();

			bool valid;
			var inputChar = ' ';
			do
			{
				var input = Console.ReadLine();
				valid = input != null && input.Trim() != "" && input.Length == 1 && (Array.IndexOf(allowedChars, input[0]) != -1);
				if (!valid)
					Console.WriteLine("Das war kein gültiger Char.");
				else
					inputChar = input[0];
			} while (!valid);

			return inputChar;
		}

		public static decimal InputDecimal(string prompt = "", decimal min = decimal.MinValue, decimal max = decimal.MaxValue)
		{
			if (prompt.Trim() != "")
				Console.WriteLine(prompt);

			bool parsed;
			decimal inputNum;
			do
			{
				var input = Console.ReadLine();
				parsed = decimal.TryParse(input, out inputNum);
				if (!parsed)
					Console.WriteLine("Das war kein Decimal.");
				else if (inputNum < min || inputNum > max)
					Console.WriteLine("Der eingegebene Wert war außerhalb des erlaubten Bereichs.");
			} while (!parsed || inputNum < min || inputNum > max);

			return inputNum;
		}

		public static double InputDouble(string prompt = "", double min = double.MinValue, double max = double.MaxValue)
		{
			if (prompt.Trim() != "")
				Console.WriteLine(prompt);

			bool parsed;
			double inputDouble;
			do
			{
				var input = Console.ReadLine();
				parsed = double.TryParse(input, out inputDouble);
				if (!parsed)
					Console.WriteLine("Das war kein Double.");
				else if (inputDouble < min || inputDouble > max)
					Console.WriteLine("Der eingegebene Wert war außerhalb des erlaubten Bereichs.");
			} while (!parsed || inputDouble < min || inputDouble > max);

			return inputDouble;
		}

		public static int InputInt32(string prompt = "", int min = int.MinValue, int max = int.MaxValue)
		{
			if (prompt.Trim() != "")
			{
				if (min != int.MinValue && max != int.MaxValue)
				{
					prompt += $" ({min}-{max})";
				}

				Console.WriteLine(prompt);
			}

			bool parsed;
			int inputInt;
			do
			{
				var input = Console.ReadLine();
				parsed = int.TryParse(input, out inputInt);
				if (!parsed)
					Console.WriteLine("Das war kein Int32.");
				else if (inputInt < min || inputInt > max)
					Console.WriteLine("Der eingegebene Wert war außerhalb des erlaubten Bereichs.");
			} while (!parsed || inputInt < min || inputInt > max);

			return inputInt;
		}

		public static int[] InputIntArray(string prompt = "", int count = 10, int min = int.MinValue, int max = int.MaxValue)
		{
			if (prompt.Trim() != "")
				Console.WriteLine(prompt);

			var inputIntList = new List<int>();
			for (var i = 0; i < count; i++)
			{
				var inputInt = InputInt32("", min, max);
				inputIntList.Add(inputInt);
			}

			return inputIntList.ToArray();
		}

		public static string InputIP(string prompt = "")
		{
			if (prompt.Trim() != "")
				Console.WriteLine(prompt);

			bool valid = false;
			var inputIP = IPAddress.Parse("0.0.0.0");
			do
			{
				var input = Console.ReadLine();
				if (input != null)
				{
					input = Regex.Replace(input, "0*([0-9]+)", "${1}");

					valid = IPAddress.TryParse(input, out inputIP);
					if (!valid)
						Console.WriteLine("Das war keine gültige IP-Adresse.");
				}
			} while (!valid);

			return inputIP.ToString();
		}

		public static DateTime InputDate(string prompt = "")
		{
			if (prompt.Trim() != "")
				Console.WriteLine(prompt);

			bool valid;
			DateTime inputDate;
			do
			{
				var input = Console.ReadLine();

				valid = DateTime.TryParse(input, out inputDate);
				if (!valid)
					Console.WriteLine("Das war kein gültiges Datum im Format dd.MM.yyyy.");
			} while (!valid);

			return inputDate;
		}

		public static Point InputPoint(string prompt = "")
		{
			if (prompt.Trim() != "")
				Console.WriteLine(prompt);

			bool valid = false;
			var inputPoint = new Point(0, 0);
			do
			{
				var input = Console.ReadLine();
				if (input != null)
				{
					var match = Regex.Match(input, "(\\d+)\\D+(\\d+)");
					if (match.Success)
					{
						var x = int.Parse(match.Groups[1].Value);
						var y = int.Parse(match.Groups[2].Value);

						inputPoint = new Point(x, y);

						valid = true;
					}
				}

				if (!valid)
					Console.WriteLine("Das war keine gültige Eingabe.");
			} while (!valid);

			return inputPoint;
		}

	}
}
