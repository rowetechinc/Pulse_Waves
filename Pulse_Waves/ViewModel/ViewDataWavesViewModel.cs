/*
 * Copyright © 2013 
 * Rowe Technology Inc.
 * All rights reserved.
 * http://www.rowetechinc.com
 * 
 * Redistribution and use in source and binary forms, with or without
 * modification is NOT permitted.
 * 
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS
 * "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT
 * LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS
 * FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE 
 * COPYRIGHT OWNER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT,
 * INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES INCLUDING,
 * BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; 
 * LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER 
 * CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT 
 * LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN 
 * ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE 
 * POSSIBILITY OF SUCH DAMAGE.
 * 
 * HISTORY
 * -----------------------------------------------------------------
 * Date            Initials    Version    Comments
 * -----------------------------------------------------------------
 * 09/30/2014      RC          4.1.0      Initial coding
 * 10/28/2014      RC          0.0.1      Save the Declination and DisplayMaxEnsemble options.
 * 
 */
namespace RTI
{
    using System.Collections.Generic;
    using Caliburn.Micro;
    using OxyPlot;
    using OxyPlot.Series;
    using System;
    using System.Collections.Concurrent;
    using ReactiveUI;
    using System.Threading.Tasks;
    using System.Diagnostics;
    using OxyPlot.Axes;
    using System.ComponentModel;
    using System.Linq;

    /// <summary>
    /// View the data Waves.  This will create all the
    /// objects to view the data graphically.
    /// </summary>
    public class ViewDataWavesViewModel : PulseViewModel, IHandle<WavesRecordEvent>, IHandle<WavesRecordFileEvent>
    {
        #region Class and Enums

        /// <summary>
        /// Struct to hold all 4 LineSeries
        /// so only 1 loop will need to be done
        /// to generate all ranges for one dataset.
        /// </summary>
        private class LineSeriesValues
        {
            public LineSeries Beam0Series { get; set; }
            public LineSeries Beam1Series { get; set; }
            public LineSeries Beam2Series { get; set; }
            public LineSeries Beam3Series { get; set; }
        }

        #endregion

        #region Defaults

        /// <summary>
        /// Default previous bin size.
        /// Number is negative so we know it has not been
        /// initialized.
        /// </summary>
        private const int DEFAULT_PREV_BIN_SIZE = -1;

        /// <summary>
        /// Default previous number of bins.
        /// Number is negative so we know it has not been
        /// initialized.
        /// </summary>
        private const int DEFAULT_PREV_NUM_BIN = -1;

        /// <summary>
        /// Maximun number of ensembles
        /// to store.
        /// </summary>
        private const int DEFAULT_MAX_ENSEMBLES = 1;

        #region Colors

        public OxyColor DEFAULT_SELECTEDBIN_0_1 = OxyColor.FromAColor(220, OxyColors.Chartreuse);

        public OxyColor DEFAULT_SELECTEDBIN_0_2 = OxyColor.FromAColor(220, OxyColors.Crimson);

        public OxyColor DEFAULT_SELECTEDBIN_0_3 = OxyColor.FromAColor(220, OxyColors.DimGray);


        public OxyColor DEFAULT_SELECTEDBIN_1_1 = OxyColor.FromAColor(220, OxyColors.Orange);

        public OxyColor DEFAULT_SELECTEDBIN_1_2 = OxyColor.FromAColor(220, OxyColors.Purple);

        public OxyColor DEFAULT_SELECTEDBIN_1_3 = OxyColor.FromAColor(220, OxyColors.Salmon);


        public OxyColor DEFAULT_SELECTEDBIN_2_1 = OxyColor.FromAColor(220, OxyColors.DeepSkyBlue);

        public OxyColor DEFAULT_SELECTEDBIN_2_2 = OxyColor.FromAColor(220, OxyColors.Beige);

        public OxyColor DEFAULT_SELECTEDBIN_2_3 = OxyColor.FromAColor(220, OxyColors.Azure);


       


        #endregion

        #endregion

        #region Variables

        /// <summary>
        ///  Setup logger
        /// </summary>
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        /// <summary>
        /// Event aggregator.
        /// </summary>
        private IEventAggregator _events;

        /// <summary>
        /// Pulse manager to manage the application.
        /// </summary>
        private PulseManager _pm;

        /// <summary>
        /// Buffer the incoming data.
        /// </summary>
        private ConcurrentQueue<Waves.WavesRecord> _buffer;

        /// <summary>
        /// Flag to know if processing the buffer.
        /// </summary>
        private bool _isProcessingBuffer;

        /// <summary>
        /// Options for this view model.
        /// </summary>
        private ViewDataWavesOptions _options;

        #endregion

        #region Properties

        #region Configuration

        /// <summary>
        /// Subsystem Data Configuration for this view.
        /// </summary>
        private SubsystemDataConfig _Config;
        /// <summary>
        /// Subsystem Data Configuration for this view.
        /// </summary>
        public SubsystemDataConfig Config
        {
            get { return _Config; }
            set
            {
                _Config = value;
                this.NotifyOfPropertyChange(() => this.Config);
                this.NotifyOfPropertyChange(() => this.IsPlayback);
            }
        }

        #endregion

        #region Display

        /// <summary>
        /// Display the CEPO index to describe this view model.
        /// </summary>
        public string Display
        {
            get
            {
                if (_Config != null)
                {
                    return _Config.IndexCodeString();
                }

                return "-";
            }
        }

        /// <summary>
        /// Display the CEPO index to describe this view model.
        /// </summary>
        public string Title
        {
            get 
            {
                return string.Format("[{0}]{1}", _Config.CepoIndex.ToString(), _Config.SubSystem.CodedDescString()); 
            }
        }

        /// <summary>
        /// Flag if this view will display playback or live data.
        /// TRUE = Playback Data
        /// </summary>
        public bool IsPlayback
        {
            get
            {
                if (_Config.Source == EnsembleSource.Playback)
                {
                    return true;
                }

                return false;
            }
        }

        /// <summary>
        /// Flag if the data came from the serial port.
        /// </summary>
        public bool IsSerial
        {
            get
            {
                if (_Config.Source == EnsembleSource.Serial)
                {
                    return true;
                }

                return false;
            }
        }

        /// <summary>
        /// Flag if the data came from the Long Term Average.
        /// </summary>
        public bool IsLta
        {
            get
            {
                if (_Config.Source == EnsembleSource.LTA)
                {
                    return true;
                }

                return false;
            }
        }

        /// <summary>
        /// Flag if the data came from the Short Term Average.
        /// </summary>
        public bool IsSta
        {
            get
            {
                if (_Config.Source == EnsembleSource.STA)
                {
                    return true;
                }

                return false;
            }
        }

        #endregion

        #region HPR

        /// <summary>
        /// Heading in degrees.
        /// </summary>
        private string _Heading;
        /// <summary>
        /// Heading in degrees.
        /// </summary>
        public string Heading
        {
            get { return _Heading; }
            set
            {
                _Heading = value;
                this.NotifyOfPropertyChange(() => this.Heading);
            }
        }

        /// <summary>
        /// Pitch in degrees.
        /// </summary>
        private string _Pitch;
        /// <summary>
        /// Pitch in degrees.
        /// </summary>
        public string Pitch
        {
            get { return _Pitch; }
            set
            {
                _Pitch = value;
                this.NotifyOfPropertyChange(() => this.Pitch);
            }
        }

        /// <summary>
        /// Roll in degrees.
        /// </summary>
        private string _Roll;
        /// <summary>
        /// Roll in degrees.
        /// </summary>
        public string Roll
        {
            get { return _Roll; }
            set
            {
                _Roll = value;
                this.NotifyOfPropertyChange(() => this.Roll);
            }
        }

        #endregion

        #region Date and Time

        /// <summary>
        /// Date.
        /// </summary>
        private string _Date;
        /// <summary>
        /// Date.
        /// </summary>
        public string Date
        {
            get { return _Date; }
            set
            {
                _Date = value;
                this.NotifyOfPropertyChange(() => this.Date);
            }
        }

        /// <summary>
        /// Time.
        /// </summary>
        private string _Time;
        /// <summary>
        /// Time.
        /// </summary>
        public string Time
        {
            get { return _Time; }
            set
            {
                _Time = value;
                this.NotifyOfPropertyChange(() => this.Time);
            }
        }

        #endregion

        #region Voltage

        /// <summary>
        /// Voltage in volts.
        /// </summary>
        private string _Voltage;
        /// <summary>
        /// Voltage in volts.
        /// </summary>
        public string Voltage
        {
            get { return _Voltage; }
            set
            {
                _Voltage = value;
                this.NotifyOfPropertyChange(() => this.Voltage);
            }
        }

        #endregion

        #region Status

        /// <summary>
        /// Status.
        /// </summary>
        private string _Status;
        /// <summary>
        /// Status.
        /// </summary>
        public string Status
        {
            get { return _Status; }
            set
            {
                _Status = value;
                this.NotifyOfPropertyChange(() => this.Status);
            }
        }

        /// <summary>
        /// Bottom Track Status.
        /// </summary>
        private string _BtStatus;
        /// <summary>
        /// Bottom Track Status.
        /// </summary>
        public string BtStatus
        {
            get { return _BtStatus; }
            set
            {
                _BtStatus = value;
                this.NotifyOfPropertyChange(() => this.BtStatus);
            }
        }

        #endregion

        #region Colors

        /// <summary>
        /// List of all the color options.
        /// </summary>
        private List<OxyColor> _beamColorList;
        /// <summary>
        /// List of all the color options.
        /// </summary>
        public List<OxyColor> BeamColorList
        {
            get { return _beamColorList; }
            set
            {
                _beamColorList = value;
                this.NotifyOfPropertyChange(() => this.BeamColorList);
            }
        }

        /// <summary>
        /// Color for Beam 0 plots property.
        /// </summary>
        private OxyColor _beam0Color;
        /// <summary>
        /// Color for Beam 0 plots property.
        /// </summary>
        public OxyColor Beam0Color
        {
            get { return _beam0Color; }
            set
            {
                _beam0Color = value;
                this.NotifyOfPropertyChange(() => this.Beam0Color);
                this.NotifyOfPropertyChange(() => this.Beam0ColorStr);
            }
        }

        /// <summary>
        /// String for the Beam 0 Color to display as a background color.
        /// </summary>
        public string Beam0ColorStr
        {
            get { return "#" + BeamColor.ColorValue(_beam0Color); }
        }

        /// <summary>
        /// Color for Beam 1 plots property.
        /// </summary>
        private OxyColor _beam1Color;
        /// <summary>
        /// Color for Beam 1 plots property.
        /// </summary>
        public OxyColor Beam1Color
        {
            get { return _beam1Color; }
            set
            {
                _beam1Color = value;
                this.NotifyOfPropertyChange(() => this.Beam1Color);
                this.NotifyOfPropertyChange(() => this.Beam1ColorStr);
            }
        }

        /// <summary>
        /// String for the Beam 1 Color to display as a background color.
        /// </summary>
        public string Beam1ColorStr
        {
            get { return "#" + BeamColor.ColorValue(_beam1Color); }
        }

        /// <summary>
        /// Color for Beam 2 plots property.
        /// </summary>
        private OxyColor _beam2Color;
        /// <summary>
        /// Color for Beam 2 plots property.
        /// </summary>
        public OxyColor Beam2Color
        {
            get { return _beam2Color; }
            set
            {
                _beam2Color = value;
                this.NotifyOfPropertyChange(() => this.Beam2Color);
                this.NotifyOfPropertyChange(() => this.Beam2ColorStr);
            }
        }

        /// <summary>
        /// String for the Beam 2 Color to display as a background color.
        /// </summary>
        public string Beam2ColorStr
        {
            get { return "#" + BeamColor.ColorValue(_beam2Color); }
        }

        /// <summary>
        /// Color for Beam 4 plots property.
        /// </summary>
        private OxyColor _beam3Color;
        /// <summary>
        /// Color for Beam 3 plots property.
        /// </summary>
        public OxyColor Beam3Color
        {
            get { return _beam3Color; }
            set
            {
                _beam3Color = value;
                this.NotifyOfPropertyChange(() => this.Beam3Color);
                this.NotifyOfPropertyChange(() => this.Beam3ColorStr);
            }
        }

