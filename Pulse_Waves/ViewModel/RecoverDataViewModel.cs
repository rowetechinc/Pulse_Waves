/*
 * Copyright © 2011 
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
 * 08/15/2012      RC          2.13       Initial coding
 * 10/17/2012      RC          2.15       Stop pinging when downloading starts.
 * 10/19/2012      RC          2.15       When downloading more than 1 file, wait before starting the next file to allow the RS-485 to transition.
 * 01/22/2013      RC          2.17       Made AdcpStatus an object and not an enum.
 * 03/20/2013      RC          2.19       In CancelDownload() when trying to delete the file, i added a try/catch block.
 * 07/29/2013      RC          3.0.6      Used ReactiveAsyncCommand instead of background workers.
 * 11/25/2013      RC          3.2.0      Removed AdcpSerialPort and only use AdcpConnection so it can also use ethernet.
 * 08/07/2014      RC          4.0.0      Updated ReactiveCommand to 6.0.
 * 01/14/2015      RC          0.0.2      Made the bin selections capable of being disabled to be backwards compatible.
 * 11/24/2015      RC          1.1.1      Select ENS and BIN as default options for playback files.
 * 
 */

namespace RTI
{
    using log4net;
    using System.Threading;
    using System.ComponentModel;
    using System.Collections.Generic;
    using System;
    using System.IO;
    using System.Windows.Input;
    using System.Windows;
    using RTI.Commands;
    using Caliburn.Micro;
    using ReactiveUI;
    using System.Threading.Tasks;
    using System.Windows.Forms;
    using System.Linq;

    /// <summary>
    /// Download data from the ADCP through the serial port.
    /// </summary> 
    public class RecoverDataViewModel : PulseViewModel
    {

        #region Variables

        // Setup logger
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        /// <summary>
        /// EventAggregator to handle passing events.
        /// </summary>
        private IEventAggregator _eventAggregator;

        /// <summary>
        /// Pulse manager.
        /// </summary>
        private PulseManager _pm;

        /// <summary>
        /// Options to save for the view model.
        /// </summary>
        private RecoverDataOptions _options;

        /// <summary>
        /// Flag to know if the download process was completed
        /// or a timeout occured.  When moving to the next file,
        /// i do not know if the we are continuing because a 
        /// file had completed a download or because the download
        /// timed out.  Set this flag to true whenever someone
        /// calles _eventWaitDownload.Set() is called.
        /// </summary>
        private bool _downloadComplete;

        #region Download

        /// <summary>
        /// Used to wait for each download to complete
        /// before starting the next download.
        /// </summary>
        private EventWaitHandle _eventWaitDownload;

        /// <summary>
        /// A flag that we are currently downloading data.
        /// This is mainly used to stop the serial output
        /// on the screen.  The data is coming in too fast
        /// and the screen is being refreshed to fast.  This
        /// will stop the screen update for serial data.
        /// </summary>
        private bool _isDownloadingData;

        /// <summary>
        /// List of files that failed download.
        /// </summary>
        private List<string> _downloadFailList;

        /// <summary>
        /// Flag to cancel the download process.
        /// </summary>
        private bool _cancelDownload;

        /// <summary>
        /// Connection to the ADCP.
        /// </summary>
        private AdcpConnection _adcpConn;

        #endregion

        #region Waves

        /// <summary>
        /// Default folder path.
        /// </summary>
        private const string DEFAULT_FOLDER_PATH = @"C:\RTI_Waves\";

        /// <summary>
        /// Encode the RTI binary data to a Matlab waves file.
        /// </summary>
        private RTI.Waves.RtiWavesEncoder _rtiWavesEncoder;

        /// <summary>
        /// Binary Codec for binary files.
        /// </summary>
        private AdcpBinaryCodecNew _adcpCodec;

        /// <summary>
        /// Event to cause the thread
        /// to go to sleep or wakeup.
        /// </summary>
        private EventWaitHandle _eventWaitData;

        /// <summary>
        /// Set a flag if the folder is set for the current import.
        /// </summary>
        private bool _folderSet;

        /// <summary>
        /// File path.
        /// </summary>
        private string _folderPath;

        /// <summary>
        /// Current file name being processed.
        /// </summary>
        private string _fileName;

        #endregion

        #endregion

        #region Properties

        #region Download

        /// <summary>
        /// Period of time to wait for the file to be downloaded before
        /// moving on to the next file.   If the download process hangs,
        /// it will wait this long to download the file.  If the download
        /// is taking to long, it will only wait this amount of time to
        /// download a file.
        /// </summary>
        public int DownloadTimeout
        {
            get { return _options.DownloadTimeout; }
            set
            {
                _options.DownloadTimeout = value;
                this.NotifyOfPropertyChange(() => this.DownloadTimeout);

                // Save the options
                SaveOptions();
            }
        }

        /// <summary>
        /// Total space on the ADCP SD card in megabytes.
        /// </summary>
        private string _downloadTotalSpace;
        /// <summary>
        /// Total space on the ADCP SD card in megabytes.
        /// </summary>
        public string DownloadTotalSpace
        {
            get { return _downloadTotalSpace; }
            set
            {
                _downloadTotalSpace = value;
                this.NotifyOfPropertyChange(() => this.DownloadTotalSpace);
            }
        }

        /// <summary>
        /// Used space on the ADCP SD card in megabytes.
        /// </summary>
        private string _downloadUsedSpace;
        /// <summary>
        /// Used space on the ADCP SD card in megabytes.
        /// </summary>
        public string DownloadUsedSpace
        {
            get { return _downloadUsedSpace; }
            set
            {
                _downloadUsedSpace = value;
                this.NotifyOfPropertyChange(() => this.DownloadUsedSpace);
            }
        }

