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
 * 11/05/2014      RC          0.0.1       Initial coding
 * 12/31/2014      RC          0.0.1       Store the latest page viewed.
 * 
 */

using Caliburn.Micro;
using log4net.Appender;
using log4net.Layout;
using log4net.Repository.Hierarchy;
using ReactiveUI;
using System.Collections.Generic;
using System.IO;
using System;

namespace RTI 
{
    /// <summary>
    /// Interface for the ShellViewModel.
    /// Empty.
    /// </summary>
    public interface IShellViewModel
    {
    }

    /// <summary>
    /// Initialize the shell view model.
    /// </summary>
    public class ShellViewModel : Conductor<object>, IShellViewModel, IDeactivate, IHandle<ViewNavEvent>
    {

        #region Variables

        /// <summary>
        /// Event Aggregator.
        /// </summary>
        private IEventAggregator _events;

        /// <summary>
        /// Used for a back button.
        /// This will allow the user to navigate
        /// back.  It will keep track of all the page visits.
        /// A limit will be set number of view events it will store.
        /// </summary>
        private Stack<ViewNavEvent> _backStack;

        /// <summary>
        /// Store the _prevViewNavEvent so that the Back Stack
        /// gets the latest view to go back to.
        /// </summary>
        private ViewNavEvent _prevViewNavEvent;

        /// <summary>
        /// Pulse manager.
        /// </summary>
        private PulseManager _pm;

        #endregion

        #region Properties

        /// <summary>
        /// Playback View Model.
        /// </summary>
        //public PlaybackViewModel PlaybackVM { get; set; }

        /// <summary>
        /// Playback View Model.
        /// </summary>
        public NavBarViewModel NavBarVM { get; set; }

        /// <summary>
        /// Set flag if the navigation bar should be visible.
        /// </summary>
        private bool _IsNavBarEnabled;
        /// <summary>
        /// Set flag if the navigation bar should be visible.
        /// </summary>
        public bool IsNavBarEnabled
        {
            get { return _IsNavBarEnabled; }
            set
            {
                _IsNavBarEnabled = value;
                this.NotifyOfPropertyChange(() => this.IsNavBarEnabled);
            }
        }

        //#region Playback

        ///// <summary>
        ///// Playback View Model.
        ///// </summary>
        //public PlaybackViewModel PlaybackVM { get; set; }

        ///// <summary>
        ///// Set flag if the playback controls should be visible.
        ///// </summary>
        //private bool _IsPlaybackEnabled;
        ///// <summary>
        ///// Set flag if the playback controls should be visible.
        ///// </summary>
        //public bool IsPlaybackEnabled
        //{
        //    get { return _IsPlaybackEnabled; }
        //    set
        //    {
        //        _IsPlaybackEnabled = value;
        //        this.NotifyOfPropertyChange(() => this.IsPlaybackEnabled);
        //    }
        //}

        //#endregion

        #endregion

        #region Commands

        /// <summary>
        /// Command to go to the terminal view.
        /// </summary>
        public ReactiveCommand<object> TerminalViewCommand { get; protected set; }

        /// <summary>
        /// Command to go to the About view.
        /// </summary>
        public ReactiveCommand<object> AboutCommand { get; protected set; }

        #endregion

        /// <summary>
        /// Initialize the application.
        /// </summary>
        public ShellViewModel(IEventAggregator events)
        {
            // To set the Window title
            // http://stackoverflow.com/questions/4615467/problem-with-binding-title-of-wpf-window-on-property-in-shell-view-model-class
            base.DisplayName = "Pulse Waves";

            // Initialize the values
            _events = events;
            events.Subscribe(this);
            _pm = IoC.Get<PulseManager>();

            // Setup ErrorLog
            SetupErrorLog();

            // Set a size of 10 views
            _backStack = new Stack<ViewNavEvent>(20);
            _prevViewNavEvent = null;

            // Set the Navigation bar viewmodel
            NavBarVM = IoC.Get<NavBarViewModel>();
            IsNavBarEnabled = true;

            // Set the Playback viewmodel
            //PlaybackVM = IoC.Get<PlaybackViewModel>();
            //IsPlaybackEnabled = false;

            // Command to view the Terimal view
            TerminalViewCommand = ReactiveCommand.Create();
            TerminalViewCommand.Subscribe(_ => _events.PublishOnUIThread(new ViewNavEvent(ViewNavEvent.ViewId.TerminalView)));

            // Command to About
            AboutCommand = ReactiveCommand.Create();
            AboutCommand.Subscribe(_ => DisplayAboutBox());

            // Display the HomeViewModel
            Handle(new ViewNavEvent(_pm.GetLastViewedPage()));
        }