        /// <summary>
        /// String for the Beam 3 Color to display as a background color.
        /// </summary>
        public string Beam3ColorStr
        {
            get { return "#" + BeamColor.ColorValue(_beam3Color); }
        }

        #endregion

        #region Range Plot

        /// <summary>
        /// Range in meters.
        /// </summary>
        private string _Range;
        /// <summary>
        /// Range in meters.
        /// </summary>
        public string Range
        {
            get { return _Range; }
            set
            {
                _Range = value;
                this.NotifyOfPropertyChange(() => this.Range);
            }
        }

        /// <summary>
        /// Bottom Track Range plot.
        /// </summary>
        public TimeSeriesPlotViewModel BottomTrackRangePlot { get; set; }

        #endregion

        #region Plots

        /// <summary>
        /// Pressure Plot.
        /// </summary>
        public TimeSeriesPlotViewModel PressurePlot { get; set; }

        /// <summary>
        /// East Velocity Plot.
        /// </summary>
        public TimeSeriesPlotViewModel EastVelocityPlot { get; set; }

        /// <summary>
        /// North Velocity Plot.
        /// </summary>
        public TimeSeriesPlotViewModel NorthVelocityPlot { get; set; }

        /// <summary>
        /// FFT Plot.
        /// </summary>
        public TimeSeriesPlotViewModel FftPlot { get; set; }

        /// <summary>
        /// Frequency Plot.
        /// </summary>
        public TimeSeriesPlotViewModel FrequencyPlot { get; set; }

        /// <summary>
        /// Period Plot.
        /// </summary>
        public TimeSeriesPlotViewModel PeriodPlot { get; set; }

        /// <summary>
        /// Spectrum Plot.
        /// </summary>
        public TimeSeriesPlotViewModel SpectrumPlot { get; set; }

        /// <summary>
        /// Wave Set Plot.
        /// </summary>
        public TimeSeriesPlotViewModel WaveSetPlot { get; set; }

        /// <summary>
        /// Sensor Set Plot.
        /// </summary>
        public TimeSeriesPlotViewModel SensorSetPlot { get; set; }

        /// <summary>
        /// Velocity Set Plot.
        /// </summary>
        public TimeSeriesPlotViewModel VelSetPlot { get; set; }

        #endregion

        #region Record Display

        /// <summary>
        /// Wave Record info.
        /// </summary>
        private string _RecordInfo;
        /// <summary>
        /// Wave Record info.
        /// </summary>
        public string RecordInfo
        {
            get { return _RecordInfo; }
            set
            {
                _RecordInfo = value;
                this.NotifyOfPropertyChange(() => this.RecordInfo);
            }
        }

        /// <summary>
        /// Wave Record date.
        /// </summary>
        private string _DateStr;
        /// <summary>
        /// Wave Record date.
        /// </summary>
        public string DateStr
        {
            get { return _DateStr; }
            set
            {
                _DateStr = value;
                this.NotifyOfPropertyChange(() => this.DateStr);
            }
        }

        /// <summary>
        /// Wave Record Serial Number.
        /// </summary>
        private string _SnStr;
        /// <summary>
        /// Wave Record Serial Number.
        /// </summary>
        public string SnStr
        {
            get { return _SnStr; }
            set
            {
                _SnStr = value;
                this.NotifyOfPropertyChange(() => this.SnStr);
            }
        }

        

        #endregion

        #region GPS

        /// <summary>
        /// Latitude.
        /// </summary>
        private string _Lat;
        /// <summary>
        /// Latitude.
        /// </summary>
        public string Lat
        {
            get { return _Lat; }
            set
            {
                _Lat = value;
                this.NotifyOfPropertyChange(() => this.Lat);
            }
        }

        /// <summary>
        /// Longitude.
        /// </summary>
        private string _Lon;
        /// <summary>
        /// Longitude.
        /// </summary>
        public string Lon
        {
            get { return _Lon; }
            set
            {
                _Lon = value;
                this.NotifyOfPropertyChange(() => this.Lon);
            }
        }

        /// <summary>
        /// GPS fix status.
        /// </summary>
        private string _GpsFix;
        /// <summary>
        /// GPS fix status.
        /// </summary>
        public string GpsFix
        {
            get { return _GpsFix; }
            set
            {
                _GpsFix = value;
                this.NotifyOfPropertyChange(() => this.GpsFix);
            }
        }

        #endregion

        #region Processor

        /// <summary>
        /// String on the results of waves processing the data.
        /// </summary>
        public string WavesProc
        {
            get 
            {
                if (_SelectedBurst != null)
                {
                    return SelectedBurst.WavesProc;
                }

                return "";
            }
        }

        /// <summary>
        /// Waves record number.
        /// </summary>
        public int WavesRecordNumber
        {
            get
            {
                if (_SelectedBurst != null)
                {
                    return SelectedBurst.WavesRecordNumber;
                }

                return 0;
            }
        }

        /// <summary>
        /// Number of Waves bands to try and display.
        /// </summary>
        public int NumWavesBands
        {
            get { return _options.NumWavesBands; }
            set
            {
                _options.NumWavesBands = value;
                this.NotifyOfPropertyChange(() => this.NumWavesBands);

                // Save the options
                _pm.UpdateViewDataWavesOptions(_options);

                if (_SelectedBurst != null)
                {
                    SelectedBurst.NumWavesBands = value;
                }
            }
        }

        /// <summary>
        /// Waves minimum frequency.
        /// </summary>
        public double WavesMinFreq
        {
            get { return _options.WavesMinFreq; }
            set
            {
                _options.WavesMinFreq = value;
                this.NotifyOfPropertyChange(() => this.WavesMinFreq);

                // Save the options
                _pm.UpdateViewDataWavesOptions(_options);

                if (_SelectedBurst != null)
                {
                    SelectedBurst.WavesMinFreq = value;
                }
            }
        }

        /// <summary>
        /// Waves Max Scale Factor.
        /// </summary>
        public double WavesMaxScaleFactor
        {
            get { return _options.WavesMaxScaleFactor; }
            set
            {
                _options.WavesMaxScaleFactor = value;
                this.NotifyOfPropertyChange(() => this.WavesMaxScaleFactor);

                // Save the options
                _pm.UpdateViewDataWavesOptions(_options);

                if (_SelectedBurst != null)
                {
                    SelectedBurst.WavesMaxScaleFactor = value;
                }
            }
        }

        /// <summary>
        /// Waves minimum height.
        /// </summary>
        public double WavesMinHeight
        {
            get { return _options.WavesMinHeight; }
            set
            {
                _options.WavesMinHeight = value;
                this.NotifyOfPropertyChange(() => this.WavesMinHeight);

                // Save the options
                _pm.UpdateViewDataWavesOptions(_options);

                if (_SelectedBurst != null)
                {
                    SelectedBurst.WavesMinHeight = value;
                }
            }
        }

        /// <summary>
        /// Set a flag if using Height sensor beam.
        /// </summary>
        public bool IsHeightSensorBeam
        {
            get { return _options.IsHeightSensorBeam; }
            set
            {
                _options.IsHeightSensorBeam = value;
                this.NotifyOfPropertyChange(() => this.IsHeightSensorBeam);

                // Save the options
                _pm.UpdateViewDataWavesOptions(_options);

                if (_SelectedBurst != null)
                {
                    SelectedBurst.IsHeightSensorBeam = value;
                }
            }
        }

        #endregion

        #region Frequency

        /// <summary>
        /// Wave Bands.
        /// </summary>
        private string _WaveBands;
        /// <summary>
        /// Wave Bands.
        /// </summary>
        public string WaveBands
        {
            get { return _WaveBands; }
            set
            {
                _WaveBands = value;
                this.NotifyOfPropertyChange(() => this.WaveBands);
            }
        }

        /// <summary>
        /// Hs.
        /// </summary>
        private string _Hs;
        /// <summary>
        /// Hs.
        /// </summary>
        public string Hs
        {
            get { return _Hs; }
            set
            {
                _Hs = value;
                this.NotifyOfPropertyChange(() => this.Hs);
            }
        }

        /// <summary>
        /// Peak Period.
        /// </summary>
        private string _PeakPeriod;
        /// <summary>
        /// Peak Period.
        /// </summary>
        public string PeakPeriod
        {
            get { return _PeakPeriod; }
            set
            {
                _PeakPeriod = value;
                this.NotifyOfPropertyChange(() => this.PeakPeriod);
            }
        }

        /// <summary>
        /// Mean Period.
        /// </summary>
        private string _MeanPeriod;
        /// <summary>
        /// Mean Period.
        /// </summary>
        public string MeanPeriod
        {
            get { return _MeanPeriod; }
            set
            {
                _MeanPeriod = value;
                this.NotifyOfPropertyChange(() => this.MeanPeriod);
            }
        }
        
        /// <summary>
        /// Peak Direction.
        /// </summary>
        private string _PeakDir;
        /// <summary>
        /// Peak Direction.
        /// </summary>
        public string PeakDir
        {
            get { return _PeakDir; }
            set
            {
                _PeakDir = value;
                this.NotifyOfPropertyChange(() => this.PeakDir);
            }
        }

        /// <summary>
        /// Peak Spread.
        /// </summary>
        private string _PeakSpread;
        /// <summary>
        /// Peak Spread.
        /// </summary>
        public string PeakSpread
        {
            get { return _PeakSpread; }
            set
            {
                _PeakSpread = value;
                this.NotifyOfPropertyChange(() => this.PeakSpread);
            }
        }

        /// <summary>
        /// Bin Velocity.
        /// </summary>
        private string _BinVelocity;
        /// <summary>
        /// Bin Velocity.
        /// </summary>
        public string BinVelocity
        {
            get { return _BinVelocity; }
            set
            {
                _BinVelocity = value;
                this.NotifyOfPropertyChange(() => this.BinVelocity);
            }
        }

        /// <summary>
        /// Bin Direction.
        /// </summary>
        private string _BinDirection;
        /// <summary>
        /// Bin Direction.
        /// </summary>
        public string BinDirection
        {
            get { return _BinDirection; }
            set
            {
                _BinDirection = value;
                this.NotifyOfPropertyChange(() => this.BinDirection);
            }
        }


        #endregion

        #region RTI Waves List

        /// <summary>
        /// List of all the wave bursts read in.
        /// </summary>
        public ReactiveList<Waves.RtiWaves> WavesBurstList { get; set; }

        /// <summary>
        /// Selected Waves Burst calculations.
        /// </summary>
        private Waves.RtiWaves _SelectedBurst;
        /// <summary>
        /// Selected Waves Burst calculations.
        /// </summary>
        public RTI.Waves.RtiWaves SelectedBurst
        {
            get { return _SelectedBurst; }
            set
            {
                _SelectedBurst = value;
                this.NotifyOfPropertyChange(() => this.SelectedBurst);

                if (_SelectedBurst != null)
                {
                    Clear();

                    // Display the record
                    Task.Run(() => DisplayData(_SelectedBurst.Record));

                    UpdateProperties();
                }
            }
        }

        #endregion 

        #endregion

        #region Commands

        /// <summary>
        /// Command to clear the plots.
        /// This will clear all the buffers.
        /// </summary>
        public ReactiveCommand<System.Reactive.Unit> ClearPlotsCommand { get; protected set; }

        /// <summary>
        /// Command to import the given matlab files.
        /// </summary>
        public ReactiveCommand<object> ImportMatlabWavesCommand { get; protected set; }

        #endregion

