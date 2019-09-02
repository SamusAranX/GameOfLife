using System;
using System.Text;
using System.Threading;
using GameOfLifeLib;
using input = GameOfLifeLib.InputExtensions;

namespace GameOfLifeConsole
{
	internal static class MainClass
	{
		public static void Main()
		{
			Console.Title = "Game of Life";
			Console.OutputEncoding = Encoding.UTF8;
			Console.CursorVisible = false;

			Console.WriteLine();

			int FPS = 15;

			object[][] title = {
				new object[] { 0, 1, 1, 0, 0, 2, 0, 0, 0, 1, 0, 1, 0, 0, 1, 1, 1, 0,   0, 0,       0, 1, 0, 0, 0, 1, 1, 1, 0, 1, 1, 1, 0, 1, 1, 1 },
				new object[] { 1, 0, 0, 0, 2, 0, 1, 0, 1, 0, 1, 0, 1, 0, 1, 0, 0, 0,   0, 0,       0, 1, 0, 0, 0, 0, 1, 0, 0, 1, 0, 0, 0, 1, 0, 0 },
				new object[] { 1, 0, 1, 0, 2, 2, 2, 0, 1, 0, 1, 0, 1, 0, 1, 1, 0, 0,   'O', 'F',   0, 1, 0, 0, 0, 0, 1, 0, 0, 1, 1, 0, 0, 1, 1, 0 },
				new object[] { 1, 0, 1, 0, 1, 0, 1, 0, 1, 0, 1, 0, 1, 0, 1, 0, 0, 0,   0, 0,       0, 1, 0, 0, 0, 0, 1, 0, 0, 1, 0, 0, 0, 1, 0, 0 },
				new object[] { 0, 1, 0, 0, 1, 0, 1, 0, 1, 0, 1, 0, 1, 0, 1, 1, 1, 0,   0, 0,       0, 1, 1, 1, 0, 1, 1, 1, 0, 1, 0, 0, 0, 1, 1, 1 }
			};

			// FULL BLOCK char
			const string block = "█";

			// box drawing chars
			const string cornerNW = "┏";
			const string cornerNE = "┓";
			const string cornerSW = "┗";
			const string cornerSE = "┛";
			const char lineVert = '┃';
			const char lineHorz = '━';

			foreach (var row in title)
			{
				if (Console.WindowWidth < row.Length * 2)
				{
					Console.SetWindowSize(row.Length * 2, Console.WindowHeight);
				}

				foreach (var ch in row)
				{
					if (ch is int)
					{
						switch (ch)
						{
						case 0:
							Console.Write("  ");
							break;
						case 1:
							Console.Write(block + block);
							break;
						case 2:
							Console.ForegroundColor = ConsoleColor.Red;
							Console.Write(block + block);
							Console.ResetColor();
							break;
						}
					}
					else if (ch is char)
					{
						Console.Write($"{ch} ");
					}
				}
				Console.WriteLine();
			}

			var size = input.InputInt32("Gib die Spielfeldgröße ein", 16, 32);

			var gol = new GameOfLife(size);
			gol.Randomize();

			var paused = false;
			var sb = new StringBuilder();
			while (true)
			{
				Console.Clear();
				sb.Clear();

				sb.AppendLine(cornerNW + $" Gen. {gol.Generation} ({gol.ActiveCells}/{gol.TotalCells}, {FPS} SPS) ".PadBoth((gol.Size) * 2, lineHorz) + cornerNE);
				for (var y = 0; y < gol.Size; y++)
				{
					sb.Append(lineVert);
					for (var x = 0; x < gol.Size; x++)
					{
						var idx = y * gol.Size + x;
						var nextStr = gol.World[idx] ? block : " ";
						sb.Append(nextStr + nextStr);
					}
					sb.Append(lineVert);
					sb.AppendLine();
				}

				var pauseBanner = paused ? " (PAUSE) " : "";
				sb.AppendLine(cornerSW + pauseBanner.PadBoth((gol.Size) * 2, lineHorz) + cornerSE);
				Console.Write(sb);

				if (Console.KeyAvailable || paused)
				{
					var key = Console.ReadKey(true).Key;
					if (key == ConsoleKey.Escape || key == ConsoleKey.Q)
					{
						// Q = quit
						break; // Bricht die umschließende while-Schleife ab
					}

					if (key == ConsoleKey.E && paused)
					{
						// E = edit cell
						var cellCoord = input.InputPoint("Gib die X/Y-Koordinaten für die umzuschaltende Zelle ein");
						gol.ToggleCell(cellCoord);
						continue; // überspringt den Rest des Schleifeninhalts
					}

					if (key == ConsoleKey.R)
					{
						// R = randomize
						gol.Randomize();
						continue;
					}

					if (key == ConsoleKey.S)
					{
						// S = simulation speed
						string[] speedPromptArr = {
							"Gib die neue Anzahl von gewünschten Simulationsschritten pro Sekunde ein",
							"Echte Geschwindigkeit kann abweichen, die Windows-Konsole ist langsam"
						};
						var speedPrompt = string.Join("\n", speedPromptArr);

						Console.Clear();
						FPS = input.InputInt32(speedPrompt, 1, 30);
						continue; // überspringt den Rest des Schleifeninhalts
					}

					if (key == ConsoleKey.C)
					{
						// C = clear
						gol.Reset();
					}
					else if (key == ConsoleKey.Spacebar)
					{
						// Simulation pausieren/fortsetzen
						paused = !paused;
					}
				}

				// Berechne das nächste Spielfeld zwar parallel, blockiere aber, bis die Methode fertig ist
				gol.UpdateAsync().Wait();

				// Dafür sorgen, dass die x Schritte pro Sekunde eingehalten werden
				Thread.Sleep(1000 / FPS);
			}
		}
	}
}