        /// <summary>
        /// Directory to download the files
        /// from the ADCP to the user's computer.
        /// </summary>
        public string DownloadDirectory
        {
            get { return _options.DownloadDirectory; }
            set
            {
                _options.DownloadDirectory = value;
                this.NotifyOfPropertyChange(() => this.DownloadDirectory);

                // Save the options
                SaveOptions();
            }
        }

        /// <summary>
        /// Flag to overwrite the file if it exist.
        /// TRUE = Overwrite the file.
        /// FALSE = Do not download the file, it already exist.
        /// </summary>
        public bool OverwriteDownloadFiles
        {
            get { return _options.OverwriteDownloadFiles; }
            set
            {
                _options.OverwriteDownloadFiles = value;
                this.NotifyOfPropertyChange(() => this.OverwriteDownloadFiles);

                // Save the options
                SaveOptions();
            }
        }

        /// <summary>
        /// Set flag if the downloaded data should be parsed
        /// or just written to the file.
        /// TRUE = Parse data and write data to a file and database.
        /// FALSE  = Write data to file only.
        /// </summary>
        public bool ParseDownloadedData
        {
            get { return _options.ParseDownloadedData; }
            set
            {
                _options.ParseDownloadedData = value;
                this.NotifyOfPropertyChange(() => this.ParseDownloadedData);

                // Save the options
                SaveOptions();
            }
        }

        /// <summary>
        /// Use this flag to select all the files or
        /// unselect all the files.  This can be a 
        /// quick way to select multiple files.
        /// </summary>
        private bool _selectAllFiles;
        /// <summary>
        /// Use this flag to select all the files or
        /// unselect all the files.  This can be a 
        /// quick way to select multiple files.
        /// </summary>
        public bool SelectAllFiles
        {
            get { return _selectAllFiles; }
            set
            {
                _selectAllFiles = value;
                this.NotifyOfPropertyChange(() => this.SelectAllFiles);

                // Set the selection for the files
                SelectAllDownloadFiles(_selectAllFiles);
            }
        }

        /// <summary>
        /// File size for the current file being downloaded.
        /// </summary>
        private long _downloadFileSize;
        /// <summary>
        /// File size for the current file being downloaded.
        /// </summary>
        public long DownloadFileSize
        {
            get { return _downloadFileSize; }
            set
            {
                _downloadFileSize = value;
                this.NotifyOfPropertyChange(() => this.DownloadFileSize);
            }
        }

        /// <summary>
        /// Download progress.  This is the current
        /// number of bytes read from the file.
        /// </summary>
        private long _downloadFileProgress;
        /// <summary>
        /// Download progress.  This is the current
        /// number of bytes read from the file.
        /// </summary>
        public long DownloadFileProgress
        {
            get { return _downloadFileProgress; }
            set
            {
                _downloadFileProgress = value;
                this.NotifyOfPropertyChange(() => this.DownloadFileProgress);
                this.NotifyOfPropertyChange(() => this.DownloadFileProgressPretty);
            }
        }

        /// <summary>
        /// A pretty version of the File progress which will
        /// show the best scale for the files downloaded.
        /// </summary>
        public string DownloadFileProgressPretty
        {
            get { return MathHelper.MemorySizeString(_downloadFileProgress); }
        }

        /// <summary>
        /// Number of files selected to download.
        /// This is used to show the progress of
        /// downloading all the files in the list.
        /// </summary>
        private int _downloadListSize;
        /// <summary>
        /// Number of files selected to download.
        /// This is used to show the progress of
        /// downloading all the files in the list.
        /// </summary>
        public int DownloadListSize
        {
            get { return _downloadListSize; }
            set
            {
                _downloadListSize = value;
                this.NotifyOfPropertyChange(() => this.DownloadListSize);
            }
        }

        /// <summary>
        /// Progress of downloading all the files in
        /// the list.
        /// </summary>
        private long _downloadListProgress;
        /// <summary>
        /// Progress of downloading all the files in
        /// the list.
        /// </summary>
        public long DownloadListProgress
        {
            get { return _downloadListProgress; }
            set
            {
                _downloadListProgress = value;
                this.NotifyOfPropertyChange(() => this.DownloadListProgress);
            }
        }

        /// <summary>
        /// Number of downloads that failed.
        /// </summary>
        public int DownloadFails
        {
            get { return _downloadFailList.Count; }
        }

        /// <summary>
        /// File name of the file being currently downloaded.
        /// </summary>
        private string _downloadFileName;
        /// <summary>
        /// File name of the file being currently downloaded.
        /// </summary>
        public string DownloadFileName
        {
            get { return _downloadFileName; }
            set
            {
                _downloadFileName = value;
                this.NotifyOfPropertyChange(() => this.DownloadFileName);
            }
        }

        /// <summary>
        /// List of all the files that can be downloaded.
        /// </summary>
        private BindingList<DownloadFile> _downloadFileList;
        /// <summary>
        /// List of all the files that can be downloaded.
        /// </summary>
        public BindingList<DownloadFile> DownloadFileList
        {
            get { return _downloadFileList; }
            set
            {
                _downloadFileList = value;
                this.NotifyOfPropertyChange(() => this.DownloadFileList);
                this.NotifyOfPropertyChange(() => this.CanDownloadData);
            }
        }

        #endregion

        #region Waves

        /// <summary>
        /// String on the results of extracting the data from the 
        /// ensembles given.
        /// </summary>
        public string WavesRecover
        {
            get { return _rtiWavesEncoder.WavesRecover; }
        }

        /// <summary>
        /// Show the loading progress ring.
        /// </summary>
        private bool _IsLoading;
        /// <summary>
        /// Show the loading progress ring.
        /// </summary>
        public bool IsLoading
        {
            get { return _IsLoading; }
            set
            {
                _IsLoading = value;
                this.NotifyOfPropertyChange(() => this.IsLoading);
            }
        }

        #region Import Waves Options