        /// <summary>
        /// Initialize the object.
        /// <param name="config">Configuration containing data source and SubsystemConfiguration.</param>
        /// </summary>
        public ViewDataWavesViewModel() 
            : base("ViewDataWavesViewModel")
        {
            // Get the Event Aggregator
            _events = IoC.Get<IEventAggregator>();
            _events.Subscribe(this);

            // Get PulseManager
            _pm = IoC.Get<PulseManager>();

            _isProcessingBuffer = false;
            _buffer = new ConcurrentQueue<Waves.WavesRecord>();

            WavesBurstList = new ReactiveList<Waves.RtiWaves>();

            // Plots
            SetupPlots();
            Beam0Color = OxyColor.Parse(BeamColor.DEFAULT_COLOR_BEAM_0);
            Beam1Color = OxyColor.Parse(BeamColor.DEFAULT_COLOR_BEAM_1);
            Beam2Color = OxyColor.Parse(BeamColor.DEFAULT_COLOR_BEAM_2);
            Beam3Color = OxyColor.Parse(BeamColor.DEFAULT_COLOR_BEAM_3);

            // Waves Processor
            SelectedBurst = new Waves.RtiWaves();

            // Get the options from the database
            _options = _pm.GetViewDataWavesOptions();

            // Create a command to clear plots
            ClearPlotsCommand = ReactiveCommand.CreateAsyncTask(_ => Task.Run(() => 
            {
                _SelectedBurst = null;
                
                // Clear data
                Clear();

                // Clear plots
                WaveSetPlot.ClearIncomingData();
                SensorSetPlot.ClearIncomingData();
                VelSetPlot.ClearIncomingData();

                System.Windows.Application.Current.Dispatcher.Invoke(new System.Action(() => { WavesBurstList.Clear(); })); 
            }));

            // Import to Waves command
            ImportMatlabWavesCommand = ReactiveCommand.Create();
            ImportMatlabWavesCommand.Subscribe(_ => ExecuteImportMatlabWaves());
        }

        /// <summary>
        /// Shutdown the object.
        /// </summary>
        public override void Dispose()
        {

        }

        #region Update Data

        /// <summary>
        /// Display the given waves record.
        /// </summary>
        /// <param name="ensemble">Waves Record.</param>
        public async Task DisplayData(Waves.WavesRecord record)
        {
            _buffer.Enqueue(record);

            // Execute async
            if (!_isProcessingBuffer)
            {
                // Execute async
                await Task.Run(() => DisplayDataExecute());
            }
        }

        /// <summary>
        /// Execute the displaying of the data async.
        /// </summary>
        private void DisplayDataExecute()
        {
            while (!_buffer.IsEmpty)
            {
                _isProcessingBuffer = true;

                // Get the latest data from the buffer
                Waves.WavesRecord record = null;
                if (_buffer.TryDequeue(out record))
                {
                    // Verify the record is good
                    if (record == null || record.WaveSamples.Count == 0)
                    {
                        _isProcessingBuffer = false;
                        continue;
                    }

                    try
                    {
                        // Update Plots
                        AddSeries(record);

                        // Display the data
                        DisplayWavesData(record);
                    }
                    catch (Exception e)
                    {
                        log.Error("Error displaying the waves record to plots.", e);
                    }
                }
            }

            _isProcessingBuffer = false;

            return;
        }


        #endregion

        #region Display Summary data

        /// <summary>
        /// Display the waves record.  
        /// </summary>
        /// <param name="ensemble">Record to display.</param>
        private void DisplayWavesData(Waves.WavesRecord record)
        {
            RecordInfo = record.InfoTxt;
            DateStr = record.DateStr;
            SnStr = record.SnStr;

            Hs = "";
            PeakPeriod = "";
            WaveBands = "";

            // All the same value
                WaveBands    += "Num Wave Bands:    " + SelectedBurst.WavesBands[0].ToString("0") + " bands";
                Hs           += "Hs:                " + SelectedBurst.WavesHs[0].ToString("0.000") + " meters";
                PeakPeriod   += "Peak Period:       " + SelectedBurst.WavesPeakPeriod[0].ToString("0.000") + " seconds";
                MeanPeriod   += "Mean Period:       " + SelectedBurst.WavesMeanPeriod[0].ToString("0.000") + " seconds\r\n";
            
            for(int x = 0; x < record.WaveCellDepth.Length; x++)
            {
                PeakDir      += "Peak Direction[" + x + "]: " + SelectedBurst.WavesPeakDir[x].ToString("0.000") + " degrees\r\n";
                PeakSpread   += "Peak Spread[" + x + "]:    " + SelectedBurst.WavesPeakSpread[x].ToString("0.000") + " degrees\r\n";
                BinVelocity  += "Bin Velocity[" + x + "]:   " + SelectedBurst.WavesAverageUVmag[x].ToString("0.000") + " m/s\r\n";
                BinDirection += "Bin Direction[" + x + "]:  " + SelectedBurst.WavesAverageUVdir[x].ToString("0.000") + " degrees\r\n";
            }
        }

        #endregion

        #region Clear

        /// <summary>
        /// Clear the plots and text.
        /// </summary>
        private void Clear()
        {
            // Clear text
            Heading = "0°";
            Pitch = "0°";
            Roll = "0°";
            Range = "0.0 m";
            //Speed = "0.0 m/s";
            BtStatus = "";
            Date = "";
            Time = "";
            Status = "";
            Lat = "";
            Lon = "";
            GpsFix = "";
            Voltage = "";

            RecordInfo = "";
            DateStr = "";
            SnStr = "";

            WaveBands = "";
            Hs = "";
            PeakPeriod = "";
            MeanPeriod = "";
            PeakDir = "";
            PeakSpread = "";
            BinVelocity = "";
            BinDirection = "";

            //_SelectedBurst = null;

            this.NotifyOfPropertyChange(() => this.WavesProc);
            this.NotifyOfPropertyChange(() => this.WavesRecordNumber);

            // Clear plots
            ClearPlots();
        }

        #endregion

        #region Plots

        #region Setup Plots

