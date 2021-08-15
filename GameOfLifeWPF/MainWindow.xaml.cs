using System;
using System.Collections.Generic;
using System.ComponentModel;
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

namespace GameOfLifeWPF {

	public sealed partial class MainWindow : INotifyPropertyChanged, IDisposable {

		private const double TIMER_INITIAL_SPEED = 1000d / 15d;

		private readonly byte[] _worldArray = new byte[512 * 512];

		private ushort _canvasSize = 128;

		private BitmapSource _golSource;

		private double _simSpeedFactor = 2.0;

		private bool _simulationCalculating;

		private bool _simulationRunning;
		private Timer _timer;

		public MainWindow() {
			this.InitializeComponent();

			this.GoL = new GameOfLife(this.CanvasSize);
			this.GoL.Randomize();
			this.UpdateImage();

			this.DataContext = this;
		}

		public GameOfLife GoL { get; }

		public bool SimulationRunning {
			get => this._simulationRunning;
			set => this.SetField(ref this._simulationRunning, value);
		}

		public double SimSpeedFactor {
			get => this._simSpeedFactor;
			set {
				if (Math.Abs(this._simSpeedFactor - value) < 0.001)
					return;

				this.SetField(ref this._simSpeedFactor, value);
				_ = this._timer?.Change(0, (int)(TIMER_INITIAL_SPEED / value));
			}
		}

		public ushort CanvasSize {
			get => this._canvasSize;
			set {
				this.SetField(ref this._canvasSize, value);
				this.ResizeAndRandomize(value);
			}
		}

		private bool CanvasActive { get; set; }

		public BitmapSource GOLSource {
			get => this._golSource;
			set => this.SetField(ref this._golSource, value);
		}

		public void Dispose() {
			this._timer?.Dispose();
		}

		private void Window_Closing(object sender, CancelEventArgs e) {
			// clean up
			this.ButtonStop_Click(null, null);
		}

		private void ResizeAndRandomize(ushort newSize) {
			this.GoL.Resize(newSize);
			this.GoL.Randomize();
			this.UpdateImage();
		}

		private async void SimulationStep(object stateInfo = null) {
			if (this._simulationCalculating)
				return;

			this._simulationCalculating = true;

			await this.GoL.UpdateAsync();
			this.UpdateImage();

			this._simulationCalculating = false;
		}

		private void UpdateImage() {
			for (var i = 0; i < this.GoL.TotalCells; i++) {
				var val = this.GoL.World[i] ? byte.MaxValue : byte.MinValue;
				this._worldArray[i] = val;
			}

			// this prevents crashes during app shutdown
			if (Application.Current == null || Application.Current.Dispatcher == null)
				return;

			// when called as a timer callback, this method will execute on a different thread
			// so we'll have to manually re-route this to the main thread
			Application.Current.Dispatcher.Invoke(() => {
				var bs = BitmapSource.Create(this.GoL.Size.Width, this.GoL.Size.Height, 96, 96, PixelFormats.Gray8, null, this._worldArray, this.GoL.Size.Width);
				this.GOLSource = bs;
			});
		}

		//
		// BUTTON HANDLERS
		//

		private void ButtonStart_Click(object sender, RoutedEventArgs e) {
			this._timer = new Timer(this.SimulationStep, true, 0, (int)(TIMER_INITIAL_SPEED / this.SimSpeedFactor));
			this.SimulationRunning = true;
		}

		private void ButtonStep_Click(object sender, RoutedEventArgs e) {
			this.SimulationStep();
		}

		private void ButtonStop_Click(object sender, RoutedEventArgs e) {
			this._timer?.Dispose();
			this._timer = null;

			this.SimulationRunning = false;
		}

		private void ButtonRandomize_Click(object sender, RoutedEventArgs e) {
			this.GoL.Randomize();
			this.UpdateImage();
		}

		private void ButtonClear_Click(object sender, RoutedEventArgs e) {
			this.GoL.Reset();
			this.UpdateImage();
		}

