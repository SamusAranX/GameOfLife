using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using GameOfLifeLib;
using Microsoft.Win32;

namespace GameOfLifeWPF
{
	[SuppressMessage("ReSharper", "MemberCanBePrivate.Global")] // Public properties are needed for bindings
	public sealed partial class MainWindow : INotifyPropertyChanged, IDisposable
	{

		#region INotifyPropertyChanged

		public event PropertyChangedEventHandler PropertyChanged;

		private void OnPropertyChanged(string propertyName)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

		private void SetField<T>(ref T field, T value, [CallerMemberName]string propertyName = null)
		{
			if (EqualityComparer<T>.Default.Equals(field, value))
				return;

			field = value;
			this.OnPropertyChanged(propertyName);
		}

		#endregion

		public GameOfLife GoL { get; }

		private bool _simulationRunning;
		public bool SimulationRunning
		{
			get => this._simulationRunning;
			set => this.SetField(ref this._simulationRunning, value);
		}

		private double _simSpeedFactor = 2.0;
		public double SimSpeedFactor
		{
			get => this._simSpeedFactor;
			set
			{
				this.SetField(ref this._simSpeedFactor, value);
				this._timer?.Change(0, (int)(TimerInitialSpeed / value));
			}
		}

		private int _canvassize = 128;
		public int CanvasSize
		{
			get => this._canvassize;
			set
			{
				this.SetField(ref this._canvassize, value);
				this.ResizeHelper(value);
			}
		}

		private bool CanvasActive { get; set; }

		private BitmapSource _golsource;
		public BitmapSource GOLSource
		{
			get => this._golsource;
			set => this.SetField(ref this._golsource, value);
		}

		private bool _simulationCalculating;
		private const double TimerInitialSpeed = 1000d / 15d;
		private Timer _timer;

		public MainWindow()
		{
			this.InitializeComponent();

			this.GoL = new GameOfLife(this.CanvasSize);
			this.GoL.Randomize();
			this.UpdateImage();

			this.DataContext = this;
		}

		public void Dispose()
		{
			this._timer?.Dispose();
		}

		private void Window_Closing(object sender, CancelEventArgs e)
		{
			// clean up
			this.ButtonStop_Click(null, null);
		}

		private void ResizeHelper(int newSize)
		{
			this.GoL.Resize(newSize);
			this.GoL.Randomize();
			this.UpdateImage();
		}

		private async void SimulationStep(object stateInfo = null)
		{
			if (this._simulationCalculating)
				return;

			this._simulationCalculating = true;

			await this.GoL.UpdateAsync();
			this.UpdateImage();

			this._simulationCalculating = false;
		}

		private void UpdateImage()
		{
			var arr = new byte[this.GoL.TotalCells];

			for (var i = 0; i < this.GoL.TotalCells; i++)
			{
				var val = this.GoL.World[i] ? 255 : 0;
				arr[i] = (byte)val;
			}

			// this prevents crashes during app shutdown
			if (Application.Current == null || Application.Current.Dispatcher == null)
				return;

			// when called as a timer callback, this method will execute on a different thread
			// so we'll have to manually re-route this to the main thread
			Application.Current.Dispatcher.Invoke(() =>
			{
				var bs = BitmapSource.Create(this.GoL.Size, this.GoL.Size, 96, 96, PixelFormats.Gray8, null, arr, this.GoL.Size);
				this.GOLSource = bs;
			});
		}

		//
		// BUTTON HANDLERS
		//

		private void ButtonStart_Click(object sender, RoutedEventArgs e)
		{
			this._timer = new Timer(this.SimulationStep, true, 0, (int)(TimerInitialSpeed / this.SimSpeedFactor));
			this.SimulationRunning = true;
		}

		private void ButtonStep_Click(object sender, RoutedEventArgs e)
		{
			this.SimulationStep();
		}

		private void ButtonStop_Click(object sender, RoutedEventArgs e)
		{
			this._timer?.Dispose();
			this._timer = null;

			this.SimulationRunning = false;
		}

		private void ButtonRandomize_Click(object sender, RoutedEventArgs e)
		{
			this.GoL.Randomize();
			this.UpdateImage();
		}

		private void ButtonClear_Click(object sender, RoutedEventArgs e)
		{
			this.GoL.Clear();
			this.UpdateImage();
		}

