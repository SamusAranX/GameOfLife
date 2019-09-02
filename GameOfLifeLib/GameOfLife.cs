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
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

		private bool SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
		{
			if (EqualityComparer<T>.Default.Equals(field, value))
				return false;
			field = value;
			this.OnPropertyChanged(propertyName);
			return true;
		}

		#endregion

		private int _size;
		private int _generation;
		private bool[] _world;
		private bool[] _nextgen;

		public enum QueryMode
		{
			World,
			NextGen
		}

		public int Size
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

		public int TotalCells => this.Size * this.Size;

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

		public GameOfLife(int size)
		{
			this.Resize(size);
		}

		public void Clear()
		{
			this.World = new bool[this.TotalCells];
			this.NextGen = new bool[this.TotalCells];
		}

		public void Reset()
		{
			this.Clear();
			this.Generation = 1;
		}

		public void Resize(int newSize)
		{
			this.Size = newSize;
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
				var x = r.Next(this.Size);
				var y = r.Next(this.Size);

				var idx = (int)(y * this.Size + x);
				newWorld[idx] = true;
			}
			this.World = newWorld;
		}

		private bool GetCell(Point p, QueryMode qm = QueryMode.World)
		{
			var idx = (int)(p.Y * this.Size + p.X);

			if (qm == QueryMode.NextGen)
				return this.NextGen[idx];

			return this.World[idx];
		}

		private bool GetCell(int x, int y, QueryMode qm = QueryMode.World)
		{
			var p = new Point { X = x, Y = y };
			return this.GetCell(p, qm);
		}

		private bool GetCell(double x, double y, QueryMode qm = QueryMode.World)
		{
			var p = new Point { X = x, Y = y };
			return this.GetCell(p, qm);
		}

		public void SetCell(Point p, bool val = true, QueryMode qm = QueryMode.World)
		{
			var idx = (int)(p.Y * this.Size + p.X);

			if (qm == QueryMode.NextGen)
				this.NextGen[idx] = val;
			else
				this.World[idx] = val;

			this.OnPropertyChanged("ActiveCells");
		}

		private void SetCell(int x, int y, bool val = true, QueryMode qm = QueryMode.World)
		{
			var p = new Point { X = x, Y = y };
			this.SetCell(p, val, qm);
		}

		public void ToggleCell(Point p, QueryMode qm = QueryMode.World)
		{
			var idx = (int)(p.Y * this.Size + p.X);

			if (qm == QueryMode.NextGen)
				this.NextGen[idx] = !this.NextGen[idx];
			else
				this.World[idx] = !this.World[idx];

			this.OnPropertyChanged("ActiveCells");
		}

		public void ToggleCell(int x, int y, QueryMode qm = QueryMode.World)
		{
			var p = new Point { X = x, Y = y };
			this.ToggleCell(p, qm);
		}

		private bool IsNeighborAlive(Point p, Point offset)
		{
			var result = false;

			var newX = p.X + offset.X;
			var newY = p.Y + offset.Y;

			if (newX >= 0 && newX < this.Size && newY >= 0 && newY < this.Size)
			{
				result = this.GetCell(newX, newY);
			}

			return result;
		}

		private void GenerationStep(int idx)
		{
			var x = idx % this.Size;
			var y = idx / this.Size;

			var neighbors = 0;

			foreach (var o in this._offsets)
			{
				neighbors += this.IsNeighborAlive(new Point() { X = x, Y = y }, o) ? 1 : 0;
			}

			var isAlive = this.GetCell(x, y);
			var stillAlive = false;

			// Fall 1: Eine tote Zelle mit genau 3 lebenden Nachbarn
			// wird in der Folgegeneration neu geboren
			if (!isAlive && neighbors == 3)
				stillAlive = true;

			// Fall 3: Eine lebende Zelle mit 2 oder 3 lebenden Nachbarn
			// bleibt in der Folgegeneration am Leben
			if (isAlive && (neighbors == 2 || neighbors == 3))
				stillAlive = true;

			// Fälle 2 und 4 werden implizit schon vom Code behandelt

			this.SetCell(x, y, stillAlive, QueryMode.NextGen);
		}

		private void Generate()
		{
			for (var idx = 0; idx < this.Size * this.Size; idx++)
				this.GenerationStep(idx);

			this.Generation++;
		}

		private Task GenerateAsync()
		{
			return Task.Factory.StartNew(() =>
			{
				Parallel.For(0, this.Size * this.Size, idx => this.GenerationStep(idx));
				this.Generation++;
			});
		}

		public void Update()
		{
			this.Generate();

			var swap = this.NextGen;
			this.NextGen = this.World;
			this.World = swap;
		}

		public async Task UpdateAsync()
		{
			await this.GenerateAsync();

			var swap = this.NextGen;
			this.NextGen = this.World;
			this.World = swap;
		}

	}
}
