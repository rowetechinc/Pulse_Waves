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
 * 09/17/2014      RC          4.1.0       Initial coding
 * 12/29/2014      RC          0.0.1       Adding Default options.
 * 01/06/2015      RC          0.0.1       Fixed bug clearing the configurations.
 * 02/19/2015      RC          0.0.2       Added button flags to set the color of the button when pressed.
 * 11/03/2015      RC          1.0.0       Added UpdateCEI() to update all the CEI commands for each VM.
 * 05/28/2016      RC          1.1.5       Added CSAVE to SetCETFPtoAdcp().
 * 10/23/2017      RC          1.2.2       Update the Prediction model on startup.
 * 
 */

using Caliburn.Micro;
using ReactiveUI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace RTI
{

    /// <summary>
    /// Setup a Waves ADCP.
    /// </summary>
    public class WavesSetupViewModel : PulseViewModel
    {
        #region Variable

        /// <summary>
        ///  Setup logger
        /// </summary>
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        /// <summary>
        /// Connection to the ADCP.
        /// </summary>
        private AdcpConnection _adcpConn;

        /// <summary>
        /// Pulse Manager.
        /// </summary>
        private PulseManager _pm;
        #endregion

        #region Properties

        #region Subsystem

        /// <summary>
        /// List of all the Waves Subsystem Configurations.
        /// </summary>
        public ReactiveList<WavesSubsystemConfigurationViewModel> SubsystemConfigList { get; set; }

        /// <summary>
        /// List of all the subsystems with a description.
        /// Used to populate the combobox.
        /// </summary>
        public SubsystemList ListOfSubsystems { get; set; }

        /// <summary>
        /// Selected Subsystem from the combobox.
        /// </summary>
        private RTI.SubsystemList.SubsystemCodeDesc _selectedSubsystem;
        /// <summary>
        /// Selected Subsystem from the combobox.
        /// </summary>
        public RTI.SubsystemList.SubsystemCodeDesc SelectedSubsystem
        {
            get { return _selectedSubsystem; }
            set
            {
                _selectedSubsystem = value;
                this.NotifyOfPropertyChange(() => this.SelectedSubsystem);
            }
        }

        #endregion

        #region ADCP Configuration

        /// <summary>
        /// The ADCP Configuration.
        /// </summary>
        private AdcpConfiguration _AdcpConfig;
        /// <summary>
        /// The ADCP Configuration.
        /// </summary>
        public AdcpConfiguration AdcpConfig
        {
            get { return _AdcpConfig; }
            set
            {
                _AdcpConfig = value;
                this.NotifyOfPropertyChange(() => this.AdcpConfig);
            }
        }

        /// <summary>
        /// The ADCP command set.
        /// </summary>
        private string _AdcpCommandSet;
        /// <summary>
        /// The ADCP command set.
        /// </summary>
        public string AdcpCommandSet
        {
            get { return _AdcpCommandSet; }
            set
            {
                _AdcpCommandSet = value;
                this.NotifyOfPropertyChange(() => this.AdcpCommandSet);
            }
        }

        /// <summary>
        /// The ADCP command set.
        /// </summary>
        private string _AdditionalAdcpCommandSet;
        /// <summary>
        /// The ADCP command set.
        /// </summary>
        public string AdditionalAdcpCommandSet
        {
            get { return _AdditionalAdcpCommandSet; }
            set
            {
                _AdditionalAdcpCommandSet = value;
                this.NotifyOfPropertyChange(() => this.AdditionalAdcpCommandSet);

                _AdcpConfig.AdditionalCommands = value;

                // Update the command set.
                UpdateCommandSet();
            }
        }

        #endregion

        #region Communication

        /// <summary>
        /// Display the receive buffer from the connected ADCP serial port.
        /// </summary>
        public string AdcpReceiveBuffer
        {
            get { return _adcpConn.ReceiveBufferString; }
        }

        #endregion

        #region ADCP Send Commands

        /// <summary>
        /// History of all the previous ADCP commands.
        /// </summary>
        private ObservableCollection<string> _AdcpCommandHistory;
        /// <summary>
        /// History of all the previous ADCP commands.
        /// </summary>
        public IEnumerable AdcpCommandHistory
        {
            get { return _AdcpCommandHistory; }
        }

        /// <summary>
        /// Command currently selected.
        /// </summary>
        private string _SelectedAdcpCommand;
        /// <summary>
        /// Command currently selected.
        /// </summary>
        public string SelectedAdcpCommand
        {
            get { return _SelectedAdcpCommand; }
            set
            {
                _SelectedAdcpCommand = value;
                this.NotifyOfPropertyChange(() => this.SelectedAdcpCommand);
                this.NotifyOfPropertyChange(() => this.NewAdcpCommand);
            }
        }

        /// <summary>
        /// New command entered by the user.
        /// This will be called when the user enters
        /// in a new command to send to the ADCP.
        /// It will update the list and set the SelectedCommand.
        /// </summary>
        public string NewAdcpCommand
        {
            get { return _SelectedAdcpCommand; }
            set
            {
                //if (_SelectedAdcpCommand != null)
                //{
                //    return;
                //}
                if (!string.IsNullOrEmpty(value))
                {
                    _AdcpCommandHistory.Insert(0, value);
                    SelectedAdcpCommand = value;
                }
            }
        }

        #endregion

        #region Hardware Connection

        /// <summary>
        /// Hardware connection used to communicate with
        /// the ADCP to configure it.
        /// </summary>
        private string _HardwareConnection;
        /// <summary>
        /// Hardware connection used to communicate with
        /// the ADCP to configure it.
        /// </summary>
        public string HardwareConnection
        {
            get { return _HardwareConnection; }
            set
            {
                _HardwareConnection = value;
                this.NotifyOfPropertyChange(() => this.HardwareConnection);
            }
        }

        #endregion

        #region SPOS

        /// <summary>
        /// SPOS description.
        /// </summary>
        public string SPOS_Desc
        {
            get
            {
                return Commands.AdcpCommands.GetSposDesc();
            }
        }

        /// <summary>
        /// SPOS Latitude. 
        /// </summary>
        private double _SPOS_Latitude;
        /// <summary>
        /// SPOS Latitude. 
        /// </summary>
        public double SPOS_Latitude
        {
            get { return _SPOS_Latitude; }
            set
            {
                _SPOS_Latitude = value;
                this.NotifyOfPropertyChange(() => this.SPOS_Latitude);

                // Set the value to the config
                _AdcpConfig.Commands.SPOS_Latitude = new DotSpatial.Positioning.Latitude(value);

                // Update the command set.
                UpdateCommandSet();
            }
        }

        /// <summary>
        /// SPOS Longitude. 
        /// </summary>
        private double _SPOS_Longitude;
        /// <summary>
        /// SPOS Longitude. 
        /// </summary>
        public double SPOS_Longitude
        {
            get { return _SPOS_Longitude; }
            set
            {
                _SPOS_Longitude = value;
                this.NotifyOfPropertyChange(() => this.SPOS_Longitude);

                // Set the value to the config
                _AdcpConfig.Commands.SPOS_Longitude = new DotSpatial.Positioning.Longitude(value);

                // Update the command set.
                UpdateCommandSet();
            }
        }

        /// <summary>
        /// SPOS Pressure Sensor Height.
        /// </summary>
        private float _SPOS_PsensHeight;
        /// <summary>
        /// SPOS Pressure Sensor Height.
        /// </summary>
        public float SPOS_PsensHeight
        {
            get { return _SPOS_PsensHeight; }
            set
            {
                _SPOS_PsensHeight = value;
                this.NotifyOfPropertyChange(() => this.SPOS_PsensHeight);

                // Set the value to the config
                _AdcpConfig.Commands.SPOS_PsensHeight = value;

                // Update the command set.
                UpdateCommandSet();
            }
        }

        /// <summary>
        /// SPOS Transducer Water Depth.
        /// </summary>
        private float _SPOS_WaterDepth;
        /// <summary>
        /// SPOS Transducer Water Depth.
        /// </summary>
        public float SPOS_WaterDepth
        {
            get { return _SPOS_WaterDepth; }
            set
            {
                _SPOS_WaterDepth = value;
                this.NotifyOfPropertyChange(() => this.SPOS_WaterDepth);

                // Set the value to the config
                _AdcpConfig.Commands.SPOS_WaterDepth = value;

                // Update the command set.
                UpdateCommandSet();
            }
        }

        #endregion

        #region CWS

        /// <summary>
        /// CWS description.
        /// </summary>
        public string CWS_Desc
        {
            get
            {
                return Commands.AdcpCommands.GetCwsDesc();
            }
        }

        /// <summary>
        /// Salinity in ppt.
        /// </summary>
        private float _CWS;
        /// <summary>
        /// Salinity in ppt.
        /// </summary>
        public float CWS
        {
            get { return _CWS; }
            set
            {
                _CWS = value;
                this.NotifyOfPropertyChange(() => this.CWS);

                // Set the value to the config
                _AdcpConfig.Commands.CWS = value;

                // Update the command set.
                UpdateCommandSet();
            }
        }

        #endregion

        #region CWT

        /// <summary>
        /// CWT description.
        /// </summary>
        public string CWT_Desc
        {
            get
            {
                return Commands.AdcpCommands.GetCwtDesc();
            }
        }

        /// <summary>
        /// Water temperature in degrees farenheit.
        /// </summary>
        private float _CWT;
        /// <summary>
        /// Water temperature in degrees farenheit.
        /// </summary>
        public float CWT
        {
            get { return _CWT; }
            set
            {
                _CWT = value;
                this.NotifyOfPropertyChange(() => this.CWT);

                // Set the value to the config
                _AdcpConfig.Commands.CWT = value;

                // Update the command set.
                UpdateCommandSet();
            }
        }

        #endregion

        #region CETFP

        /// <summary>
        /// CETFP description.
        /// </summary>
        public string CETFP_Desc
        {
            get
            {
                return Commands.AdcpCommands.GetCetfpDesc();
            }
        }

        /// <summary>
        /// Time of the first ping.
        /// </summary>
        private DateTime _CETFP;
        /// <summary>
        /// Time of the first ping.
        /// </summary>
        public DateTime CETFP
        {
            get { return _CETFP; }
            set
            {
                _CETFP = value;
                this.NotifyOfPropertyChange(() => this.CETFP);

                // Set the value to the config
                _AdcpConfig.Commands.CETFP = value;

                // Update the command set.
                UpdateCommandSet();
            }
        }

        #endregion

        #region CEOUTPUT

        /// <summary>
        /// CEOUTPUT description.
        /// </summary>
        public string CEOUTPUT_Desc
        {
            get
            {
                return Commands.AdcpCommands.GetCeoutputDesc();
            }
        }

        /// <summary>
        /// Turn on or off outputing data to the serial port.
        /// </summary>
        private bool _CEOUTPUT;
        /// <summary>
        /// Turn on or off outputing data to the serial port.
        /// </summary>
        public bool CEOUTPUT
        {
            get { return _CEOUTPUT; }
            set
            {
                _CEOUTPUT = value;
                this.NotifyOfPropertyChange(() => this.CEOUTPUT);

                // Set the value to the config
                if (value)
                {
                    _AdcpConfig.Commands.CEOUTPUT = Commands.AdcpCommands.AdcpOutputMode.Binary;
                }
                else
                {
                    _AdcpConfig.Commands.CEOUTPUT = Commands.AdcpCommands.AdcpOutputMode.Disable;
                }

                // Update the command set.
                UpdateCommandSet();
            }
        }

        #endregion

        #region CERECORD

        /// <summary>
        /// CERECORD description.
        /// </summary>
        public string CERECORD_Desc
        {
            get
            {
                return Commands.AdcpCommands.GetCerecordDesc();
            }
        }

        /// <summary>
        /// Turn on or off recording to the SD card.
        /// </summary>
        private bool _CERECORD;
        /// <summary>
        /// Turn on or off recording to the SD card.
        /// </summary>
        public bool CERECORD
        {
            get { return _CERECORD; }
            set
            {
                _CERECORD = value;
                this.NotifyOfPropertyChange(() => this.CERECORD);

                // Set the value to the config
                if (value)
                {
                    _AdcpConfig.Commands.CERECORD_EnsemblePing = Commands.AdcpCommands.AdcpRecordOptions.Enable;
                }
                else
                {
                    _AdcpConfig.Commands.CERECORD_EnsemblePing = Commands.AdcpCommands.AdcpRecordOptions.Disable;
                }

                // Update the command set.
                UpdateCommandSet();
            }
        }

        #endregion

        #region Read Adcp Time

        
        /// <summary>
        /// Water temperature in degrees farenheit.
        /// </summary>
        private string _ReadAdcpTime;
        /// <summary>
        /// Water temperature in degrees farenheit.
        /// </summary>
        public string ReadAdcpTime
        {
            get { return _ReadAdcpTime; }
            set
            {
                _ReadAdcpTime = value;
                this.NotifyOfPropertyChange(() => this.ReadAdcpTime);
            }
        }

        

        #endregion

        #region Set Start Time Flag

        /// <summary>
        /// Water temperature in degrees farenheit.
        /// </summary>
        private bool _IsSetStartTimeSet;
        /// <summary>
        /// Water temperature in degrees farenheit.
        /// </summary>
        public bool IsSetStartTimeSet
        {
            get { return _IsSetStartTimeSet; }
            set
            {
                _IsSetStartTimeSet = value;
                this.NotifyOfPropertyChange(() => this.IsSetStartTimeSet);
            }
        }

        #endregion

        #region Prediction Model

        /// <summary>
        /// Deployment Duration.
        /// </summary>
        private UInt32 _DeploymentDuration;
        /// <summary>
        /// Deployment Duration.
        /// </summary>
        public UInt32 DeploymentDuration
        {
            get { return _DeploymentDuration; }
            set
            {
                _DeploymentDuration = value;
                this.NotifyOfPropertyChange(() => this.DeploymentDuration);

                _AdcpConfig.DeploymentOptions.Duration = value;
                UpdateCommandSet();

                // Update the VM
                UpdateDeploymentDuration();
            }
        }

        /// <summary>
        /// Predicted battery usage.
        /// </summary>
        public string PredictedPowerUsage { get; set; }

        /// <summary>
        /// Predicted number of batteries.
        /// </summary>
        public string PredictedNumberOfBatteries { get; set; }

        /// <summary>
        /// Predicted memory usage.
        /// </summary>
        public string PredictedMemoryUsage { get; set; }

        #endregion

        #region Default Options Selection

        /// <summary>
        /// A list of all the default options and there descriptions.
        /// </summary>
        public ReactiveList<DefaultOptionsSelections> DefaultOptionsList { get; set; }

        /// <summary>
        /// Deployment Duration.
        /// </summary>
        private DefaultOptionsSelections _SelectedDefaultOption;
        /// <summary>
        /// Deployment Duration.
        /// </summary>
        public DefaultOptionsSelections SelectedDefaultOption
        {
            get { return _SelectedDefaultOption; }
            set
            {
                _SelectedDefaultOption = value;
                this.NotifyOfPropertyChange(() => this.PredictedPowerUsage);
            }
        }

        #endregion

        #region Button Clicked

        /// <summary>
        /// Button 1 Flag.
        /// </summary>
        private bool _Button1;
        /// <summary>
        /// Button 1 Flag.
        /// </summary>
        public bool Button1
        {
            get { return _Button1; }
            set
            {
                _Button1 = value;
                this.NotifyOfPropertyChange(() => this.Button1);
            }
        }

        /// <summary>
        /// Button 2 Flag.
        /// </summary>
        private bool _Button2;
        /// <summary>
        /// Button 2 Flag.
        /// </summary>
        public bool Button2
        {
            get { return _Button2; }
            set
            {
                _Button2 = value;
                this.NotifyOfPropertyChange(() => this.Button2);
            }
        }

        /// <summary>
        /// Button 3 Flag.
        /// </summary>
        private bool _Button3;
        /// <summary>
        /// Button 3 Flag.
        /// </summary>
        public bool Button3
        {
            get { return _Button3; }
            set
            {
                _Button3 = value;
                this.NotifyOfPropertyChange(() => this.Button3);
            }
        }

        /// <summary>
        /// Button 4 Flag.
        /// </summary>
        private bool _Button4;
        /// <summary>
        /// Button 4 Flag.
        /// </summary>
        public bool Button4
        {
            get { return _Button4; }
            set
            {
                _Button4 = value;
                this.NotifyOfPropertyChange(() => this.Button4);
            }
        }

        /// <summary>
        /// Button 5 Flag.
        /// </summary>
        private bool _Button5;
        /// <summary>
        /// Button 5 Flag.
        /// </summary>
        public bool Button5
        {
            get { return _Button5; }
            set
            {
                _Button5 = value;
                this.NotifyOfPropertyChange(() => this.Button5);
            }
        }

        /// <summary>
        /// Button 6 Flag.
        /// </summary>
        private bool _Button6;
        /// <summary>
        /// Button 6 Flag.
        /// </summary>
        public bool Button6
        {
            get { return _Button6; }
            set
            {
                _Button6 = value;
                this.NotifyOfPropertyChange(() => this.Button6);
            }
        }

        /// <summary>
        /// Button 7 Flag.
        /// </summary>
        private bool _Button7;
        /// <summary>
        /// Button 7 Flag.
        /// </summary>
        public bool Button7
        {
            get { return _Button7; }
            set
            {
                _Button7 = value;
                this.NotifyOfPropertyChange(() => this.Button7);
            }
        }

        /// <summary>
        /// Button 8 Flag.
        /// </summary>
        private bool _Button8;
        /// <summary>
        /// Button 8 Flag.
        /// </summary>
        public bool Button8
        {
            get { return _Button8; }
            set
            {
                _Button8 = value;
                this.NotifyOfPropertyChange(() => this.Button8);
            }
        }

        /// <summary>
        /// Button 9 Flag.
        /// </summary>
        private bool _Button9;
        /// <summary>
        /// Button 9 Flag.
        /// </summary>
        public bool Button9
        {
            get { return _Button9; }
            set
            {
                _Button9 = value;
                this.NotifyOfPropertyChange(() => this.Button9);
            }
        }

        #endregion

        #endregion

        #region Commands

        /// <summary>
        /// Command to send a BREAK.
        /// </summary>
        public ReactiveCommand<System.Reactive.Unit> SendBreakCommand { get; protected set; }

        /// <summary>
        /// Command to stop the DVL.
        /// </summary>
        public ReactiveCommand<System.Reactive.Unit> StopDvlCommand { get; protected set; }

        /// <summary>
        /// Command to foramt the SD card.
        /// </summary>
        public ReactiveCommand<System.Reactive.Unit> FormatSdCommand { get; protected set; }

        /// <summary>
        /// Command to read the ADCP.
        /// </summary>
        public ReactiveCommand<System.Reactive.Unit> ReadAdcpCommand { get; protected set; }

        /// <summary>
        /// Command to send a command to the terminal.
        /// </summary>
        public ReactiveCommand<System.Reactive.Unit> SendCommand { get; protected set; }

        /// <summary>
        /// Command to clear the terminal.
        /// </summary>
        public ReactiveCommand<System.Reactive.Unit> ClearTerminalCommand { get; protected set; }

        /// <summary>
        /// Command to clear the command set.
        /// </summary>
        public ReactiveCommand<System.Reactive.Unit> ClearCommandSetCommand { get; protected set; }

        /// <summary>
        /// Command to set the ADCP time.
        /// </summary>
        public ReactiveCommand<System.Reactive.Unit> SetAdcpTimeCommand { get; protected set; }

        /// <summary>
        /// Command to set the CEFTP.
        /// </summary>
        public ReactiveCommand<System.Reactive.Unit> SetStartTimeCommand { get; protected set; }
        

        /// <summary>
        /// Command to send the command set.
        /// </summary>
        public ReactiveCommand<System.Reactive.Unit> SendCommandSetCommand { get; protected set; }

        /// <summary>
        /// Command to send the START DVL.
        /// </summary>
        public ReactiveCommand<System.Reactive.Unit> SendDvlStartCommand { get; protected set; }

        /// <summary>
        /// Command to save the command set to a file.
        /// </summary>
        public ReactiveCommand<System.Reactive.Unit> SaveCommandSetCommand { get; protected set; }

        /// <summary>
        /// Command to import a command set.
        /// </summary>
        public ReactiveCommand<object> ImportCommandSetCommand { get; protected set; }

        /// <summary>
        /// Command to add a subsystem.
        /// </summary>
        public ReactiveCommand<object> AddSubsystemCommand { get; protected set; }

        /// <summary>
        /// Command to read the ADCP time.
        /// </summary>
        public ReactiveCommand<System.Reactive.Unit> ReadAdcpTimeCommand { get; protected set; }

        /// <summary>
        /// Command to zero the pressure sensor.
        /// </summary>
        public ReactiveCommand<System.Reactive.Unit> ZeroPressureSensorCommand { get; protected set; }

        /// <summary>
        /// Command to set the default values..
        /// </summary>
        public ReactiveCommand<System.Reactive.Unit> SetDefaultCommand { get; protected set; }

        /// <summary>
        /// Command to set the CETFP to the current date and time.
        /// </summary>
        public ReactiveCommand<System.Reactive.Unit> RefreshCetfpCommand { get; protected set; } 

        #endregion

        /// <summary>
        /// Initialize the view model.
        /// </summary>
        public WavesSetupViewModel()
            : base("Waves Setup")
        {
            // Initialize values
            _adcpConn = IoC.Get<AdcpConnection>();
            _adcpConn.ReceiveDataEvent += new AdcpConnection.ReceiveDataEventHandler(_adcpConnection_ReceiveDataEvent);
            _pm = IoC.Get<PulseManager>();

            SubsystemConfigList = new ReactiveList<WavesSubsystemConfigurationViewModel>();

            _AdcpCommandHistory = new ObservableCollection<string>();

            // Get the last used config
            _AdcpConfig = _pm.GetAdcpConfig();

            // Initialize list
            Init();

            // Update the subsystem config list
            var configs = _AdcpConfig.SubsystemConfigDict.Values.ToArray();
            for (int x = 0; x < configs.Length; x++)
            {
                SubsystemConfigList.Add(new WavesSubsystemConfigurationViewModel(ref configs[x], this));
            }

            // Update the deployment duration for all the VM
            UpdateDeploymentDuration();

            // Create the Subsystem List
            ListOfSubsystems = new SubsystemList();

            // Send BREAK command
            SendBreakCommand = ReactiveCommand.CreateAsyncTask(_ => Task.Run(() => SendBreak()));

            // Send Format SD card command
            FormatSdCommand = ReactiveCommand.CreateAsyncTask(_ => Task.Run(() => FormatSdCard()));

            // Read the ADCP time
            ReadAdcpTimeCommand = ReactiveCommand.CreateAsyncTask(_ => Task.Run(() => ReadAdcpTimeFromAdcp()));

            // Send BREAK command
            StopDvlCommand = ReactiveCommand.CreateAsyncTask(_ => Task.Run(() => StopAdcp()));

            // Read the ADCP command
            ReadAdcpCommand = ReactiveCommand.CreateAsyncTask(_ => Task.Run(() => ReadAdcp()));

            // Send command to the terminal
            SendCommand = ReactiveCommand.CreateAsyncTask(_ => Task.Run(() => SendTerminal()));

            // Clear the terminal
            ClearTerminalCommand = ReactiveCommand.CreateAsyncTask(_ => Task.Run(() => ClearTerminal()));

            // Clear the command set
            ClearCommandSetCommand = ReactiveCommand.CreateAsyncTask(_ => Task.Run(() => ClearCommandSet()));

            // Set the ADCP time
            SetAdcpTimeCommand = ReactiveCommand.CreateAsyncTask(_ => Task.Run(() => SetAdcpTime()));

            // Set the CETFP to the ADCP
            SetStartTimeCommand = ReactiveCommand.CreateAsyncTask(_ => Task.Run(() => SetCETFPtoAdcp()));

            // Send the command set to the ADCP
            SendCommandSetCommand = ReactiveCommand.CreateAsyncTask(_ => Task.Run(() => SendCommandSet()));

            // Send the command start the DVL
            SendDvlStartCommand = ReactiveCommand.CreateAsyncTask(_ => Task.Run(() => SendStart()));

            // Save the command set to a file
            SaveCommandSetCommand = ReactiveCommand.CreateAsyncTask(_ => Task.Run(() => SaveCommandSet()));

            // Command to zero the pressure sensor
            ZeroPressureSensorCommand = ReactiveCommand.CreateAsyncTask(_ => Task.Run(() => ZeroPressureSensor()));

            // Command to zero the pressure sensor
            SetDefaultCommand = ReactiveCommand.CreateAsyncTask(_ => Task.Run(() => SetDefaultOptions()));

            // Command to reset the CETFP value
            RefreshCetfpCommand = ReactiveCommand.CreateAsyncTask(_ => Task.Run(() => SetCETFP()));

            // Add Subsystem
            AddSubsystemCommand = ReactiveCommand.Create();
            AddSubsystemCommand.Subscribe(_ => AddSubsystem());

            // Import an ADCP command set
            ImportCommandSetCommand = ReactiveCommand.Create();
            ImportCommandSetCommand.Subscribe(_ => ImportCommandSet());

            // Update the command set.
            UpdateCommandSet();
        }

        /// <summary>
        /// Shutdown the view model.
        /// </summary>
        public override void Dispose()
        {
            if (_adcpConn != null)
            {
                _adcpConn.ReceiveDataEvent -= _adcpConnection_ReceiveDataEvent;
            }
        }

        #region Init

        /// <summary>
        /// Initialize all the list.
        /// </summary>
        private void Init()
        {
            ReadAdcpTime = "";
            IsSetStartTimeSet = true;
            CETFP = AdcpConfig.Commands.CETFP;

            // CWT
            CWT = AdcpConfig.Commands.CWT;

            // CWS
            // Default salinity is 35 ppt for the ocean
            CWS = 35;

            // SPOS
            SPOS_Latitude = AdcpConfig.Commands.SPOS_Latitude.DecimalDegrees;
            SPOS_Longitude = AdcpConfig.Commands.SPOS_Longitude.DecimalDegrees;
            SPOS_PsensHeight = AdcpConfig.Commands.SPOS_PsensHeight;
            SPOS_WaterDepth = AdcpConfig.Commands.SPOS_WaterDepth;

            // CEOUTPUT
            if(AdcpConfig.Commands.CEOUTPUT != Commands.AdcpCommands.AdcpOutputMode.Disable)
            {
                CEOUTPUT = true;
            }
            else
            {
                CEOUTPUT = false;
            }

            // CERECORD
            if (AdcpConfig.Commands.CERECORD_EnsemblePing != Commands.AdcpCommands.AdcpRecordOptions.Disable)
            {
                CERECORD = true;
            }
            else
            {
                CERECORD = false;
            }

            // Additional Command Set
            AdditionalAdcpCommandSet = AdcpConfig.AdditionalCommands;

            // Prediction model
            DeploymentDuration = AdcpConfig.DeploymentOptions.Duration;
            PredictedPowerUsage = "";
            PredictedMemoryUsage = "";

            // Default options
            DefaultOptionsList = new ReactiveList<DefaultOptionsSelections>();
            DefaultOptionsList.Add(new DefaultOptionsSelections(Subsystem.SUB_1_2MHZ_4BEAM_20DEG_PISTON_2));
            DefaultOptionsList.Add(new DefaultOptionsSelections(Subsystem.SUB_1_2MHZ_4BEAM_20DEG_PISTON_2, Subsystem.SUB_1_2MHZ_VERT_PISTON_A));
            DefaultOptionsList.Add(new DefaultOptionsSelections(Subsystem.SUB_600KHZ_4BEAM_20DEG_PISTON_3));
            DefaultOptionsList.Add(new DefaultOptionsSelections(Subsystem.SUB_600KHZ_4BEAM_20DEG_PISTON_3, Subsystem.SUB_600KHZ_VERT_PISTON_B));
            DefaultOptionsList.Add(new DefaultOptionsSelections(Subsystem.SUB_600KHZ_4BEAM_20DEG_PISTON_3, Subsystem.SUB_1_2MHZ_4BEAM_20DEG_PISTON_45OFFSET_6));
            DefaultOptionsList.Add(new DefaultOptionsSelections(Subsystem.SUB_300KHZ_4BEAM_20DEG_PISTON_4));
            DefaultOptionsList.Add(new DefaultOptionsSelections(Subsystem.SUB_300KHZ_4BEAM_20DEG_PISTON_4, Subsystem.SUB_300KHZ_VERT_PISTON_C));
            DefaultOptionsList.Add(new DefaultOptionsSelections(Subsystem.SUB_300KHZ_4BEAM_20DEG_PISTON_4, Subsystem.SUB_1_2MHZ_4BEAM_20DEG_PISTON_45OFFSET_6));
            DefaultOptionsList.Add(new DefaultOptionsSelections(Subsystem.SUB_300KHZ_4BEAM_20DEG_PISTON_4, Subsystem.SUB_600KHZ_4BEAM_20DEG_PISTON_45OFFSET_7));
            SelectedDefaultOption = DefaultOptionsList[0];

            InitButtons();
        }

        #endregion

        #region Command Set

        /// <summary>
        /// Get the command set from the configuration created.
        /// </summary>
        /// <returns>List of all the commands.</returns>
        private List<string> GetCommandSet()
        {
            List<string> commands = new List<string>();

            if (_AdcpConfig != null)
            {
                // Add the system commands
                commands.AddRange(_AdcpConfig.Commands.GetWavesCommandList());

                // Add the subsystem commands
                foreach(var vm in SubsystemConfigList)
                {
                    commands.AddRange(vm.GetCommandList());
                }
            }

            // Add Additional Commands
            commands.Add(_AdditionalAdcpCommandSet);

            // Add the CSAVE command
            commands.Add(RTI.Commands.AdcpCommands.CMD_CSAVE);

            return commands;
        }

        /// <summary>
        /// Update the command set with the latest information.
        /// </summary>
        public void UpdateCommandSet()
        {
            // Get the command set from the configuration
            List<string> commands = GetCommandSet();

            // Update the string
            UpdateCommandSetStr(commands);

            // Update the prediction model
            UpdatePrediction();

            // Save the config to the options
            _pm.UpdateAdcpConfig(_AdcpConfig);
        }

        /// <summary>
        /// Create a string of all the commands.
        /// </summary>
        /// <param name="commands">Commands to create the string.</param>
        private void UpdateCommandSetStr(List<string> commands)
        {
            // Go through all the commands
            StringBuilder sb = new StringBuilder();
            foreach(var cmd in commands)
            {
                sb.AppendLine(cmd);
            }

            // Update the string
            AdcpCommandSet = sb.ToString();
        }

        /// <summary>
        /// Array of all the commands.
        /// </summary>
        /// <returns></returns>
        private string[] GetCommandSetStrLines()
        {
            string[] strs = AdcpCommandSet.Split('\n');

            return strs;
        }

        #endregion

        #region Configurations

        /// <summary>
        /// Setup the configurations based off the
        /// currently set AdcpConfig.
        /// </summary>
        private void SetupConfiguration()
        {
            HardwareConnection = AdcpConfig.EngPort;

            System.Windows.Application.Current.Dispatcher.Invoke(new System.Action(() =>
            {
                // Add VM and update subsystem list
                ListOfSubsystems.Clear();
                var configs = _AdcpConfig.SubsystemConfigDict.Values.ToArray();

                for (int x = 0; x < configs.Length; x++)
                {
                    // Update Subsystem List
                    ListOfSubsystems.Add(new SubsystemList.SubsystemCodeDesc(configs[x].SubsystemConfig.SubSystem.Code));

                    // Have it select the first option by default
                    if (ListOfSubsystems.Count >= 1)
                    {
                        SelectedSubsystem = ListOfSubsystems[0];
                    }

                    // Update the command set.
                    UpdateCommandSet();

                    // Add VM
                    AddVM(ref configs[x]);
                }
            }));
        }

        /// <summary>
        /// Add a configuration to the list.
        /// This will add the configuration to the ADCP Config and 
        /// also create a VM to update its values.
        /// </summary>
        /// <param name="code"></param>
        private void AddConfiguration(byte code)
        {
            // Create a subsystem
            var ss = new Subsystem(code);

            // Add the subsytem to the serial number if it does not exist
            // CEPO will be validated against the serial number
            _AdcpConfig.SerialNumber.AddSubsystem(ss);

            AdcpSubsystemConfig config = null;
            _AdcpConfig.AddConfiguration(ss, out config);

            // Turn Off BT
            config.Commands.CBTON = false;

            // Update the command set.
            UpdateCommandSet();

            // Add the VM
            AddVM(ref config);
        }

        /// <summary>
        /// Add the VM to the list.
        /// </summary>
        /// <param name="config">Subsystem config.</param>
        private void AddVM(ref AdcpSubsystemConfig config)
        {
            // Create the VM
            var wavesSubConfig = new WavesSubsystemConfigurationViewModel(ref config, this);

            if (wavesSubConfig != null)
            {
                System.Windows.Application.Current.Dispatcher.Invoke(new System.Action(() =>
                {
                    // Set the deployment duration for the prediction model
                    wavesSubConfig._PredictionModelInput.DeploymentDuration = _DeploymentDuration;

                    SubsystemConfigList.Add(wavesSubConfig);
                    this.NotifyOfPropertyChange(() => this.SubsystemConfigList);

                    // Update the command list
                    UpdateCommandSet();
                }));
            }
        }

        /// <summary>
        /// Remove the view model.
        /// </summary>
        /// <param name="vm">Viewmodel to remove.</param>
        public void RemoveVM(WavesSubsystemConfigurationViewModel vm)
        {
            // Remove the subsystem config from the config
            _AdcpConfig.RemoveAdcpSubsystemConfig(vm.AdcpSubConfig);

            // If it is last subsystem config in the list,
            // remove the subsystem from the serial number
            bool containSS = false;
            foreach (var config in _AdcpConfig.SubsystemConfigDict.Values)
            {
                if (config.SubsystemConfig.SubSystem == vm.AdcpSubConfig.SubsystemConfig.SubSystem)
                {
                    containSS = true;
                }
            }
            if(!containSS)
            {
                _AdcpConfig.SerialNumber.RemoveSubsystem(vm.AdcpSubConfig.SubsystemConfig.SubSystem);
            }

            // Remove the vm from the list
            System.Windows.Application.Current.Dispatcher.Invoke(new System.Action(() => SubsystemConfigList.Remove(vm)));

            // Dispose of the VM
            vm.Dispose();

            // Update the commandset
            UpdateCommandSet();
        }

        /// <summary>
        /// Clear the configuration.
        /// </summary>
        private void ClearConfiguration()
        {
            // Clear the command set
            ClearCommandSet();

            // Remove the old configuraon
            System.Windows.Application.Current.Dispatcher.Invoke(new System.Action(() => SubsystemConfigList.Clear()));
        }

        #endregion

        #region Commands

        /// <summary>
        /// Send a BREAK statement.
        /// </summary>
        private void SendBreak()
        {
            // Ensure the serial port is open
            if (_adcpConn.IsOpen())
            {
                _adcpConn.SendBreak();
            }
        }

        /// <summary>
        /// Send a command to format the SD card.
        /// Warn the user that the data will be deleted
        /// if they continue.
        /// </summary>
        private void FormatSdCard()
        {
            // Verify the user want to delete all the files
            if (System.Windows.MessageBox.Show("Are you sure you want to delete all the files?", "SD Card Format Warning", MessageBoxButton.OKCancel, MessageBoxImage.Warning) == MessageBoxResult.OK)
            {
                // Ensure the serial port is open
                if (_adcpConn.IsOpen())
                {                    // Send the command to format the SD card
                    _adcpConn.SendData(RTI.Commands.AdcpCommands.CMD_DSFORMAT);

                    Button3 = true;
                }
            }
        }

        /// <summary>
        /// Read in the ADCP time.
        /// </summary>
        private void ReadAdcpTimeFromAdcp()
        {
            // Ensure the serial port is open
            if (_adcpConn.IsOpen())
            {
                DateTime dt = _adcpConn.GetAdcpDateTime();
                ReadAdcpTime = dt.ToShortDateString() + " " + dt.ToShortTimeString();

                if(IsSetStartTimeSet)
                {
                    CETFP = dt;
                }

                Button6 = true;
            }
        }

        /// <summary>
        /// Send the CETFP command.
        /// </summary>
        private void SetCETFPtoAdcp()
        {
            if(_adcpConn.IsOpen())
            {
                // CETFP
                _adcpConn.SendData(_AdcpConfig.Commands.CETFP_CmdStr());

                // CSAVE
                _adcpConn.SendData(RTI.Commands.AdcpCommands.CMD_CSAVE);

                Button7 = true;
            }
        }

        /// <summary>
        /// Set the CETFP to the current date and time.
        /// </summary>
        private void SetCETFP()
        {
            CETFP = DateTime.Now;
        }

        /// <summary>
        /// Send a command to zero the pressure sensor.
        /// </summary>
        private void ZeroPressureSensor()
        {
            if (_adcpConn.IsOpen())
            {
                // CPZ
                _adcpConn.SendData(RTI.Commands.AdcpCommands.CMD_CPZ);

                Button8 = true;
            }
        }

        private void SetDefaults()
        {
            // Set all the default values
            _AdcpConfig.Commands.SetDefaults();
            //foreach (var subConfig in _AdcpConfig.SubsystemConfigDict.Values)
            //{
            //    subConfig.SetDefault();
            //}
            foreach (var vm in SubsystemConfigList)
            {
                vm.AdcpSubConfig.SetDefault();
                vm.Init();
            }


            // Reinitialize the view
            Init();
        }

        /// <summary>
        /// Send a STOP command to the DVL.
        /// </summary>
        private void StopAdcp()
        {
            InitButtons();

            // Ensure the serial port is open
            if (_adcpConn.IsOpen())
            {
                _adcpConn.StopPinging();

                Button1 = true;
            }
        }

        /// <summary>
        /// Read the settings from the ADCP.
        /// </summary>
        private void ReadAdcp()
        {
            if(_adcpConn.IsOpen())
            {
                // Store the deployment options
                var deploymentOptions = _AdcpConfig.DeploymentOptions;

                // Clear configuration
                ClearConfiguration();

                // Get the configruation
                AdcpConfig = _adcpConn.GetAdcpConfiguration();

                // Setup the configuraion
                SetupConfiguration();

                // Set Deployment Duration
                _AdcpConfig.DeploymentOptions = deploymentOptions;

                // Update the prediction model
                UpdatePrediction();

                Button2 = true;
            }            
        }

        /// <summary>
        /// Send a command to the terminal.
        /// </summary>
        private void SendTerminal()
        {
            // Send the command
            _adcpConn.SendDataWaitReply(SelectedAdcpCommand);

            // Clear the command
            SelectedAdcpCommand = "";
        }

        /// <summary>
        /// Clear the terminal.
        /// </summary>
        private void ClearTerminal()
        {
            _adcpConn.ReceiveBufferString = "";
            this.NotifyOfPropertyChange(() => this.AdcpReceiveBuffer);
        }

        /// <summary>
        /// Clear the Command set.
        /// </summary>
        private void ClearCommandSet()
        {
            AdcpCommandSet = "";
        }

        /// <summary>
        /// Set the ADCP time.
        /// </summary>
        private void SetAdcpTime()
        {
            _adcpConn.SetLocalSystemDateTime();

            Button5 = true;
        }

        /// <summary>
        /// Send the command set to the ADCP.
        /// </summary>
        private void SendCommandSet()
        {
            // Get the command set from the configuration
            //List<string> commands = GetCommandSet();
            List<string> commands = GetCommandSetStrLines().ToList();

            // Send the commands to the ADCP
            _adcpConn.SendCommands(commands);

            Button4 = true;
        }

        /// <summary>
        /// Send the command start pinging the DVL.
        /// </summary>
        private void SendStart()
        {
            _adcpConn.SendData(RTI.Commands.AdcpCommands.CMD_START_PINGING);

            Button9 = true;
        }

        /// <summary>
        /// Save the command set to a file.
        /// </summary>
        private void SaveCommandSet()
        {
            try
            {
                // Get the project dir
                // Create the file name
                string prjDir = @"c:\RTI_Configuration_Files";

                // Create the directory if it does not exist
                if(!Directory.Exists(prjDir))
                {
                    Directory.CreateDirectory(prjDir);
                }

                DateTime now = DateTime.Now;
                string year = now.Year.ToString("0000");
                string month = now.Month.ToString("00");
                string day = now.Day.ToString("00");
                string hours = now.Hour.ToString("00");
                string minutes = now.Minute.ToString("00");
                string seconds = now.Second.ToString("00");
                string fileName = string.Format("Commands_{0}{1}{2}{3}{4}{5}.txt", year, month, day, hours, minutes, seconds);
                string cmdFilePath = prjDir + @"\" + fileName;

                // Get the commands
                //string[] lines = GetCommandSet().ToArray();
                string[] lines = GetCommandSetStrLines();

                // Create a text file in the project
                System.IO.File.WriteAllLines(cmdFilePath, lines);

                // Set the filepath to the console output
                _adcpConn.ReceiveBufferString = "";
                _adcpConn.ReceiveBufferString = "File saved to: " + cmdFilePath;
                this.NotifyOfPropertyChange(() => this.AdcpReceiveBuffer);
            }
            catch (Exception) { }
        }

        /// <summary>
        /// Import a command set from a file.
        /// </summary>
        private void ImportCommandSet()
        {
            string fileName = "";
            try
            {
                // Show the FolderBrowserDialog.
                System.Windows.Forms.OpenFileDialog dialog = new System.Windows.Forms.OpenFileDialog();
                dialog.Filter = "All files (*.*)|*.*";
                dialog.Multiselect = false;

                System.Windows.Forms.DialogResult result = dialog.ShowDialog();
                if (result == System.Windows.Forms.DialogResult.OK)
                {
                    // Get the files selected
                    fileName = dialog.FileName;

                    // Clear configuration
                    ClearConfiguration();

                    // Set the command set
                    AdcpCommandSet = File.ReadAllText(fileName);

                    // Decode the command set to apply to the configuration
                    //AdcpConfig = RTI.Commands.AdcpCommands.DecodeCSHOW(AdcpCommandSet, new SerialNumber());
                    AdcpConfig = RTI.Commands.AdcpCommands.DecodeCommandSet(AdcpCommandSet, new SerialNumber());

                    // Setup the configuraion
                    SetupConfiguration();
                }
            }
            catch (Exception e)
            {
                log.Error(string.Format("Error reading command set from {0}", fileName), e);
            }
        }

        /// <summary>
        /// Add a subsystem to the configuration and add a display.
        /// </summary>
        private void AddSubsystem()
        {
            if (_selectedSubsystem != null)
            {
                // Add config
                AddConfiguration(_selectedSubsystem.Code);
            }
        }

        #endregion

        #region Burst Mode

        /// <summary>
        /// Enable or disable burst mode for all the
        /// VM.
        /// </summary>
        /// <param name="flag">Flag to send.</param>
        public void UpdateBurstMode(bool flag)
        {
            // Update the VM
            foreach (var vm in SubsystemConfigList)
            {
                vm.UpdateBurstMode(flag);
            }
        }
        
        /*foreach(var vm in SubsystemConfigList)
            {
                if(vm == wvm)
                {
                    return vm.Desc.Substring(1, 1);
                }
            }*/

///<summary>
///Called by a specific subsystem to see if it is connected to any other subsystems
/// </summary>
public string setUpBurstInterleavedWarning(WavesSubsystemConfigurationViewModel wvm)
        {
            string subsystemNumber = "";
            
            foreach (var vm in SubsystemConfigList)
            {
                if (vm == wvm)
                {
                    subsystemNumber = vm.Desc.Substring(1, 1);
                }
            }
            foreach(var VM in SubsystemConfigList)
            {
                if (VM.Desc.Substring(1, 1) == subsystemNumber) return (int.Parse(subsystemNumber)+1).ToString();
            }
            return null;
        }
        /// <summary>
        /// Enable or disable burst mode interleaving for all the
        /// VM.
        /// </summary>
        /// <param name="flag">Flag to send.</param>
        public void UpdateBurstModeInterleaved(bool flag)
        {
            // Update the VM
            // This should be in a pair
            // So only a 4 beam and vertical should match
            foreach (var vm in SubsystemConfigList)
            {
                vm.UpdateBurstModeInterleaved(flag);
            }
        }

        #endregion

        #region CEI

        /// <summary>
        /// Share the CEI with all the VM.
        /// </summary>
        /// <param name="value">Value to send.</param>
        public void UpdateCEI(float value)
        {
            AdcpConfig.Commands.CEI = new RTI.Commands.TimeValue(value);

            // Update the VM
            foreach (var vm in SubsystemConfigList)
            {
                vm.UpdateCEI(value);
            }
        }

        #endregion

        #region Prediction Model

        /// <summary>
        /// Update the deployment duration to
        /// the VM predictors.
        /// </summary>
        private void UpdateDeploymentDuration()
        {
            // Update the VM
            foreach (var vm in SubsystemConfigList)
            {
                vm._PredictionModelInput.DeploymentDuration = DeploymentDuration;
            }

            // Update the prediction model
            UpdatePrediction();
        }

        /// <summary>
        /// Update with the latest prediction model values
        /// for battery and memory usage.
        /// </summary>
        private void UpdatePrediction()
        {
            double predictedPowerUsage = 0.0;    // watt/hr
            long predictedMemoryUsage = 0;         // bytes
            double numBatt = 0.0;

            // Update the VM
            foreach (var vm in SubsystemConfigList)
            {
                predictedMemoryUsage += vm.GetMemoryUsage();

                // Power Usage
                var ssWattHrUsage = vm.GetWattHrUsage();
                predictedPowerUsage += ssWattHrUsage;
                vm.PredictedPowerUsage = ssWattHrUsage.ToString("0.00") + " Watt/Hr";           // Set the VM for the pwr usage
                numBatt += vm.GetTotalBatteryUsage(); 

                //if (!vm.CBI_Enabled)
                //{
                    //predictedBatteryUsage += vm.AdcpPredictor.TotalPower;
                    //predictedMemoryUsage += vm.AdcpPredictor.DataSizeBytes;
                    //numBatt += vm.AdcpPredictor.NumberBatteryPacks;
                    
                //}
                //else
                //{
                //    int samplesPerBurst = vm.CBI_NumEnsembles;
                    

                //    // Calculate burst power usage
                //    predictedMemoryUsage += AdcpPredictor.WavesRecordBytesPerBurst(samplesPerBurst, vm.CWPBN);
                //    //predictedBatteryUsage += AdcpPredictor.WavesWattHoursPerBurst
                //}
            }


            PredictedPowerUsage = predictedPowerUsage.ToString("0.00") + " watt/hr";
            PredictedMemoryUsage = MathHelper.MemorySizeString(predictedMemoryUsage);
            PredictedNumberOfBatteries = numBatt.ToString("0.0") + " batteries"; 

            this.NotifyOfPropertyChange(() => this.PredictedPowerUsage);
            this.NotifyOfPropertyChange(() => this.PredictedMemoryUsage);
            this.NotifyOfPropertyChange(() => this.PredictedNumberOfBatteries);
        }

        #endregion

        #region Default Options

        /// <summary>
        /// Set the default options based off the selected subsystem.
        /// </summary>
        private void SetDefaultOptions()
        {
            // Clear out the current subsystems
            int count = SubsystemConfigList.Count;
            for (int x = 0; x < count ; x++)
            {
                RemoveVM(SubsystemConfigList.Last());
            }

            float blank = 0.0f;
            float binSize = 0.0f;
            float lag = 0.0f;

            // Subsystem 1 defaults
            ProfileDefaults(SelectedDefaultOption.Code1, out blank, out binSize, out lag);

            // Add the subsystem to the list
            AddConfiguration(SelectedDefaultOption.Code1);

            // Set the default values
            SubsystemConfigList[0].CBI_Enabled = true;
            SubsystemConfigList[0].CBI_BurstInterval = 3600;
            SubsystemConfigList[0].CBI_NumEnsembles = 4096;
            SubsystemConfigList[0].CEI = 0.4f;
            SubsystemConfigList[0].CWPBN = 30;
            SubsystemConfigList[0].CWPBL = blank;
            SubsystemConfigList[0].CWPBS = binSize;
            SubsystemConfigList[0].CWPBB_LagLength = lag;
            

            // Ensure a second subsystem is selected
            if(SelectedDefaultOption.Code2 != 0)
            {
                float blank2 = 0.0f;
                float binSize2 = 0.0f;
                float lag2 = 0.0f;

                // Subsystem 2 defaults
                ProfileDefaults(SelectedDefaultOption.Code2, out blank2, out binSize2, out lag2);

                // Add the subsystem to the list
                AddConfiguration(SelectedDefaultOption.Code2);

                // Set the default values
                SubsystemConfigList[1].CBI_Enabled = true;
                SubsystemConfigList[1].CBI_BurstInterval = 3600;
                SubsystemConfigList[1].CBI_NumEnsembles = 4096;
                SubsystemConfigList[1].CEI = 0.4f;
                SubsystemConfigList[1].CWPBN = 30;
                SubsystemConfigList[1].CWPBL = blank2;
                SubsystemConfigList[1].CWPBS = binSize2;
                SubsystemConfigList[1].CWPBB_LagLength = lag2;

                // Set Interleaved for first and second subsystem
                // Only the first subsystem needs to be turned on
                SubsystemConfigList[0].CBI_Interleaved = true;
                SubsystemConfigList[1].CBI_Interleaved = false;

            }

            // Other settings
            CEOUTPUT = true;
            CERECORD = true;
            DeploymentDuration = 10;
            SPOS_Latitude = 0.0;
            SPOS_Longitude = 0.0;
            SPOS_WaterDepth = 10.0f;
            SPOS_PsensHeight = 0.5f;
            CWT = 10.0f;
            CWS = 35;

        }

        /// <summary>
        /// Get the default values based off the subsystem code.
        /// </summary>
        /// <param name="ssCode">Subsystem code.</param>
        /// <param name="Blank">Blank value.</param>
        /// <param name="BinSize">Bin size.</param>
        /// <param name="Lag">Lag.</param>
        public static void ProfileDefaults(byte ssCode, out float Blank, out float BinSize, out float Lag)
        {
            Blank = 0;
            BinSize = 0;
            Lag = 0;
            switch (ssCode)
            {
                default:
                    break;
                case Subsystem.SUB_1_2MHZ_VERT_PISTON_A:
                case Subsystem.SUB_1_2MHZ_4BEAM_20DEG_PISTON_2:
                case Subsystem.SUB_1_2MHZ_4BEAM_20DEG_PISTON_45OFFSET_6:
                    Blank = 0.5f;
                    BinSize = .25f;
                    Lag = 0.10f;
                    break;
                case Subsystem.SUB_600KHZ_VERT_PISTON_B:
                case Subsystem.SUB_600KHZ_4BEAM_20DEG_PISTON_3:
                case Subsystem.SUB_600KHZ_4BEAM_20DEG_PISTON_45OFFSET_7:
                    Blank = 0.5f;
                    BinSize = 0.5f;
                    Lag = 0.20f;
                    break;
                case Subsystem.SUB_600KHZ_4BEAM_30DEG_ARRAY_I:
                    Blank = 0.5f;
                    BinSize = 0.5f;
                    Lag = 0.20f;
                    break;
                case Subsystem.SUB_600KHZ_4BEAM_15DEG_ARRAY_O:
                    Blank = 0.5f;
                    BinSize = 0.5f;
                    Lag = 0.20f;
                    break;
                case Subsystem.SUB_300KHZ_VERT_PISTON_C:
                case Subsystem.SUB_300KHZ_4BEAM_20DEG_PISTON_4:
                case Subsystem.SUB_300KHZ_4BEAM_20DEG_PISTON_45OFFSET_8:
                    Blank = 1.0f;
                    BinSize = 1.0f;
                    Lag = 0.40f;
                    break;
                case Subsystem.SUB_300KHZ_4BEAM_30DEG_ARRAY_J:
                    Blank = 1.0f;
                    BinSize = 1.0f;
                    Lag = 0.40f;
                    break;
                case Subsystem.SUB_300KHZ_4BEAM_15DEG_ARRAY_P:
                    Blank = 1.0f;
                    BinSize = 1.0f;
                    Lag = 0.40f;
                    break;
                case Subsystem.SUB_150KHZ_4BEAM_30DEG_ARRAY_K:
                    Blank = 2.0f;
                    BinSize = 2.0f;
                    Lag = 0.80f;
                    break;
                case Subsystem.SUB_150KHZ_4BEAM_15DEG_ARRAY_Q:
                    Blank = 2.0f;
                    BinSize = 2.0f;
                    Lag = 0.80f;
                    break;
                case Subsystem.SUB_75KHZ_4BEAM_30DEG_ARRAY_L:
                    Blank = 4.0f;
                    BinSize = 4.0f;
                    Lag = 1.60f;
                    break;
                case Subsystem.SUB_75KHZ_4BEAM_15DEG_ARRAY_R:
                    Blank = 4.0f;
                    BinSize = 4.0f;
                    Lag = 1.60f;
                    break;
                case Subsystem.SUB_38KHZ_4BEAM_30DEG_ARRAY_M:
                    Blank = 8.0f;
                    BinSize = 8.0f;
                    Lag = 3.20f;
                    break;
                case Subsystem.SUB_38KHZ_4BEAM_15DEG_ARRAY_S:
                    Blank = 8.0f;
                    BinSize = 8.0f;
                    Lag = 3.20f;
                    break;
                case Subsystem.SUB_20KHZ_4BEAM_30DEG_ARRAY_N:
                    Blank = 16.0f;
                    BinSize = 16.0f;
                    Lag = 6.40f;
                    break;
                case Subsystem.SUB_20KHZ_4BEAM_15DEG_ARRAY_T:
                    Blank = 16.0f;
                    BinSize = 16.0f;
                    Lag = 6.40f;
                    break;
            }
        }

        #endregion

        #region Button Click

        /// <summary>
        /// Initilize the buttons.
        /// </summary>
        private void InitButtons()
        {
            Button1 = false;
            Button2 = false;
            Button3 = false;
            Button4 = false;
            Button5 = false;
            Button6 = false;
            Button7 = false;
            Button8 = false;
            Button9 = false;
        }

        #endregion

        #region EventHandler

        /// <summary>
        /// Event handler when receiving serial data.
        /// </summary>
        /// <param name="data">Data received from the serial port.</param>
        private void _adcpConnection_ReceiveDataEvent(byte[] data)
        {
            this.NotifyOfPropertyChange(() => this.AdcpReceiveBuffer);
        }

        #endregion

    }
}