		private void ButtonImport_Click(object sender, RoutedEventArgs e) {
			var wasRunning = this.SimulationRunning;
			if (wasRunning)
				this.ButtonStop_Click(null, null);

			var ofd = new OpenFileDialog {
				Title = "Open File",
				Multiselect = false,
				Filter = "Image Files|*.png;*.gif;*.jpg;*.jpeg;*.bmp",
			};

			var dialogShown = ofd.ShowDialog();
			if (dialogShown.HasValue && dialogShown.Value) {
				BitmapSource bitmap;
				try {
					bitmap = new BitmapImage(new Uri(ofd.FileName));
				} catch (NotSupportedException) {
					_ = MessageBox.Show("The file needs to be a valid image.");
					return;
				}

				if (bitmap.PixelWidth != bitmap.PixelHeight) {
					// Image needs to be square
					_ = MessageBox.Show("The image needs to be square.");
					return;
				}

				var maxSize = this.SizeSlider.Maximum;
				if (bitmap.PixelWidth > maxSize) {
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
					golArray[i] = pixArray[i] > 0x7f;

				// Update canvas size and GameOfLife board
				this.CanvasSize = (ushort)bitmap.PixelWidth;
				this.GoL.World = golArray;
				this.UpdateImage();
			} else {
				if (wasRunning)
					this.ButtonStart_Click(null, null);
			}
		}

		private void ButtonExport_Click(object sender, RoutedEventArgs e) {
			var wasRunning = this.SimulationRunning;
			if (wasRunning)
				this.ButtonStop_Click(null, null);

			var sfd = new SaveFileDialog {
				Title = "Export As",
				FileName = "gameoflife.png",
				Filter = "PNG Files|*.png",
			};

			var dialogShown = sfd.ShowDialog();
			if (dialogShown.HasValue && dialogShown.Value) {
				BitmapEncoder encoder = new PngBitmapEncoder();
				encoder.Frames.Add(BitmapFrame.Create(this.GOLSource));

				using var fileStream = new FileStream(sfd.FileName, FileMode.Create);
				encoder.Save(fileStream);
			}

			if (wasRunning)
				this.ButtonStart_Click(null, null);
		}

		//
		// DRAWING FUNCTIONS
		//

		private void GOLCanvas_MouseDown(object sender, MouseButtonEventArgs e) {
			this.CanvasActive = true;

			// ensure a simple click adds/removes pixels as well
			this.GOLCanvas_MouseMove(sender, e);
		}

		private void GOLCanvas_MouseUp(object sender, MouseButtonEventArgs e) {
			this.CanvasActive = false;
		}

		private void GOLCanvas_MouseMove(object sender, MouseEventArgs e) {
			if (!this.CanvasActive)
				return;

			bool newCellState;
			var maxCoordVal = this.GoL.Size.Width - 1; // Using .Width here because in this scenario, height and width are the same

			if (e.LeftButton == MouseButtonState.Pressed && e.RightButton == MouseButtonState.Released)
				newCellState = true;
			else if (e.LeftButton == MouseButtonState.Released && e.RightButton == MouseButtonState.Pressed)
				newCellState = false;
			else {
				this.CanvasActive = false;
				return;
			}

			var canvasPoint = e.GetPosition((Canvas)sender);
			var scaleDivisor = ((Canvas)sender).ActualWidth / this.GoL.Size.Width; // see above
			var canvasX = (int)Math.Floor(canvasPoint.X / scaleDivisor);
			var canvasY = (int)Math.Floor(canvasPoint.Y / scaleDivisor);

			if ((canvasX < 0) | (canvasX > maxCoordVal))
				return;

			if ((canvasY < 0) | (canvasY > maxCoordVal))
				return;

			this.GoL.SetCell(canvasX, canvasY, newCellState);

			this.UpdateImage();
		}

		//
		// OTHER EVENTS
		//

		// Enables changing slider values by scrolling
		private void SizeSlider_Scroll(object sender, MouseWheelEventArgs e) {
			var slider = (Slider)sender;
			var sign = Math.Sign(e.Delta);

			if (sign == 0)
				return;

			if ((int)slider.Value == (int)slider.Minimum && sign < 0)
				return;

			if ((int)slider.Value == (int)slider.Maximum && sign > 0)
				return;

			slider.Value = Math.Max(Math.Min(slider.Value + (slider.LargeChange * sign), slider.Maximum), slider.Minimum);
		}

		#region INotifyPropertyChanged

		public event PropertyChangedEventHandler PropertyChanged;

		private void OnPropertyChanged(string propertyName) {
			this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

		private void SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = null) {
			if (EqualityComparer<T>.Default.Equals(field, value))
				return;

			field = value;
			this.OnPropertyChanged(propertyName);
		}

		#endregion

	}

}