        /// <summary>
        /// Display the About box.
        /// </summary>
        private void DisplayAboutBox()
        {
            string aboutInfo = "";
            aboutInfo += "Pulse Waves\n";
            aboutInfo += "Version: " + Pulse_Waves.Commons.VERSION + " " + Pulse_Waves.Commons.VERSION_ADDITIONAL + "\n";
            aboutInfo += "Pulse Display Version: " + PulseDisplay.Version.VERSION + " " + PulseDisplay.Version.VERSION_ADDITIONAL + "\n";
            aboutInfo += "RTI Version: " + Core.Commons.VERSION + " " + Core.Commons.RTI_VERSION_ADDITIONAL + "\n";
            aboutInfo += "© 2014 Rowe Technology Inc.\n";
            aboutInfo += "All Rights Reserved.";

            System.Windows.MessageBox.Show(aboutInfo, "About", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
        }

        /// <summary>
        /// Shutdown the view model.
        /// </summary>
        /// <param name="close"></param>
        void IDeactivate.Deactivate(bool close)
        {
            // Shutdown the pulse manager
            PulseManager pm = IoC.Get<PulseManager>();
            if (pm != null)
            {
                pm.Dispose();
            }

            // Shutdown the singleton DvlSetupViewModel
            WavesSetupViewModel vmWavesSetup = IoC.Get<WavesSetupViewModel>();
            if (vmWavesSetup != null)
            {
                vmWavesSetup.Dispose();
            }

            // Shutdown the singleton DvlSetupViewModel
            RecoverDataViewModel download = IoC.Get<RecoverDataViewModel>();
            if (download != null)
            {
                download.Dispose();
            }

            // Shutdown the singleton UpdateFirmwareViewModel
            UpdateFirmwareViewModel updateFirm = IoC.Get<UpdateFirmwareViewModel>();
            if (updateFirm != null)
            {
                updateFirm.Dispose();
            }


            // Shutdown the singleton  PlaybackViewModel
            PlaybackViewModel playback = IoC.Get<PlaybackViewModel>();
            if (playback != null)
            {
                playback.Dispose();
            }

            // Shutdown the singleton  ViewDataWavesViewModel
            ViewDataWavesViewModel wavesView = IoC.Get<ViewDataWavesViewModel>();
            if (wavesView != null)
            {
                wavesView.Dispose();
            }

            // Shutdown the singleton CompassCalViewModel
            CompassCalViewModel compassCalView = IoC.Get<CompassCalViewModel>();
            if (compassCalView != null)
            {
                compassCalView.Dispose();
            }

            // Shutdown the singleton CompassUtilityViewModel
            CompassUtilityViewModel compassUtilityView = IoC.Get<CompassUtilityViewModel>();
            if (compassUtilityView != null)
            {
                compassUtilityView.Dispose();
            }

            // Shutdown the last active item
            DeactivateItem(ActiveItem, true);

            // MAKE THIS THE LAST THING TO SHUTDOWN
            // Shutdown the ADCP connection
            AdcpConnection adcp = IoC.Get<AdcpConnection>();
            if (adcp != null)
            {
                adcp.Dispose();
            }

            // Shutdown the applicaton and all the threads
            Environment.Exit(Environment.ExitCode);
        }

        #region Error Logger

        /// <summary>
        /// Setup the error log.
        /// </summary>
        private void SetupErrorLog()
        {
            Hierarchy hierarchy = (Hierarchy)log4net.LogManager.GetRepository();
            hierarchy.Root.RemoveAllAppenders(); /*Remove any other appenders*/

            //RollingFileAppender rollingFileAppender = new RollingFileAppender();
            //rollingFileAppender.AppendToFile = true;
            //rollingFileAppender.LockingModel = new FileAppender.MinimalLock();
            //rollingFileAppender.File = Pulse.Commons.GetErrorLogPath();
            //rollingFileAppender.MaxFileSize = 1048576 * 1;  // 1mb
            //rollingFileAppender.MaxSizeRollBackups = 1;
            //rollingFileAppender.StaticLogFileName = true;
            //PatternLayout pl = new PatternLayout();
            //pl.ConversionPattern = "%-5level %date [%thread] %-22.22c{1} - %m%n";
            //pl.ActivateOptions();
            //rollingFileAppender.Layout = pl;
            //rollingFileAppender.ActivateOptions();
            //log4net.Config.BasicConfigurator.Configure(rollingFileAppender);

            FileAppender fileAppender = new FileAppender();
            fileAppender.AppendToFile = true;
            fileAppender.LockingModel = new FileAppender.MinimalLock();
            fileAppender.File = Pulse.Commons.GetErrorLogPath();
            PatternLayout pl = new PatternLayout();
            string pulseVer = PulseDisplay.Version.VERSION + PulseDisplay.Version.VERSION_ADDITIONAL;
            string rtiVer = Core.Commons.VERSION + Core.Commons.RTI_VERSION_ADDITIONAL;
            pl.ConversionPattern = "%d [%2%t] %-5p [%-10c] Pulse:" + pulseVer + " RTI:" + rtiVer + "   %m%n%n";
            pl.ActivateOptions();

            // If not Admin
            // Only log Error and Fatal errors
            if (!Pulse.Commons.IsAdmin())
            {
                fileAppender.AddFilter(new log4net.Filter.LevelMatchFilter() { LevelToMatch = log4net.Core.Level.Error });          // Log Error
                fileAppender.AddFilter(new log4net.Filter.LevelMatchFilter() { LevelToMatch = log4net.Core.Level.Fatal });          // Log Fatal
                fileAppender.AddFilter(new log4net.Filter.DenyAllFilter());                                                         // Reject all other errors
            }

            fileAppender.Layout = pl;
            fileAppender.ActivateOptions();
            log4net.Config.BasicConfigurator.Configure(fileAppender);
        }

        /// <summary>
        /// Clear the Error Log.
        /// </summary>
        public void ClearErrorLog()
        {
            using (FileStream stream = new FileStream(Pulse.Commons.GetErrorLogPath(), FileMode.Create))
            {
                using (TextWriter writer = new StreamWriter(stream))
                {
                    writer.WriteLine("");
                }
            }
        }

        #endregion

        #region ViewNavEvent

        /// <summary>
        /// Event handler for the ViewNavEvent.
        /// </summary>
        /// <param name="navEvent">Message for the event.</param>
        public void Handle(ViewNavEvent navEvent)
        {
            // Check if its a back event or a new view
            // to display
            if (navEvent.ID == ViewNavEvent.ViewId.Back)
            {
                if (_backStack.Count > 0)
                {
                    _prevViewNavEvent = _backStack.Pop();

                    NavigateToView(_prevViewNavEvent);
                }
            }
            else
            {
                // Show the view
                NavigateToView(navEvent);

                // Set the preview view to the back stack
                // and store the current view to put on the stack later next time
                if (_prevViewNavEvent != null)
                {
                    _backStack.Push(_prevViewNavEvent);
                }

                // Store the event if it is not back
                _prevViewNavEvent = navEvent;
            }

        }

        /// <summary>
        /// Navigate to the view based off the
        /// event given.
        /// </summary>
        /// <param name="message"></param>
        private void NavigateToView(ViewNavEvent message)
        {
            // Store the latest page viewed
            _pm.UpdateLastViewedPage(message.ID);

            switch (message.ID)
            {
                //case ViewNavEvent.ViewId.HomeView:
                //    var vmHome = IoC.Get<HomeViewModel>();
                //    //MenuLinks = vmHome.GetMenu();
                //    ActivateItem(vmHome);
                //    IsNavBarEnabled = true;
                //    IsPlaybackEnabled = false;
                //    break;
                //case ViewNavEvent.ViewId.AboutView:
                //    var aboutHome = IoC.Get<AboutViewModel>();
                //    ActivateItem(aboutHome);
                //    IsNavBarEnabled = true;
                //    IsPlaybackEnabled = false;
                //    break;
                case ViewNavEvent.ViewId.TerminalView:
                    var adcpConn = IoC.Get<AdcpConnection>();
                    ActivateItem(adcpConn.TerminalVM);
                    IsNavBarEnabled = true;
                    //IsPlaybackEnabled = false;
                    break;
                case ViewNavEvent.ViewId.WavesSetupView:
                    var wavesSetup = IoC.Get<WavesSetupViewModel>();
                    ActivateItem(wavesSetup);
                    IsNavBarEnabled = true;
                    //IsPlaybackEnabled = false;
                    break;
                case ViewNavEvent.ViewId.DownloadDataView:
                    var dlView = IoC.Get<RecoverDataViewModel>();
                    ActivateItem(dlView);
                    IsNavBarEnabled = true;
                    //IsPlaybackEnabled = false;
                    break;
                case ViewNavEvent.ViewId.UpdateFirmwareView:
                    var firm = IoC.Get<UpdateFirmwareViewModel>();
                    ActivateItem(firm);
                    IsNavBarEnabled = true;
                    //IsPlaybackEnabled = false;
                    break;
                case ViewNavEvent.ViewId.WavesView:
                    var viewData = IoC.Get<ViewDataWavesViewModel>();
                    ActivateItem(viewData);
                    IsNavBarEnabled = true;
                    //IsPlaybackEnabled = false;
                    break;
                case ViewNavEvent.ViewId.CompassCalView:
                    var ccData = IoC.Get<CompassCalViewModel>();
                    ActivateItem(ccData);
                    IsNavBarEnabled = true;
                    //IsPlaybackEnabled = false;
                    break;
            }
        }

        #endregion

    }
}