        /// <summary>
        /// Setup all the plots.
        /// </summary>
        private void SetupPlots()
        {
            #region East Velocity Plot

            // East Velocity Plot
            EastVelocityPlot = new TimeSeriesPlotViewModel(new SeriesType(new DataSource(DataSource.eSource.Waves), new BaseSeriesType(BaseSeriesType.eBaseSeriesType.Base_Waves_East_Vel)));
            EastVelocityPlot.Plot.Title = "East Velocity";
            //EastVelocityPlot.SetLeftAxis(new LinearAxis
            //{
            //    Position = AxisPosition.Left,
            //    //MajorStep = 1,
            //    //Minimum = 0,
            //    //Maximum = _maxDataSets,
            //    TicklineColor = OxyColors.White,
            //    MajorGridlineStyle = LineStyle.Solid,
            //    MinorGridlineStyle = LineStyle.Solid,
            //    MajorGridlineColor = OxyColor.FromAColor(40, OxyColors.White),
            //    MinorGridlineColor = OxyColor.FromAColor(20, OxyColors.White),
            //    TickStyle = OxyPlot.Axes.TickStyle.Inside,                               // Put tick lines inside the plot
            //    MinimumPadding = 0,                                                 // Start at axis edge   
            //    MaximumPadding = 0,                                                 // Start at axis edge
            //    //IsAxisVisible = true,
            //    MajorStep = 1,
            //    //MinorStep = 0.5,
            //    //MajorTickSize = 0.1,
            //    Unit = "m/s"
            //});

            #endregion

            #region North Velocity Plot

            // North Velocity Plot
            NorthVelocityPlot = new TimeSeriesPlotViewModel(new SeriesType(new DataSource(DataSource.eSource.Waves), new BaseSeriesType(BaseSeriesType.eBaseSeriesType.Base_Waves_North_Vel)));
            NorthVelocityPlot.Plot.Title = "North Velocity";
            //NorthVelocityPlot.SetLeftAxis(new LinearAxis
            //{
            //    Position = AxisPosition.Left,
            //    //MajorStep = 1,
            //    //Minimum = 0,
            //    //Maximum = _maxDataSets,
            //    TicklineColor = OxyColors.White,
            //    MajorGridlineStyle = LineStyle.Solid,
            //    MinorGridlineStyle = LineStyle.Solid,
            //    MajorGridlineColor = OxyColor.FromAColor(40, OxyColors.White),
            //    MinorGridlineColor = OxyColor.FromAColor(20, OxyColors.White),
            //    TickStyle = OxyPlot.Axes.TickStyle.Inside,                               // Put tick lines inside the plot
            //    MinimumPadding = 0,                                                 // Start at axis edge   
            //    MaximumPadding = 0,                                                 // Start at axis edge
            //    //IsAxisVisible = true,
            //    MajorStep = 1,
            //    //MajorTickSize = 0.25,
            //    Unit = "m/s"
            //});

            #endregion

            #region Pressure Plot

            // Pressure Plot
            PressurePlot = new TimeSeriesPlotViewModel(new SeriesType(new DataSource(DataSource.eSource.Waves), new BaseSeriesType(BaseSeriesType.eBaseSeriesType.Base_Waves_Pressure_And_Height)));
            PressurePlot.Plot.Title = "Pressure and Wave Height";
            //PressurePlot.SetLeftAxis(new LinearAxis
            //{
            //    Position = AxisPosition.Left,
            //    //MajorStep = 1,
            //    //Minimum = 0,
            //    //Maximum = _maxDataSets,
            //    TicklineColor = OxyColors.White,
            //    MajorGridlineStyle = LineStyle.Solid,
            //    MinorGridlineStyle = LineStyle.Solid,
            //    MajorGridlineColor = OxyColor.FromAColor(40, OxyColors.White),
            //    MinorGridlineColor = OxyColor.FromAColor(20, OxyColors.White),
            //    TickStyle = OxyPlot.Axes.TickStyle.Inside,                               // Put tick lines inside the plot
            //    MinimumPadding = 0,                                                 // Start at axis edge   
            //    MaximumPadding = 0,                                                 // Start at axis edge
            //    //IsAxisVisible = true,
            //    MajorStep = 0.25,
            //    //MajorTickSize = 1,
            //    Unit = "m"
            //});

            #endregion

            #region FFT Plot

            // FFT Plot
            FftPlot = new TimeSeriesPlotViewModel(new SeriesType(new DataSource(DataSource.eSource.Waves), new BaseSeriesType(BaseSeriesType.eBaseSeriesType.Base_Waves_FFT)));
            FftPlot.Plot.LegendPosition = LegendPosition.BottomCenter;
            FftPlot.Plot.LegendOrientation = LegendOrientation.Horizontal;
            FftPlot.Plot.LegendPlacement = LegendPlacement.Outside;
            FftPlot.Plot.LegendMargin = 0;
            FftPlot.Plot.Title = "FFT";
            ////FftPlot.Plot.LegendMargin = 20;
            //FftPlot.SetLeftAxis(new LinearAxis
            //{
            //    Position = AxisPosition.Left,
            //    //MajorStep = 1,
            //    //Minimum = 0,
            //    //Maximum = _maxDataSets,
            //    TicklineColor = OxyColors.White,
            //    MajorGridlineStyle = LineStyle.Solid,
            //    MinorGridlineStyle = LineStyle.Solid,
            //    MajorGridlineColor = OxyColor.FromAColor(40, OxyColors.White),
            //    MinorGridlineColor = OxyColor.FromAColor(20, OxyColors.White),
            //    TickStyle = OxyPlot.Axes.TickStyle.Inside,                               // Put tick lines inside the plot
            //    MinimumPadding = 0,                                                 // Start at axis edge   
            //    MaximumPadding = 0,                                                 // Start at axis edge
            //    //IsAxisVisible = true,
            //    //MajorStep = 1,
            //    //MinorStep = 0.5,
            //    //MajorTickSize = 0.1,
            //    Maximum = 100,
            //    Unit = "m^2/Hz"
            //});

            #endregion

            #region Frequency Plot

            // Frequency Plot
            FrequencyPlot = new TimeSeriesPlotViewModel(new SeriesType(new DataSource(DataSource.eSource.Waves), new BaseSeriesType(BaseSeriesType.eBaseSeriesType.Base_Waves_Frequency)));
            FrequencyPlot.Plot.LegendPosition = LegendPosition.BottomCenter;
            FrequencyPlot.Plot.LegendOrientation = LegendOrientation.Horizontal;
            FrequencyPlot.Plot.LegendPlacement = LegendPlacement.Outside;
            FrequencyPlot.Plot.LegendMargin = 0;
            FrequencyPlot.Plot.Title = "Frequency";
            //FrequencyPlot.ClearAllAxis();
            //FrequencyPlot.SetAxis(new LinearAxis
            //{
            //    Position = AxisPosition.Bottom,
            //    //MajorStep = 1,
            //    Minimum = 0,
            //    //Maximum = _maxDataSets,
            //    TicklineColor = OxyColors.White,
            //    MajorGridlineStyle = LineStyle.Solid,
            //    MinorGridlineStyle = LineStyle.Solid,
            //    MajorGridlineColor = OxyColor.FromAColor(40, OxyColors.White),
            //    MinorGridlineColor = OxyColor.FromAColor(20, OxyColors.White),
            //    TickStyle = OxyPlot.Axes.TickStyle.Inside,                               // Put tick lines inside the plot
            //    MinimumPadding = 0,                                                 // Start at axis edge   
            //    MaximumPadding = 0,                                                 // Start at axis edge
            //    //IsAxisVisible = true,
            //    //MajorStep = 1,
            //    //MinorStep = 0.5,
            //    Unit = "Frequency (Hz)"
            //});
            //FrequencyPlot.SetLeftAxis(new LinearAxis
            //{
            //    Position = AxisPosition.Right,
            //    //MajorStep = 1,
            //    Minimum = 0,
            //    Maximum = 360,
            //    TicklineColor = OxyColors.White,
            //    MajorGridlineStyle = LineStyle.Solid,
            //    MinorGridlineStyle = LineStyle.Solid,
            //    MajorGridlineColor = OxyColor.FromAColor(40, OxyColors.White),
            //    MinorGridlineColor = OxyColor.FromAColor(20, OxyColors.White),
            //    TickStyle = OxyPlot.Axes.TickStyle.Inside,                               // Put tick lines inside the plot
            //    MinimumPadding = 0,                                                 // Start at axis edge   
            //    MaximumPadding = 0,                                                 // Start at axis edge
            //    TitleColor = OxyColor.FromAColor(220, OxyColor.Parse(BeamColor.DEFAULT_COLOR_BEAM_1)),
            //    TextColor = OxyColor.FromAColor(220, OxyColor.Parse(BeamColor.DEFAULT_COLOR_BEAM_1)),
            //    //IsAxisVisible = true,
            //    MajorStep = 90,
            //    //MinorStep = 0.5,
            //    //MajorTickSize = 0.1,
            //    Unit = "deg",
            //    Key = "dir"
            //});
            //FrequencyPlot.SetLeftAxis(new LinearAxis
            //{
            //    Position = AxisPosition.Right,
            //    //MajorStep = 1,
            //    PositionTier = 1,
            //    Minimum = 0,
            //    Maximum = 80,
            //    TicklineColor = OxyColors.White,
            //    MajorGridlineStyle = LineStyle.Solid,
            //    MinorGridlineStyle = LineStyle.Solid,
            //    MajorGridlineColor = OxyColor.FromAColor(40, OxyColors.White),
            //    MinorGridlineColor = OxyColor.FromAColor(20, OxyColors.White),
            //    TickStyle = OxyPlot.Axes.TickStyle.Inside,                               // Put tick lines inside the plot
            //    MinimumPadding = 0,                                                 // Start at axis edge   
            //    MaximumPadding = 0,                                                 // Start at axis edge
            //    TitleColor = OxyColor.FromAColor(220, OxyColor.Parse(BeamColor.DEFAULT_COLOR_BEAM_2)),
            //    TextColor = OxyColor.FromAColor(220, OxyColor.Parse(BeamColor.DEFAULT_COLOR_BEAM_2)),
            //    //IsAxisVisible = true,
            //    MajorStep = 20,
            //    //MinorStep = 0.5,
            //    //MajorTickSize = 0.1,
            //    Unit = "deg",
            //    Key = "spr"
            //});
            //FrequencyPlot.SetLeftAxis(new LinearAxis
            //{
            //    Position = AxisPosition.Left,
            //    //MajorStep = 1,
            //    Minimum = 0,
            //    Maximum = 4,
            //    TicklineColor = OxyColors.White,
            //    MajorGridlineStyle = LineStyle.Solid,
            //    MinorGridlineStyle = LineStyle.Solid,
            //    MajorGridlineColor = OxyColor.FromAColor(40, OxyColors.White),
            //    MinorGridlineColor = OxyColor.FromAColor(20, OxyColors.White),
            //    TickStyle = OxyPlot.Axes.TickStyle.Inside,                               // Put tick lines inside the plot
            //    MinimumPadding = 0,                                                 // Start at axis edge   
            //    MaximumPadding = 0,                                                 // Start at axis edge
            //    TitleColor = OxyColor.FromAColor(220, OxyColor.Parse(BeamColor.DEFAULT_COLOR_BEAM_0)),
            //    TextColor = OxyColor.FromAColor(220, OxyColor.Parse(BeamColor.DEFAULT_COLOR_BEAM_0)),
            //    //IsAxisVisible = true,
            //    //MajorStep = 20,
            //    //MinorStep = 0.5,
            //    //MajorTickSize = 0.1,
            //    Unit = "m",
            //    Key = "sp"
            //});

            #endregion

            #region Period Plot

            // Period Plot
            PeriodPlot = new TimeSeriesPlotViewModel(new SeriesType(new DataSource(DataSource.eSource.Waves), new BaseSeriesType(BaseSeriesType.eBaseSeriesType.Base_Waves_Period)));
            PeriodPlot.Plot.LegendPosition = LegendPosition.BottomCenter;
            PeriodPlot.Plot.LegendOrientation = LegendOrientation.Horizontal;
            PeriodPlot.Plot.LegendPlacement = LegendPlacement.Outside;
            PeriodPlot.Plot.LegendMargin = 0;
            PeriodPlot.Plot.Title = "Period";
            //PeriodPlot.ClearAllAxis();
            //PeriodPlot.SetAxis(new LinearAxis
            //{
            //    Position = AxisPosition.Bottom,
            //    //MajorStep = 1,
            //    Minimum = 0,
            //    Maximum = 30,
            //    StartPosition = 1,
            //    EndPosition = 0,
            //    TicklineColor = OxyColors.White,
            //    MajorGridlineStyle = LineStyle.Solid,
            //    MinorGridlineStyle = LineStyle.Solid,
            //    MajorGridlineColor = OxyColor.FromAColor(40, OxyColors.White),
            //    MinorGridlineColor = OxyColor.FromAColor(20, OxyColors.White),
            //    TickStyle = OxyPlot.Axes.TickStyle.Inside,                               // Put tick lines inside the plot
            //    MinimumPadding = 0,                                                 // Start at axis edge   
            //    MaximumPadding = 0,                                                 // Start at axis edge
            //    //IsAxisVisible = true,
            //    //MajorStep = 1,
            //    //MinorStep = 0.5,
            //    Unit = "period (sec)"
            //});
            //PeriodPlot.SetLeftAxis(new LinearAxis
            //{
            //    Position = AxisPosition.Right,
            //    //MajorStep = 1,
            //    Minimum = 0,
            //    Maximum = 360,
            //    TicklineColor = OxyColors.White,
            //    MajorGridlineStyle = LineStyle.Solid,
            //    MinorGridlineStyle = LineStyle.Solid,
            //    MajorGridlineColor = OxyColor.FromAColor(40, OxyColors.White),
            //    MinorGridlineColor = OxyColor.FromAColor(20, OxyColors.White),
            //    TickStyle = OxyPlot.Axes.TickStyle.Inside,                               // Put tick lines inside the plot
            //    MinimumPadding = 0,                                                 // Start at axis edge   
            //    MaximumPadding = 0,                                                 // Start at axis edge
            //    TitleColor = OxyColor.FromAColor(220, OxyColor.Parse(BeamColor.DEFAULT_COLOR_BEAM_1)),
            //    TextColor = OxyColor.FromAColor(220, OxyColor.Parse(BeamColor.DEFAULT_COLOR_BEAM_1)),
            //    //IsAxisVisible = true,
            //    MajorStep = 90,
            //    //MinorStep = 0.5,
            //    //MajorTickSize = 0.1,
            //    Unit = "deg",
            //    Key = "dir"
            //});
            //PeriodPlot.SetLeftAxis(new LinearAxis
            //{
            //    Position = AxisPosition.Right,
            //    //MajorStep = 1,
            //    PositionTier = 1,
            //    Minimum = 0,
            //    Maximum = 80,
            //    TicklineColor = OxyColors.White,
            //    MajorGridlineStyle = LineStyle.Solid,
            //    MinorGridlineStyle = LineStyle.Solid,
            //    MajorGridlineColor = OxyColor.FromAColor(40, OxyColors.White),
            //    MinorGridlineColor = OxyColor.FromAColor(20, OxyColors.White),
            //    TickStyle = OxyPlot.Axes.TickStyle.Inside,                               // Put tick lines inside the plot
            //    MinimumPadding = 0,                                                 // Start at axis edge   
            //    MaximumPadding = 0,                                                 // Start at axis edge
            //    TitleColor = OxyColor.FromAColor(220, OxyColor.Parse(BeamColor.DEFAULT_COLOR_BEAM_2)),
            //    TextColor = OxyColor.FromAColor(220, OxyColor.Parse(BeamColor.DEFAULT_COLOR_BEAM_2)),
            //    //IsAxisVisible = true,
            //    MajorStep = 20,
            //    //MinorStep = 0.5,
            //    //MajorTickSize = 0.1,
            //    Unit = "deg",
            //    Key = "spr"
            //});
            //PeriodPlot.SetLeftAxis(new LinearAxis
            //{
            //    Position = AxisPosition.Left,
            //    //MajorStep = 1,
            //    Minimum = 0,
            //    Maximum = 4,
            //    TicklineColor = OxyColors.White,
            //    MajorGridlineStyle = LineStyle.Solid,
            //    MinorGridlineStyle = LineStyle.Solid,
            //    MajorGridlineColor = OxyColor.FromAColor(40, OxyColors.White),
            //    MinorGridlineColor = OxyColor.FromAColor(20, OxyColors.White),
            //    TickStyle = OxyPlot.Axes.TickStyle.Inside,                               // Put tick lines inside the plot
            //    MinimumPadding = 0,                                                 // Start at axis edge   
            //    MaximumPadding = 0,                                                 // Start at axis edge
            //    TitleColor = OxyColor.FromAColor(220, OxyColor.Parse(BeamColor.DEFAULT_COLOR_BEAM_0)),
            //    TextColor = OxyColor.FromAColor(220, OxyColor.Parse(BeamColor.DEFAULT_COLOR_BEAM_0)),
            //    //IsAxisVisible = true,
            //    //MajorStep = 20,
            //    //MinorStep = 0.5,
            //    //MajorTickSize = 0.1,
            //    Unit = "m",
            //    Key = "sp"
            //});

            #endregion

            #region Spectrum Plot

            // Spectrum Plot
            SpectrumPlot = new TimeSeriesPlotViewModel(new SeriesType(new DataSource(DataSource.eSource.Waves), new BaseSeriesType(BaseSeriesType.eBaseSeriesType.Base_Waves_Spectrum)));
            SpectrumPlot.Plot.LegendPosition = LegendPosition.BottomCenter;
            SpectrumPlot.Plot.LegendOrientation = LegendOrientation.Horizontal;
            SpectrumPlot.Plot.LegendPlacement = LegendPlacement.Outside;
            SpectrumPlot.Plot.LegendMargin = 0;
            SpectrumPlot.Plot.Title = "Uncorrected Subsurface Energy Spectrum";
            //SpectrumPlot.Plot.LegendMargin = 20;
            //SpectrumPlot.ClearAllAxis();
            //SpectrumPlot.SetAxis(new LinearAxis
            //{
            //    Position = AxisPosition.Bottom,
            //    //MajorStep = 1,
            //    Minimum = 0,
            //    //Maximum = 30,
            //    //StartPosition = 1,
            //    //EndPosition = 0,
            //    TicklineColor = OxyColors.White,
            //    MajorGridlineStyle = LineStyle.Solid,
            //    MinorGridlineStyle = LineStyle.Solid,
            //    MajorGridlineColor = OxyColor.FromAColor(40, OxyColors.White),
            //    MinorGridlineColor = OxyColor.FromAColor(20, OxyColors.White),
            //    TickStyle = OxyPlot.Axes.TickStyle.Inside,                               // Put tick lines inside the plot
            //    MinimumPadding = 0,                                                 // Start at axis edge   
            //    MaximumPadding = 0,                                                 // Start at axis edge
            //    //IsAxisVisible = true,
            //    //MajorStep = 1,
            //    //MinorStep = 0.5,
            //    Unit = "frequency (Hz)"
            //});
            //SpectrumPlot.SetLeftAxis(new LinearAxis
            //{
            //    Position = AxisPosition.Left,
            //    //MajorStep = 1,
            //    Minimum = 0,
            //    Maximum = 4,
            //    TicklineColor = OxyColors.White,
            //    MajorGridlineStyle = LineStyle.Solid,
            //    MinorGridlineStyle = LineStyle.Solid,
            //    MajorGridlineColor = OxyColor.FromAColor(40, OxyColors.White),
            //    MinorGridlineColor = OxyColor.FromAColor(20, OxyColors.White),
            //    TickStyle = OxyPlot.Axes.TickStyle.Inside,                               // Put tick lines inside the plot
            //    MinimumPadding = 0,                                                 // Start at axis edge   
            //    MaximumPadding = 0,                                                 // Start at axis edge
            //    //IsAxisVisible = true,
            //    MajorStep = 1,
            //    MinorStep = 0.5,
            //    //MajorTickSize = 0.1,
            //    Unit = "m^2/Hz"
            //});

            #endregion

            #region Wave Set Plot

            // Wave Set Plot
            WaveSetPlot = new TimeSeriesPlotViewModel(new SeriesType(new DataSource(DataSource.eSource.Waves), new BaseSeriesType(BaseSeriesType.eBaseSeriesType.Base_Waves_Wave_Set)));
            WaveSetPlot.Plot.LegendPosition = LegendPosition.BottomCenter;
            WaveSetPlot.Plot.LegendOrientation = LegendOrientation.Horizontal;
            WaveSetPlot.Plot.LegendPlacement = LegendPlacement.Outside;
            WaveSetPlot.Plot.LegendMargin = 0;
            WaveSetPlot.Plot.Title = "Wave Set";
            //WaveSetPlot.ClearAllAxis();
            //WaveSetPlot.SetAxis(new LinearAxis
            //{
            //    Position = AxisPosition.Bottom,
            //    //MajorStep = 1,
            //    Minimum = 0,
            //    //Maximum = _maxDataSets,
            //    TicklineColor = OxyColors.White,
            //    MajorGridlineStyle = LineStyle.Solid,
            //    MinorGridlineStyle = LineStyle.Solid,
            //    MajorGridlineColor = OxyColor.FromAColor(40, OxyColors.White),
            //    MinorGridlineColor = OxyColor.FromAColor(20, OxyColors.White),
            //    TickStyle = OxyPlot.Axes.TickStyle.Inside,                               // Put tick lines inside the plot
            //    MinimumPadding = 0,                                                 // Start at axis edge   
            //    MaximumPadding = 0,                                                 // Start at axis edge
            //    //IsAxisVisible = true,
            //    MajorStep = 1,
            //    MinorStep = 0.5,
            //    Unit = "Days"
            //});
            //WaveSetPlot.SetLeftAxis(new LinearAxis
            //{
            //    Position = AxisPosition.Left,
            //    //MajorStep = 1,
            //    Minimum = 0,
            //    //Maximum = 360,
            //    TicklineColor = OxyColors.White,
            //    MajorGridlineStyle = LineStyle.Solid,
            //    MinorGridlineStyle = LineStyle.Solid,
            //    MajorGridlineColor = OxyColor.FromAColor(40, OxyColors.White),
            //    MinorGridlineColor = OxyColor.FromAColor(20, OxyColors.White),
            //    TickStyle = OxyPlot.Axes.TickStyle.Inside,                               // Put tick lines inside the plot
            //    MinimumPadding = 0,                                                 // Start at axis edge   
            //    MaximumPadding = 0,                                                 // Start at axis edge
            //    TitleColor = OxyColor.FromAColor(220, OxyColor.Parse(BeamColor.DEFAULT_COLOR_BEAM_0)),
            //    TextColor = OxyColor.FromAColor(220, OxyColor.Parse(BeamColor.DEFAULT_COLOR_BEAM_0)),
            //    //IsAxisVisible = true,
            //    //MajorStep = 90,
            //    //MinorStep = 0.5,
            //    //MajorTickSize = 0.1,
            //    Unit = "m",
            //    Key = "Hs"
            //});
            //WaveSetPlot.SetLeftAxis(new LinearAxis
            //{
            //    Position = AxisPosition.Left,
            //    PositionTier = 1,
            //    //MajorStep = 1,
            //    Minimum = 0,
            //    //Maximum = 360,
            //    TicklineColor = OxyColors.White,
            //    MajorGridlineStyle = LineStyle.Solid,
            //    MinorGridlineStyle = LineStyle.Solid,
            //    MajorGridlineColor = OxyColor.FromAColor(40, OxyColors.White),
            //    MinorGridlineColor = OxyColor.FromAColor(20, OxyColors.White),
            //    TickStyle = OxyPlot.Axes.TickStyle.Inside,                               // Put tick lines inside the plot
            //    MinimumPadding = 0,                                                 // Start at axis edge   
            //    MaximumPadding = 0,                                                 // Start at axis edge
            //    TitleColor = OxyColor.FromAColor(220, OxyColor.Parse(BeamColor.DEFAULT_COLOR_BEAM_2)),
            //    TextColor = OxyColor.FromAColor(220, OxyColor.Parse(BeamColor.DEFAULT_COLOR_BEAM_2)),
            //    //IsAxisVisible = true,
            //    //MajorStep = 90,
            //    //MinorStep = 0.5,
            //    //MajorTickSize = 0.1,
            //    Unit = "s",
            //    Key = "period"
            //});
            //WaveSetPlot.SetLeftAxis(new LinearAxis
            //{
            //    Position = AxisPosition.Right,
            //    //MajorStep = 1,
            //    Minimum = 0,
            //    Maximum = 360,
            //    TicklineColor = OxyColors.White,
            //    MajorGridlineStyle = LineStyle.Solid,
            //    MinorGridlineStyle = LineStyle.Solid,
            //    MajorGridlineColor = OxyColor.FromAColor(40, OxyColors.White),
            //    MinorGridlineColor = OxyColor.FromAColor(20, OxyColors.White),
            //    TickStyle = OxyPlot.Axes.TickStyle.Inside,                               // Put tick lines inside the plot
            //    MinimumPadding = 0,                                                 // Start at axis edge   
            //    MaximumPadding = 0,                                                 // Start at axis edge
            //    TitleColor = OxyColor.FromAColor(220, OxyColor.Parse(BeamColor.DEFAULT_COLOR_BEAM_1)),
            //    TextColor = OxyColor.FromAColor(220, OxyColor.Parse(BeamColor.DEFAULT_COLOR_BEAM_1)),
            //    //IsAxisVisible = true,
            //    MajorStep = 90,
            //    //MinorStep = 0.5,
            //    //MajorTickSize = 0.1,
            //    Unit = "deg",
            //    Key = "dir"
            //});

            #endregion

            #region Sensor Set Plot

            // Sensor Set Plot
            SensorSetPlot = new TimeSeriesPlotViewModel(new SeriesType(new DataSource(DataSource.eSource.Waves), new BaseSeriesType(BaseSeriesType.eBaseSeriesType.Base_Waves_Sensor_Set)));
            SensorSetPlot.Plot.LegendPosition = LegendPosition.BottomCenter;
            SensorSetPlot.Plot.LegendOrientation = LegendOrientation.Horizontal;
            SensorSetPlot.Plot.LegendPlacement = LegendPlacement.Outside;
            SensorSetPlot.Plot.LegendMargin = 0;
            SensorSetPlot.Plot.Title = "Sensor Set";
            //SensorSetPlot.ClearAllAxis();
            //SensorSetPlot.SetAxis(new LinearAxis
            //{
            //    Position = AxisPosition.Bottom,
            //    //MajorStep = 1,
            //    Minimum = 0,
            //    //Maximum = _maxDataSets,
            //    TicklineColor = OxyColors.White,
            //    MajorGridlineStyle = LineStyle.Solid,
            //    MinorGridlineStyle = LineStyle.Solid,
            //    MajorGridlineColor = OxyColor.FromAColor(40, OxyColors.White),
            //    MinorGridlineColor = OxyColor.FromAColor(20, OxyColors.White),
            //    TickStyle = OxyPlot.Axes.TickStyle.Inside,                               // Put tick lines inside the plot
            //    MinimumPadding = 0,                                                 // Start at axis edge   
            //    MaximumPadding = 0,                                                 // Start at axis edge
            //    //IsAxisVisible = true,
            //    MajorStep = 1,
            //    MinorStep = 0.5,
            //    Unit = "Days"
            //});
            //SensorSetPlot.SetLeftAxis(new LinearAxis
            //{
            //    Position = AxisPosition.Left,
            //    //MajorStep = 1,
            //    Minimum = 0,
            //    //Maximum = 360,
            //    TicklineColor = OxyColors.White,
            //    MajorGridlineStyle = LineStyle.Solid,
            //    MinorGridlineStyle = LineStyle.Solid,
            //    MajorGridlineColor = OxyColor.FromAColor(40, OxyColors.White),
            //    MinorGridlineColor = OxyColor.FromAColor(20, OxyColors.White),
            //    TickStyle = OxyPlot.Axes.TickStyle.Inside,                               // Put tick lines inside the plot
            //    MinimumPadding = 0,                                                 // Start at axis edge   
            //    MaximumPadding = 0,                                                 // Start at axis edge
            //    TitleColor = OxyColor.FromAColor(220, OxyColor.Parse(BeamColor.DEFAULT_COLOR_BEAM_0)),
            //    TextColor = OxyColor.FromAColor(220, OxyColor.Parse(BeamColor.DEFAULT_COLOR_BEAM_0)),
            //    //IsAxisVisible = true,
            //    //MajorStep = 90,
            //    //MinorStep = 0.5,
            //    //MajorTickSize = 0.1,
            //    Unit = "m",
            //    Key = "pressure"
            //});
            //SensorSetPlot.SetLeftAxis(new LinearAxis
            //{
            //    Position = AxisPosition.Right,
            //    //MajorStep = 1,
            //    Minimum = 0,
            //    //Maximum = 360,
            //    TicklineColor = OxyColors.White,
            //    MajorGridlineStyle = LineStyle.Solid,
            //    MinorGridlineStyle = LineStyle.Solid,
            //    MajorGridlineColor = OxyColor.FromAColor(40, OxyColors.White),
            //    MinorGridlineColor = OxyColor.FromAColor(20, OxyColors.White),
            //    TickStyle = OxyPlot.Axes.TickStyle.Inside,                               // Put tick lines inside the plot
            //    MinimumPadding = 0,                                                 // Start at axis edge   
            //    MaximumPadding = 0,                                                 // Start at axis edge
            //    TitleColor = OxyColor.FromAColor(220, OxyColor.Parse(BeamColor.DEFAULT_COLOR_BEAM_1)),
            //    TextColor = OxyColor.FromAColor(220, OxyColor.Parse(BeamColor.DEFAULT_COLOR_BEAM_1)),
            //    //IsAxisVisible = true,
            //    //MajorStep = 90,
            //    //MinorStep = 0.5,
            //    //MajorTickSize = 0.1,
            //    Unit = "deg C",
            //    Key = "temp"
            //});
            //SensorSetPlot.SetLeftAxis(new LinearAxis
            //{
            //    Position = AxisPosition.Left,
            //    PositionTier = 1,
            //    //MajorStep = 1,
            //    Minimum = 0,
            //    //Maximum = 360,
            //    TicklineColor = OxyColors.White,
            //    MajorGridlineStyle = LineStyle.Solid,
            //    MinorGridlineStyle = LineStyle.Solid,
            //    MajorGridlineColor = OxyColor.FromAColor(40, OxyColors.White),
            //    MinorGridlineColor = OxyColor.FromAColor(20, OxyColors.White),
            //    TickStyle = OxyPlot.Axes.TickStyle.Inside,                               // Put tick lines inside the plot
            //    MinimumPadding = 0,                                                 // Start at axis edge   
            //    MaximumPadding = 0,                                                 // Start at axis edge
            //    TitleColor = OxyColor.FromAColor(220, OxyColor.Parse(BeamColor.DEFAULT_COLOR_BEAM_2)),
            //    TextColor = OxyColor.FromAColor(220, OxyColor.Parse(BeamColor.DEFAULT_COLOR_BEAM_2)),
            //    //IsAxisVisible = true,
            //    //MajorStep = 90,
            //    //MinorStep = 0.5,
            //    //MajorTickSize = 0.1,
            //    Unit = "m",
            //    Key = "vh"
            //});

            #endregion

            #region Velocity Plot

            // Vel Set Plot
            VelSetPlot = new TimeSeriesPlotViewModel(new SeriesType(new DataSource(DataSource.eSource.Waves), new BaseSeriesType(BaseSeriesType.eBaseSeriesType.Base_Waves_Velocity_Series)));
            VelSetPlot.Plot.LegendPosition = LegendPosition.BottomCenter;
            VelSetPlot.Plot.LegendOrientation = LegendOrientation.Horizontal;
            VelSetPlot.Plot.LegendPlacement = LegendPlacement.Outside;
            VelSetPlot.Plot.LegendMargin = 0;
            VelSetPlot.Plot.Title = "Velocity Series";
            //VelSetPlot.ClearAllAxis();
            //VelSetPlot.SetAxis(new LinearAxis
            //{
            //    Position = AxisPosition.Bottom,
            //    //MajorStep = 1,
            //    Minimum = 0,
            //    //Maximum = _maxDataSets,
            //    TicklineColor = OxyColors.White,
            //    MajorGridlineStyle = LineStyle.Solid,
            //    MinorGridlineStyle = LineStyle.Solid,
            //    MajorGridlineColor = OxyColor.FromAColor(40, OxyColors.White),
            //    MinorGridlineColor = OxyColor.FromAColor(20, OxyColors.White),
            //    TickStyle = OxyPlot.Axes.TickStyle.Inside,                               // Put tick lines inside the plot
            //    MinimumPadding = 0,                                                 // Start at axis edge   
            //    MaximumPadding = 0,                                                 // Start at axis edge
            //    //IsAxisVisible = true,
            //    MajorStep = 1,
            //    MinorStep = 0.5,
            //    Unit = "Days"
            //});
            //VelSetPlot.SetLeftAxis(new LinearAxis
            //{
            //    Position = AxisPosition.Left,
            //    //MajorStep = 1,
            //    Minimum = 0,
            //    //Maximum = 360,
            //    TicklineColor = OxyColors.White,
            //    MajorGridlineStyle = LineStyle.Solid,
            //    MinorGridlineStyle = LineStyle.Solid,
            //    MajorGridlineColor = OxyColor.FromAColor(40, OxyColors.White),
            //    MinorGridlineColor = OxyColor.FromAColor(20, OxyColors.White),
            //    TickStyle = OxyPlot.Axes.TickStyle.Inside,                               // Put tick lines inside the plot
            //    MinimumPadding = 0,                                                 // Start at axis edge   
            //    MaximumPadding = 0,                                                 // Start at axis edge
            //    TitleColor = OxyColor.FromAColor(220, OxyColor.Parse(BeamColor.DEFAULT_COLOR_BEAM_0)),
            //    TextColor = OxyColor.FromAColor(220, OxyColor.Parse(BeamColor.DEFAULT_COLOR_BEAM_0)),
            //    //IsAxisVisible = true,
            //    //MajorStep = 90,
            //    //MinorStep = 0.5,
            //    //MajorTickSize = 0.1,
            //    Unit = "m/s",
            //    Key = "uvMag"
            //});
            //VelSetPlot.SetLeftAxis(new LinearAxis
            //{
            //    Position = AxisPosition.Right,
            //    PositionTier = 1,
            //    //MajorStep = 1,
            //    Minimum = -180.0,
            //    Maximum = 180.0,
            //    TicklineColor = OxyColors.White,
            //    MajorGridlineStyle = LineStyle.Solid,
            //    MinorGridlineStyle = LineStyle.Solid,
            //    MajorGridlineColor = OxyColor.FromAColor(40, OxyColors.White),
            //    MinorGridlineColor = OxyColor.FromAColor(20, OxyColors.White),
            //    TickStyle = OxyPlot.Axes.TickStyle.Inside,                               // Put tick lines inside the plot
            //    MinimumPadding = 0,                                                 // Start at axis edge   
            //    MaximumPadding = 0,                                                 // Start at axis edge
            //    TitleColor = OxyColor.FromAColor(220, OxyColor.Parse(BeamColor.DEFAULT_COLOR_BEAM_1)),
            //    TextColor = OxyColor.FromAColor(220, OxyColor.Parse(BeamColor.DEFAULT_COLOR_BEAM_1)),
            //    //IsAxisVisible = true,
            //    MajorStep = 90,
            //    //MinorStep = 0.5,
            //    //MajorTickSize = 0.1,
            //    Unit = "Deg",
            //    Key = "uvDir"
            //});

            #endregion
        }

