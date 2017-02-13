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
 * 09/26/2014      RC          4.1.0       Initial coding
 * 11/03/2015      RC          1.0.0       Added UpdateCEI() to update all the CEI in each VM.
 * 11/04/2015      RC          1.0.0       Added CBI_Interleaved.
 * 03/16/2016      RC          1.1.3       Added Range Tracking.
 * 06/07/2016      RC          1.1.6       Fixed bug with prediction models for subsystem being out of sync with main prediciton model.
 *                                         Fixed bug where CWPP was 1 but CWPTBP had a value.
 */

using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RTI
{
    /// <summary>
    /// Waves subsystem configuration.
    /// </summary>
    public class WavesSubsystemConfigurationViewModel : PulseViewModel
    {
        #region Variables

        /// <summary>
        /// Waves Setup VM to remove itself.
        /// </summary>
        private WavesSetupViewModel _wavesSetupVM;

        /// <summary>
        /// Subsystem configuration to set the commands.
        /// </summary>
        public AdcpSubsystemConfig AdcpSubConfig { get; set; }

        /// <summary>
        /// String for Range Tracking Mode Off.
        /// </summary>
        private const string RangeTrackingModeOff = "OFF";

        /// <summary>
        /// String for Range Tracking Mode Off.
        /// </summary>
        private const string RangeTrackingModeBin = "BIN";

        /// <summary>
        /// String for Range Tracking Mode Off.
        /// </summary>
        private const string RangeTrackingModePressure = "PRESSURE";

        #endregion

        #region Properties

        /// <summary>
        /// String of the description of subsystem configuration.
        /// </summary>
        private string _Desc;
        /// <summary>
        /// String of the description of subsystem configuration.
        /// </summary>
        public string Desc
        {
            get { return _Desc; }
            set
            {
                _Desc = value;
                this.NotifyOfPropertyChange(() => this.Desc);
            }
        }

        #region CBI

        /// <summary>
        /// Enable the CBI command for burst mode.
        /// </summary>
        private bool _CBI_Enabled;
        /// <summary>
        /// Enable the CBI command for burst mode.
        /// </summary>
        public bool CBI_Enabled
        {
            get { return _CBI_Enabled; }
            set
            {
                _CBI_Enabled = value;
                this.NotifyOfPropertyChange(() => this.CBI_Enabled);
                this.NotifyOfPropertyChange(() => this.CBI_Disabled);

                // Update the display
                _wavesSetupVM.UpdateCommandSet();

                // Update the prediction model
                UpdatePredictionModel();

                // Update all the other VM that
                // we are in burst mode now
                _wavesSetupVM.UpdateBurstMode(value);
            }
        }

        /// <summary>
        /// Opposite of CBI_Enabled.
        /// </summary>
        public bool CBI_Disabled
        {
            get
            {
                return !CBI_Enabled;
            }
        }

        /// <summary>
        /// CBI description.
        /// </summary>
        public string CBI_Desc
        {
            get
            {
                return Commands.AdcpSubsystemCommands.GetCbiDesc();
            }
        }

        /// <summary>
        /// CBI Burst Interval.
        /// </summary>
        public float CBI_BurstInterval
        {
            get { return (float)AdcpSubConfig.Commands.CBI_BurstInterval.ToSecondsD(); }
            set
            {
                 AdcpSubConfig.Commands.CBI_BurstInterval = new RTI.Commands.TimeValue(value);

                // Update the display
                _wavesSetupVM.UpdateCommandSet();

                this.NotifyOfPropertyChange(() => this.CBI_BurstInterval);
                this.NotifyOfPropertyChange(() => this.CBI_DescStr);
                this.NotifyOfPropertyChange(() => this.ShowCbiWarning);
                this.NotifyOfPropertyChange(() => this.CBI_WarningStr);
            }
        }

        /// <summary>
        /// CBI Burst Interleaved flag.  If this is set true, then all the burst will occur at the same time.
        /// If this is false, then the first burst will occur the the next burst after the first is completed.
        /// </summary>
        public bool CBI_Interleaved
        {
            get { return (bool)AdcpSubConfig.Commands.CBI_BurstPairFlag; }
            set
            {
                //AdcpSubConfig.Commands.CBI_BurstPairFlag = value;
                //this.NotifyOfPropertyChange(() => this.CBI_Interleaved);

                //this.NotifyOfPropertyChange(() => this.CBI_BurstInterval);
                //this.NotifyOfPropertyChange(() => this.CBI_DescStr);
                //this.NotifyOfPropertyChange(() => this.ShowCbiWarning);
                //this.NotifyOfPropertyChange(() => this.CBI_WarningStr);

                //// Update all the other VM that
                //// we are in burst mode interleaved now
                //_wavesSetupVM.UpdateBurstModeInterleaved(value);
                UpdateBurstModeInterleaved(value);


            }
        }

        /// <summary>
        /// CBI Number of ensembles.
        /// </summary>
        public ushort CBI_NumEnsembles
        {
            get { return AdcpSubConfig.Commands.CBI_NumEnsembles; }
            set
            {
                AdcpSubConfig.Commands.CBI_NumEnsembles = value;

                // Update the display
                _wavesSetupVM.UpdateCommandSet();

                this.NotifyOfPropertyChange(() => this.CBI_NumEnsembles);
                this.NotifyOfPropertyChange(() => this.CBI_DescStr);
                this.NotifyOfPropertyChange(() => this.ShowCbiWarning);
                this.NotifyOfPropertyChange(() => this.CBI_WarningStr);
            }
        }

        /// <summary>
        /// Burst Interval as a descriptive string.
        /// </summary>
        public string CBI_DescStr
        {
            get { return AdcpSubConfig.Commands.CBI_DescStr(_wavesSetupVM.AdcpConfig.Commands.CEI); }
        }

        ///<summary>
        ///Burst timing warning message for interleaved subsystems.
        /// </summary>
        public string _CBI_InterleavedMessage = "";
        public string CBI_InterleavedMessage
        {
            get { return _CBI_InterleavedMessage; }
            set { _CBI_InterleavedMessage = value;
                this.NotifyOfPropertyChange(() => this.CBI_InterleavedMessage);
                    }
        }

        /// <summary>
        /// Flag if a warning should be displayed.
        /// </summary>
        public bool ShowCbiWarning
        {
            get { return CBI_Enabled && !string.IsNullOrEmpty(CBI_WarningStr); }
        }

        /// <summary>
        /// Warning message if the CBI command is not properly set.
        /// </summary>
        public string CBI_WarningStr
        {
            get { return AdcpSubConfig.Commands.CBI_WarningStr(_wavesSetupVM.AdcpConfig.Commands.CEI); }
        }


        #endregion

        #region CWPBL

        /// <summary>
        /// CWPBL description.
        /// </summary>
        public string CWPBL_Desc
        {
            get
            {
                return Commands.AdcpSubsystemCommands.GetCwpblDesc();
            }
        }

        /// <summary>
        /// Water Profile blank.
        /// </summary>
        public float CWPBL
        {
            get { return AdcpSubConfig.Commands.CWPBL; }
            set
            {
                AdcpSubConfig.Commands.CWPBL = value;
                this.NotifyOfPropertyChange(() => this.CWPBL);

                // Update predictor
                AdcpPredictor.CWPBL = value;
                UpdatePredictionModel();

                // Update the display
                _wavesSetupVM.UpdateCommandSet();
            }
        }

        #endregion

        #region CWPBS

        /// <summary>
        /// CWPBS description.
        /// </summary>
        public string CWPBS_Desc
        {
            get
            {
                return Commands.AdcpSubsystemCommands.GetCwpbsDesc();
            }
        }

        /// <summary>
        /// Water Profile Bin Size.
        /// </summary>
        public float CWPBS
        {
            get { return AdcpSubConfig.Commands.CWPBS; }
            set
            {
                AdcpSubConfig.Commands.CWPBS = value;
                this.NotifyOfPropertyChange(() => this.CWPBS);

                // Update predictor
                AdcpPredictor.CWPBS = value;
                UpdatePredictionModel();

                // Update the display
                _wavesSetupVM.UpdateCommandSet();
            }
        }

        #endregion

        #region CWPBN

        /// <summary>
        /// CWPBN description.
        /// </summary>
        public string CWPBN_Desc
        {
            get
            {
                return Commands.AdcpSubsystemCommands.GetCwpbnDesc();
            }
        }

        /// <summary>
        /// Water Profile Number of Bins.
        /// </summary>
        public ushort CWPBN
        {
            get { return AdcpSubConfig.Commands.CWPBN; }
            set
            {
                AdcpSubConfig.Commands.CWPBN = value;
                this.NotifyOfPropertyChange(() => this.CWPBN);

                // Update predictor
                AdcpPredictor.CWPBN = value;
                UpdatePredictionModel();

                // Update the display
                _wavesSetupVM.UpdateCommandSet();
            }
        }

        #endregion

        #region CWPP

        /// <summary>
        /// CWPP description.
        /// </summary>
        public string CWPP_Desc
        {
            get
            {
                return Commands.AdcpSubsystemCommands.GetCwppDesc();
            }
        }

        /// <summary>
        /// Water Profile Number of pings per ensemble.
        /// </summary>
        public ushort CWPP
        {
            get { return AdcpSubConfig.Commands.CWPP; }
            set
            {
                // Do nothing if the value is to small
                if(value <= 0)
                {
                    value = 1;
                }


                // If there is only 1 ping, then set the TBP to 0
                if (value == 1)
                {
                    CWPTBP = 0;
                }

                AdcpSubConfig.Commands.CWPP = value;
                this.NotifyOfPropertyChange(() => this.CWPP);

                // Update predictor
                AdcpPredictor.CWPP = value;
                UpdatePredictionModel();

                // Update the display
                _wavesSetupVM.UpdateCommandSet();
            }
        }

        #endregion

        #region CWPBB

        /// <summary>
        /// CWPBB description.
        /// </summary>
        public string CWPBB_Desc
        {
            get
            {
                return Commands.AdcpSubsystemCommands.GetCwpbbDesc();
            }
        }

        /// <summary>
        /// Water Profile Broadband lag length.
        /// </summary>
        public float CWPBB_LagLength
        {
            get { return AdcpSubConfig.Commands.CWPBB_LagLength; }
            set
            {
                AdcpSubConfig.Commands.CWPBB_LagLength = value;
                this.NotifyOfPropertyChange(() => this.CWPBB_LagLength);

                // Update predictor
                AdcpPredictor.CWPBB_LagLength = value;
                UpdatePredictionModel();

                // Update the display
                _wavesSetupVM.UpdateCommandSet();
            }
        }

        #endregion

        #region CWPTBP

        /// <summary>
        /// CWPTBP description.
        /// </summary>
        public string CWPTBP_Desc
        {
            get
            {
                return Commands.AdcpSubsystemCommands.GetCwptbpDesc();
            }
        }

        /// <summary>
        /// Water Profile Time between pings.
        /// </summary>
        public float CWPTBP
        {
            get { return AdcpSubConfig.Commands.CWPTBP; }
            set
            {
                AdcpSubConfig.Commands.CWPTBP = value;
                this.NotifyOfPropertyChange(() => this.CWPTBP);

                // Update predictor
                AdcpPredictor.CWPTBP = value;
                UpdatePredictionModel();

                // Update the display
                _wavesSetupVM.UpdateCommandSet();
            }
        }

        #endregion

        #region CWPTBP

        /// <summary>
        /// CBTON description.
        /// </summary>
        public string CBTON_Desc
        {
            get
            {
                return Commands.AdcpSubsystemCommands.GetCbtonDesc();
            }
        }

        /// <summary>
        /// Bottom Track on or off.
        /// </summary>
        public bool CBTON
        {
            get { return AdcpSubConfig.Commands.CBTON; }
            set
            {
                AdcpSubConfig.Commands.CBTON = value;
                this.NotifyOfPropertyChange(() => this.CBTON);

                // Update predictor
                AdcpPredictor.CBTON = value;
                UpdatePredictionModel();

                // Update the display
                _wavesSetupVM.UpdateCommandSet();
            }
        }

        #endregion

        #region CEI

        /// <summary>
        /// CEI description.
        /// </summary>
        public string CEI_Desc
        {
            get
            {
                return Commands.AdcpCommands.GetCeiDesc();
            }
        }

        /// <summary>
        /// Ensemble Interval as a descriptive string.
        /// </summary>
        public string CEI_DescStr
        {
            get { return _wavesSetupVM.AdcpConfig.Commands.CEI_DescStr(); }
        }

        /// <summary>
        /// CEI value in Burst Mode.
        /// </summary>
        public float CEI
        {
            get { return (float)_wavesSetupVM.AdcpConfig.Commands.CEI.ToSecondsD(); }
            set
            {
                //_wavesSetupVM.AdcpConfig.Commands.CEI = new RTI.Commands.TimeValue(value);

                // Update the display
                //_wavesSetupVM.UpdateCommandSet();

                //// Display Time span
                //DisplayTimeSpan(value);

                //this.NotifyOfPropertyChange(() => this.CEI);
                //this.NotifyOfPropertyChange(() => this.CEI_Timespan);
                //this.NotifyOfPropertyChange(() => this.CEI_DescStr);
                //this.NotifyOfPropertyChange(() => this.CBI_DescStr);
                //this.NotifyOfPropertyChange(() => this.ShowCbiWarning);
                //this.NotifyOfPropertyChange(() => this.CBI_WarningStr);

                //// Update predictor
                //AdcpPredictor.CEI = value;
                //UpdatePredictionModel();

                // Update all the VM with the new CEI
                _wavesSetupVM.UpdateCEI(value);
            }
        }

        /// <summary>
        /// Time span as a string
        /// </summary>
        private string _CEI_Timespan;
        /// <summary>
        /// Time span as a string
        /// </summary>
        public string CEI_Timespan
        {
            get { return _CEI_Timespan; }
            set
            {
                _CEI_Timespan = value;
                this.NotifyOfPropertyChange(() => this.CEI_Timespan);
            }
        }
        

        #endregion

        #region Predictor Model

        /// <summary>
        /// ADCP Prediction model.
        /// </summary>
        public AdcpPredictor AdcpPredictor { get; set; }

        /// <summary>
        /// ADCP Prediction model predicted Profile Range.
        /// </summary>
        private string _PredictedProfileRange;
        /// <summary>
        /// ADCP Prediction model predicted Profile Range.
        /// </summary>
        public string PredictedProfileRange
        {
            get { return _PredictedProfileRange; }
            set
            {
                _PredictedProfileRange = value;
                this.NotifyOfPropertyChange(() => this.PredictedProfileRange);
            }
        }

        /// <summary>
        /// ADCP Prediction model predicted maximum Velocity.
        /// </summary>
        private string _MaximumVelocity;
        /// <summary>
        /// ADCP Prediction model predicted maximum Velocity.
        /// </summary>
        public string MaximumVelocity
        {
            get { return _MaximumVelocity; }
            set
            {
                _MaximumVelocity = value;
                this.NotifyOfPropertyChange(() => this.MaximumVelocity);
            }
        }

        /// <summary>
        /// ADCP Prediction model predicted standard deviation.
        /// </summary>
        private string _StandardDeviation;
        /// <summary>
        /// ADCP Prediction model predicted standard deviation.
        /// </summary>
        public string StandardDeviation
        {
            get { return _StandardDeviation; }
            set
            {
                _StandardDeviation = value;
                this.NotifyOfPropertyChange(() => this.StandardDeviation);
            }
        }

        /// <summary>
        /// ADCP Prediction model predicted Power Usage.
        /// </summary>
        private string _PredictedPowerUsage;
        /// <summary>
        /// ADCP Prediction model predicted Power Usage.
        /// </summary>
        public string PredictedPowerUsage
        {
            get { return _PredictedPowerUsage; }
            set
            {
                _PredictedPowerUsage = value;
                this.NotifyOfPropertyChange(() => this.PredictedPowerUsage);
            }
        }

        #endregion

        #region Range Tracking

        /// <summary>
        /// CWPTBP description.
        /// </summary>
        public List<string> RangeTrackingList
        {
            get
            {
                return new List<string>() { RangeTrackingModeOff, RangeTrackingModeBin, RangeTrackingModePressure };
            }
        }

        /// <summary>
        /// CWPRT description.
        /// </summary>
        public string CWPRT_Desc
        {
            get
            {
                return Commands.AdcpSubsystemCommands.GetCwprtDesc();
            }
        }

        /// <summary>
        /// Range Tracking mode.
        /// </summary>
        public string RangeTrackingMode
        {
            get
            {
                switch(AdcpSubConfig.Commands.CWPRT_Mode)
                {
                    default:
                    case 0:
                        return RangeTrackingModeOff;
                    case 1:
                        return RangeTrackingModeBin;
                    case 2:
                        return RangeTrackingModePressure;
                }
            }
            set
            {
                switch(value)
                {
                    default:
                    case RangeTrackingModeOff:
                        AdcpSubConfig.Commands.CWPRT_Mode = 0;
                        IsBurstModeBin = false;
                        IsBurstModePressure = false;
                        break;
                    case RangeTrackingModeBin:
                        AdcpSubConfig.Commands.CWPRT_Mode = 1;
                        IsBurstModeBin = true;
                        IsBurstModePressure = false;
                        if (AdcpSubConfig.Commands.CWPRT_FirstBin == 0 && AdcpSubConfig.Commands.CWPRT_LastBin == 0)        // Check if not initialized with values
                        {
                            RangeTrackingN1 = 0;
                            RangeTrackingN2 = 30;
                        }
                        break;
                    case RangeTrackingModePressure:
                        AdcpSubConfig.Commands.CWPRT_Mode = 2;
                        IsBurstModeBin = false;
                        IsBurstModePressure = true;
                        if (AdcpSubConfig.Commands.CWPRT_FirstBin == 0)                                                     // Check if not initialized with values
                        {
                            RangeTrackingN1 = 0.5f;
                        }
                        break;
                }

                this.NotifyOfPropertyChange(() => this.RangeTrackingMode);

                // Update the display
                _wavesSetupVM.UpdateCommandSet();
            }
        }

        /// <summary>
        /// Flag for the Range Tracking mode.
        /// </summary>
        private bool _IsBurstModeBin;
        /// <summary>
        /// Flag for the Range Tracking mode.
        /// </summary>
        public bool IsBurstModeBin
        {
            get
            {
                return _IsBurstModeBin;
            }
            set
            {
                _IsBurstModeBin = value;
                this.NotifyOfPropertyChange(() => this.IsBurstModeBin);
            }
        }

        /// <summary>
        /// Flag for the Range Tracking mode.
        /// </summary>
        private bool _IsBurstModePressure;
        /// <summary>
        /// Flag for the Range Tracking mode.
        /// </summary>
        public bool IsBurstModePressure
        {
            get
            {
                return _IsBurstModePressure;
            }
            set
            {
                _IsBurstModePressure = value;
                this.NotifyOfPropertyChange(() => this.IsBurstModePressure);
            }
        }

        /// <summary>
        /// Range Tracking N1 paramter.
        /// </summary>
        public float RangeTrackingN1
        {
            get
            {
                return AdcpSubConfig.Commands.CWPRT_FirstBin; 
            }
            set
            {
                AdcpSubConfig.Commands.CWPRT_FirstBin = value;
                this.NotifyOfPropertyChange(() => this.RangeTrackingN1);

                // Update the display
                _wavesSetupVM.UpdateCommandSet();
            }
        }

        /// <summary>
        /// Range Tracking N2 paramter.
        /// </summary>
        public ushort RangeTrackingN2
        {
            get
            {
                return AdcpSubConfig.Commands.CWPRT_LastBin;
            }
            set
            {
                AdcpSubConfig.Commands.CWPRT_LastBin = value;
                this.NotifyOfPropertyChange(() => this.RangeTrackingN2);

                // Update the display
                _wavesSetupVM.UpdateCommandSet();
            }
        }

        #endregion

        #endregion

        #region Commands

        /// <summary>
        /// Command to remove a subsystem.
        /// </summary>
        public ReactiveCommand<object> RemoveSubsystemCommand { get; protected set; }

        #endregion

        /// <summary>
        /// Initialize the view model.
        /// </summary>
        /// <param name="config">Subsystem configuration.</param>
        /// <param name="wavesSetupVM">Waves Setup VM.</param>
        public WavesSubsystemConfigurationViewModel(ref AdcpSubsystemConfig config, WavesSetupViewModel wavesSetupVM)
            : base("Waves Subsystem Configuration")
        {
            // Initialize values
            _wavesSetupVM = wavesSetupVM;
            AdcpSubConfig = config;
            Desc = config.ToString();

            Init();

            // Add Subsystem
            RemoveSubsystemCommand = ReactiveCommand.Create();
            RemoveSubsystemCommand.Subscribe(_ => RemoveSubsystem());

            CBI_Interleaved = false;
        }

        /// <summary>
        /// Dispose of the VM.
        /// </summary>
        public override void Dispose()
        {
            
        }

        /// <summary>
        /// Initialize the values.
        /// </summary>
        public void Init()
        {
            AdcpPredictor = new AdcpPredictor(new AdcpPredictorUserInput());

            if (AdcpSubConfig.Commands.CBI_BurstInterval.ToSecondsD() > 0)
            {
                CBI_Enabled = true;
            }
            else
            {
                CBI_Enabled = false;
            }

            CWPBL = AdcpSubConfig.Commands.CWPBL;
            CWPBS = AdcpSubConfig.Commands.CWPBS;
            CWPBN = AdcpSubConfig.Commands.CWPBN;
            CWPP = AdcpSubConfig.Commands.CWPP;
            CWPTBP = AdcpSubConfig.Commands.CWPTBP;
            CWPBB_LagLength = AdcpSubConfig.Commands.CWPBB_LagLength;
            CBI_BurstInterval = (float)AdcpSubConfig.Commands.CBI_BurstInterval.ToSecondsD();
            CBI_NumEnsembles = AdcpSubConfig.Commands.CBI_NumEnsembles;
            CBI_Interleaved = AdcpSubConfig.Commands.CBI_BurstPairFlag;
            switch (AdcpSubConfig.Commands.CWPRT_Mode)
            {
                default:
                case 0:
                    RangeTrackingMode = RangeTrackingModeOff;
                    break;
                case 1:
                    RangeTrackingMode = RangeTrackingModeBin;
                    break;
                case 2:
                    RangeTrackingMode = RangeTrackingModePressure;
                    break;
            }
            RangeTrackingN1 = AdcpSubConfig.Commands.CWPRT_FirstBin;
            RangeTrackingN2 = AdcpSubConfig.Commands.CWPRT_LastBin;



            // Default for a waves system
            AdcpSubConfig.Commands.CWPON = true;
            AdcpSubConfig.Commands.CWPBB_TransmitPulseType = Commands.AdcpSubsystemCommands.eCWPBB_TransmitPulseType.BROADBAND;

            // Set CEI from the AdcpConfig
            CEI = (float)_wavesSetupVM.AdcpConfig.Commands.CEI.ToSecondsD();

            // Display Time span
            DisplayTimeSpan(CEI);

            // Create the user input and recreate the predictor
            AdcpPredictorUserInput predInput = new AdcpPredictorUserInput(AdcpSubConfig.SubsystemConfig.SubSystem)
            {
                CWPBL = CWPBL,
                CWPBS = CWPBS,
                CWPBN = CWPBN,
                CWPP = CWPP,
                CWPBB_LagLength = CWPBB_LagLength,
                CEI = CEI
            };
            AdcpPredictor = new AdcpPredictor(predInput);
        }

        /// <summary>
        /// Remove the subsystem.
        /// </summary>
        private void RemoveSubsystem()
        {
            _wavesSetupVM.RemoveVM(this);
        }


        /// <summary>
        /// Create a pretty timespan for the given seconds.
        /// </summary>
        /// <param name="time">Time in seconds.</param>
        private void DisplayTimeSpan(float time)
        {
            float milliseconds = time * 1000.0f;

            // Get the whole value that is disvisable by 1000 for the seconds
            // The remainder is the milliseconds left over
            int seconds = (int)(milliseconds / 1000.0f);
            int remainder = (int)(milliseconds % 1000.0f);

            TimeSpan ts = new TimeSpan(0, 0, 0, seconds, remainder);
            CEI_Timespan = MathHelper.TimeSpanPrettyFormat(ts);
        }

        #region Prediction Model

        /// <summary>
        /// Update the Prediction model displays.
        /// </summary>
        private void UpdatePredictionModel()
        {
            // If using burst, use the waves
            // calculation model to predict
            if (CBI_Enabled)
            {
                double range = 0.0;
                double sd = 0.0;
                double maxVel = 0.0;
                double firstBinLocation = 0.0;
                AdcpPredictor.WavesModelPUV(AdcpSubConfig.SubsystemConfig.SubSystem,                // ss
                                            CWPBS,                                                  // Bin Size
                                            CWPBL,                                                  // Blank
                                            CWPBB_LagLength,                                        // Lag Length
                                            out range,                                              // Range
                                            out sd,                                                 // STD
                                            out maxVel,                                             // Max Vel
                                            out firstBinLocation);                                  // First bin location

                // Output the strings
                PredictedProfileRange = range.ToString("0.00") + " m";
                MaximumVelocity = maxVel.ToString("0.00") + " m/s";
                StandardDeviation = sd.ToString("0.00") + " m/s";
            }
            else
            {
                // Use the standard predictor to get the prediction
                PredictedProfileRange = AdcpPredictor.PredictedProfileRange.ToString("0.00") + " m";
                MaximumVelocity = AdcpPredictor.MaximumVelocity.ToString("0.00") + " m/s";
                StandardDeviation = AdcpPredictor.StandardDeviation.ToString("0.00") + " m/s";
                PredictedPowerUsage = AdcpPredictor.TotalPower.ToString("0.00") + " Watt/Hr";
            }
        }

        /// <summary>
        /// Return the number of bytes the burst will use.
        /// </summary>
        /// <returns>Number of bytes used in a burst.</returns>
        public long GetBurstMemoryUsage()
        {
            return AdcpPredictor.WavesRecordBytesPerBurst(CBI_NumEnsembles, CWPBN);
        }

        /// <summary>
        /// Get the total memory used for the setup.
        /// If we are in burst mode, calculate the number bytes
        /// per burst and the number of burst per deployment duration.
        /// 
        /// If not in burst mode, use the prediction model.
        /// </summary>
        /// <returns>Number of bytes per deployment duration.</returns>
        public long GetMemoryUsage()
        {
            if(CBI_Enabled)
            {
                return AdcpPredictor.WavesRecordBytesPerDeployment(CBI_NumEnsembles, CWPBN, AdcpPredictor.DeploymentDuration, CBI_BurstInterval);
            }
            else
            {
                return AdcpPredictor.DataSizeBytes;
            }
        }

        /// <summary>
        /// Get the total watt hours for a deployment.
        /// </summary>
        /// <param name="adcpConfig">ADCP Configuration.</param>
        /// <returns>Watt Hours used in the deployment.</returns>
        public double GetWattHrUsage(AdcpConfiguration adcpConfig)
        {
            if(_CBI_Enabled)
            {
                return AdcpPredictor.WavesRecordWattHours(AdcpSubConfig.SubsystemConfig.SubSystem,          // Subsystem 
                                                            adcpConfig,                                     // ADCP Configuration
                                                            CBI_NumEnsembles,                               // Samples in Burst
                                                            CEI,                                            // Sample Rate
                                                            AdcpPredictor.DeploymentDuration,               // Deployment Duration
                                                            CBI_BurstInterval);                             // Number of beams in Primary ADCP
            }
            else
            {
                return AdcpPredictor.TotalPower;
            }
        }

        /// <summary>
        /// Get the total number of batteries for a deployment.
        /// </summary>
        /// <returns>Total batteries in the deployment.</returns>
        public double GetTotalBatteryUsage(AdcpConfiguration adcpConfig)
        {
            if(_CBI_Enabled)
            {
                double pwrUsage = GetWattHrUsage(adcpConfig);
                return pwrUsage / AdcpPredictor.ActualBatteryPower;
            }
            else
            {
                return AdcpPredictor.NumberBatteryPacks;
            }
        }

        #endregion

        #region Burst Mode

        /// <summary>
        /// Update the burst mode based off other VM selections.
        /// </summary>
        /// <param name="flag">Flag if burst mode or not.</param>
        public void UpdateBurstMode(bool flag)
        {
            _CBI_Enabled = flag;
            this.NotifyOfPropertyChange(() => this.CBI_Enabled);
            this.NotifyOfPropertyChange(() => this.CBI_Disabled);

            // If the values are 0, set some default values
            if(CBI_NumEnsembles == 0 || CBI_BurstInterval == 0)
            {
                CBI_NumEnsembles = 4096;
                CBI_BurstInterval = 3600;
                CBI_Interleaved = true;
                CEI = 0.4f;
            }

            // Update the display
            _wavesSetupVM.UpdateCommandSet();

            // Update the prediction model
            UpdatePredictionModel();
        }

        /// <summary>
        /// Update the burst mode interleaved based off other VM selections.
        /// </summary>
        /// <param name="flag">Flag if burst mode or not.</param>
        public void UpdateBurstModeInterleaved(bool flag)
        {
            AdcpSubConfig.Commands.CBI_BurstPairFlag = flag;
            this.NotifyOfPropertyChange(() => this.CBI_Interleaved);

            // Update the display
            _wavesSetupVM.UpdateCommandSet();
            
            this.NotifyOfPropertyChange(() => this.CBI_Interleaved);
            this.NotifyOfPropertyChange(() => this.CBI_DescStr);
            this.NotifyOfPropertyChange(() => this.ShowCbiWarning);
            this.NotifyOfPropertyChange(() => this.CBI_WarningStr);

            // Update the display
            _wavesSetupVM.UpdateCommandSet();

            // Update the prediction model
            UpdatePredictionModel();

            //Create a warning that indicates which subsystem is interleaved with which
            if (CBI_Interleaved == true)
            {
                string nextSubsystemNumber = _wavesSetupVM.setUpBurstInterleavedWarning(this);
                if (nextSubsystemNumber != null)
                {
                    CBI_InterleavedMessage = "Subsystem " + Desc.Substring(1, 1) + " is Interleaved with subsystem " + nextSubsystemNumber;
                }
            }
            if (CBI_Interleaved == false) CBI_InterleavedMessage = "";
            
    }

        #endregion

        /// <summary>
        /// Update the CEI command.
        /// </summary>
        /// <param name="value">Value to set.</param>
        public void UpdateCEI(float value)
        {
            // Display Time span
            DisplayTimeSpan(value);

            this.NotifyOfPropertyChange(() => this.CEI);
            this.NotifyOfPropertyChange(() => this.CEI_Timespan);
            this.NotifyOfPropertyChange(() => this.CEI_DescStr);
            this.NotifyOfPropertyChange(() => this.CBI_DescStr);
            this.NotifyOfPropertyChange(() => this.ShowCbiWarning);
            this.NotifyOfPropertyChange(() => this.CBI_WarningStr);

            // Update predictor
            AdcpPredictor.CEI = value;
            UpdatePredictionModel();

            // Update the display
            _wavesSetupVM.UpdateCommandSet();
        }

        /// <summary>
        /// Get all the commands for this subsystem.
        /// Determine if CBI is enabled or disabled (Burst mode).
        /// If burst mode is enabled, remove the CWPTBP command.
        /// If burst mode is disabled, remove the CBI command.
        /// </summary>
        /// <returns>List of all the commands for a waves system based off this configuration.</returns>
        public List<string> GetCommandList()
        {
            var cmds = AdcpSubConfig.Commands.GetWavesCommandList();

            // If CBI is disabled, remove the CBI command
            if(!_CBI_Enabled)
            {
                for (int x = 0; x < cmds.Count; x++ )
                {
                    if(cmds[x].Contains(Commands.AdcpSubsystemCommands.CMD_CBI))
                    {
                        cmds.RemoveAt(x);
                    }
                }
            }
            // Remove the CWPTBP command
            else
            {
                for (int x = 0; x < cmds.Count; x++)
                {
                    if (cmds[x].Contains(Commands.AdcpSubsystemCommands.CMD_CWPTBP))
                    {
                        cmds.RemoveAt(x);
                    }
                }
            }

            return cmds;
        }
    }
}
