using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace GameOfLifeLib
{
	public sealed class GameOfLife : INotifyPropertyChanged
	{
		#region INotifyPropertyChanged

		public event PropertyChangedEventHandler PropertyChanged;

		private void OnPropertyChanged(string propertyName)
		{
			this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

		private void SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
		{
			if (EqualityComparer<T>.Default.Equals(field, value)) return;

			field = value;
			this.OnPropertyChanged(propertyName);
		}

		#endregion

		private FieldSize _size;

		private int _generation;
		private bool[] _world;
		private bool[] _nextgen;

		public FieldSize Size
		{
			get => this._size;
			private set
			{
				this.SetField(ref this._size, value);
				this.OnPropertyChanged("TotalCells");
			}
		}

		public int Generation
		{
			get => this._generation;
			private set => this.SetField(ref this._generation, value);
		}

		public bool[] World
		{
			get => this._world;
			set
			{
				this.SetField(ref this._world, value);
				this.OnPropertyChanged("ActiveCells");
			}
		}

		private bool[] NextGen
		{
			get => this._nextgen;
			set => this.SetField(ref this._nextgen, value);
		}

		public bool WrapAround { get; set; }

		public int TotalCells => this.Size.Width * this.Size.Height;

		public int ActiveCells => this.World.Count(c => c);

		private readonly Point[] _offsets = {
			new Point { X = -1, Y =  0 },
			new Point { X = -1, Y =  1 },
			new Point { X =  0, Y =  1 },
			new Point { X =  1, Y =  1 },
			new Point { X =  1, Y =  0 },
			new Point { X =  1, Y = -1 },
			new Point { X =  0, Y = -1 },
			new Point { X = -1, Y = -1 }
		};

		public GameOfLife(FieldSize size)
		{
			this.Resize(size);
		}

		public GameOfLife(ushort size)
		{
			this.Resize(new FieldSize(size, size));
		}

		public void Reset()
		{
			this.World = new bool[this.TotalCells];
			this.NextGen = new bool[this.TotalCells];

			this.Generation = 1;
		}

		public void Resize(FieldSize newSize)
		{
			this.Size = newSize;
			this.Reset();
		}

		public void Resize(ushort newSize)
		{
			this.Size = new FieldSize(newSize, newSize);
			this.Reset();
		}

		public void Randomize(double fraction = 0.2)
		{
			this.Reset();

			var r = new Random();
			var newWorld = new bool[this.TotalCells];
			var num = this.TotalCells * fraction;
			for (var i = 0; i < num; i++)
			{
				var x = r.Next(this.Size.Width);
				var y = r.Next(this.Size.Height);

				var idx = y * this.Size.Width + x;
				newWorld[idx] = true;
			}
			this.World = newWorld;
		}

		private bool GetWorldCell(int x, int y)
		{
			return this.World[y * this.Size.Width + x];
		}

		private void SetCell(Point p, bool newValue, bool nextGen = false, bool propertyChanged = true)
		{
			var idx = p.Y * this.Size.Width + p.X;

			if (nextGen)
				this.NextGen[idx] = newValue;
			else
				this.World[idx] = newValue;

			if (propertyChanged)
				this.OnPropertyChanged("ActiveCells");
		}

		public void SetCell(int x, int y, bool newValue, bool nextGen = false, bool propertyChanged = true)
		{
			var p = new Point(x, y);
			this.SetCell(p, newValue, nextGen, propertyChanged);
		}

		public void ToggleCell(Point p, bool nextGen = false)
		{
			var idx = p.Y * this.Size.Width + p.X;

			if (nextGen)
				this.NextGen[idx] = !this.NextGen[idx];
			else
				this.World[idx] = !this.World[idx];

			this.OnPropertyChanged("ActiveCells");
		}

		// better implementation of the modulo operator because the default C# implementation is broken and unusable
		private static int Mod(int i, int m)
		{
			var r = i % m;
			return r < 0 ? r + m : r;
		}

		private bool IsNeighborAlive(int x, int y, int offsetX, int offsetY)
		{
			var newX = x + offsetX;
			var newY = y + offsetY;

			if (!this.WrapAround && (newX < 0 || newX >= this.Size.Width || newY < 0 || newY >= this.Size.Height))
				return false;

			if (!this.WrapAround) 
				return this.GetWorldCell(newX, newY);

			newX = Mod(newX, this.Size.Width);
			newY = Mod(newY, this.Size.Height);

			return this.GetWorldCell(newX, newY);
		}

		private void GenerationStep(int idx)
		{
			var x = idx % this.Size.Width;
			var y = idx / this.Size.Width;

			byte neighbors = 0;
			foreach (var o in this._offsets)
			{
				if (this.IsNeighborAlive(x, y, o.X, o.Y))
					neighbors++;
			}

			var isAlive = this.GetWorldCell(x, y);

			// Rules according to Wikipedia: https://en.wikipedia.org/wiki/Conway%27s_Game_of_Life#Rules 
			// Rule 2: Any live cell with two or three live neighbours lives on to the next generation.
			var stillAlive = isAlive && (neighbors == 2 || neighbors == 3);

			// Rule 4: Any dead cell with exactly three live neighbours becomes a live cell, as if by reproduction.
			stillAlive |= !isAlive && neighbors == 3;

			// Rules 1 and 3 get handled implicitly

			this.SetCell(new Point(x, y), stillAlive, true, false);
		}

		public async Task UpdateAsync()
		{
			await Task.Factory.StartNew(() =>
			{
				Parallel.For(0, this.TotalCells, this.GenerationStep);
			});
			this.Generation++;

			this.OnPropertyChanged("ActiveCells");

			var swap = this.NextGen;
			this.NextGen = this.World;
			this.World = swap;
		}

	}
}