        #endregion

        #region Update Plots

        /// <summary>
        /// Add the data to the plots.
        /// </summary>
        /// <param name="ensemble">Ensemble to get the data.</param>
        private void AddSeries(Waves.WavesRecord record)
        {
            // Pressure
            UpdatePressurePlot(record);

            // East Velocity
            UpdateEastVelocityPlot(record);

            // North Velocity
            UpdateNorthVelocityPlot(record);

            // FFT 
            UpdateFftPlot(record);

            // Frequency 
            UpdateFrequencyPlot(record);

            // Period
            UpdatePeriodPlot(record);

            // Spectrum
            UpdateSpectrumPlot(record);
        }

        #endregion

        #region Clear Plots

        /// <summary>
        /// Clear all the values for the plots.
        /// </summary>
        public void ClearPlots()
        {
            // Clear plots
            PressurePlot.ClearSeries();
            EastVelocityPlot.ClearSeries();
            NorthVelocityPlot.ClearSeries();
            FftPlot.ClearSeries();
            FrequencyPlot.ClearSeries();
            PeriodPlot.ClearSeries();
            SpectrumPlot.ClearSeries();
        }

        #endregion

        #region Pressure 

        /// <summary>
        /// Update the pressure plot.
        /// </summary>
        /// <param name="record">Record to get the latest data.</param>
        private void UpdatePressurePlot(Waves.WavesRecord record)
        {
            // Create a series
            var series = new TimeSeries("Pressure", DEFAULT_SELECTEDBIN_0_1);
            var hSeries = new TimeSeries("Height", DEFAULT_SELECTEDBIN_1_1);

            // Add the data to the series
            for (int x = 0; x < record.WaveSamples.Count; x++)
            {

                series.Points.Add(new DataPoint(x, record.WaveSamples[x].VertPressure));
                hSeries.Points.Add(new DataPoint(x, record.WaveSamples[x].VertRangeTracking));
                //hSeries.Points.Add(new DataPoint(x, record.WaveSamples[x].VertBeamHeight));
            }

            // Add a series to the plot
            PressurePlot.AddSeries(series);
            PressurePlot.AddSeries(hSeries);
        }