        /// <summary>
        /// Correlation threshold for data screening.
        /// </summary>
        public float CorrelationThreshold
        {
            get { return _options.CorrelationThreshold; }
            set
            {
                _options.CorrelationThreshold = value;
                this.NotifyOfPropertyChange(() => this.CorrelationThreshold);

                // Save the options
                SaveOptions();
            }
        }

        /// <summary>
        /// Pressure sensor offset.
        /// </summary>
        public float PressureOffset
        {
            get { return _options.PressureOffset; }
            set
            {
                _options.PressureOffset = value;
                this.NotifyOfPropertyChange(() => this.PressureOffset);

                // Save the options
                SaveOptions();
            }
        }

        /// <summary>
        /// List of all the height source.
        /// </summary>
        public ReactiveList<RTI.RecoverDataOptions.HeightSource> HeightSourceList { get; set; }

        /// <summary>
        /// Beam height source.
        /// </summary>
        public RTI.RecoverDataOptions.HeightSource BeamHeightSource
        {
            get { return _options.BeamHeightSource; }
            set
            {
                _options.BeamHeightSource = value;
                this.NotifyOfPropertyChange(() => this.BeamHeightSource);

                // Save the options
                SaveOptions();
            }
        }

        /// <summary>
        /// Beam height source.
        /// </summary>
        public List<string> BinList { get; set; }

        ///// <summary>
        ///// Selected bins.
        ///// </summary>
        //private List<string> _SelectedBins;
        ///// <summary>
        ///// Beam height source.
        ///// </summary>
        //public List<string> SelectedBins
        //{
        //    get { return _SelectedBins; }
        //    set
        //    {
        //        _SelectedBins = value;
        //        this.NotifyOfPropertyChange(() => this.SelectedBins);

        //        //_rtiWavesEncoder.SelectedBins = value;
        //    }
        //}

        /// <summary>
        /// Selected bin 1.
        /// </summary>
        public string Bin1Selection
        {
            get { return _options.Bin1Selection; }
            set
            {
                _options.Bin1Selection = value;
                this.NotifyOfPropertyChange(() => this.Bin1Selection);

                // Save the options
                SaveOptions();
            }
        }

        /// <summary>
        /// Selected bin 2.
        /// </summary>
        public string Bin2Selection
        {
            get { return _options.Bin2Selection; }
            set
            {
                _options.Bin2Selection = value;
                this.NotifyOfPropertyChange(() => this.Bin2Selection);

                // Save the options
                SaveOptions();
            }
        }

        /// <summary>
        /// Selected bin 3.
        /// </summary>
        public string Bin3Selection
        {
            get { return _options.Bin3Selection; }
            set
            {
                _options.Bin3Selection = value;
                this.NotifyOfPropertyChange(() => this.Bin3Selection);

                // Save the options
                SaveOptions();
            }
        }

        /// <summary>
        /// Latitude for environmental record.
        /// </summary>
        public float Latitude
        {
            get { return _options.Latitude; }
            set
            {
                _options.Latitude = value;
                this.NotifyOfPropertyChange(() => this.Latitude);

                // Save the options
                SaveOptions();
            }
        }

        /// <summary>
        /// Longitude for environmental record.
        /// </summary>
        public float Longitude
        {
            get { return _options.Longitude; }
            set
            {
                _options.Longitude = value;
                this.NotifyOfPropertyChange(() => this.Longitude);

                // Save the options
                SaveOptions();
            }
        }

        /// <summary>
        /// Pressure Sensor Height for environmental record.
        /// </summary>
        public float PressureSensorHeight
        {
            get { return _options.PressureSensorHeight; }
            set
            {
                _options.PressureSensorHeight = value;
                this.NotifyOfPropertyChange(() => this.PressureSensorHeight);

                // Save the options
                SaveOptions();
            }
        }

        #endregion

        #endregion

        #endregion

        #region Command Check

        /// <summary>
        /// Flag if you begin downloading the data.
        /// </summary>
        public bool CanDownloadData
        {
            get
            {
                // Check Adcp connection open
                if (!_adcpConn.IsOpen())
                {
                    return false;
                }

                // Check download settings
                if (DownloadFileList == null || DownloadFileList.Count <= 0 || _isDownloadingData)
                {
                    return false;
                }

                return true;
            }
        }

        /// <summary>
        /// Flag if you can cancel the download.
        /// </summary>
        public bool CanCancelDownload
        {
            get
            {
                if (_isDownloadingData)
                {
                    return true;
                }

                return false;
            }
        }

        /// <summary>
        /// Flag if we can Populate the download list.
        /// </summary>
        public bool CanPopulateDownloadList
        {
            get
            {
                // Currently downloading, so disable the button
                if (_isDownloadingData)
                {
                    return false;
                }

                return true;
            }
        }

        /// <summary>
        /// Flag if we can format the SD card.
        /// </summary>
        public bool CanFormatSdCard
        {
            get
            {
                if (!_isDownloadingData)
                {
                    return true;
                }

                return false;
            }
        }

        #endregion

        #region Commands

        /// <summary>
        /// Command to download the data async.
        /// </summary>
        public ReactiveCommand<System.Reactive.Unit> DownloadDataCommand { get; protected set; }

        /// <summary>
        /// Command to cancel the download process.
        /// </summary>
        public ReactiveCommand<System.Reactive.Unit> CancelDownloadCommand { get; protected set; }

        /// <summary>
        /// Command to get a list of all the files to download.
        /// </summary>
        public ReactiveCommand<System.Reactive.Unit> PopulateDownloadListCommand { get; protected set; }

        /// <summary>
        /// Command to format the SD card in the ADCP.
        /// </summary>
        public ReactiveCommand<System.Reactive.Unit> FormatSdCardCommand { get; protected set; }

        /// <summary>
        /// Command to export the given binary files to a Waves file.
        /// </summary>
        public ReactiveCommand<object> ImportBinaryWavesCommand { get; protected set; }

