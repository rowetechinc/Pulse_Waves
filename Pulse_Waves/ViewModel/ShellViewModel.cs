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
using System.Windows;
using AutoUpdaterDotNET;
using System.Net;

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
        ///  Setup logger
        /// </summary>
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);


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

        #region AutoUpdate

        /// <summary>
        /// Flag to determine if we are looking for the update.
        /// </summary>
        private bool _IsCheckingForUpdates;
        /// <summary>
        /// Flag to determine if we are looking for the update.
        /// </summary>
        public bool IsCheckingForUpdates
        {
            get { return _IsCheckingForUpdates; }
            set
            {
                _IsCheckingForUpdates = value;
                this.NotifyOfPropertyChange(() => this.IsCheckingForUpdates);
            }
        }

        /// <summary>
        /// RTI Pulse Update URL.
        /// </summary>
        private string _PulseWavesUpdateUrl;
        /// <summary>
        /// RTI Pulse Update URL.
        /// </summary>
        public string PulseWavesUpdateUrl
        {
            get { return _PulseWavesUpdateUrl; }
            set
            {
                _PulseWavesUpdateUrl = value;
                this.NotifyOfPropertyChange(() => this.PulseWavesUpdateUrl);
            }
        }

        /// <summary>
        /// A string to nofity the user if the version is not update to date.
        /// </summary>
        private string _PulseWavesVersionUpdateToDate;
        /// <summary>
        /// A string to nofity the user if the version is not update to date.
        /// </summary>
        public string PulseWavesVersionUpdateToDate
        {
            get { return _PulseWavesVersionUpdateToDate; }
            set
            {
                _PulseWavesVersionUpdateToDate = value;
                this.NotifyOfPropertyChange(() => this.PulseWavesVersionUpdateToDate);
            }
        }

        #endregion

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

            // Auto Update
            IsCheckingForUpdates = false;
            PulseWavesVersionUpdateToDate = "Checking for an update...";
            PulseWavesUpdateUrl = "";
            // Check for updates to the applications
            CheckForUpdates();

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
            aboutInfo += "All Rights Reserved.\n";
            aboutInfo += "\n";
            aboutInfo += PulseWavesVersionUpdateToDate + "\n";
            aboutInfo += PulseWavesUpdateUrl;

            System.Windows.MessageBox.Show(aboutInfo, "About", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
        }

        /// <summary>
        /// Shutdown the view model.
        /// </summary>
        /// <param name="close"></param>
        void IDeactivate.Deactivate(bool close)
        {
            AutoUpdater.CheckForUpdateEvent -= AutoUpdaterOnCheckForUpdateEvent;

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

        #region Auto Update

        /// <summary>
        /// Check for updates to the application.  This will download the version of the application from 
        /// website/pulse/Pulse_AppCast.xml.  It will then check the version against the verison of this application
        /// set in Properties->AssemblyInfo.cs.  If the one on the website is greater, it will display a message 
        /// to update the application.
        /// 
        /// Also subscribe to the event to determine if an update is necssary.
        /// </summary>
        private void CheckForUpdates()
        {
            string url = @"http://www.rowetechinc.co/pulse/PulseWaves_AppCast.xml";

            try
            {
                WebRequest request = WebRequest.Create(url);
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                if (response != null && response.StatusCode == HttpStatusCode.OK && response.ResponseUri == new System.Uri(url))
                {
                    IsCheckingForUpdates = true;
                    AutoUpdater.Start(url);
                    AutoUpdater.CheckForUpdateEvent += AutoUpdaterOnCheckForUpdateEvent;
                }
                response.Close();
            }
            catch (System.Net.WebException e)
            {
                // No Internet connection, so do nothing
                log.Error("No Internet connection to check for updates.", e);
            }
            catch (Exception e)
            {
                log.Error("Error checking for an update on the web.", e);
            }
        }

        /// <summary>
        /// Event handler for the AutoUpdater.   This will get if an update is available
        /// and if so, which version is available.
        /// </summary>
        /// <param name="args">Results for checking if an update exist.</param>
        private void AutoUpdaterOnCheckForUpdateEvent(UpdateInfoEventArgs args)
        {
            if (args != null)
            {
                if (!args.IsUpdateAvailable)
                {
                    PulseWavesVersionUpdateToDate = string.Format("Pulse is up to date");
                    PulseWavesUpdateUrl = "";
                }
                else
                {
                    PulseWavesVersionUpdateToDate = string.Format("Pulse version {0} is available", args.CurrentVersion);
                    PulseWavesUpdateUrl = args.DownloadURL;
                }
                // Unsubscribe
                AutoUpdater.CheckForUpdateEvent -= AutoUpdaterOnCheckForUpdateEvent;
                IsCheckingForUpdates = false;


                if (args.IsUpdateAvailable)
                {
                    MessageBoxResult dialogResult;
                    if (args.Mandatory)
                    {
                        dialogResult =
                            MessageBox.Show(@"There is new version " + args.CurrentVersion + "  available. \nYou are using version " + args.InstalledVersion + ". \nThis is required update. \nPress Ok to begin updating the application.",
                                            @"Update Available",
                                            MessageBoxButton.OK,
                                            MessageBoxImage.Information);
                    }
                    else
                    {
                        dialogResult =
                            MessageBox.Show(
                                @"There is new version " + args.CurrentVersion + " available. \nYou are using version " + args.InstalledVersion + ".  \nDo you want to update the application now?",
                                @"Update Available",
                                MessageBoxButton.YesNo,
                                MessageBoxImage.Information);
                    }

                    if (dialogResult.Equals(MessageBoxResult.Yes))
                    {
                        try
                        {
                            if (AutoUpdater.DownloadUpdate())
                            {
                                //Application.Current.Exit();
                                System.Windows.Application.Current.Shutdown();
                            }
                        }
                        catch (Exception exception)
                        {
                            MessageBox.Show(exception.Message,
                                exception.GetType().ToString(),
                                MessageBoxButton.OK,
                                MessageBoxImage.Error);
                        }
                    }
                }
                else
                {
                    //MessageBox.Show(@"There is no update available please try again later.", 
                    //                @"No update available",
                    //                MessageBoxButton.OK,
                    //                MessageBoxImage.Information);
                }
            }
            else
            {
                //MessageBox.Show(
                //        @"There is a problem reaching update server please check your internet connection and try again later.",
                //        @"Update check failed", 
                //        MessageBoxButton.OK,
                //        MessageBoxImage.Error);
            }
        }

        #endregion

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