        #endregion

        #region East Velocity

        /// <summary>
        /// Update the East Velocity plot.
        /// </summary>
        /// <param name="record">Record to get the latest data.</param>
        private void UpdateEastVelocityPlot(Waves.WavesRecord record)
        {
            // Check if data exist
            if (record.WaveSamples.Count < 0)
            {
                return;
            }

            // For each selected bin, create a line series
            for (int selectedBin = 0; selectedBin < record.WaveSamples[0].EastTransformData.GetLength(0); selectedBin++)
            {
                // Create a series
                OxyColor seriesColor;
                if (selectedBin == 0)
                {
                    seriesColor = DEFAULT_SELECTEDBIN_0_1;
                }
                else if(selectedBin == 1)
                {
                    seriesColor = DEFAULT_SELECTEDBIN_0_2;
                }
                else if (selectedBin == 2)
                {
                    seriesColor = DEFAULT_SELECTEDBIN_1_3;
                }
                else
                {
                    seriesColor = DEFAULT_SELECTEDBIN_1_1;
                }
                var series = new TimeSeries("Bin " + selectedBin.ToString(), seriesColor);

                // Add the data to the series
                for (int x = 0; x < record.WaveSamples.Count; x++)
                {
                    if (record.WaveSamples[x].EastTransformData[selectedBin] != DataSet.Ensemble.BAD_VELOCITY)
                    {
                        series.Points.Add(new DataPoint(x, record.WaveSamples[x].EastTransformData[selectedBin]));
                    }
                }

                // Add the series to the plot
                EastVelocityPlot.AddSeries(series);
            }
        }

        #endregion

        #region North Velocity

        /// <summary>
        /// Update the North Velocity plot.
        /// </summary>
        /// <param name="record">Record to get the latest data.</param>
        private void UpdateNorthVelocityPlot(Waves.WavesRecord record)
        {
            // Check if data exist
            if (record.WaveSamples.Count < 0)
            {
                return;
            }

            // For each selected bin, create a line series
            for (int selectedBin = 0; selectedBin < record.WaveSamples[0].NorthTransformData.GetLength(0); selectedBin++)
            {
                // Create a series
                OxyColor seriesColor;
                if (selectedBin == 0)
                {
                    seriesColor = DEFAULT_SELECTEDBIN_0_1;
                }
                else if (selectedBin == 1)
                {
                    seriesColor = DEFAULT_SELECTEDBIN_1_1;
                }
                else if (selectedBin == 2)
                {
                    seriesColor = DEFAULT_SELECTEDBIN_2_1;
                }
                else
                {
                    seriesColor = DEFAULT_SELECTEDBIN_0_2;
                }
                var series = new TimeSeries("Bin " + selectedBin.ToString(), seriesColor);

                // Add the data to the series
                for (int x = 0; x < record.WaveSamples.Count; x++)
                {
                    if (record.WaveSamples[x].NorthTransformData[selectedBin] != DataSet.Ensemble.BAD_VELOCITY)
                    {
                        series.Points.Add(new DataPoint(x, record.WaveSamples[x].NorthTransformData[selectedBin]));
                    }
                }

                // Add a series to the plot
                NorthVelocityPlot.AddSeries(series);
            }
        }

        #endregion

        #region FFT

