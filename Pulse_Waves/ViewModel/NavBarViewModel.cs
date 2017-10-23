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
 * 08/19/2013      RC          3.0.7      Initial coding
 * 08/23/2013      RC          3.0.7      Added ScreenDataCommand.
 * 02/12/2014      RC          3.2.3      Added VesselMountCommand.
 * 07/09/2014      RC          3.4.0      Added AveragingCommand.
 * 08/07/2014      RC          4.0.0      Updated ReactiveCommand to 6.0.
 * 10/23/2017      RC          1.2.2      Fixed bug with path to WaVectorExe.txt.  Added more logic for missing file. 
 * 
 */

using Caliburn.Micro;
using ReactiveUI;

namespace RTI
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Text;

    /// <summary>
    /// TODO: Update summary.
    /// </summary>
    public class NavBarViewModel : PulseViewModel
    {

        #region Variables

        /// <summary>
        ///  Setup logger
        /// </summary>
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);


        /// <summary>
        /// Event aggregator.
        /// </summary>
        private readonly IEventAggregator _events;

        #endregion

        #region Commands

        /// <summary>
        /// Command to go back in the application.
        /// </summary>
        public ReactiveCommand<object> BackCommand { get; protected set; }

        /// <summary>
        /// Command to go Update Firmware View.
        /// </summary>
        public ReactiveCommand<object> UpdateFirmwareCommand { get; protected set; }

        /// <summary>
        /// Command to go Configure View.
        /// </summary>
        public ReactiveCommand<object> ConfigureCommand { get; protected set; }

        /// <summary>
        /// Command to go ViewData View.
        /// </summary>
        public ReactiveCommand<object> DownloadDataCommand { get; protected set; }

        /// <summary>
        /// Command to go Terminal View.
        /// </summary>
        public ReactiveCommand<object> TerminalCommand { get; protected set; }

        /// <summary>
        /// Command to go Waves view View.
        /// </summary>
        public ReactiveCommand<object> WavesCommand { get; protected set; }

        /// <summary>
        /// Command to recover the Waves data View.
        /// </summary>
        public ReactiveCommand<object> RecoverCommand { get; protected set; }

        /// <summary>
        /// Command to do a compass calibration.
        /// </summary>
        public ReactiveCommand<object> CompassCalCommand { get; protected set; }

        /// <summary>
        /// Command to go to Wavector.
        /// </summary>
        public ReactiveCommand<object> WavectorCommand { get; protected set; }
        

        #endregion

        /// <summary>
        /// Initalize values.
        /// </summary>
        public NavBarViewModel()
            : base("Nav")
        {
            _events = IoC.Get<IEventAggregator>();

            // Command to go back a view
            BackCommand = ReactiveCommand.Create();
            BackCommand.Subscribe(_ => _events.PublishOnUIThread(new ViewNavEvent(ViewNavEvent.ViewId.Back)));

            // Command to go to Update Firmware View
            UpdateFirmwareCommand = ReactiveCommand.Create();
            UpdateFirmwareCommand.Subscribe(_ => _events.PublishOnUIThread(new ViewNavEvent(ViewNavEvent.ViewId.UpdateFirmwareView)));

            // Command to go to Setup Waves View
            ConfigureCommand = ReactiveCommand.Create();
            ConfigureCommand.Subscribe(_ => _events.PublishOnUIThread(new ViewNavEvent(ViewNavEvent.ViewId.WavesSetupView)));

            // Command to go to Download Data View
            DownloadDataCommand = ReactiveCommand.Create();
            DownloadDataCommand.Subscribe(_ => _events.PublishOnUIThread(new ViewNavEvent(ViewNavEvent.ViewId.DownloadDataView)));

            // Command to go to Terminal View
            TerminalCommand = ReactiveCommand.Create();
            TerminalCommand.Subscribe(_ => _events.PublishOnUIThread(new ViewNavEvent(ViewNavEvent.ViewId.TerminalView)));

            // Command to go to Waves View
            WavesCommand = ReactiveCommand.Create();
            WavesCommand.Subscribe(_ => _events.PublishOnUIThread(new ViewNavEvent(ViewNavEvent.ViewId.WavesView)));

            // Command to go to compass cal
            CompassCalCommand = ReactiveCommand.Create();
            CompassCalCommand.Subscribe(_ => _events.PublishOnUIThread(new ViewNavEvent(ViewNavEvent.ViewId.CompassCalView)));

            // Command to go to WaVector
            WavectorCommand = ReactiveCommand.Create();
            WavectorCommand.Subscribe(_ => WaveVectorLoad());
        }

        /// <summary>
        /// Shutdown the object.
        /// </summary>
        public override void Dispose()
        {
            
        }

        /// <summary>
        /// Allow the users to select all the files they would like to process.
        /// Write all the file paths to WaVectorSelectedFiles.txt.
        /// The processed files are the converted files from binary format to 
        /// a processed waves format with up to 3 bins selected.
        /// 
        /// Then run the WaVector application by going into the
        /// WaVectorExe.txt file and getting the path to the application.
        /// 
        /// Run the application and give the path to the file
        /// which contains all the selected files as the parameter.
        /// 
        /// If WaVectorExe.txt is missing or empty, give a warning to the user.
        /// </summary>
        private void WaveVectorLoad()
        {
            try
            {
                // Select files to load
                // Show the FolderBrowserDialog.
                System.Windows.Forms.OpenFileDialog dialog = new System.Windows.Forms.OpenFileDialog();
                dialog.Filter = "All files (*.*)|*.*";
                dialog.Multiselect = true;

                System.Windows.Forms.DialogResult result = dialog.ShowDialog();
                if (result == System.Windows.Forms.DialogResult.OK)
                {

                    // Load files to a text file
                    string selectedFiles = "";
                    foreach(var file in dialog.FileNames)
                    {
                        selectedFiles += file + ",";
                    }

                    // Create a text file and write the files selected
                    // Set a file path for the file
                    string selectedFilesPath = RTI.Pulse_Waves.Commons.GetAppStorageDir() + @"\WaVectorSelectedFiles.txt";
                    System.IO.File.WriteAllText(selectedFilesPath, selectedFiles);

                    // Run Wavector
                    RunWavector();

                }
            }
            catch(Exception e)
            {
                log.Error("Error loading wavector.", e);
            }
        }

        /// <summary>
        /// Check if the wave vector path is given in the file WaVectorExe.txt.
        /// If it is not, try to create the file with a default path.
        /// If that still does not work, give a warning to the user.
        /// 
        /// If the path does exist, execute Wavector with the file containing the list of all the files to load into Wavector (WaVectorSelectedFiles.txt).
        /// </summary>
        private bool RunWavector()
        {
            // Run the command based off the file
            string exePath = RTI.Pulse_Waves.Commons.GetAppStorageDir() + @"\WaVectorExe.txt";
            if (File.Exists(exePath))
            {
                // Read in the file and execute the command
                string strCmd = File.ReadAllText(exePath);

                // Check if a path was given
                if (string.IsNullOrEmpty(strCmd))
                {
                    System.Windows.MessageBox.Show("WaVector executable path file is empty.  Please enter the WaVector executable path in the file.  " + RTI.Pulse_Waves.Commons.GetAppStorageDir() + @"\WaVectorExe.txt", "Missing Exe Path", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                    return false;
                }
                else if(!File.Exists(strCmd))
                {
                    System.Windows.MessageBox.Show("WaVector executable does not exist.  Please enter the WaVector executable path in the file.  " + RTI.Pulse_Waves.Commons.GetAppStorageDir() + @"\WaVectorExe.txt", "Incorrect Exe Path", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                    return false;
                }
                else
                {
                    // Execute command
                    string strCmdText = RTI.Pulse_Waves.Commons.GetAppStorageDir() + @"\WaVectorSelectedFiles.txt";
                    System.Diagnostics.Process.Start(strCmd, strCmdText);
                    return true;
                }
            }
            else
            {
                string oldPath = @"C:\Program Files\WaveForce Technologies\Wavector\application\Wavector.exe";
                string newPath_6_3 = @"C:\Program Files (x86)\WaveForce Technologies\Wavector\Wavector_6_3.exe";

                if (File.Exists(oldPath))
                {
                    // File does not exist so create a blank file
                    using (StreamWriter sw = new StreamWriter(RTI.Pulse_Waves.Commons.GetAppStorageDir() + @"\WaVectorExe.txt", false))
                    {
                        // Just create the file, write the default path and close it
                        sw.Write(oldPath);
                    }

                    // Retry finding the file and passing the data
                    if (!RunWavector())
                    {
                        // Give a warning
                        System.Windows.MessageBox.Show("WaVector executable path file does not exist at: " + RTI.Pulse.Commons.GetAppStorageDir() + @"\WaVectorExe.txt.  A blank file will be created.  Please enter the WaVector executable path in the file.", "Missing Exe Path", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                        return false;
                    }
                }
                else if (File.Exists(newPath_6_3))
                {
                    // File does not exist so create a blank file
                    using (StreamWriter sw = new StreamWriter(RTI.Pulse_Waves.Commons.GetAppStorageDir() + @"\WaVectorExe.txt", false))
                    {
                        // Just create the file, write the default path and close it
                        sw.Write(newPath_6_3);
                    }

                    // Retry finding the file and passing the data
                    if (!RunWavector())
                    {
                        // Give a warning
                        System.Windows.MessageBox.Show("WaVector executable path file does not exist at: " + RTI.Pulse.Commons.GetAppStorageDir() + @"\WaVectorExe.txt.  A blank file will be created.  Please enter the WaVector executable path in the file.", "Missing Exe Path", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                        return false;
                    }
                }
                else
                {
                    // Give a warning
                    System.Windows.MessageBox.Show("Wavector is not found installed on your computer.\nPlease install Wavector, then update the WaVector executable path in the file: " + RTI.Pulse.Commons.GetAppStorageDir() + @"\WaVectorExe.txt.\n  A blank file will be created.  Please enter the WaVector executable path in the file.", "Missing Exe Path", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);

                    // File does not exist so create a blank file
                    using (StreamWriter sw = new StreamWriter(RTI.Pulse_Waves.Commons.GetAppStorageDir() + @"\WaVectorExe.txt", false))
                    {
                        // Just create the file and close it
                        sw.Write("");
                    }

                    return false;
                }

            }
            return true;
        }

    }
}
