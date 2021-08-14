using System;
using System.Text;
using System.Threading;

using GameOfLifeLib;

using input = GameOfLifeLib.InputExtensions;

namespace GameOfLifeConsole {

	internal static class MainClass {

		public static void Main() {
			Console.Title = "Game of Life";
			Console.OutputEncoding = Encoding.UTF8;
			Console.CursorVisible = false;

			Console.WriteLine();

			var fps = 15;

			object[][] title = {
				new object[] { 0, 1, 1, 0, 0, 2, 0, 0, 0, 1, 0, 1, 0, 0, 1, 1, 1, 0, 0, 0, 0, 1, 0, 0, 0, 1, 1, 1, 0, 1, 1, 1, 0, 1, 1, 1 },
				new object[] { 1, 0, 0, 0, 2, 0, 1, 0, 1, 0, 1, 0, 1, 0, 1, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0, 0, 1, 0, 0, 0, 1, 0, 0 },
				new object[] { 1, 0, 1, 0, 2, 2, 2, 0, 1, 0, 1, 0, 1, 0, 1, 1, 0, 0, 'O', 'F', 0, 1, 0, 0, 0, 0, 1, 0, 0, 1, 1, 0, 0, 1, 1, 0 },
				new object[] { 1, 0, 1, 0, 1, 0, 1, 0, 1, 0, 1, 0, 1, 0, 1, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0, 0, 1, 0, 0, 0, 1, 0, 0 },
				new object[] { 0, 1, 0, 0, 1, 0, 1, 0, 1, 0, 1, 0, 1, 0, 1, 1, 1, 0, 0, 0, 0, 1, 1, 1, 0, 1, 1, 1, 0, 1, 0, 0, 0, 1, 1, 1 }
			};

			// FULL BLOCK char
			const string BLOCK = "█";

			// box drawing chars
			const string CORNER_NW = "┏";
			const string CORNER_NE = "┓";
			const string CORNER_SW = "┗";
			const string CORNER_SE = "┛";
			const char LINE_VERT = '┃';
			const char LINE_HORZ = '━';

			foreach (var row in title) {
				if (OperatingSystem.IsWindows() && Console.WindowWidth < row.Length * 2) {
					Console.SetWindowSize(row.Length * 2, Console.WindowHeight);
				}

				foreach (var ch in row) {
					if (ch is int) {
						switch (ch) {
							case 0:
								Console.Write("  ");

								break;
							case 1:
								Console.Write(BLOCK + BLOCK);

								break;
							case 2:
								Console.ForegroundColor = ConsoleColor.Red;
								Console.Write(BLOCK + BLOCK);
								Console.ResetColor();

								break;
						}
					} else if (ch is char) {
						Console.Write($"{ch} ");
					}
				}

				Console.WriteLine();
			}

			var size = input.InputInt32("Enter desired board size", 16, 32);

			var gol = new GameOfLife((ushort)size);
			gol.Randomize();

			var paused = false;
			var sb = new StringBuilder();
			while (true) {
				Console.Clear();
				sb.Clear();

				sb.AppendLine(CORNER_NW + $" Gen. {gol.Generation} ({gol.ActiveCells}/{gol.TotalCells}, {fps} SPS) ".PadBoth(gol.Size.Width * 2, LINE_HORZ) + CORNER_NE);
				for (var y = 0; y < gol.Size.Height; y++) {
					sb.Append(LINE_VERT);
					for (var x = 0; x < gol.Size.Width; x++) {
						var idx = y * gol.Size.Width + x;
						var nextStr = gol.World[idx] ? BLOCK : " ";
						sb.Append(nextStr + nextStr);
					}

					sb.Append(LINE_VERT);
					sb.AppendLine();
				}

				var pauseBanner = paused ? " (PAUSED) " : "";
				sb.AppendLine(CORNER_SW + pauseBanner.PadBoth((gol.Size.Width) * 2, LINE_HORZ) + CORNER_SE);
				Console.Write(sb);

				if (Console.KeyAvailable || paused) {
					var key = Console.ReadKey(true).Key;
					if (key == ConsoleKey.Escape || key == ConsoleKey.Q) {
						// Q = quit
						break; // Breaks outer while loop
					}

					if (key == ConsoleKey.E && paused) {
						// E = edit cell
						var cellCoord = input.InputPoint("Enter X/Y coordinates to toggle a cell");
						gol.ToggleCell(cellCoord);

						continue;
					}

					if (key == ConsoleKey.R) {
						// R = randomize
						gol.Randomize();

						continue;
					}

					if (key == ConsoleKey.S) {
						// S = simulation speed
						string[] speedPromptArr = { "Enter the new desired number of updates per second", "Actual speed may vary. The Windows console is slow" };
						var speedPrompt = string.Join("\n", speedPromptArr);

						Console.Clear();
						fps = input.InputInt32(speedPrompt, 1, 30);

						continue;
					}

					if (key == ConsoleKey.C) {
						// C = clear
						gol.Reset();
					} else if (key == ConsoleKey.Spacebar) {
						// Pause/Resume simulation
						paused = !paused;
					}
				}

				gol.UpdateAsync().Wait();

				// Make sure the app runs at FPS steps per second
				Thread.Sleep(1000 / fps);
			}
		}

	}

}