        #endregion

        /// <summary>
        /// Initialize values.
        /// </summary>
        public RecoverDataViewModel()
            : base("RecoversDataViewModel")
        {
            // Initialize values
            _adcpConn = IoC.Get<AdcpConnection>();
            _pm = IoC.Get<PulseManager>();
            _eventAggregator = IoC.Get<IEventAggregator>();
            _eventAggregator.Subscribe(this);

            // Get the latest options
            _options = _pm.GetRecoverDataOptions();

            // Download values
            _isDownloadingData = false;
            DownloadTotalSpace = "";
            DownloadUsedSpace = "";
            _selectAllFiles = true;
            DownloadFileList = new BindingList<DownloadFile>();
            _downloadFailList = new List<string>();
            _cancelDownload = false;

            IsLoading = false;
            _folderSet = false;
            _folderPath = DEFAULT_FOLDER_PATH;
            _fileName = "";
            _eventWaitData = new EventWaitHandle(false, EventResetMode.AutoReset);

            HeightSourceList = new ReactiveList<RTI.RecoverDataOptions.HeightSource>();
            HeightSourceList.Add(RTI.RecoverDataOptions.HeightSource.Beam0);
            HeightSourceList.Add(RTI.RecoverDataOptions.HeightSource.Beam1);
            HeightSourceList.Add(RTI.RecoverDataOptions.HeightSource.Beam2);
            HeightSourceList.Add(RTI.RecoverDataOptions.HeightSource.Beam3);
            HeightSourceList.Add(RTI.RecoverDataOptions.HeightSource.Vertical);


            BinList = new List<string>();
            BinList.Add(RecoverDataOptions.DISABLE_BIN_SELECTION);
            for (int x = 0; x < DataSet.Ensemble.MAX_NUM_BINS; x++)
            {
                BinList.Add(x.ToString());
            }

            // Waves processing
            _rtiWavesEncoder = new Waves.RtiWavesEncoder();

            _adcpCodec = new AdcpBinaryCodecNew();
            _adcpCodec.ProcessDataEvent += new AdcpBinaryCodecNew.ProcessDataEventHandler(_adcpCodec_ProcessDataEvent);
            _adcpCodec.ProcessDataCompleteEvent += new AdcpBinaryCodecNew.ProcessDataCompleteEventHandler(_adcpCodec_ProcessDataCompleteEvent);

            // Subscribe to recevie download events from the serial port and ethernet
            SubscribeDownloadEvents();

            // Create a wait handle to wait between each download
            _eventWaitDownload = new EventWaitHandle(false, EventResetMode.AutoReset);

            // Create a command to Download the data from the ADCP
            DownloadDataCommand = ReactiveCommand.CreateAsyncTask(this.WhenAny(x => x.CanDownloadData, x => x.Value),
                                                                _ => Task.Run(() => DownloadData()));

            // Create a command to cancel the download process
            CancelDownloadCommand = ReactiveCommand.CreateAsyncTask(this.WhenAny(x => x.CanCancelDownload, x => x.Value),
                                                                _ => Task.Run(() => CancelDownload()));

            // Create a command to populate the download list
            PopulateDownloadListCommand = ReactiveCommand.CreateAsyncTask(this.WhenAny(x => x.CanPopulateDownloadList, x => x.Value),
                                                                _ => Task.Run(() => OnPopulateDownloadList()));

            // Create a command to format the SD card
            FormatSdCardCommand = ReactiveCommand.CreateAsyncTask(this.WhenAny(x => x.CanFormatSdCard, x => x.Value),
                                                                _ => Task.Run(() => OnFormatSdCard()));

            // Export to Waves command
            ImportBinaryWavesCommand = ReactiveCommand.Create();
            ImportBinaryWavesCommand.Subscribe(_ => ExecuteImportBinaryWaves());
        }

        /// <summary>
        /// Shutdown the view.
        /// </summary>
        public override void Dispose()
        {
            // Unsubscribe
            UnsubscribeDownloadEvents();
            _eventAggregator.Unsubscribe(this);

            _adcpCodec.ProcessDataEvent -= _adcpCodec_ProcessDataEvent;
            _adcpCodec.ProcessDataCompleteEvent -= _adcpCodec_ProcessDataCompleteEvent;
            _adcpCodec.Dispose();

            // Cancel any downloads
            CancelDownload();

            // Dispose after canceling the download
            _eventWaitDownload.Dispose();
        }

        #region Methods

        #region Serial Port

        /// <summary>
        /// Subscribe to the serial port to get the latest download progress.
        /// </summary>
        private void SubscribeDownloadEvents()
        {
            // Subscribe to recevie download and upload events from the serial port
            if (_adcpConn != null)
            {
                _adcpConn.DownloadProgressEvent += new AdcpConnection.DownloadProgressEventHandler(On_DownloadProgressEvent);
                _adcpConn.DownloadCompleteEvent += new AdcpConnection.DownloadCompleteEventHandler(On_DownloadCompleteEvent);
                _adcpConn.DownloadFileSizeEvent += new AdcpConnection.DownloadFileSizeEventHandler(On_DownloadFileSizeEvent);
            }
        }

        /// <summary>
        /// Unsubscribe from the serial port if the serial port is going to change.
        /// </summary>
        public void UnsubscribeDownloadEvents()
        {
            // Unsubscribe
            if (_adcpConn != null)
            {
                _adcpConn.DownloadProgressEvent -= On_DownloadProgressEvent;
                _adcpConn.DownloadCompleteEvent -= On_DownloadCompleteEvent;
                _adcpConn.DownloadFileSizeEvent -= On_DownloadFileSizeEvent;
            }
        }

        #endregion

        #region Download Data