        /// <summary>
        /// Update the FFT plot.
        /// </summary>
        /// <param name="record">Records to get the number of samples.</param>
        private void UpdateFftPlot(Waves.WavesRecord record)
        {
            // Create P series
            var pSeries = new TimeSeries("p", DEFAULT_SELECTEDBIN_2_1);

            // Add the data to the series
            for (int x = 0; x < record.WaveSamples.Count/2; x++)
            {
                // Plot only good values
                if (!double.IsNaN(SelectedBurst.pSpectrum[x].Magnitude))
                {
                    pSeries.Points.Add(new DataPoint(x, SelectedBurst.pSpectrum[x].Magnitude));
                }
            }

            // Create H series
            var hSeries = new TimeSeries("h", DEFAULT_SELECTEDBIN_2_2);

            // Add the data to the series
            for (int x = 0; x < record.WaveSamples.Count/2; x++)
            {
                // Plot only good values
                if (!double.IsNaN(SelectedBurst.hSpectrum[x].Magnitude))
                {
                    hSeries.Points.Add(new DataPoint(x, SelectedBurst.hSpectrum[x].Magnitude));
                }
            }

            for (int bin = 0; bin < record.WaveCellDepth.Length; bin++)
            {
                // Create U series
                OxyColor seriesColor;
                if (bin == 0)
                {
                    seriesColor = DEFAULT_SELECTEDBIN_1_1;
                }
                else if (bin == 1)
                {
                    seriesColor = DEFAULT_SELECTEDBIN_1_2;
                }
                else
                {
                    seriesColor = DEFAULT_SELECTEDBIN_1_3;
                }
                var uSeries = new TimeSeries("u Bin " + bin, seriesColor);

                // Add the data to the series
                for (int x = 0; x < record.WaveSamples.Count / 2; x++)
                {
                    // Plot only good values
                    if (!double.IsNaN(SelectedBurst.eastSpectrum[bin][x].Magnitude))
                    {
                        uSeries.Points.Add(new DataPoint(x, SelectedBurst.eastSpectrum[bin][x].Magnitude));
                    }
                }

                // Create V series
                OxyColor vSeriesColor;
                if (bin == 0)
                {
                    vSeriesColor = DEFAULT_SELECTEDBIN_0_1;
                }
                else if (bin == 1)
                {
                    vSeriesColor = DEFAULT_SELECTEDBIN_0_2;
                }
                else
                {
                    vSeriesColor = DEFAULT_SELECTEDBIN_0_3;
                }
                var vSeries = new TimeSeries("v Bin " + bin, vSeriesColor);

                // Add the data to the series
                for (int x = 0; x < record.WaveSamples.Count / 2; x++)
                {
                    // Plot only good values
                    if (!double.IsNaN(SelectedBurst.northSpectrum[bin][x].Magnitude))
                    {
                        vSeries.Points.Add(new DataPoint(x, SelectedBurst.northSpectrum[bin][x].Magnitude));
                    }
                }

                FftPlot.AddSeries(uSeries);
                FftPlot.AddSeries(vSeries);
            }

            // Add a series to the plot
            FftPlot.AddSeries(pSeries);
            FftPlot.AddSeries(hSeries);
        }

        #endregion

        #region Frequency

        /// <summary>
        /// Update the FFT plot.
        /// </summary>
        /// <param name="record">Records to get the number of samples.</param>
        private void UpdateFrequencyPlot(Waves.WavesRecord record)
        {
            // Create P series
            var dirSeries = new TimeSeries("Dir", OxyColor.FromAColor(220, OxyColor.Parse(BeamColor.DEFAULT_COLOR_BEAM_1)));
            dirSeries.YAxisKey = "dir";

            var sprR2Series = new TimeSeries("SprR2", OxyColor.FromAColor(220, OxyColor.Parse(BeamColor.DEFAULT_COLOR_BEAM_3)));
            sprR2Series.YAxisKey = "spr";

            var sprR1Series = new TimeSeries("SprR1", OxyColor.FromAColor(220, OxyColor.Parse(BeamColor.DEFAULT_COLOR_BEAM_2)));
            sprR1Series.YAxisKey = "spr";

            var spSeries = new TimeSeries("Sp(m)", OxyColor.FromAColor(220, OxyColor.Parse(BeamColor.DEFAULT_COLOR_BEAM_0)));
            spSeries.YAxisKey = "sp";

            // Add the data to the series
            for (int x = 0; x < SelectedBurst.WavesBands[0] / 2; x++)
            {
                // Plot only good values
                spSeries.Points.Add(new DataPoint(SelectedBurst.WavesBandedFrequency[x], SelectedBurst.WavesSp[x]));
                dirSeries.Points.Add(new DataPoint(SelectedBurst.WavesBandedFrequency[x], SelectedBurst.WavesDir[x]));
                sprR2Series.Points.Add(new DataPoint(SelectedBurst.WavesBandedFrequency[x], SelectedBurst.WavesSpread[x]));
                sprR1Series.Points.Add(new DataPoint(SelectedBurst.WavesBandedFrequency[x], SelectedBurst.WavesSpreadR1[x]));
            }

            // Add a series to the plot
            FrequencyPlot.AddSeries(spSeries);
            FrequencyPlot.AddSeries(dirSeries);
            FrequencyPlot.AddSeries(sprR2Series);
            FrequencyPlot.AddSeries(sprR1Series);
        }

        #endregion

        #region Period

        /// <summary>
        /// Update the Period plot.
        /// </summary>
        /// <param name="record">Records to get the number of samples.</param>
        private void UpdatePeriodPlot(Waves.WavesRecord record)
        {
            // Create P series
            var dirSeries = new TimeSeries("Dir", OxyColor.FromAColor(220, OxyColor.Parse(BeamColor.DEFAULT_COLOR_BEAM_1)));
            dirSeries.YAxisKey = "dir";

            var sprR2Series = new TimeSeries("SprR2", OxyColor.FromAColor(220, OxyColor.Parse(BeamColor.DEFAULT_COLOR_BEAM_3)));
            sprR2Series.YAxisKey = "spr";

            var sprR1Series = new TimeSeries("SprR1", OxyColor.FromAColor(220, OxyColor.Parse(BeamColor.DEFAULT_COLOR_BEAM_2)));
            sprR1Series.YAxisKey = "spr";

            var spSeries = new TimeSeries("Sp(m)", OxyColor.FromAColor(220, OxyColor.Parse(BeamColor.DEFAULT_COLOR_BEAM_0)));
            spSeries.YAxisKey = "sp";

            // Add the data to the series
            for (int x = 0; x < SelectedBurst.WavesBands[0] / 2; x++)
            {
                // Plot only good values
                spSeries.Points.Add(new DataPoint(SelectedBurst.WavesBandedFrequency[x] * 100.0, SelectedBurst.WavesSp[x]));
                dirSeries.Points.Add(new DataPoint(SelectedBurst.WavesBandedPeriod[x], SelectedBurst.WavesDir[x]));
                sprR2Series.Points.Add(new DataPoint(SelectedBurst.WavesBandedPeriod[x], SelectedBurst.WavesSpread[x]));
                sprR1Series.Points.Add(new DataPoint(SelectedBurst.WavesBandedPeriod[x], SelectedBurst.WavesSpreadR1[x]));
            }

            // Add a series to the plot
            PeriodPlot.AddSeries(spSeries);
            PeriodPlot.AddSeries(dirSeries);
            PeriodPlot.AddSeries(sprR2Series);
            PeriodPlot.AddSeries(sprR1Series);
        }

        #endregion

        #region Spectrum

        /// <summary>
        /// Update the Spectrum plot.
        /// </summary>
        /// <param name="record">Records to get the number of samples.</param>
        private void UpdateSpectrumPlot(Waves.WavesRecord record)
        {
            // Create P series
            var pSeries = new TimeSeries("p", OxyColor.FromAColor(220, OxyColor.Parse(BeamColor.DEFAULT_COLOR_BEAM_0)));
            var hSeries = new TimeSeries("h", OxyColor.FromAColor(220, OxyColor.Parse(BeamColor.DEFAULT_COLOR_BEAM_3)));
            var uvSeries = new TimeSeries("v", OxyColor.FromAColor(220, OxyColors.Salmon));

            //var spSeries = new WavesTimeSeries();
            //spSeries.Title = "Sp(m)";
            //spSeries.Color = OxyColor.FromAColor(220, OxyColor.Parse(BeamColor.DEFAULT_COLOR_BEAM_3));

            // Add the data to the series
            for (int x = 0; x < SelectedBurst.WavesBands[0] / 2; x++)
            {
                // Plot only good values
                //spSeries.Points.Add(new DataPoint(_rtiWaves.WavesBandedFrequency[x], _rtiWaves.WavesSp[x]));
                pSeries.Points.Add(new DataPoint(SelectedBurst.WavesBandedFrequency[x] / 100.0, SelectedBurst.pWavesBandedSpectrum[x]));
                uvSeries.Points.Add(new DataPoint(SelectedBurst.WavesBandedPeriod[x] / 100.0, SelectedBurst.uvWavesBandedSpectrum[x]));
                hSeries.Points.Add(new DataPoint(SelectedBurst.WavesBandedPeriod[x] / 100.0, SelectedBurst.hWavesBandedSpectrum[x]));
            }
      
            for (int bin = 0; bin < record.WaveCellDepth.Length; bin++)
            {
                OxyColor vSeriesColor;
                OxyColor uSeriesColor;
                switch(bin)
                {
                    default:
                        vSeriesColor = OxyColor.FromAColor(220, OxyColors.OrangeRed);
                        uSeriesColor = OxyColor.FromAColor(220, OxyColors.SandyBrown);
                        break;
                    case 0:
                        vSeriesColor = OxyColor.FromAColor(220, OxyColor.Parse(BeamColor.DEFAULT_COLOR_BEAM_2));
                        uSeriesColor = OxyColor.FromAColor(220, OxyColor.Parse(BeamColor.DEFAULT_COLOR_BEAM_1));
                        break;
                    case 1:
                        vSeriesColor = OxyColor.FromAColor(220, OxyColors.PaleGoldenrod);
                        uSeriesColor = OxyColor.FromAColor(220, OxyColors.PaleGreen);
                        break;
                    case 2:
                        vSeriesColor = OxyColor.FromAColor(220, OxyColors.PaleTurquoise);
                        uSeriesColor = OxyColor.FromAColor(220, OxyColors.PeachPuff);
                        break;
                }

                var uSeries = new TimeSeries("u[" + bin + "]", uSeriesColor);
                var vSeries = new TimeSeries("v[" + bin + "]", vSeriesColor);


                // Add the data to the series
                for (int x = 0; x < SelectedBurst.WavesBands[bin] / 2; x++)
                {
                    uSeries.Points.Add(new DataPoint(SelectedBurst.WavesBandedPeriod[x] / 100.0, SelectedBurst.uWavesBandedSpectrum[bin][x]));
                    vSeries.Points.Add(new DataPoint(SelectedBurst.WavesBandedPeriod[x] / 100.0, SelectedBurst.vWavesBandedSpectrum[bin][x]));
                }

                SpectrumPlot.AddSeries(uSeries);
                SpectrumPlot.AddSeries(vSeries);
            }

            // Add a series to the plot
            SpectrumPlot.AddSeries(pSeries);
            SpectrumPlot.AddSeries(hSeries);
            SpectrumPlot.AddSeries(uvSeries);
        }

        #endregion

        #region Wave Set

        /// <summary>
        /// Update the Wave set plot.
        /// </summary>
        /// <param name="burst">Waves Burst.</param>
        private void UpdateWaveSetPlot(Waves.RtiWaves burst)
        {
            float range = ((float)(burst.Record.FirstSampleTime - WavesBurstList.First().Record.FirstSampleTime));        // What portion of a day = range/24;

            string pSeriesKey = "period";
            string hSeriesKey = "Hs";
            string dirSeriesKey = "dir";


            if (WaveSetPlot.Plot.Series.Count <= 0)
            {
                //for (int bin = 0; bin < burst.Record.WaveCellDepth.Length; bin++)
                //{

                    var hSeries = new TimeSeries(hSeriesKey, OxyColor.FromAColor(220, OxyColor.Parse(BeamColor.DEFAULT_COLOR_BEAM_0)));
                    hSeries.YAxisKey = hSeriesKey;

                    var dirSeries = new TimeSeries("Dir(deg)",OxyColor.FromAColor(220, OxyColor.Parse(BeamColor.DEFAULT_COLOR_BEAM_1)) );
                    dirSeries.YAxisKey = dirSeriesKey;

                    // Create P series
                    var pSeries = new TimeSeries("Period(s)", OxyColor.FromAColor(220, OxyColor.Parse(BeamColor.DEFAULT_COLOR_BEAM_2)));
                    pSeries.YAxisKey = pSeriesKey;

                    // Add the series
                    pSeries.Points.Add(new DataPoint(range, burst.WavesPeakPeriod[0]));
                    hSeries.Points.Add(new DataPoint(range, burst.WavesHs[0]));
                    dirSeries.Points.Add(new DataPoint(range, burst.WavesPeakDir[0]));

                    // Add a series to the plot
                    WaveSetPlot.AddSeries(pSeries);
                    WaveSetPlot.AddSeries(hSeries);
                    WaveSetPlot.AddSeries(dirSeries);
                //}
            }
            else
            {
                // Find the series and update them
                foreach (TimeSeries series in WaveSetPlot.Plot.Series)
                {
                    // Period
                    if (series.YAxisKey == pSeriesKey)
                    {
                        series.Points.Add(new DataPoint(range, burst.WavesPeakPeriod[0]));
                    }

                    // Height
                    if (series.YAxisKey == hSeriesKey)
                    {
                        series.Points.Add(new DataPoint(range, burst.WavesHs[0]));
                    }

                    // Peak Direction
                    if (series.YAxisKey == dirSeriesKey)
                    {
                        series.Points.Add(new DataPoint(range, burst.WavesPeakDir[0]));
                    }
                }

                // After the line series have been updated
                // Refresh the plot with the latest data.
                WaveSetPlot.Plot.InvalidatePlot(true);

            }
        }

