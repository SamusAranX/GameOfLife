using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

using GameOfLifeUWP.GameOfLifeLib;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using Windows.ApplicationModel.Core;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;
using Windows.UI.Core;
using Windows.UI.Xaml.Media.Imaging;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace GameOfLifeUWP
{
    public sealed partial class MainPage
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

	    public GameOfLife GOL { get; }

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

	    private ImageSource _golsource;
	    public ImageSource GOLSource
	    {
		    get => this._golsource;
		    set => this.SetField(ref this._golsource, value);
	    }

	    private bool _simulationCalculating;
	    private const double TimerInitialSpeed = 1000d / 15d;
	    private Timer _timer;
		public MainPage()
        {
            this.InitializeComponent();

            ApplicationView.PreferredLaunchViewSize = new Size(640, 800);
            ApplicationView.PreferredLaunchWindowingMode = ApplicationViewWindowingMode.PreferredLaunchViewSize;

            this.GOL = new GameOfLife(this.CanvasSize);

			this.DataContext = this;
		}

		private void MainPage_OnLoaded(object sender, RoutedEventArgs e)
		{
			this.GOL.Randomize();
			this.UpdateImage();
		}

		private void Window_Closing(object sender, CancelEventArgs e)
		{
			// clean up
			// stop the simulation here
			//this.ButtonStop_Click(null, null);
		}

		private void ResizeHelper(int newSize)
		{
			this.GOL.Resize(newSize);
			this.GOL.Randomize();
			this.UpdateImage();
		}

		private async void UpdateImage()
		{
			var arr = new byte[this.GOL.TotalCells];

			for (var i = 0; i < this.GOL.TotalCells; i++)
			{
				var val = this.GOL.World[i] ? 255 : 0;
				arr[i] = (byte)val;
			}

			var sb = new SoftwareBitmap(BitmapPixelFormat.Gray8, this.GOL.Size, this.GOL.Size, BitmapAlphaMode.Ignore);
			sb.CopyFromBuffer(arr.AsBuffer());
			sb = SoftwareBitmap.Convert(sb, BitmapPixelFormat.Bgra8);

			var sbs = new SoftwareBitmapSource();
			await sbs.SetBitmapAsync(sb);

			this.GOLSource = sbs;
		}
    }
}