		private void ButtonImport_Click(object sender, RoutedEventArgs e)
		{
			var wasRunning = this.SimulationRunning;
			if (wasRunning)
				this.ButtonStop_Click(null, null);

			var ofd = new OpenFileDialog
			{
				Title = "Open File",
				Multiselect = false,
				Filter = "Image Files|*.png;*.gif;*.jpg"
			};

			var dialogShown = ofd.ShowDialog();
			if (dialogShown.HasValue && dialogShown.Value)
			{
				BitmapSource bitmap = new BitmapImage(new Uri(ofd.FileName));

				if (bitmap.PixelWidth != bitmap.PixelHeight)
				{
					// Image needs to be square
					MessageBox.Show("The image needs to be square.");
					return;
				}

				var maxSize = this.SizeSlider.Maximum;
				if (bitmap.PixelWidth > maxSize)
				{
					var scaleFactor = maxSize / bitmap.PixelWidth;
					bitmap = new TransformedBitmap(bitmap, new ScaleTransform(scaleFactor, scaleFactor));
				}

				// Convert imported bitmap to Gray8 format
				var fcb = new FormatConvertedBitmap();
				fcb.BeginInit();
				fcb.Source = bitmap;
				fcb.DestinationFormat = PixelFormats.Gray8;
				fcb.EndInit();

				// pixArray will be copied to a bitmap later on
				// golArray will be passed to the GameOfLife instance
				var pixArray = new byte[bitmap.PixelWidth * bitmap.PixelHeight];
				var golArray = new bool[pixArray.Length];
				fcb.CopyPixels(pixArray, bitmap.PixelWidth, 0);

				// Pixels brighter than 0x7f (127) are considered "white"
				for (var i = 0; i < pixArray.Length; i++)
				{
					golArray[i] = pixArray[i] > 0x7f;
				}

				// Update canvas size and GameOfLife board
				this.CanvasSize = bitmap.PixelWidth;
				this.GoL.World = golArray;
				this.UpdateImage();
			}
			else
			{
				if (wasRunning)
					this.ButtonStart_Click(null, null);
			}
		}

		private void ButtonExport_Click(object sender, RoutedEventArgs e)
		{
			var wasRunning = this.SimulationRunning;
			if (wasRunning)
				this.ButtonStop_Click(null, null);

			var sfd = new SaveFileDialog
			{
				Title = "Export As",
				FileName = "gameoflife.png",
				Filter = "PNG Files|*.png"
			};

			var dialogShown = sfd.ShowDialog();
			if (dialogShown.HasValue && dialogShown.Value)
			{
				BitmapEncoder encoder = new PngBitmapEncoder();
				encoder.Frames.Add(BitmapFrame.Create(this.GOLSource));

				using (var fileStream = new FileStream(sfd.FileName, FileMode.Create))
				{
					encoder.Save(fileStream);
				}
			}

			if (wasRunning)
				this.ButtonStart_Click(null, null);
		}

		//
		// DRAWING FUNCTIONS
		//

		private void GOLCanvas_MouseDown(object sender, MouseButtonEventArgs e)
		{
			this.CanvasActive = true;

			// ensure a simple click adds/removes pixels as well
			this.GOLCanvas_MouseMove(sender, e);
		}

		private void GOLCanvas_MouseUp(object sender, MouseButtonEventArgs e)
		{
			this.CanvasActive = false;
		}

		private void GOLCanvas_MouseMove(object sender, MouseEventArgs e)
		{
			if (!this.CanvasActive)
				return;

			bool newCellState;
			var maxCoordVal = this.GoL.Size - 1;

			if (e.LeftButton == MouseButtonState.Pressed && e.RightButton == MouseButtonState.Released)
				newCellState = true;
			else if (e.LeftButton == MouseButtonState.Released && e.RightButton == MouseButtonState.Pressed)
				newCellState = false;
			else
			{
				this.CanvasActive = false;
				return;
			}

			var canvasPoint = e.GetPosition((Canvas)sender);
			var scaleDivisor = ((Canvas)sender).ActualWidth / this.GoL.Size;
			var canvasX = (int)Math.Floor(canvasPoint.X / scaleDivisor);
			var canvasY = (int)Math.Floor(canvasPoint.Y / scaleDivisor);

			if (canvasX < 0 | canvasX > maxCoordVal)
				return;

			if (canvasY < 0 | canvasY > maxCoordVal)
				return;

			this.GoL.SetCell(canvasX, canvasY, newCellState);

			this.UpdateImage();
		}

		//
		// OTHER EVENTS
		//

		// Enables changing slider values by scrolling
		private void SizeSlider_Scroll(object sender, MouseWheelEventArgs e)
		{
			var slider = (Slider)sender;
			var sign = Math.Sign(e.Delta);

			if (sign == 0)
				return;

			if ((int)slider.Value == (int)slider.Minimum && sign < 0)
				return;

			if ((int)slider.Value == (int)slider.Maximum && sign > 0)
				return;

			slider.Value = Math.Max(Math.Min(slider.Value + slider.LargeChange * sign, slider.Maximum), slider.Minimum);
		}
	}
}