        #endregion

        #region Sensor Set

        /// <summary>
        /// Update the Sensor set plot.
        /// </summary>
        /// <param name="burst">RTI Waves burst.</param>
        private void UpdateSensorSetPlot(Waves.RtiWaves burst)
        {
            float range = ((float)(burst.Record.FirstSampleTime - WavesBurstList.First().Record.FirstSampleTime));        // What portion of a day = range/24

            string pSeriesKey = "pressure";
            string tempSeriesKey = "temp";
            string vhSeriesKey = "vh";


            if (SensorSetPlot.Plot.Series.Count <= 0)
            {
                // Create P series
                var pSeries = new TimeSeries("Pressure(m)", OxyColor.FromAColor(220, OxyColor.Parse(BeamColor.DEFAULT_COLOR_BEAM_0)));
                pSeries.YAxisKey = pSeriesKey;

                var tempSeries = new TimeSeries("Temp(deg)", OxyColor.FromAColor(220, OxyColor.Parse(BeamColor.DEFAULT_COLOR_BEAM_1)));
                tempSeries.YAxisKey = tempSeriesKey;

                var vhSeries = new TimeSeries("VH(m)", OxyColor.FromAColor(220, OxyColor.Parse(BeamColor.DEFAULT_COLOR_BEAM_2)));
                vhSeries.YAxisKey = vhSeriesKey;

                //for (int bin = 0; bin < record.WaveCellDepth.Length; bin++)
                //{
                pSeries.Points.Add(new DataPoint(range, burst.WavesRecordAveragePressure[0]));
                tempSeries.Points.Add(new DataPoint(range, burst.WavesRecordAverageTemperature[0]));
                vhSeries.Points.Add(new DataPoint(range, burst.WavesRecordAverageHeight[0]));
                //}


                // Add a series to the plot
                SensorSetPlot.AddSeries(pSeries);
                SensorSetPlot.AddSeries(tempSeries);
                SensorSetPlot.AddSeries(vhSeries);
            }
            else
            {
                // Find the series and update them
                foreach (TimeSeries series in SensorSetPlot.Plot.Series)
                {
                    // Period
                    if (series.YAxisKey == pSeriesKey)
                    {
                        series.Points.Add(new DataPoint(range, burst.WavesRecordAveragePressure[0]));
                    }

                    // Height
                    if (series.YAxisKey == tempSeriesKey)
                    {
                        series.Points.Add(new DataPoint(range, burst.WavesRecordAverageTemperature[0]));
                    }

                    // Peak Direction
                    if (series.YAxisKey == vhSeriesKey)
                    {
                        series.Points.Add(new DataPoint(range, burst.WavesRecordAverageHeight[0]));
                    }
                }

                // After the line series have been updated
                // Refresh the plot with the latest data.
                SensorSetPlot.Plot.InvalidatePlot(true);

            }
        }

        #endregion

        #region Velocity Set

        /// <summary>
        /// Update the Velocity set plot.
        /// </summary>
        /// <param name="burst">RTI Waves burst.</param>
        private void UpdateVelocitySetPlot(Waves.RtiWaves burst)
        {
            float range = ((float)(burst.Record.FirstSampleTime - WavesBurstList.First().Record.FirstSampleTime));        // What portion of a day = range/24;

            string uvMagSeriesKey = "uvMag";
            string uvDirSeriesKey = "uvDir";


            if (VelSetPlot.Plot.Series.Count <= 0)
            {
                for (int bin = 0; bin < burst.Record.WaveCellDepth.Length; bin++)
                {
                    OxyColor uvMagSeriesColor = DEFAULT_SELECTEDBIN_0_1;
                    OxyColor uvDirColor = DEFAULT_SELECTEDBIN_0_2;
                    if(bin == 0)
                    {
                        uvMagSeriesColor = DEFAULT_SELECTEDBIN_0_1;
                        uvDirColor = DEFAULT_SELECTEDBIN_0_2;
                    }
                    if(bin == 1)
                    {
                        uvMagSeriesColor = DEFAULT_SELECTEDBIN_1_1;
                        uvDirColor = DEFAULT_SELECTEDBIN_1_2;
                    }
                    if (bin == 2)
                    {
                        uvMagSeriesColor = DEFAULT_SELECTEDBIN_2_1;
                        uvDirColor = DEFAULT_SELECTEDBIN_2_2;
                    }

                    // Create UV Mag series
                    var uvMagSeries = new TimeSeries("uvMag[" + bin + "](m/s)", uvMagSeriesColor);
                    uvMagSeries.YAxisKey = uvMagSeriesKey;

                    // Create UV Direction
                    var uvDir = new TimeSeries("uvDir[" + bin + "](Deg)", uvDirColor);
                    uvDir.YAxisKey = uvDirSeriesKey;

                    // Add points to series
                    uvMagSeries.Points.Add(new DataPoint(range, burst.WavesAverageUVmag[bin]));
                    uvDir.Points.Add(new DataPoint(range, burst.WavesAverageUVdir[bin]));

                    // Add a series to the plot
                    VelSetPlot.AddSeries(uvMagSeries);
                    VelSetPlot.AddSeries(uvDir);
                }
            }
            else
            {
                // Find the series and update them
                foreach (TimeSeries series in VelSetPlot.Plot.Series)
                {
                    switch(series.Title)
                    {
                        case "uvMag[0](m/s)":
                            series.Points.Add(new DataPoint(range, burst.WavesAverageUVmag[0]));
                            break;
                        case "uvMag[1](m/s)":
                            series.Points.Add(new DataPoint(range, burst.WavesAverageUVmag[1]));
                            break;
                        case "uvMag[2](m/s)":
                            series.Points.Add(new DataPoint(range, burst.WavesAverageUVmag[2]));
                            break;
                        case "uvDir[0](Deg)":
                            series.Points.Add(new DataPoint(range, burst.WavesAverageUVdir[0]));
                            break;
                        case "uvDir[1](Deg)":
                            series.Points.Add(new DataPoint(range, burst.WavesAverageUVdir[1]));
                            break;
                        case "uvDir[2](Deg)":
                            series.Points.Add(new DataPoint(range, burst.WavesAverageUVdir[2]));
                            break;

                        default:
                            break;
                    }
                }

                // After the line series have been updated
                // Refresh the plot with the latest data.
                VelSetPlot.Plot.InvalidatePlot(true);

            }
        }

        #endregion

        #endregion

        #region RTI Waves

        /// <summary>
        /// Recreate the waves processor.
        /// Temporary store the options so when
        /// the processor is recreated, they can be
        /// reset.
        /// </summary>
        /// <param name="waveBands">This value changes for each burst, so get the value when started.</param>
        private void RecreateRtiWaves(int waveBands)
        {
            // Set default values
            int wavesBands = Waves.RtiWaves.DEFAULT_WAVES_MAX_BANDS;
            double wavesMinFreq = Waves.RtiWaves.DEFAULT_MIN_FREQ;
            double wavesMaxScaleFactor = Waves.RtiWaves.DEFAULT_MAX_SCALE_FACTOR;
            double wavesMinHeight = Waves.RtiWaves.DEFAULT_MIN_HEIGHT;
            bool isHeightSensorBeam = Waves.RtiWaves.DEFAULT_IS_HEIGHT_SENSOR_BEAM;

            if (_SelectedBurst != null)
            {
                // Get all the old settings
                wavesBands = waveBands;
                wavesMinFreq = _SelectedBurst.WavesMinFreq;
                wavesMaxScaleFactor = _SelectedBurst.WavesMaxScaleFactor;
                wavesMinHeight = _SelectedBurst.WavesMinHeight;
                isHeightSensorBeam = _SelectedBurst.IsHeightSensorBeam;
            }

            // Recreete the processor
            _SelectedBurst = new Waves.RtiWaves();

            // Reset the settings
            _SelectedBurst.NumWavesBands = wavesBands;
            _SelectedBurst.WavesMinFreq = wavesMinFreq;
            _SelectedBurst.WavesMaxScaleFactor = wavesMaxScaleFactor;
            _SelectedBurst.WavesMinHeight = wavesMinHeight;
            _SelectedBurst.IsHeightSensorBeam = isHeightSensorBeam;
        }

        #endregion

        #region Import Matlab Files

        /// <summary>
        /// Import the data the RTI data and export
        /// to matlab waves format.
        /// </summary>
        private async void ExecuteImportMatlabWaves()
        {
            //bool OK = false;
            System.Windows.Forms.OpenFileDialog openFileDialog1 = new System.Windows.Forms.OpenFileDialog();

            openFileDialog1.InitialDirectory = "";
            openFileDialog1.Filter = "mat files (*.mat)|*.mat|All files (*.*)|*.*";
            openFileDialog1.FilterIndex = 0;
            openFileDialog1.RestoreDirectory = true;
            openFileDialog1.Multiselect = true;

            if (openFileDialog1.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                // Read in Matlab file
                await ReadInMatlabFile(openFileDialog1.FileNames);
            }
        }

        /// <summary>
        /// Read in the matlab file and load it.
        /// </summary>
        /// <param name="filePaths">File paths to the wave record.</param>
        /// <returns></returns>
        private async Task ReadInMatlabFile(string[] filePaths)
        {
            try
            {
                // Clear any previous data
                Clear();

                // Store this value because it changes for each file
                int tempWaveBands = _options.NumWavesBands;

                for (int x = 0; x < filePaths.Length; x++)
                {
                    // Recreate 
                    RecreateRtiWaves(tempWaveBands);

                    // Import a matlab file
                    await Task.Run(() => SelectedBurst.ImportMatlabWaves(true, filePaths[x]));

                    // List the burst to the list
                    await System.Windows.Application.Current.Dispatcher.BeginInvoke(new System.Action(() => { WavesBurstList.Add(SelectedBurst); }));

                    // Update the display with the latest data
                    UpdateProperties();
                }

                // Display the record
                await Task.Run(() => DisplayData(SelectedBurst.Record));

                // Update the wave set plots
                foreach (var burst in WavesBurstList)
                {
                    // Wave Set
                    UpdateWaveSetPlot(burst);

                    // Sensor Set
                    UpdateSensorSetPlot(burst);

                    // Velocity Set
                    UpdateVelocitySetPlot(burst);
                }
            }
            catch(Exception e)
            {
                log.Error("Error reading in Matlab files.", e);
            }
        }

        #endregion

        #region Update Properties

        /// <summary>
        /// Update the properties.
        /// </summary>
        private void UpdateProperties()
        {
            this.NotifyOfPropertyChange(() => this.WavesRecordNumber);
            this.NotifyOfPropertyChange(() => this.WavesProc);
            this.NotifyOfPropertyChange(() => this.IsHeightSensorBeam);
            this.NotifyOfPropertyChange(() => this.WavesBurstList);
            this.NotifyOfPropertyChange(() => this.SelectedBurst);
        }

        #endregion

        #region EventHandlers

        /// <summary>
        /// Receive handler for a Waves Record.
        /// </summary>
        /// <param name="message">Waves Record event.</param>
        public void Handle(WavesRecordEvent message)
        {
            // Display the data
            Task.Run(() => DisplayData(message.Record));
        }

        #endregion

        /// <summary>
        /// Eventhandler for a wave record file path.
        /// </summary>
        /// <param name="message">Wave record file path.</param>
        public void Handle(WavesRecordFileEvent message)
        {
            Task.Run(() => ReadInMatlabFile(message.RecordFilePath));
        }
    }
}