        /// <summary>
        /// Create the directory where the download file
        /// will be stored.
        /// Return true if the folder was created or already
        /// existed.
        /// </summary>
        /// <param name="dirName">Directory name.</param>
        /// <returns>True = Folder was created or already existed.</returns>
        private bool CreateDirectory(string dirName)
        {
            DirectoryInfo di = new DirectoryInfo(dirName);
            try
            {
                // Determine whether the directory exists.
                if (!di.Exists)
                {
                    // Try to create the directory.
                    di.Create();
                }
            }
            catch
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Populate the list with all the possible files
        /// that can be downloaded.  This will be all the
        /// ENS files on the ADCP.
        /// </summary>
        /// <param name="directoryListing">List of files.</param>
        private void PopulateDownloadList(RTI.Commands.AdcpDirListing directoryListing)
        {
            try
            {
                // Clear the current list
                DownloadFileList.Clear();

                // Set the total and used space
                DownloadTotalSpace = directoryListing.TotalSpace.ToString() + " MB";
                DownloadUsedSpace = directoryListing.UsedSpace.ToString() + " MB";

                // Create a list of all the ENS files
                for (int x = 0; x < directoryListing.DirListing.Count; x++)
                {
                    DownloadFileList.Add(new DownloadFile(directoryListing.DirListing[x]) { IsSelected = SelectAllFiles });
                }

                this.NotifyOfPropertyChange(() => this.CanDownloadData);
            }
            catch (Exception e)
            {
                log.Error("Error downloading list of files.", e);
            }
        }

        /// <summary>
        /// Select or unselect all the files based off
        /// the value given.
        /// </summary>
        /// <param name="select">True = Select all Files / False = Unselect all files.</param>
        private void SelectAllDownloadFiles(bool select)
        {
            // Set whether to select or unselect the file
            foreach (DownloadFile file in DownloadFileList)
            {
                file.IsSelected = select;
            }

            this.NotifyOfPropertyChange(() => this.DownloadFileList);
            this.NotifyOfPropertyChange(() => this.CanDownloadData);
        }

        /// <summary>
        /// Attempt to download the file.  If the download fails, delete the
        /// bad file.  Send a status message that download failed for the file.
        /// Then move to next file by waking up the event wait.
        /// </summary>
        /// <param name="filename">File name to download.</param>
        private void DownloadData(string filename)
        {
            // Download the data
            if (!_adcpConn.DownloadData(DownloadDirectory, filename, false))
            {
                // If the download failed
                // remove the file and add it to the list of failed downloads
                // so they can try to be redownloaded
                DownloadFail(filename);

                // Move to the next file
                _downloadComplete = true;
                _eventWaitDownload.Set();
            }
        }

        /// <summary>
        /// When the download fails, we need to delete the bad file and
        /// add the bad file to the retry list so it can try to redownload
        /// it after all the remaining files are downloaded.
        /// </summary>
        /// <param name="filename">Filename of the download that failed.</param>
        private void DownloadFail(string filename)
        {
            try
            {
                // If the file could not be downloaded
                // Remove the bad file
                if (File.Exists(DownloadDirectory + "\\" + filename))
                {
                    File.Delete(DownloadDirectory + "\\" + filename);
                }
            }
            catch (Exception e)
            {
                log.Warn("Error Deleteing Failed Download.", e);
            }

            // Add to download fail list
            _downloadFailList.Add(filename);
            this.NotifyOfPropertyChange(() => this.DownloadFails);

            // Send a status that the download failed
            //_eventAggregator.PublishOnUIThread(new StatusEvent(string.Format("Downloading {0} failed.", filename), MessageBoxImage.Error));
        }

        /// <summary>
        /// Download the data from the ADCP.  This will download the file selected
        /// in the list.
        /// </summary>
        private void DownloadData()
        {
            // The D command will cancel any pending downloads
            // Send it twice to first ignore the last packet sent, then
            // stop the download process
            _adcpConn.SendData(string.Format("{0}", RTI.Commands.AdcpCommands.CMD_DS_CANCEL));
            _adcpConn.SendData(string.Format("{0}", RTI.Commands.AdcpCommands.CMD_DS_CANCEL));

            // Turn off updating the serial data
            _isDownloadingData = true;
            _eventAggregator.PublishOnUIThread(new AdcpStatus(eAdcpStatus.Downloading));
            this.NotifyOfPropertyChange(() => this.CanDownloadData);
            this.NotifyOfPropertyChange(() => this.CanCancelDownload);
            this.NotifyOfPropertyChange(() => this.CanPopulateDownloadList);
            this.NotifyOfPropertyChange(() => this.CanFormatSdCard);

            // Reset any previous cancels
            // Reset the wait if has been set previously
            _eventWaitDownload.Reset();
            _downloadComplete = false;
            _cancelDownload = false;

            // Get the number of files selected
            // to monitor the progress
            DownloadListSize = 0;
            DownloadListProgress = 0;
            _downloadFailList = new List<string>();
            this.NotifyOfPropertyChange(() => this.DownloadFails);
            DownloadFileName = "";
            foreach (DownloadFile file in DownloadFileList)
            {
                if (file.IsSelected)
                {
                    DownloadListSize++;
                }
            }

            // Check if the bg worker is cancelled
            if (_cancelDownload)
            {
                //e.Cancel = true;
                return;
            }

            // Go through each file in the list
            for (int x = 0; x < DownloadFileList.Count; x++)
            {
                // Set the flag for the download process to determine
                // if a timeout or completed download occured
                _downloadComplete = false;

                // Check if the bg worker is cancelled
                if (_cancelDownload)
                {
                    return;
                }

                // If the file is selected
                // Download the data
                if (DownloadFileList[x].IsSelected)
                {
                    // Initialize the values
                    DownloadFileSize = 0;
                    DownloadFileProgress = 0;
                    DownloadFileName = DownloadFileList[x].FileInfo.FileName;

                    // Create the directory if the folder
                    // does  not exist
                    if (!CreateDirectory(DownloadDirectory))
                    {
                        // If there was a issue creating
                        // the directory stop now
                        //e.Cancel = true;
                        return;
                    }

                    // Create the file path
                    string path = DownloadDirectory + "\\" + DownloadFileList[x].FileInfo.FileName;

                    // If the file already exist, check if the user
                    // wants the file overwritten
                    if (!OverwriteDownloadFiles)
                    {
                        // Skip the file if it exist
                        if (File.Exists(path))
                        {
                            // Move to the next file
                            DownloadListProgress++;
                            _downloadComplete = true;
                            _eventWaitDownload.Set();
                        }
                        else
                        {
                            // Download the data from the serial port
                            DownloadData(DownloadFileList[x].FileInfo.FileName);
                        }
                    }
                    else
                    {
                        // Download the data from the serial port
                        DownloadData(DownloadFileList[x].FileInfo.FileName);
                    }

                    // Wait until the complete event is received
                    // before starting the next download
                    // If a timeout occurs it either means the download took too
                    // long or the download is hung.
                    _eventWaitDownload.WaitOne(1000 * 60 * DownloadTimeout);

                    // If this flag was not set,
                    // then a timeout occurred and
                    // consider the download for this
                    // file a failure.
                    if (!_downloadComplete)
                    {
                        DownloadFail(DownloadFileList[x].FileInfo.FileName);
                    }

                    // Allow the RS-485 to reset before moving to the next file
                    Thread.Sleep(AdcpSerialPort.WAIT_STATE);
                }
            }

            // Check if the bg worker is cancelled
            if (_cancelDownload)
            {
                return;
            }

            // Retry downloading failed files
            RetryDownloadFails();

            // Complete the download process
            On_DownloadDataCompleted();
        }

        /// <summary>
        /// Retry to download any of the files that fail.
        /// If the file can be downloaded, remove it from the
        /// fail list.  If still cannot be downloaded, delete
        /// the file.
        /// </summary>
        private void RetryDownloadFails()
        {
            for (int x = 0; x < _downloadFailList.Count; x++)
            {
                // If the download succeeds, remove it from the list
                if (_adcpConn.DownloadData(DownloadDirectory, _downloadFailList[x], ParseDownloadedData))
                {
                    _downloadFailList.RemoveAt(x);
                    this.NotifyOfPropertyChange(() => this.DownloadFails);
                }
                else
                {
                    // If still fails
                    // Remove the bad file
                    if (File.Exists(DownloadDirectory + "\\" + _downloadFailList[x]))
                    {
                        File.Delete(DownloadDirectory + "\\" + _downloadFailList[x]);
                    }
                }

                // Wait until the complete event is received
                // before starting the next download
                _eventWaitDownload.WaitOne(1000 * 60 * DownloadTimeout);

                // Allow the RS-485 to reset before moving to the next file
                Thread.Sleep(AdcpSerialPort.WAIT_STATE);
            }
        }

        /// <summary>
        /// When the downloading is complete, this method will be called.
        /// This will update the buttons and begin outputing data to the
        /// screen again.
        /// </summary>
        private void On_DownloadDataCompleted()
        {
            // Turn on displaying serial data
            _isDownloadingData = false;
            _eventAggregator.PublishOnUIThread(new AdcpStatus(eAdcpStatus.Connected));
            this.NotifyOfPropertyChange(() => this.CanDownloadData);
            this.NotifyOfPropertyChange(() => this.CanCancelDownload);
            this.NotifyOfPropertyChange(() => this.CanPopulateDownloadList);
            this.NotifyOfPropertyChange(() => this.CanFormatSdCard);

            // Check how the download completed
            if (_cancelDownload)
            {
                // Clear the list of downloads
                DownloadFileList.Clear();
            }

            // Parsing the data
            // Take the download list and convert the
            // files to waves matlab files
            if(ParseDownloadedData)
            {
                List<string> fileList = new List<string>();

                // Create a list of all the files
                foreach(var file in DownloadFileList)
                {
                    string fileName = DownloadDirectory + @"\" + file.FileName;
                    fileList.Add(fileName);
                }

                // Convert the file list to matlab files
                ImportBinaryFiles(fileList.ToArray());
            }
        }

        /// <summary>
        /// Cancel the download background worker.
        /// If the background worker is doing any work,
        /// this will cancel the download process.
        /// </summary>
        private void CancelDownload()
        {
            if (_adcpConn != null)
            {
                // Cancel the download process in the serial port
                _adcpConn.CancelDownload();
            }

            try
            {
                // Delete the file that was currently downloading
                if (File.Exists(DownloadDirectory + "\\" + DownloadFileName))
                {
                    File.Delete(DownloadDirectory + "\\" + DownloadFileName);
                }
            }
            catch (Exception e)
            {
                log.Error(string.Format("Could not access the file: {0} to delete.", DownloadFileName), e);
            }

            // Wake up the bgworker if its asleep
            // so it can stop
            _downloadComplete = true;
            _cancelDownload = true;

            _isDownloadingData = false;

            this.NotifyOfPropertyChange(() => this.CanDownloadData);
            this.NotifyOfPropertyChange(() => this.CanCancelDownload);
            this.NotifyOfPropertyChange(() => this.CanPopulateDownloadList);
            this.NotifyOfPropertyChange(() => this.CanFormatSdCard);

            try
            {
                _eventWaitDownload.Set();
            }
            catch (Exception e) 
            { 
                log.Warn("Error stopping the download process.", e); 
            }
        }

        #endregion

        #region Generate Waves folder

        /// <summary>
        /// Set a folder path based off the ensemble serial number and date and time.
        /// </summary>
        /// <param name="ensemble">Ensemble to get serial number.</param>
        private void SetFolderPath(DataSet.Ensemble ensemble)
        {
            // Get the serial number and the date and time
            SerialNumber serial = new SerialNumber();
            DateTime dt = DateTime.Now;
            if (ensemble.IsEnsembleAvail)
            {
                serial = ensemble.EnsembleData.SysSerialNumber;
                dt = ensemble.EnsembleData.EnsDateTime;
            }

            //// Set file path and check if it exist
            //_filePath = DEFAULT_FOLDER_PATH + @"\" + serial.ToString() + "_" + dt.ToString("yyyyMMddHHmmss") + @"\";

            //int index = 0;
            //while (Directory.Exists(_filePath))
            //{
            //    _filePath = DEFAULT_FOLDER_PATH + @"\" + serial.ToString() + "_" + dt.ToString("yyyyMMddHHmmss") + "_" + index++ + @"\";
            //}

            _folderPath = DownloadDirectory + @"\";

            _folderSet = true;
        }

        #endregion

        #endregion

        #region Options

        /// <summary>
        /// Save the options to the JSON file.
        /// </summary>
        private void SaveOptions()
        {
            _pm.UpdateRecoverDataOptions(_options);
        }

        #endregion

        #region Commands

        #region Format SD Card Command

        /// <summary>
        /// Send command to format the SD card.
        /// </summary>
        private void OnFormatSdCard()
        {
            // Verify the user want to delete all the files
            if (System.Windows.MessageBox.Show("Are you sure you want to delete all the files?", "SD Card Format Warning", MessageBoxButton.OKCancel, MessageBoxImage.Warning) == MessageBoxResult.OK)
            {
                // Send the command to format the SD card
                _adcpConn.SendData(AdcpCommands.CMD_DSFORMAT);
            }
        }

        #endregion

        #region Populate Download List Command

        /// <summary>
        /// Populate the list of available files to download.
        /// </summary>
        private void OnPopulateDownloadList()
        {
            // If the ADCP is pinging, make it stop
            _adcpConn.StopPinging();

            // The D command will cancel any pending downloads
            // Send it twice to first ignore the last packet sent, then
            // stop the download process
            _adcpConn.SendData(string.Format("{0}", RTI.Commands.AdcpCommands.CMD_DS_CANCEL));
            _adcpConn.SendData(string.Format("{0}", RTI.Commands.AdcpCommands.CMD_DS_CANCEL));

            // Send command to the ADCP to give a list of all the files
            RTI.Commands.AdcpDirListing dirListing = _adcpConn.GetDirectoryListing();

            // Populate the list with all the files found
            System.Windows.Application.Current.Dispatcher.BeginInvoke(new System.Action(() => PopulateDownloadList(dirListing)));
        }

        #endregion

        #region Import Binary Files

        /// <summary>
        /// Import the data the RTI data and export
        /// to matlab waves format.
        /// </summary>
        private void ExecuteImportBinaryWaves()
        {
            ImportBinaryData();
        }

        /// <summary>
        /// Ask the user for which files to import.
        /// Then being the import process.
        /// </summary>
        public void ImportBinaryData()
        {
            // Show the FolderBrowserDialog.
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Filter = "Ensemble files (*.bin, *.ens)|*.bin; *.ens|ENS files (*.ens)|*.ens|BIN files (*.bin)|*.bin|(*.*)|*.*";
            dialog.Multiselect = true;

            DialogResult result = dialog.ShowDialog();
            if (result == DialogResult.OK)
            {
                // Get the files selected
                string[] files = dialog.FileNames;

                // Import the files
                Task.Run(() => ImportBinaryFiles(files));
            }
        }

        /// <summary>
        /// Import the files and decode them.
        /// </summary>
        /// <param name="files">List of all the files.</param>
        public async void ImportBinaryFiles(string[] files)
        {
            // Show progress ring
            IsLoading = true;

            // Set flag to set new folder
            _folderSet = false;

            // Reset the RTI waves
            _rtiWavesEncoder = new Waves.RtiWavesEncoder();
            this.NotifyOfPropertyChange(() => this.WavesRecover);

            // Add all the files
            foreach (var filePath in files)
            {
                // Open the file
                if (File.Exists(filePath))
                {
                    //using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                    //{
                    //    if (fileStream.CanRead)
                    //    {
                    //        using (BufferedStream bs = new BufferedStream(fileStream))
                    //        {
                    //            // Get the file size
                    //            long fileSize = fileStream.Length;

                    //            // Read in the file
                    //            byte[] buffer = new byte[fileSize];
                    //            var count = await bs.ReadAsync(buffer, 0, (int)fileSize);

                    //            // Set the file name
                    //            string fileName = Path.GetFileName(filePath);
                    //            fileName = fileName.Replace('B', 'W');                              // Replace B with W
                    //            _fileName = fileName.Replace(Path.GetExtension(filePath), ".mat");  // Replace the file extension with .mat

                    //            // Add buffer to the codecs
                    //            //_adcpCodec.AddIncomingData(buffer);

                    //            // Wait for file complete
                    //            //_eventWaitData.WaitOne();
                    //        }
                    //    }
                    //}

                    // Set the file name
                    string fileName = Path.GetFileName(filePath);
                    if (fileName.Contains('B'))
                    {
                        fileName = fileName.Replace('B', 'W');                              // Replace B with W
                    }
                    if(fileName.Contains('A'))
                    {
                        fileName = fileName.Replace('A', 'W');                              // Replace A with W
                    }
                    _fileName = fileName.Replace(Path.GetExtension(filePath), ".mat");  // Replace the file extension with .mat

                    // Get all the files from the codec
                    AdcpBinaryCodecReadFile codec = new RTI.AdcpBinaryCodecReadFile();
                    var list = codec.GetEnsembles(filePath);

                    // Add the ensembles to the waves encoder 
                    foreach(var ens in list)
                    {
                        AddEnsemble(ens.RawEnsemble, ens.Ensemble);
                    }

                    // Cleanup the data in the encoder
                    _rtiWavesEncoder.FileComplete();
                }
            }

            // Cleanup the data and output the result
            IsLoading = false;
            List<string> fileList = _rtiWavesEncoder.Cleanup();
            this.NotifyOfPropertyChange(() => this.WavesRecover);

            // Go to view page
            _eventAggregator.PublishOnUIThread(new ViewNavEvent(ViewNavEvent.ViewId.WavesView));

            //// Publish the records
            //foreach (var record in _rtiWavesEncoder.WavesRecords)
            //{
            //    if (record.WaveSamples.Count > 0)
            //    {
            //        _eventAggregator.PublishOnUIThread(new WavesRecordEvent(record));
            //    }
            //}

            // Publish the filepath to the last record 
            _eventAggregator.PublishOnUIThread(new WavesRecordFileEvent(fileList.ToArray()));
        }


        private void AddEnsemble(byte[] ensembleRaw, DataSet.Ensemble ensemble)
        {
            // Create the velocity vectors for the ensemble
            DataSet.VelocityVectorHelper.CreateVelocityVector(ref ensemble);

            // Check if the folder needs to be set
            if (!_folderSet)
            {
                SetFolderPath(ensemble);
            }

            //// Generate the list based off the selections
            //var selectedBins = new List<int>();
            //if (Bin1Selection != RecoverDataOptions.DISABLE_BIN_SELECTION)
            //{
            //    selectedBins.Add(Convert.ToInt32(Bin1Selection));
            //}
            //if (Bin2Selection != RecoverDataOptions.DISABLE_BIN_SELECTION)
            //{
            //    selectedBins.Add(Convert.ToInt32(Bin2Selection));
            //}
            //if (Bin3Selection != RecoverDataOptions.DISABLE_BIN_SELECTION)
            //{
            //    selectedBins.Add(Convert.ToInt32(Bin3Selection));
            //}

            // Send the ensemble to the waves processor
            _rtiWavesEncoder.EncodeWavesMatlab(ensemble, _folderPath, _fileName, _options);
        }

        #endregion

        #endregion

        #region Event Handlers

        #region Download

        /// <summary>
        /// Event handler when a file has completed being
        /// downloaded.
        /// </summary>
        /// <param name="fileName">File name of the completed download.</param>
        /// <param name="goodDownload">Flag set to determine if the download was good or bad.</param>
        private void On_DownloadCompleteEvent(string fileName, bool goodDownload)
        {
            // Just in case the correct file size was not used
            // make it look like the file has been complete
            // downloaded by setting the download progress to the file size.
            // If the download was NOT good, then do not look complete.
            if (goodDownload)
            {
                foreach (DownloadFile file in DownloadFileList)
                {
                    if (file.FileInfo.FileName.Equals(fileName))
                    {
                        file.DownloadProgress = file.DownloadFileSize;
                    }
                }
            }

            // Unblock the wait so the next
            // file can be downloaded
            _downloadComplete = true;
            _eventWaitDownload.Set();

            // Increment the Download List progress
            DownloadListProgress++;
        }

        /// <summary>
        /// Set the file size for the given file name.
        /// This will set the file size in the list of 
        /// download files.
        /// </summary>
        /// <param name="fileName">File Name.</param>
        /// <param name="fileSize">Size of the file in bytes.</param>
        private void On_DownloadFileSizeEvent(string fileName, long fileSize)
        {
            // Set the current file size for the progressbar
            DownloadFileSize = fileSize;

            foreach (DownloadFile file in DownloadFileList)
            {
                if (file.FileInfo.FileName.Equals(fileName))
                {
                    // If we get a bad file size
                    // try to use the file size given
                    // in the object,  it will be
                    // off by a rounding factor, but
                    // it is better then nothing
                    if (fileSize <= 0)
                    {
                        // Convert file size to bytes
                        DownloadFileSize = file.DownloadFileSize;
                    }
                    else
                    {
                        DownloadFileSize = fileSize;
                        file.DownloadFileSize = fileSize;
                    }


                }
            }

            //Debug.WriteLine("Download File Size: {0}  {1}", fileSize, DownloadFileSize);
        }

        /// <summary>
        /// Progress of the downloading file.  This will give the number
        /// of bytes currently written to the file.
        /// </summary>
        /// <param name="fileName">File name of file in progress.</param>
        /// <param name="bytesWritten">Number of bytes written to file.</param>
        private void On_DownloadProgressEvent(string fileName, long bytesWritten)
        {
            // Set the progress of downloading the current file
            DownloadFileProgress = bytesWritten;

            // Update the list progress bar
            foreach (DownloadFile file in DownloadFileList)
            {
                if (file.FileInfo.FileName.Equals(fileName))
                {
                    // Convert file size to bytes
                    file.DownloadProgress = bytesWritten;
                }
            }
        }

        #endregion

        #region Waves

        /// <summary>
        /// Receive the ensemble from the codec.
        /// Then set the ensemble size in bytes 
        /// so that the next ensemble can be found quicker.
        /// Set the flag that the ensemble was found.
        /// Then store the ensemble for playback.
        /// </summary>
        /// <param name="ensembleRaw">Ensemble binary data.</param>
        /// <param name="ensemble">Ensemble object.</param>
        private void _adcpCodec_ProcessDataEvent(byte[] ensembleRaw, DataSet.Ensemble ensemble)
        {
            AddEnsemble(ensembleRaw, ensemble);
        }

        /// <summary>
        /// Handle events when the codec has completed processing all the data.
        /// </summary>
        private void _adcpCodec_ProcessDataCompleteEvent()
        {
            // Cleanup the data in the encoder
            _rtiWavesEncoder.FileComplete();

            // Wake up the thread to continue processing
            _eventWaitData.Set();
        }

        #endregion

        #endregion
    }
}
