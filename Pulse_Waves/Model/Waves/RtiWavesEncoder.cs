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
 * 02/13/2015      RC          0.0.2      Initial coding
 * 10/15/2015      RC          0.0.3      Cleanup and recode the matlab encoding.
 * 10/16/2015      RC          0.0.3      Fixed bug with encoding to matlab with the columns not be written correctly.
 * 11/04/2015      RC          1.0.0      Fixed bug if the burst is only a 4beam or vertical, no interleave, then set the correct sample time in WavesCreateMatFile().
 * 11/23/20117     RC          1.2.2      Fixed a bug iterating through the list of MATLAB files and list is changed.
 * 03/27/2020      RC          3.4.17     Handling new firmware version with BurstID and BurstIndex.
 * 
 */

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RTI
{
    namespace Waves
    {

        /// <summary>
        /// Encode the RTI waves data to a matlab file.
        /// 
        /// LAT - Latitude
        /// LON - Longitude
        /// WFT - First sample time in burst.
        /// WDT - Tile length of each sample.
        /// WHP - Pressure Sensor height.
        /// WHV - Wave Cell Depth.
        /// WUS - East velocity.
        /// WVS - North velocity.
        /// WZS - Vertical velocity.
        /// WB0 - Beam velocity beam 0.
        /// WB1 - Beam velocity beam 1.
        /// WB2 - Beam velocity beam 2.
        /// WB3 - Beam velocity beam 3.
        /// WHG - Heading.
        /// WPH - Pitch.
        /// WRL - Roll.
        /// WPS - Pressure.
        /// WTS - Water Temperture.
        /// WHS - Wave Height source. (User Select.  Range Tracking Beam or Vertical Beam)
        /// WR0 - Range Tracking Beam 0.
        /// WR1 - Range Tracking Beam 1.
        /// WR2 - Range Tracking Beam 2.
        /// WR3 - Range Tracking Beam 3.
        /// WAH - Average of Range Tracking.
        /// WZ0 - Beam Velocity Vertical Beam.
        /// WZP - Pressure Vertical Beam.
        /// WZR - Range Tracking Vertical Beam.
        /// 
        /// </summary>
        class RtiWavesEncoder
        {
            #region Class and Enum

            /// <summary>
            /// Store the incoming ensemble data.
            /// </summary>
            public class WavesEnsembleData
            {
                /// <summary>
                /// Ensemble data.
                /// </summary>
                public DataSet.Ensemble Ensemble { get; set; }

                /// <summary>
                /// Height source.
                /// </summary>
                public RTI.RecoverDataOptions.HeightSource HeightSource { get; set; } 

                /// <summary>
                /// File directory.
                /// </summary>
                public string FileDirectory { get; set; }

                /// <summary>
                /// File Name.
                /// </summary>
                public string FileName { get; set; }

                /// <summary>
                /// Waves data options.
                /// </summary>
                public RecoverDataOptions Options { get; set; }

                /// <summary>
                /// Initialize the values.
                /// </summary>
                /// <param name="ens">Ensemble.</param>
                /// <param name="CorThres">Correlation Threshold.</param>
                /// <param name="Poffset">Pressure Offset.</param>
                /// <param name="fDir">File Directory.</param>
                public WavesEnsembleData(DataSet.Ensemble ens, string fDir, string fileName, RecoverDataOptions options)
                {
                    Ensemble = ens;
                    FileDirectory = fDir;
                    FileName = fileName;
                    Options = options;
                }
            }

            

            #endregion

            #region Variables

            /// <summary>
            /// Setup logger to report errors.
            /// </summary>
            private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

            #region Waves Matlab

            //int WavesSampleNumber = 0;
            //int LastWavesSampleNumber = 0;                                  // Used to store the last WavesSampleNumber to know when completing reading all the records
            //int WavesVerticalSampleNumber = 0;

            //int WavesTotalSamples = 0;
            //int WavesTotalVerticalSamples = 0;
            //int WavesGoodSamples = 0;
            //int WavesBadSamples = 0;
            //int WavesRecordCount = 0;

            //float WavesCellDepth = 0;

            //const int WavesMaxRows = 8192;
            //const int WavesMaxColumns = 101;

            //float[,] WZBM = new float[6, WavesMaxRows];                     // Vertical beam, Z Beam data 
            //float[] WZHS = new float[WavesMaxRows];                         // Verticanl beam height, Z Beam Height

            //float[,] WTS = new float[WavesMaxColumns, WavesMaxRows];        // Waves time stamp in Matlab serial data number
            //float[,] WPS = new float[WavesMaxColumns, WavesMaxRows];        // Waves pressure series in meters
            //float[,] WUS = new float[WavesMaxColumns, WavesMaxRows];        // Waves East series in m/s
            //float[,] WVS = new float[WavesMaxColumns, WavesMaxRows];        // Waves North series in m/s

            //float[,] WHS = new float[WavesMaxColumns, WavesMaxRows];        // Height source

            //float[,] WZ0 = new float[WavesMaxColumns, WavesMaxRows];        // Vertical Beam Data Beam 0
            //float[,] WZ1 = new float[WavesMaxColumns, WavesMaxRows];        // Vertical Beam Data Beam 1
            //float[,] WZ2 = new float[WavesMaxColumns, WavesMaxRows];        // Vertical Beam Data Beam 2
            //float[,] WZ3 = new float[WavesMaxColumns, WavesMaxRows];        // Vertical Beam Data Beam 3

            //float[,] WB0 = new float[WavesMaxColumns, WavesMaxRows];        // Beam Data Beam 0
            //float[,] WB1 = new float[WavesMaxColumns, WavesMaxRows];        // Beam Data Beam 1 Data
            //float[,] WB2 = new float[WavesMaxColumns, WavesMaxRows];        // Beam Data Beam 2 Data
            //float[,] WB3 = new float[WavesMaxColumns, WavesMaxRows];        // Beam Data Beam 3 Data

            //float[,] WHG = new float[WavesMaxColumns, WavesMaxRows];        // Heading
            //float[,] WPH = new float[WavesMaxColumns, WavesMaxRows];        // Pitch
            //float[,] WRL = new float[WavesMaxColumns, WavesMaxRows];        // Roll
            //double[] WFT = new double[WavesMaxColumns];                     // Sample Time

            //float[, ,] WBM = new float[6, 4, WavesMaxRows];                 // Beam Velocity data

            //double[] WtS = new double[WavesMaxRows];                        // Time Stamp in seconds

            //string WavesDateStr = "";
            //string WavesSNStr = "";

            //float[,] WSHS = new float[4, WavesMaxRows];                     // Range Tracking Range

            //private EnsembleData _lastEnsembleData = null;

            #endregion

            #region Extract Data

            /// <summary>
            /// Previous ensemble number.
            /// </summary>
            //private int _prevEnsNum = 0;

            /// <summary>
            /// Previous 4 beam ensemble.
            /// </summary>
            //private DataSet.Ensemble _prevEns4Beam = null;

            /// <summary>
            /// Previous 1 beam ensemble.
            /// </summary>
            //private DataSet.Ensemble _prevEns1Beam = null;

            /// <summary>
            /// Previous 4 beam flag.
            /// </summary>
            //private bool _prevIs4Beam = true;

            #endregion

            #region Buffer

            /// <summary>
            /// Buffer to store incoming data.
            /// </summary>
            public ConcurrentQueue<WavesEnsembleData> _ensembleBuffer = new ConcurrentQueue<WavesEnsembleData>();
            
            /// <summary>
            /// Flag to know if processing the buffer.
            /// </summary>
            public bool _isProcessingBuffer;

            #endregion

            #region Project Burst

            /// <summary>
            /// Project file to hold all incoming burst data to process.
            /// </summary>
            Project _burstPrj;

            /// <summary>
            /// Heading offset for burst processing.
            /// </summary>
            float _headingOffset;


            /// <summary>
            /// Pitch offset for burst processing.
            /// </summary>
            float _pitchOffset;

            /// <summary>
            /// Roll offset for burst processing.
            /// </summary>
            float _rollOffset;

            /// <summary>
            /// Flag to replace pressure data with vertical beam range data.
            /// </summary>
            bool _isReplacePressure;

            /// <summary>
            /// Output Directory.
            /// </summary>
            string _burstOutputDir;

            /// <summary>
            /// Burst Options.
            /// </summary>
            RecoverDataOptions _burstOptions;

            #endregion

            #endregion

            #region Properties

            public List<WavesRecord> WavesRecords { get; set; }

            /// <summary>
            /// String on the results of extracting the data from the 
            /// ensembles given.
            /// </summary>
            public string WavesRecover { get; set; }

            /// <summary>
            /// Waves Velocity bin.
            /// </summary>
            //public int WavesBin { get; set; }

            /// <summary>
            /// Waves Record number.
            /// </summary>
            //public int WavesRecordNumber { get; set; }

            /// <summary>
            /// Last set file directory.
            /// </summary>
            public string FileDirectory { get; set; }

            /// <summary>
            /// Last set file name.
            /// </summary>
            //public string FileName { get; set; }

            #endregion

            /// <summary>
            /// Initialize the data.
            /// </summary>
            public RtiWavesEncoder()
            {
                _ensembleBuffer = new ConcurrentQueue<WavesEnsembleData>();
                _isProcessingBuffer = false;

                //WavesBin = 1;
                WavesRecover = "";
                //WavesRecordNumber = 0;
                //WavesSampleNumber = 0;
                //LastWavesSampleNumber = 0;
                FileDirectory = "";

                // Create the WavesRecord list and add an initial record
                WavesRecords = new List<WavesRecord>();
                WavesRecords.Add(new WavesRecord());

                // Init the burst processing variables
                _burstPrj = null;
                _headingOffset = 0.0f;
                _pitchOffset = 0.0f;
                _rollOffset = 0.0f;
                _isReplacePressure = false;
                _burstOutputDir = "";
                _burstOptions = new RecoverDataOptions();

            }

            #region Encode Matlab File

            /// <summary>
            /// Buffer the incoming data and process it
            /// </summary>
            /// <param name="ens">Ensemble.</param>
            /// <param name="fDir">File Directory.</param>
            /// <param name="fileName">File name being processed.</param>
            /// <param name="options">Options.</param>
            public void EncodeWavesMatlab(DataSet.Ensemble ens, string fDir, string fileName, RecoverDataOptions options) 
            {
                // Add the data to the buffer
                _ensembleBuffer.Enqueue(new WavesEnsembleData(ens, fDir, fileName, options));

                // If not processing, start processing
                if (!_isProcessingBuffer)
                {
                    EncodeWaves();
                }
            }

            #region Burst Processing File

            /// <summary>
            /// Pass in the raw ensemble files.  This will break
            /// up the large raw ensemble files into burst ensemble files.
            /// Each burst will have a file created.  Those files are then
            /// passed to the importer to create MATLAB files.
            /// </summary>
            /// <param name="files"></param>
            /// <param name="outputDir"></param>
            /// <param name="burstOptions"></param>
            /// <returns>Array of all the files to process.</returns>
            public string[] CreateWaveEnsembleFiles(string[] files, 
                                          string outputDir,
                                          RecoverDataOptions burstOptions)
            {
                // Create a project to store all the burst files we are uploading
                // Set the folder to the create the project
                // Create a file name for the burst file
                // Do not set the serial number, it will be set by the project later
                _burstPrj = new Project("burst", outputDir, "");

                // Set the offset values
                // If the offset value is 0.0, then the offset will not be used
                _headingOffset = burstOptions.HeadingOffset;
                _pitchOffset = burstOptions.PitchOffset;
                _rollOffset = burstOptions.RollOffset;
                _isReplacePressure = burstOptions.IsReplacePressure;
                _burstOptions = burstOptions;
                _burstOutputDir = outputDir;

                // Add all the files
                foreach (var filePath in files)
                {
                    // Open the file
                    if (File.Exists(filePath))
                    {
                        // Get all the files from the codec
                        AdcpBinaryCodecReadFile codec = new RTI.AdcpBinaryCodecReadFile();
                        codec.ProcessDataEvent += Codec_ProcessDataEvent;

                        // Get all the ensembles but process the data through events
                        codec.GetEnsembles(filePath, false);
                    }
                }

                // Check the ensemble firmware version
                // If the firmware version is greater than 0.2.136
                DataSet.Ensemble firstEns = _burstPrj.GetFirstEnsemble();
                if (firstEns != null && firstEns.IsEnsembleAvail)
                {
                    if (firstEns.EnsembleData.SysFirmware.FirmwareRevision <= 136 &&
                        firstEns.EnsembleData.SysFirmware.FirmwareMinor == 2)
                    {
                        return files;
                    }
                }


                // Process the Burst project
                return ProcessBurstProject(_burstPrj).ToArray();

            }

            /// <summary>
            /// Process the waves data received from the files.  This is to create a MATLAB file
            /// to process with Wavector.
            /// </summary>
            /// <param name="ensemble">Raw Ensemble data.</param>
            /// <param name="adcpData">Ensemble object.</param>
            private void Codec_ProcessDataEvent(byte[] ensemble, DataSet.Ensemble adcpData)
            {
                _burstPrj.RecordDbEnsemble(adcpData,
                                           _headingOffset,
                                           _pitchOffset,
                                           _rollOffset,
                                           _isReplacePressure);
            }


            /// <summary>
            /// Create burst ensemble files.  For each burst, a
            /// file will be created.  This will look through the project
            /// for all the unique burst indexes.  It will then group all the
            /// ensembles with the same burst index and put them in an ensemble
            /// file.
            /// It will then return a list of all the files created.
            /// </summary>
            /// <param name="burstProject">Project file to get the burst ensembles.</param>
            /// <returns>A list of all the files created.</returns>
            private List<string> ProcessBurstProject(Project burstProject)
            {
                // Create a list of files created
                List<string> wavesFiles = new List<string>();

                // Create a database reader to get the ensembles
                AdcpDatabaseReader dbReader = new AdcpDatabaseReader();

                // Get all the unique burst Index and burst ID
                DataTable dtBurstIndex = dbReader.GetBurstIndexList(burstProject);

                // Get all the ensembles for each burst and process
                for (int x = 0; x < dtBurstIndex.Rows.Count; x++)
                {
                    int burstID = Convert.ToInt32(dtBurstIndex.Rows[x]["BurstID"]);
                    int burstIndex = Convert.ToInt32(dtBurstIndex.Rows[x]["BurstIndex"]);

                    // Create a burst filename
                    // Burst_ID_000INDEX.m
                    // string burstFilename = "W_" + burstID.ToString("D03") + burstIndex.ToString("D06") + ".ens";
                    string burstFilename = "W" + burstIndex.ToString("D07") + ".ens";

                    // Create a file path
                    string filePath = Path.Combine(_burstOutputDir, burstFilename);

                    // Create a write to write the waves file
                    BinaryWriter writer = new BinaryWriter(File.Open(filePath, FileMode.Create, FileAccess.Write));

                    // Store the wave file path
                    wavesFiles.Add(filePath);

                    // Get all the Ensembles for the burst
                    DataTable burstEns = dbReader.GetBurstEnsembles(burstProject, burstID, burstIndex);

                    // Add all the ensembles to the encoder
                    for (int ensIndex = 0; ensIndex < burstEns.Rows.Count; ensIndex++)
                    {
                        // Get the ensemble
                        DataSet.Ensemble ens = dbReader.DataTabletoEnsemble(burstEns.Rows[ensIndex]);

                        // Write the data to a waves file
                        writer.Write(ens.Encode());
                    }
                }

                return wavesFiles;
            }

            #endregion

            /// <summary>
            /// Extract the waves data from the ensemble.
            /// Use the correlation threshold to determine if the
            /// data is good.  Then write the data to a matlab file.
            /// </summary>
            /// <param name="ens">Ensemble to extract the data.</param>
            /// <param name="CorThres">Correlation threshold.</param>
            /// <param name="Poffset">Pressure offset.</param>
            /// <param name="fDir">File directory.</param>
            private void EncodeWaves()
            {
                // Process the buffer
                while (_ensembleBuffer.Count > 0)
                {
                    _isProcessingBuffer = true;

                    // Try to get the data from the buffer
                    WavesEnsembleData data = null;
                    _ensembleBuffer.TryDequeue(out data);

                    // If data found, then process the data
                    if (data != null)
                    {

                        try
                        {
                            // Check if data is available
                            if (!data.Ensemble.IsEnsembleAvail || !data.Ensemble.IsAncillaryAvail || !data.Ensemble.IsBeamVelocityAvail || !data.Ensemble.IsCorrelationAvail)
                            {
                                //System.Windows.Forms.MessageBox.Show("Data not available to generate wave data");
                                Debug.WriteLine("Data not available to generate wave data");
                                break;
                            }

                            // Set last ensemble
                            // This can also be used as a flag that we got data
                            //_lastEnsembleData = data;

                            // Set file directory
                            FileDirectory = data.FileDirectory;

                            // Set the file name
                            //FileName = data.FileName;

                            // Set the record number.
                            WavesRecords.Last().RecordNumber = GetRecordNumber(data.FileName);

                            // Get the waves data
                            ExtractWaves(data.Ensemble, data.Options);

                            //WavesRecover += ".";
                        }
                        catch (Exception ex)
                        {
                            //System.Windows.Forms.MessageBox.Show(String.Format("caughtH: {0}", ex.GetType().ToString()));
                            string exceptionMessage = String.Format("caughtH: {0}", ex.GetType().ToString());
                            Debug.WriteLine(exceptionMessage);
                            log.Error(exceptionMessage, ex);
                        }
                    }
                }

                _isProcessingBuffer = false;
            }

            /// <summary>
            /// Finialize the the wave record.
            /// Then create a new waves record and add it to
            /// the list of records.  All new incoming data 
            /// will then go into this new waves record.
            /// </summary>
            public void FileComplete()
            {
                // Validate all the data within the record
                // This will clean up any missing ensembles
                // so the data sequential
                //WavesRecords.Last().Validate();

                // Add a new record for the next file read in
                WavesRecords.Add(new WavesRecord());
            }

            /// <summary>
            /// Complete loading the file.
            /// This will calculate the final values
            /// after all the data has been processed for a burst.
            /// </summary>
            /// <returns>List of all the files created.</returns>
            public List<string> Cleanup()
            {
                // Remove any empty wave records
                for(int x = 0; x < WavesRecords.Count; x++)
                {
                    if(WavesRecords[x].WaveSamples.Count <= 0)
                    {
                        WavesRecords.RemoveAt(x);
                    }
                }

                // Get waves stats
                string results = WavesStats();

                // Generate a Matlab file based off the data collected
                List<string> fileList = new List<string>();
                fileList = WavesCreateMatFile();

                results += "Generating Matlab file: \r\n";
                foreach (var fileName in fileList)
                {
                    results += fileName + "\r\n";
                }

                WavesRecover = results + WavesRecover;

                // Reset the value
                //WavesRecordCount = 0;

                return fileList;
            }

            /// <summary>
            /// Get the stats from the wave record.
            /// A sample is a bin measured.
            /// </summary>
            /// <returns>Stats as a string.</returns>
            private string WavesStats()
            {
                int goodCount = 0;                                      // Count all the good samples
                int badCount = 0;                                       // Count all the bad samples
                int totalCount = 0;                                     // Count all the samples collected from all the records
                //int VertTotalCount = 0;                                 // Count all the vertical samples collected

                foreach (var record in WavesRecords)
                {
                    // Ensure data was found
                    if(record.WaveSamples.Count > 0)
                    {
                        foreach (var samp in record.WaveSamples)
                        {
                            // Check for all the good beams in the bin
                            int good = 0;
                            for (int bin = 0; bin < record.SelectedBins.Count; bin++)
                            {
                                for (int beam = 0; beam < samp.BeamVel.GetLength(1); beam++)
                                {
                                    // Beam 0
                                    if (samp.BeamVel[bin, beam] != DataSet.Ensemble.BAD_VELOCITY)
                                    {
                                        good++;
                                    }
                                }
                            }

                            // Vertical beam sample
                            if (samp.IsVerticalSample)
                            {
                                if(good >= 1)
                                {
                                    goodCount++;
                                }
                                else
                                {
                                    badCount++;
                                }
                            }
                            else
                            {
                                // If 3 beams or more are good, then mark as good
                                if (good >= 3)
                                {
                                    goodCount++;
                                }
                                else
                                {
                                    badCount++;
                                }
                            }

                            // Count all the samples
                            totalCount++;
                        }
                    }
                }

                string results;
                results = "Records Read In  = " + (GetWaveRecordCount()).ToString() + "\r\n";           // After completing a file, a new record is added, so remove the last one
                results += "Good Samples     = " + goodCount.ToString() + "\r\n";
                results += "Bad Samples      = " + badCount.ToString() + "\r\n";
                results += "Total Samples    = " + totalCount.ToString() + "\r\n";
                //results += "Vertical Samples = " + WavesTotalVerticalSamples.ToString() + "\r\n";

                return results;
            }

            
            /// <summary>
            /// Extract the waves data from the ensemble.
            /// </summary>
            /// <param name="ens">Ensemble.</param>
            /// <param name="options">Wave options.</param>
            public void ExtractWaves(DataSet.Ensemble ens, RecoverDataOptions options)
            {
                // Check if data is available
                if (!ens.IsEnsembleAvail || !ens.IsAncillaryAvail || !ens.IsBeamVelocityAvail || !ens.IsCorrelationAvail)
                {
                    //System.Windows.Forms.MessageBox.Show("Data not available to generate wave data");
                    Debug.WriteLine("Data not available to generate wave data");
                    return;
                }

                // Send the data to the correct processor
                if(ens.EnsembleData.NumBeams > 1)
                {
                    Process4BeamEnsemble(ens, options);
                }
                else
                {
                    ProcessVerticalBeamEnsemble(ens, options);
                }
                
            }

            /// <summary>
            /// Process the values for the from the first ensemble.
            /// This includes the wave cell depths, first sample time and the 
            /// latitude, longitude and pressure sensor height.
            /// </summary>
            /// <param name="ens">Ensemble data.</param>
            /// <param name="options">Options.</param>
            /// </summary>
            private void ProcessFirstEnsemble(DataSet.Ensemble ens, RecoverDataOptions options)
            {

                // Get the list of selected bins
                var selectedBins = options.GetSelectedBinList();

                // Create wave sample
                WavesSample sample = new WavesSample(ens.EnsembleData.NumBeams, selectedBins);
                sample.IsVerticalSample = false;

                #region Wave Cell Depths

                // Create the WavesCellDepth array
                WavesRecords.Last().WaveCellDepth = new float[selectedBins.Count];

                // Verify the selected bins are good
                for (int bin = 0; bin < selectedBins.Count; bin++)
                {
                    if (selectedBins[bin] > ens.EnsembleData.NumBins)
                    {
                        selectedBins[bin] = ens.EnsembleData.NumBins - 1;           // Fix if the select bin is greater than allowable
                    }
                    if (selectedBins[bin] < 0)
                    {
                        selectedBins[bin] = 0;                                      // Check if the selected bin is to small
                    }

                    // Set the Waves Cell Depth
                    WavesRecords.Last().WaveCellDepth[bin] = ens.AncillaryData.FirstBinRange + selectedBins[bin] * ens.AncillaryData.BinSize;
                }

                // Set Selected bins
                WavesRecords.Last().SelectedBins = selectedBins;

                #endregion

                #region First Sample Time

                int year = ens.EnsembleData.Year;
                int month = ens.EnsembleData.Month;
                int day = ens.EnsembleData.Day;
                int hour = ens.EnsembleData.Hour;
                int minute = ens.EnsembleData.Minute;
                int second = ens.EnsembleData.Second;
                int hsec = ens.EnsembleData.HSec;

                int JDN = rtitime_JulianDayNumber(year, month, day);
                var timeStampSeconds = 24.0 * 3600.0 * JDN + 3600.0 * hour + 60.0 * minute + second + hsec / 100.0;

                if (WavesRecords.Count > 0)
                {
                    // If first record, set the string for date and time and serial number
                    if (WavesRecords.Last().WaveSamples.Count == 0)
                    {
                        // Set the date and time
                        WavesRecords.Last().DateStr = year.ToString("D04") + "/"
                                     + month.ToString("D02") + "/"
                                     + day.ToString("D02") + " "
                                     + hour.ToString("D02") + ":"
                                     + minute.ToString("D02") + ":"
                                     + second.ToString("D02") + "."
                                     + hsec.ToString("D02");

                        // Set the serial number string
                        WavesRecords.Last().SnStr = ens.EnsembleData.SysSerialNumber.ToString();

                        double FirstSampleTime = timeStampSeconds;                                       // Get the first time stamp

                        FirstSampleTime /= (24.0 * 3600.0);                                                     // Convert to days                
                        FirstSampleTime -= 1721059.0;                                                           // Adjust for matlab serial date numbers
                        FirstSampleTime += 0.000011574;
                        WavesRecords.Last().FirstSampleTime = FirstSampleTime;                                  // Set the first sample time for the waves record
                    }

                    #endregion

                #region Latitude, Longitude and Pressure Sensor Height


                    if (ens.IsNmeaAvail && ens.NmeaData.IsGpggaAvail())
                    {
                        WavesRecords.Last().Latitude = ens.NmeaData.GPGGA.Position.Latitude.DecimalDegrees;
                        WavesRecords.Last().Longitude = ens.NmeaData.GPGGA.Position.Longitude.DecimalDegrees;
                    }
                    else
                    {
                        WavesRecords.Last().Latitude = options.Latitude;
                        WavesRecords.Last().Longitude = options.Longitude;
                    }

                    WavesRecords.Last().PressureSensorHeight = options.PressureSensorHeight;
                }

                #endregion

            }

            /// <summary>
            /// Extract the 4 beam waves data from the ensemble.
            /// </summary>
            /// <param name="ens">Ensemble data.</param>
            /// <param name="options">Wave options.</param>
            /// </summary>
            private void Process4BeamEnsemble(DataSet.Ensemble ens, RecoverDataOptions options)
            {
                // Check if data is available
                if (!ens.IsEnsembleAvail || !ens.IsAncillaryAvail || !ens.IsBeamVelocityAvail || !ens.IsCorrelationAvail)
                {
                    //System.Windows.Forms.MessageBox.Show("Data not available to generate wave data");
                    Debug.WriteLine("Data not available to generate wave data");
                    return;
                }

                // Get the list of selected bins
                var selectedBins = options.GetSelectedBinList();

                // Set the environmental data if this is the first 4 beam ensemble
                if(!WavesRecords.Last().IsFirstSampleSet)
                {
                    ProcessFirstEnsemble(ens, options);
                }

                // Create wave sample
                WavesSample sample = new WavesSample(ens.EnsembleData.NumBeams, selectedBins);
                sample.IsVerticalSample = false;

                #region Pressure WaterTemp Heading Pitch Roll

                sample.EnsembleNumber = ens.EnsembleData.EnsembleNumber;
                sample.Pressure = ens.AncillaryData.TransducerDepth + options.PressureOffset;      // Pressure and Pressure offset
                sample.WaterTemp = ens.AncillaryData.WaterTemp;                                     // Water Temp
                sample.Heading = ens.AncillaryData.Heading;                                         // Heading
                sample.Pitch = ens.AncillaryData.Pitch;                                             // Pitch
                sample.Roll = ens.AncillaryData.Roll;                                               // Roll

                #endregion

                #region Sample Time Stamp

                // Length of Sample
                int year = ens.EnsembleData.Year;
                int month = ens.EnsembleData.Month;
                int day = ens.EnsembleData.Day;
                int hour = ens.EnsembleData.Hour;
                int minute = ens.EnsembleData.Minute;
                int second = ens.EnsembleData.Second;
                int hsec = ens.EnsembleData.HSec;
                int JDN = rtitime_JulianDayNumber(year, month, day);
                sample.TimeStampSeconds = 24.0 * 3600.0 * JDN + 3600.0 * hour + 60.0 * minute + second + hsec / 100.0;

                #endregion

                try
                {
                    #region Beam Velocity

                    // Get the beam data for the selected bins
                    int index = 0;
                    foreach (var bin in selectedBins)
                    {
                        // Beam 0
                        if (ens.CorrelationData.CorrelationData[bin, DataSet.Ensemble.BEAM_0_INDEX] >= options.CorrelationThreshold)
                        {
                            sample.BeamVel[index, DataSet.Ensemble.BEAM_0_INDEX] = ens.BeamVelocityData.BeamVelocityData[bin, DataSet.Ensemble.BEAM_0_INDEX];
                        }
                        else
                        {
                            sample.BeamVel[index, DataSet.Ensemble.BEAM_0_INDEX] = DataSet.Ensemble.BAD_VELOCITY;
                        }

                        // Beam 1
                        if (ens.CorrelationData.CorrelationData[bin, DataSet.Ensemble.BEAM_1_INDEX] >= options.CorrelationThreshold)
                        {
                            sample.BeamVel[index, DataSet.Ensemble.BEAM_1_INDEX] = ens.BeamVelocityData.BeamVelocityData[bin, DataSet.Ensemble.BEAM_1_INDEX];
                        }
                        else
                        {
                            sample.BeamVel[index, DataSet.Ensemble.BEAM_1_INDEX] = DataSet.Ensemble.BAD_VELOCITY;
                        }

                        // Beam 2
                        if (ens.CorrelationData.CorrelationData[bin, DataSet.Ensemble.BEAM_2_INDEX] >= options.CorrelationThreshold)
                        {
                            sample.BeamVel[index, DataSet.Ensemble.BEAM_2_INDEX] = ens.BeamVelocityData.BeamVelocityData[bin, DataSet.Ensemble.BEAM_2_INDEX];
                        }
                        else
                        {
                            sample.BeamVel[index, DataSet.Ensemble.BEAM_2_INDEX] = DataSet.Ensemble.BAD_VELOCITY;
                        }

                        // Beam 3
                        if (ens.CorrelationData.CorrelationData[bin, DataSet.Ensemble.BEAM_3_INDEX] >= options.CorrelationThreshold)
                        {
                            sample.BeamVel[index, DataSet.Ensemble.BEAM_3_INDEX] = ens.BeamVelocityData.BeamVelocityData[bin, DataSet.Ensemble.BEAM_3_INDEX];
                        }
                        else
                        {
                            sample.BeamVel[index, DataSet.Ensemble.BEAM_3_INDEX] = DataSet.Ensemble.BAD_VELOCITY;
                        }

                        // Index for each bin selected
                        index++;
                    }

                    #endregion

                    #region WUS WVS WZS

                    // East Velocity
                    // North Velocity
                    // Vertical Velocity
                    if (ens.IsEarthVelocityAvail)
                    {
                        index = 0;
                        foreach (var bin in selectedBins)
                        {
                            sample.EastTransformData[index] = ens.EarthVelocityData.EarthVelocityData[bin, DataSet.Ensemble.BEAM_EAST_INDEX];
                            sample.NorthTransformData[index] = ens.EarthVelocityData.EarthVelocityData[bin, DataSet.Ensemble.BEAM_NORTH_INDEX];
                            sample.VerticalTransformData[index] = ens.EarthVelocityData.EarthVelocityData[bin, DataSet.Ensemble.BEAM_VERTICAL_INDEX];

                            index++;
                        }
                    }

                    #endregion

                    #region Range Tracking, Vertical Beam Height and Wave Height Source

                    // Check if Range tracking
                    //if (ens.IsRangeTrackingAvail && ens.RangeTrackingData.Range.Length > 3)
                    if (ens.IsRangeTrackingAvail )
                    {
                        // Store the range tracking values
                        // Average the Range tracking value and set the VertBeamHeight
                        float avgVertHt = ens.RangeTrackingData.GetAverageRange();

                        // Store the range tracking values
                        for (int i = 0; i < ens.RangeTrackingData.Range.Length; i++)
                        {
                            sample.RangeTracking[i] = ens.RangeTrackingData.Range[i];
                        }

                        // Set vertical beam height just in case there is no vertical beam height
                        sample.VertBeamHeight = avgVertHt;
                    }
                    else
                    {
                        // Set bad value
                        for (int i = 0; i < ens.EnsembleData.NumBeams; i++)
                        {
                            sample.RangeTracking[i] = -1.0f;
                        }

                        // Set bad value
                        sample.VertBeamHeight = -1.0f;
                    }

                    // Cleanup
                    // Have Vertical Height data (Average of all range tracking)
                    // Check VertBeamHeight and use Pressure as backup
                    if (sample.Pressure != 0)
                    {
                        if (sample.VertBeamHeight > 1.2f * sample.Pressure || sample.VertBeamHeight < 0.8f * sample.Pressure)
                        {
                            sample.VertBeamHeight = sample.Pressure;
                        }
                    }

                    // Have Slant height data ?
                    // Check RangeTracking and use Pressure as backup
                    if (sample.RangeTracking[DataSet.Ensemble.BEAM_0_INDEX] != -1)
                    {
                        for (int x = 0; x < ens.RangeTrackingData.Range.Length; x++)
                        {
                            if (sample.Pressure != 0)
                            {
                                if (sample.RangeTracking[x] > 1.2f * sample.Pressure || sample.RangeTracking[x] < 0.8f * sample.Pressure)
                                {
                                    sample.RangeTracking[x] = sample.Pressure;
                                }
                            }
                        }
                    }

                    // Height source
                    switch ((int)Convert.ChangeType(options.BeamHeightSource, typeof(int)))
                    {
                        default:
                        case 4:
                            sample.Height = sample.VertBeamHeight;
                            break;
                        case 0:
                            sample.Height = sample.RangeTracking[DataSet.Ensemble.BEAM_0_INDEX];
                            break;
                        case 1:
                            sample.Height = sample.RangeTracking[DataSet.Ensemble.BEAM_1_INDEX];
                            break;
                        case 2:
                            sample.Height = sample.RangeTracking[DataSet.Ensemble.BEAM_2_INDEX];
                            break;
                        case 3:
                            sample.Height = sample.RangeTracking[DataSet.Ensemble.BEAM_3_INDEX];
                            break;
                    }

                    #endregion

                    // Add the latest sample to the record
                    // When a 4 beam ensemble is decoded
                    WavesRecords.Last().WaveSamples.Add(sample);
                }
                catch (Exception ex)
                {
                    string exceptionmessage = String.Format("Error processing 4 Beam data : {0}", ex.GetType().ToString());
                    Debug.WriteLine(exceptionmessage);
                    log.Error(exceptionmessage, ex);
                }


            }

            /// <summary>
            /// Extract the vertical beam waves data from the ensemble.
            /// </summary>
            /// <param name="ens">Ensemble data.</param>
            /// <param name="options">Wave options.</param>
            /// </summary>
            private void ProcessVerticalBeamEnsemble(DataSet.Ensemble ens, RecoverDataOptions options)
            {
                // Check if data is available
                if (!ens.IsEnsembleAvail || !ens.IsAncillaryAvail || !ens.IsBeamVelocityAvail || !ens.IsCorrelationAvail)
                {
                    //System.Windows.Forms.MessageBox.Show("Data not available to generate wave data");
                    Debug.WriteLine("Data not available to generate wave data");
                    return;
                }

                // Get the list of selected bins
                var selectedBins = options.GetSelectedBinList();

                // Create wave sample
                WavesSample sample = new WavesSample(ens.EnsembleData.NumBeams, selectedBins);
                sample.IsVerticalSample = true;

                #region Sample Time Stamp

                // Length of Sample
                int year = ens.EnsembleData.Year;
                int month = ens.EnsembleData.Month;
                int day = ens.EnsembleData.Day;
                int hour = ens.EnsembleData.Hour;
                int minute = ens.EnsembleData.Minute;
                int second = ens.EnsembleData.Second;
                int hsec = ens.EnsembleData.HSec;
                int JDN = rtitime_JulianDayNumber(year, month, day);
                sample.TimeStampSeconds = 24.0 * 3600.0 * JDN + 3600.0 * hour + 60.0 * minute + second + hsec / 100.0;

                #endregion

                #region Pressure WaterTemp Heading Pitch Roll

                sample.EnsembleNumber = ens.EnsembleData.EnsembleNumber;
                sample.Pressure = ens.AncillaryData.TransducerDepth + options.PressureOffset;       // Pressure and Pressure offset
                sample.VertPressure = sample.Pressure;
                sample.WaterTemp = ens.AncillaryData.WaterTemp;                                     // Water Temp
                sample.Heading = ens.AncillaryData.Heading;                                         // Heading
                sample.Pitch = ens.AncillaryData.Pitch;                                             // Pitch
                sample.Roll = ens.AncillaryData.Roll;                                               // Roll

                #endregion

                try
                {
                    #region Vertical Beam Velocity

                    // Get the vertical beam data for selected bins
                    int index = 0;
                    foreach (var bin in selectedBins)
                    {
                        // Check if the beam meets the correlation threshold
                        if (ens.CorrelationData.CorrelationData[bin, DataSet.Ensemble.BEAM_0_INDEX] >= options.CorrelationThreshold)
                        {
                            // Set the velocity
                            sample.VertBeam[index] = ens.BeamVelocityData.BeamVelocityData[bin, DataSet.Ensemble.BEAM_0_INDEX];
                        }
                        else
                        {
                            // Set bad velocity
                            sample.VertBeam[index] = DataSet.Ensemble.BAD_VELOCITY;
                        }

                        // Index for each bin selected
                        index++;
                    }

                    #endregion

                    #region Range Tracking and Vertical Beam Height

                    // Check if range tracking
                    if (ens.IsRangeTrackingAvail && ens.RangeTrackingData.Range.Length > 0)
                    {
                        // Set vertical beam height
                        sample.RangeTracking[DataSet.Ensemble.BEAM_0_INDEX] = ens.RangeTrackingData.Range[DataSet.Ensemble.BEAM_0_INDEX];
                        sample.VertRangeTracking = ens.RangeTrackingData.Range[DataSet.Ensemble.BEAM_0_INDEX];
                        sample.VertBeamHeight = ens.RangeTrackingData.Range[DataSet.Ensemble.BEAM_0_INDEX];
                    }
                    else
                    {
                        // Set bad value
                        sample.RangeTracking[DataSet.Ensemble.BEAM_0_INDEX] = -1.0f;
                        sample.VertRangeTracking = -1.0f;
                    }

                    // CLEANUP
                    //// Check vertical beam height and use Pressue as backup
                    //if (sample.Pressure != 0)
                    //{
                    //    if (sample.VertBeamHeight > 1.2f * sample.Pressure || sample.VertBeamHeight < 0.8f * sample.Pressure)
                    //    {
                    //        sample.VertBeamHeight = sample.Pressure;
                    //    }
                    //}

                    // Have Slant height data 
                    // Check Range Tracking and use pressure as backup
                    if (sample.RangeTracking[DataSet.Ensemble.BEAM_0_INDEX] != -1)
                    {
                        if (sample.Pressure != 0)
                        {
                            if (sample.RangeTracking[DataSet.Ensemble.BEAM_0_INDEX] > 1.2f * sample.Pressure || sample.RangeTracking[DataSet.Ensemble.BEAM_0_INDEX] < 0.8f * sample.Pressure)
                            {
                                sample.RangeTracking[DataSet.Ensemble.BEAM_0_INDEX] = sample.Pressure;
                                sample.VertRangeTracking = sample.Pressure;
                                sample.VertBeamHeight = sample.Pressure;
                            }
                        }
                    }

                    sample.Height = sample.VertBeamHeight;

                    #endregion

                    // Add the latest sample to the record
                    // When a Vertical beam ensemble is decoded
                    WavesRecords.Last().WaveSamples.Add(sample);

                }
                catch (Exception ex)
                {
                    string exceptionmessage = String.Format("Error processing Vertical Beam data: {0}", ex.GetType().ToString());
                    Debug.WriteLine(exceptionmessage);
                    log.Error(exceptionmessage, ex);
                }
            }

            /// <summary>
            /// Get the number of waves records.
            /// This will verify that a waves records is not empty.
            /// When a new file is complete, it will generate the next
            /// empty waves records.
            /// </summary>
            /// <returns></returns>
            private int GetWaveRecordCount()
            {
                int count = 0;

                foreach(var record in WavesRecords)
                {
                    if(record.WaveSamples.Count > 0)
                    {
                        count++;
                    }
                }

                return count;
            }

            /// <summary>
            /// Get the record number for the currently set
            /// file name.  This includes the record number.
            /// 
            /// LAT - Latitude
            /// LON - Longitude
            /// WFT - First sample time in burst.
            /// WDT - Tile length of each sample.
            /// WHP - Pressure Sensor height.
            /// WHV - Wave Cell Depth.
            /// WUS - East velocity.
            /// WVS - North velocity.
            /// WZS - Vertical velocity.
            /// WB0 - Beam velocity beam 0.
            /// WB1 - Beam velocity beam 1.
            /// WB2 - Beam velocity beam 2.
            /// WB3 - Beam velocity beam 3.
            /// WHG - Heading.
            /// WPH - Pitch.
            /// WRL - Roll.
            /// WPS - Pressure.
            /// WTS - Water Temperture.
            /// WHS - Wave Height source. (User Select.  Range Tracking Beam or Vertical Beam)
            /// WR0 - Range Tracking Beam 0.
            /// WR1 - Range Tracking Beam 1.
            /// WR2 - Range Tracking Beam 2.
            /// WR3 - Range Tracking Beam 3.
            /// WAH - Average of Range Tracking.
            /// WZ0 - Beam Velocity Vertical Beam.
            /// WZP - Pressure Vertical Beam.
            /// WZR - Range Tracking Vertical Beam.
            /// 
            /// </summary>
            /// <returns>Record number from the file name set.</returns>
            private int GetRecordNumber(string fileName)
            {
                string recordNum = fileName.Replace(".mat", "");
                recordNum = recordNum.Replace("W", "");

                int num = 0;
                int.TryParse(recordNum, out num);
                
                return num;
            }

            /// <summary>
            /// Create a matlab file.
            /// </summary>
            /// <returns>File name generated.</returns>
            public List<string> WavesCreateMatFile()
            {
                // List of all the files created
                List<string> fileList = new List<string>();

                /* 
                 * WPS waves pressure series in meters
                 * WUS waves East series in m/s
                 * WVS waves North series in m/s
                 * WSS waves surface series in meters
                 * WTS waves time stamp in Matlab serial data number
                 */

                int wavesRecords = GetWaveRecordCount();
                //int wavesRecordNumber = 0;

                // Go through every record
                for(int wr = 0; wr < WavesRecords.Count; wr++)
                {
                    var record = WavesRecords[wr];

                    // Verify that record has data
                    if (record.WaveSamples.Count > 0)
                    {
                        // Increment the waves record number
                        //wavesRecordNumber++;

                        int wusSampleCount = 0;
                        int wvsSampleCount = 0;
                        int wzsSampleCount = 0;
                        int sampleCount4Beam = 0;
                        int sampleCountVertBeam = 0;
                        int selectedBinCount = record.SelectedBins.Count;
                        double latitude = 0.0;
                        double longitude = 0.0;
                        string info = "";
                        float dt = 0.0f;
                        float pressureSensorHeight = 0.0f;
                        List<byte> wavesCellDepthBuff = new List<byte>();   // WHV

                        var wus = new List<byte>[record.SelectedBins.Count];
                        var wvs = new List<byte>[record.SelectedBins.Count];
                        var wzs = new List<byte>[record.SelectedBins.Count];
                        var beamVel0 = new List<byte>[record.SelectedBins.Count];
                        var beamVel1 = new List<byte>[record.SelectedBins.Count];
                        var beamVel2 = new List<byte>[record.SelectedBins.Count];
                        var beamVel3 = new List<byte>[record.SelectedBins.Count];
                        var beamVelVert = new List<byte>[record.SelectedBins.Count];

                        for (int x = 0; x < record.SelectedBins.Count; x++)
                        {
                            wus[x] = new List<byte>();
                            wvs[x] = new List<byte>();
                            wzs[x] = new List<byte>();
                            beamVel0[x] = new List<byte>();
                            beamVel1[x] = new List<byte>();
                            beamVel2[x] = new List<byte>();
                            beamVel3[x] = new List<byte>();
                            beamVelVert[x] = new List<byte>();
                        }

                        List<byte> wusBuff = new List<byte>();              // WUS
                        List<byte> wvsBuff = new List<byte>();              // WVS
                        List<byte> wzsBuff = new List<byte>();              // WZS
                        List<byte> hdgBuff = new List<byte>();              // WHG
                        List<byte> ptchBuff = new List<byte>();             // WPH
                        List<byte> rollBuff = new List<byte>();             // WRL
                        List<byte> pressureBuff = new List<byte>();         // WPS
                        List<byte> waterTempBuff = new List<byte>();        // WTS
                        List<byte> heightBuff = new List<byte>();           // WHS
                        List<byte> beam0Buff = new List<byte>();            // WB0
                        List<byte> beam1Buff = new List<byte>();            // WB1
                        List<byte> beam2Buff = new List<byte>();            // WB2
                        List<byte> beam3Buff = new List<byte>();            // WB3
                        List<byte> avgHSBuff = new List<byte>();            // WAH
                        List<byte> rngTrkB0Buff = new List<byte>();         // WR0
                        List<byte> rngTrkB1Buff = new List<byte>();         // WR1
                        List<byte> rngTrkB2Buff = new List<byte>();         // WR2
                        List<byte> rngTrkB3Buff = new List<byte>();         // WR3

                        List<byte> beam0VertBuff = new List<byte>();        // WZ0
                        List<byte> pressuresVertBuff = new List<byte>();    // WZP
                        List<byte> rngTrkVertBuff = new List<byte>();       // WZR

                        double firstTimeStamp = 0.0;
                        double secondTimeStamp = 0.0;
                        bool vertBeamFound = false;

                        foreach (var samp in record.WaveSamples)
                        {
                            // Vertical Beam Data
                            if (samp.IsVerticalSample)
                            {
                                #region Vertical Beam

                                // Set each beam velocity for the selected bins
                                for (int x = 0; x < record.SelectedBins.Count; x++)
                                {
                                    // Vertical Beam Velocity
                                    beamVelVert[x].AddRange(MathHelper.FloatToByteArray(samp.VertBeam[x]));
                                }

                                // Pressure
                                pressuresVertBuff.AddRange(MathHelper.FloatToByteArray(samp.Pressure));
                                // Range Tracking
                                rngTrkVertBuff.AddRange(MathHelper.FloatToByteArray(samp.RangeTracking[DataSet.Ensemble.BEAM_0_INDEX]));


                                // Looking for a cycle of 4beam then vertical
                                // If we already found 1 4beam sample and now we
                                // are on a vertical beam sample, a cycle has been
                                // completed
                                if(firstTimeStamp != 0.0)
                                {
                                    vertBeamFound = true;
                                }

                                sampleCountVertBeam++;

                            #endregion
                            }
                            else
                            {
                                #region 4 Beam Data

                                // Collect the time stamps to get the dt
                                // Ensure a cycle has been completed before taking the
                                // second sample.  A cycle is if a 4beam then vertical has 
                                // been seen
                                if (firstTimeStamp == 0.0 || secondTimeStamp == 0.0)
                                {
                                    if (firstTimeStamp == 0.0)
                                    {
                                        firstTimeStamp = samp.TimeStampSeconds;
                                    }
                                    else
                                    {
                                        // First sample taken
                                        // Check if a vert has been found
                                        if (vertBeamFound)
                                        {
                                            secondTimeStamp = samp.TimeStampSeconds;
                                        }
                                    }
                                }

                                // Heading
                                hdgBuff.AddRange(MathHelper.FloatToByteArray(samp.Heading));
                                // Pitch
                                ptchBuff.AddRange(MathHelper.FloatToByteArray(samp.Pitch));
                                // Pitch
                                rollBuff.AddRange(MathHelper.FloatToByteArray(samp.Roll));
                                // Pressure
                                pressureBuff.AddRange(MathHelper.FloatToByteArray(samp.Pressure));
                                // Water Temp
                                waterTempBuff.AddRange(MathHelper.FloatToByteArray(samp.WaterTemp));
                                // Height
                                heightBuff.AddRange(MathHelper.FloatToByteArray(samp.Height));
                                // Average Range Tracking Height
                                avgHSBuff.AddRange(MathHelper.FloatToByteArray(samp.VertBeamHeight));

                                // Set for each selected bin
                                for (int x = 0; x < record.SelectedBins.Count; x++)
                                {
                                    wus[x].AddRange(MathHelper.FloatToByteArray(samp.EastTransformData[x]));            // East Vel
                                    wusSampleCount++;
                                    wvs[x].AddRange(MathHelper.FloatToByteArray(samp.NorthTransformData[x]));           // North Vel
                                    wvsSampleCount++;
                                    wzs[x].AddRange(MathHelper.FloatToByteArray(samp.VerticalTransformData[x]));        // Vertical Vel
                                    wzsSampleCount++;

                                    // Beam Velocity
                                    for (int beam = 0; beam < samp.BeamVel.Length; beam++)
                                    {
                                        if (beam == 0)
                                            beamVel0[x].AddRange(MathHelper.FloatToByteArray(samp.BeamVel[x, DataSet.Ensemble.BEAM_0_INDEX]));
                                        if(beam == 1)
                                            beamVel1[x].AddRange(MathHelper.FloatToByteArray(samp.BeamVel[x, DataSet.Ensemble.BEAM_1_INDEX]));
                                        if(beam == 2)
                                            beamVel2[x].AddRange(MathHelper.FloatToByteArray(samp.BeamVel[x, DataSet.Ensemble.BEAM_2_INDEX]));
                                        if(beam == 3)
                                            beamVel3[x].AddRange(MathHelper.FloatToByteArray(samp.BeamVel[x, DataSet.Ensemble.BEAM_3_INDEX]));
                                    }
                                }


                                // Range Tracking
                                for (int beam = 0; beam < samp.RangeTracking.Length; beam++)
                                {
                                    if (beam == 0)
                                        rngTrkB0Buff.AddRange(MathHelper.FloatToByteArray(samp.RangeTracking[beam]));
                                    if (beam == 1)
                                        rngTrkB1Buff.AddRange(MathHelper.FloatToByteArray(samp.RangeTracking[beam]));
                                    if (beam == 2)
                                        rngTrkB2Buff.AddRange(MathHelper.FloatToByteArray(samp.RangeTracking[beam]));
                                    if (beam == 3)
                                        rngTrkB3Buff.AddRange(MathHelper.FloatToByteArray(samp.RangeTracking[beam]));
                                }

                                sampleCount4Beam++;

                                #endregion
                            }
                        }

                        // If no vertical beam was found or if the first or second time
                        // stamp were sent, then the burst was only a 4beam or vertical burst, not interleaved
                        // So take the first two samples to get the time stamps.
                        if(!vertBeamFound || firstTimeStamp == 0.0 || secondTimeStamp == 0.0 )
                        {
                            if(record.WaveSamples.Count >= 2)
                            {
                                firstTimeStamp = record.WaveSamples[0].TimeStampSeconds;
                                secondTimeStamp = record.WaveSamples[1].TimeStampSeconds;
                             }
                        }

                        // Combine the selected bin data together to make the columns correct
                        // Each column is added one after the other to have the buffer in the
                        // correct order.  A column is all the data for a selected bin.
                        // 1 2 3
                        // 1 2 3
                        // 1 2 3
                        // . . .
                        for (int bin = 0; bin < beamVelVert.Length; bin++)
                        {
                            beam0VertBuff.AddRange(beamVelVert[bin]);
                        }
                        for (int bin = 0; bin < wus.Length; bin++)
                        {
                            wusBuff.AddRange(wus[bin]);
                        }
                        for (int bin = 0; bin < wvs.Length; bin++)
                        {
                            wvsBuff.AddRange(wvs[bin]);
                        }
                        for (int bin = 0; bin < wzs.Length; bin++)
                        {
                            wzsBuff.AddRange(wzs[bin]);
                        }
                        for (int bin = 0; bin < beamVel0.Length; bin++)
                        {
                            beam0Buff.AddRange(beamVel0[bin]);
                        }
                        for (int bin = 0; bin < beamVel1.Length; bin++)
                        {
                            beam1Buff.AddRange(beamVel1[bin]);
                        }
                        for (int bin = 0; bin < beamVel2.Length; bin++)
                        {
                            beam2Buff.AddRange(beamVel2[bin]);
                        }
                        for (int bin = 0; bin < beamVel3.Length; bin++)
                        {
                            beam3Buff.AddRange(beamVel3[bin]);
                        }

                        // Set lat and lon
                        latitude = record.Latitude;
                        longitude = record.Longitude;
                        pressureSensorHeight = record.PressureSensorHeight;
                        info = record.DateStr + ", Record No. " + record.RecordNumber.ToString() + ", SN" + record.SnStr;

                        // Time length for a sample
                        if (record.WaveSamples.Count > 3)
                        {
                            //dt = (float)(record.WaveSamples[2].TimeStampSeconds - record.WaveSamples[1].TimeStampSeconds);
                            dt = (float)(secondTimeStamp - firstTimeStamp);
                        }

                        // Get the cell depth for each selected bin
                        for (int bin = 0; bin < selectedBinCount; bin++ )
                        {
                            wavesCellDepthBuff.AddRange(MathHelper.FloatToByteArray(record.WaveCellDepth[bin]));
                        }

                        // Create a buffer for all the data
                        List<byte> buffer = new List<byte>();

                        //readme
                        // Info String
                        buffer.AddRange(MathHelper.Int32ToByteArray(11));                   // Indicate floating point text
                        buffer.AddRange(MathHelper.Int32ToByteArray(1));                    // Rows
                        buffer.AddRange(MathHelper.Int32ToByteArray(info.Length));          //columns
                        buffer.AddRange(MathHelper.Int32ToByteArray(0));                    //imaginary
                        buffer.AddRange(MathHelper.Int32ToByteArray(4));                    //name length
                        buffer.Add((byte)'t');
                        buffer.Add((byte)'x');
                        buffer.Add((byte)'t');
                        buffer.Add(0);

                        for (int i = 0; i < info.Length; i++)
                        {
                            buffer.AddRange(MathHelper.FloatToByteArray(info[i]));
                        }

                        // Test location 32° 51.901'N, 117° 15.571'W
                        // Lat and Long should be in degrees (convert minutes and seconds)
                        // Use negative degrees for west and south

                        // Latitude
                        buffer.AddRange(MathHelper.Int32ToByteArray(0));                            //indicate double
                        buffer.AddRange(MathHelper.Int32ToByteArray(1));                            //rows - 1 per record
                        buffer.AddRange(MathHelper.Int32ToByteArray(1));                            //columns - 1 per record
                        buffer.AddRange(MathHelper.Int32ToByteArray(0));                            //imaginary
                        buffer.AddRange(MathHelper.Int32ToByteArray(4));                            //name length
                        buffer.Add((byte)'l');
                        buffer.Add((byte)'a');
                        buffer.Add((byte)'t');
                        buffer.Add(0);
                        buffer.AddRange(MathHelper.DoubleToByteArray(latitude));                    // Lat

                        // Longitude
                        buffer.AddRange(MathHelper.Int32ToByteArray(0));                            //indicate double
                        buffer.AddRange(MathHelper.Int32ToByteArray(1));                            //rows - 1 per record
                        buffer.AddRange(MathHelper.Int32ToByteArray(1));                            //columns - 1 per record
                        buffer.AddRange(MathHelper.Int32ToByteArray(0));                            //imaginary
                        buffer.AddRange(MathHelper.Int32ToByteArray(4));                            //name length
                        buffer.Add((byte)'l');
                        buffer.Add((byte)'o');
                        buffer.Add((byte)'n');
                        buffer.Add(0);
                        buffer.AddRange(MathHelper.DoubleToByteArray(longitude));                   // Lon

                        // WFT
                        // First Sample Time
                        buffer.AddRange(MathHelper.Int32ToByteArray(0));                            //indicate double
                        buffer.AddRange(MathHelper.Int32ToByteArray(1));                            //rows - 1 per record
                        buffer.AddRange(MathHelper.Int32ToByteArray(1));                            //columns - 1 per record
                        buffer.AddRange(MathHelper.Int32ToByteArray(0));                            //imaginary
                        buffer.AddRange(MathHelper.Int32ToByteArray(4));                            //name length
                        buffer.Add((byte)'w');
                        buffer.Add((byte)'f');
                        buffer.Add((byte)'t');
                        buffer.Add(0);
                        buffer.AddRange(MathHelper.DoubleToByteArray(record.FirstSampleTime));      // WFT

                        // WDT
                        // Time length for a sample
                        buffer.AddRange(MathHelper.Int32ToByteArray(10));                           //indicate floating point
                        buffer.AddRange(MathHelper.Int32ToByteArray(1));                            //rows - 1 per record
                        buffer.AddRange(MathHelper.Int32ToByteArray(1));                            //columns - 1 per record
                        buffer.AddRange(MathHelper.Int32ToByteArray(0));                            //imaginary
                        buffer.AddRange(MathHelper.Int32ToByteArray(4));                            //name length
                        buffer.Add((byte)'w');
                        buffer.Add((byte)'d');
                        buffer.Add((byte)'t');
                        buffer.Add(0);
                        buffer.AddRange(MathHelper.FloatToByteArray(dt));                           // WDT

                        // WHP
                        // Pressre Sensor Height
                        buffer.AddRange(MathHelper.Int32ToByteArray(10));                           //indicate floating point
                        buffer.AddRange(MathHelper.Int32ToByteArray(1));                            //rows - 1 record
                        buffer.AddRange(MathHelper.Int32ToByteArray(1));                            //columns - 1 per record
                        buffer.AddRange(MathHelper.Int32ToByteArray(0));                            //imaginary
                        buffer.AddRange(MathHelper.Int32ToByteArray(4));                            //name length
                        buffer.Add((byte)'w');
                        buffer.Add((byte)'h');
                        buffer.Add((byte)'p');
                        buffer.Add(0);
                        buffer.AddRange(MathHelper.FloatToByteArray(pressureSensorHeight));         // WHP - height of pressure sensor above the bottom

                        // WHV
                        // Wave Cell Depth                          
                        buffer.AddRange(MathHelper.Int32ToByteArray(10));                           //indicate floating point
                        buffer.AddRange(MathHelper.Int32ToByteArray(1));                            //rows
                        buffer.AddRange(MathHelper.Int32ToByteArray(selectedBinCount));             //columns - 1 each selected bin
                        buffer.AddRange(MathHelper.Int32ToByteArray(0));                            //imaginary
                        buffer.AddRange(MathHelper.Int32ToByteArray(4));                            //name length
                        buffer.Add((byte)'w');
                        buffer.Add((byte)'h');
                        buffer.Add((byte)'v');
                        buffer.Add(0);
                        buffer.AddRange(wavesCellDepthBuff);


                        // ==== 4 Beam Data ====

                        // WUS
                        // East Velocity
                        buffer.AddRange(MathHelper.Int32ToByteArray(10));                           //indicate floating point
                        buffer.AddRange(MathHelper.Int32ToByteArray(sampleCount4Beam));             //rows _ 1 for each sample
                        buffer.AddRange(MathHelper.Int32ToByteArray(selectedBinCount));             //columns - 1 each selected bin
                        buffer.AddRange(MathHelper.Int32ToByteArray(0));                            //imaginary
                        buffer.AddRange(MathHelper.Int32ToByteArray(4));                            //name length
                        buffer.Add((byte)'w');
                        buffer.Add((byte)'u');
                        buffer.Add((byte)'s');
                        buffer.Add(0);
                        buffer.AddRange(wusBuff);                                                   // WUS - East Velocity Data

                        // WVS
                        // North Velocity
                        buffer.AddRange(MathHelper.Int32ToByteArray(10));                           //indicate floating point
                        buffer.AddRange(MathHelper.Int32ToByteArray(sampleCount4Beam));             //rows - 1 for each sample
                        buffer.AddRange(MathHelper.Int32ToByteArray(selectedBinCount));             //columns - 1 each selected bin
                        buffer.AddRange(MathHelper.Int32ToByteArray(0));                            //imaginary
                        buffer.AddRange(MathHelper.Int32ToByteArray(4));                            //name length
                        buffer.Add((byte)'w');
                        buffer.Add((byte)'v');
                        buffer.Add((byte)'s');
                        buffer.Add(0);
                        buffer.AddRange(wvsBuff);

                        // WZS
                        // Vertical Velocity
                        buffer.AddRange(MathHelper.Int32ToByteArray(10));                           //indicate floating point
                        buffer.AddRange(MathHelper.Int32ToByteArray(sampleCount4Beam));             //rows - 1 for each sample
                        buffer.AddRange(MathHelper.Int32ToByteArray(selectedBinCount));             //columns - 1 each selected bin
                        buffer.AddRange(MathHelper.Int32ToByteArray(0));                            //imaginary
                        buffer.AddRange(MathHelper.Int32ToByteArray(4));                            //name length
                        buffer.Add((byte)'w');
                        buffer.Add((byte)'z');
                        buffer.Add((byte)'s');
                        buffer.Add(0);
                        buffer.AddRange(wzsBuff);

                        // WB0
                        // Beam Velocity Beam 0
                        buffer.AddRange(MathHelper.Int32ToByteArray(10));                           //indicate floating point
                        buffer.AddRange(MathHelper.Int32ToByteArray(sampleCount4Beam));             //rows - 1 for each sample
                        buffer.AddRange(MathHelper.Int32ToByteArray(selectedBinCount));             //columns - 1 each selected bin
                        buffer.AddRange(MathHelper.Int32ToByteArray(0));                            //imaginary
                        buffer.AddRange(MathHelper.Int32ToByteArray(4));                            //name length
                        buffer.Add((byte)'w');
                        buffer.Add((byte)'b');
                        buffer.Add((byte)'0');
                        buffer.Add(0);
                        buffer.AddRange(beam0Buff);

                        // WB1
                        // Beam Velocity Beam 1
                        buffer.AddRange(MathHelper.Int32ToByteArray(10));                           //indicate floating point
                        buffer.AddRange(MathHelper.Int32ToByteArray(sampleCount4Beam));             //rows - 1 for each sample
                        buffer.AddRange(MathHelper.Int32ToByteArray(selectedBinCount));             //columns - 1 each selected bin
                        buffer.AddRange(MathHelper.Int32ToByteArray(0));                            //imaginary
                        buffer.AddRange(MathHelper.Int32ToByteArray(4));                            //name length
                        buffer.Add((byte)'w');
                        buffer.Add((byte)'b');
                        buffer.Add((byte)'1');
                        buffer.Add(0);
                        buffer.AddRange(beam1Buff);

                        // WB2
                        // Beam Velocity Beam 2
                        buffer.AddRange(MathHelper.Int32ToByteArray(10));                           //indicate floating point
                        buffer.AddRange(MathHelper.Int32ToByteArray(sampleCount4Beam));             //rows - 1 for each sample
                        buffer.AddRange(MathHelper.Int32ToByteArray(selectedBinCount));             //columns - 1 each selected bin
                        buffer.AddRange(MathHelper.Int32ToByteArray(0));                            //imaginary
                        buffer.AddRange(MathHelper.Int32ToByteArray(4));                            //name length
                        buffer.Add((byte)'w');
                        buffer.Add((byte)'b');
                        buffer.Add((byte)'2');
                        buffer.Add(0);
                        buffer.AddRange(beam2Buff);

                        // WB3
                        // Beam Velocity Beam 3
                        buffer.AddRange(MathHelper.Int32ToByteArray(10));                           //indicate floating point
                        buffer.AddRange(MathHelper.Int32ToByteArray(sampleCount4Beam));             //rows - 1 for each sample
                        buffer.AddRange(MathHelper.Int32ToByteArray(selectedBinCount));             //columns - 1 each selected bin
                        buffer.AddRange(MathHelper.Int32ToByteArray(0));                            //imaginary
                        buffer.AddRange(MathHelper.Int32ToByteArray(4));                            //name length
                        buffer.Add((byte)'w');
                        buffer.Add((byte)'b');
                        buffer.Add((byte)'3');
                        buffer.Add(0);
                        buffer.AddRange(beam3Buff);

                        // WHG
                        // Heading
                        buffer.AddRange(MathHelper.Int32ToByteArray(10));                           //indicate floating point
                        buffer.AddRange(MathHelper.Int32ToByteArray(sampleCount4Beam));             //rows - 1 for each sample
                        buffer.AddRange(MathHelper.Int32ToByteArray(1));                            //columns - 1 per sample
                        buffer.AddRange(MathHelper.Int32ToByteArray(0));                            //imaginary
                        buffer.AddRange(MathHelper.Int32ToByteArray(4));                            //name length
                        buffer.Add((byte)'w');
                        buffer.Add((byte)'h');
                        buffer.Add((byte)'g');
                        buffer.Add(0);
                        buffer.AddRange(hdgBuff);


                        // WPH
                        // Pitch
                        buffer.AddRange(MathHelper.Int32ToByteArray(10));                           //indicate floating point
                        buffer.AddRange(MathHelper.Int32ToByteArray(sampleCount4Beam));             //rows - 1 for each sample
                        buffer.AddRange(MathHelper.Int32ToByteArray(1));                            //columns - 1 per sample
                        buffer.AddRange(MathHelper.Int32ToByteArray(0));                            //imaginary
                        buffer.AddRange(MathHelper.Int32ToByteArray(4));                            //name length
                        buffer.Add((byte)'w');
                        buffer.Add((byte)'p');
                        buffer.Add((byte)'h');
                        buffer.Add(0);
                        buffer.AddRange(ptchBuff);

                        // WRL
                        // Roll
                        buffer.AddRange(MathHelper.Int32ToByteArray(10));                           //indicate floating point
                        buffer.AddRange(MathHelper.Int32ToByteArray(sampleCount4Beam));             //rows - 1 for each sample
                        buffer.AddRange(MathHelper.Int32ToByteArray(1));                            //columns - 1 per sample
                        buffer.AddRange(MathHelper.Int32ToByteArray(0));                            //imaginary
                        buffer.AddRange(MathHelper.Int32ToByteArray(4));                            //name length
                        buffer.Add((byte)'w');
                        buffer.Add((byte)'r');
                        buffer.Add((byte)'l');
                        buffer.Add(0);
                        buffer.AddRange(rollBuff);

                        // WPS
                        // Pressure
                        buffer.AddRange(MathHelper.Int32ToByteArray(10));                           //indicate floating point
                        buffer.AddRange(MathHelper.Int32ToByteArray(sampleCount4Beam));             //rows - 1 for each sample
                        buffer.AddRange(MathHelper.Int32ToByteArray(1));                            //columns - 1 per sample
                        buffer.AddRange(MathHelper.Int32ToByteArray(0));                            //imaginary
                        buffer.AddRange(MathHelper.Int32ToByteArray(4));                            //name length
                        buffer.Add((byte)'w');
                        buffer.Add((byte)'p');
                        buffer.Add((byte)'s');
                        buffer.Add(0);
                        buffer.AddRange(pressureBuff);

                        // WTS
                        // Water Temp
                        buffer.AddRange(MathHelper.Int32ToByteArray(10));                           //indicate floating point
                        buffer.AddRange(MathHelper.Int32ToByteArray(sampleCount4Beam));             //rows - 1 for each sample
                        buffer.AddRange(MathHelper.Int32ToByteArray(1));                            //columns - 1 per sample
                        buffer.AddRange(MathHelper.Int32ToByteArray(0));                            //imaginary
                        buffer.AddRange(MathHelper.Int32ToByteArray(4));                            //name length
                        buffer.Add((byte)'w');
                        buffer.Add((byte)'t');
                        buffer.Add((byte)'s');
                        buffer.Add(0);
                        buffer.AddRange(waterTempBuff);

                        // WHS
                        // Wave Height source (Average of RT or a single RT value)
                        buffer.AddRange(MathHelper.Int32ToByteArray(10));                           //indicate floating point
                        buffer.AddRange(MathHelper.Int32ToByteArray(sampleCount4Beam));             //rows - 1 for each sample
                        buffer.AddRange(MathHelper.Int32ToByteArray(1));                            //columns - 1 per sample
                        buffer.AddRange(MathHelper.Int32ToByteArray(0));                            //imaginary
                        buffer.AddRange(MathHelper.Int32ToByteArray(4));                            //name length
                        buffer.Add((byte)'w');
                        buffer.Add((byte)'h');
                        buffer.Add((byte)'s');
                        buffer.Add(0);
                        buffer.AddRange(heightBuff);

                        // WAH
                        // Average height (Average of all Range Tracking)
                        buffer.AddRange(MathHelper.Int32ToByteArray(10));                           //indicate floating point
                        buffer.AddRange(MathHelper.Int32ToByteArray(sampleCount4Beam));             //rows - 1 for each sample
                        buffer.AddRange(MathHelper.Int32ToByteArray(1));                            //columns - 1 per sample
                        buffer.AddRange(MathHelper.Int32ToByteArray(0));                            //imaginary
                        buffer.AddRange(MathHelper.Int32ToByteArray(4));                            //name length
                        buffer.Add((byte)'w');
                        buffer.Add((byte)'a');
                        buffer.Add((byte)'h');
                        buffer.Add(0);
                        buffer.AddRange(avgHSBuff);

                        // WR0
                        // Range Tracking Beam 0
                        buffer.AddRange(MathHelper.Int32ToByteArray(10));                           //indicate floating point
                        buffer.AddRange(MathHelper.Int32ToByteArray(sampleCount4Beam));             //rows - 1 for each sample
                        buffer.AddRange(MathHelper.Int32ToByteArray(1));                            //columns - 1 per sample
                        buffer.AddRange(MathHelper.Int32ToByteArray(0));                            //imaginary
                        buffer.AddRange(MathHelper.Int32ToByteArray(4));                            //name length
                        buffer.Add((byte)'w');
                        buffer.Add((byte)'r');
                        buffer.Add((byte)'0');
                        buffer.Add(0);
                        buffer.AddRange(rngTrkB0Buff);

                        // WR1
                        // Range Tracking Beam 1
                        buffer.AddRange(MathHelper.Int32ToByteArray(10));                           //indicate floating point
                        buffer.AddRange(MathHelper.Int32ToByteArray(sampleCount4Beam));             //rows - 1 for each sample
                        buffer.AddRange(MathHelper.Int32ToByteArray(1));                            //columns - 1 per sample
                        buffer.AddRange(MathHelper.Int32ToByteArray(0));                            //imaginary
                        buffer.AddRange(MathHelper.Int32ToByteArray(4));                            //name length
                        buffer.Add((byte)'w');
                        buffer.Add((byte)'r');
                        buffer.Add((byte)'1');
                        buffer.Add(0);
                        buffer.AddRange(rngTrkB1Buff);

                        // WR2
                        // Range Tracking Beam 2
                        buffer.AddRange(MathHelper.Int32ToByteArray(10));                           //indicate floating point
                        buffer.AddRange(MathHelper.Int32ToByteArray(sampleCount4Beam));             //rows - 1 for each sample
                        buffer.AddRange(MathHelper.Int32ToByteArray(1));                            //columns - 1 per sample
                        buffer.AddRange(MathHelper.Int32ToByteArray(0));                            //imaginary
                        buffer.AddRange(MathHelper.Int32ToByteArray(4));                            //name length
                        buffer.Add((byte)'w');
                        buffer.Add((byte)'r');
                        buffer.Add((byte)'2');
                        buffer.Add(0);
                        buffer.AddRange(rngTrkB2Buff);

                        // WR3
                        // Range Tracking Beam 3
                        buffer.AddRange(MathHelper.Int32ToByteArray(10));                           //indicate floating point
                        buffer.AddRange(MathHelper.Int32ToByteArray(sampleCount4Beam));             //rows - 1 for each sample
                        buffer.AddRange(MathHelper.Int32ToByteArray(1));                            //columns - 1 per sample
                        buffer.AddRange(MathHelper.Int32ToByteArray(0));                            //imaginary
                        buffer.AddRange(MathHelper.Int32ToByteArray(4));                            //name length
                        buffer.Add((byte)'w');
                        buffer.Add((byte)'r');
                        buffer.Add((byte)'3');
                        buffer.Add(0);
                        buffer.AddRange(rngTrkB3Buff);


                        // ==== Vertical Beam Data ====

                        // WZ0
                        // Vertical Beam Velocity Beam 0
                        buffer.AddRange(MathHelper.Int32ToByteArray(10));                           //indicate floating point
                        buffer.AddRange(MathHelper.Int32ToByteArray(sampleCountVertBeam));          //rows - 1 for each sample
                        buffer.AddRange(MathHelper.Int32ToByteArray(selectedBinCount));             //columns - 1 each selected bin
                        buffer.AddRange(MathHelper.Int32ToByteArray(0));                            //imaginary
                        buffer.AddRange(MathHelper.Int32ToByteArray(4));                            //name length
                        buffer.Add((byte)'w');
                        buffer.Add((byte)'z');
                        buffer.Add((byte)'0');
                        buffer.Add(0);
                        buffer.AddRange(beam0VertBuff);

                        // WZP
                        // Vertical Beam Pressure
                        buffer.AddRange(MathHelper.Int32ToByteArray(10));                           //indicate floating point
                        buffer.AddRange(MathHelper.Int32ToByteArray(sampleCountVertBeam));          //rows - 1 for each sample
                        buffer.AddRange(MathHelper.Int32ToByteArray(1));                            //columns - 1 per sample
                        buffer.AddRange(MathHelper.Int32ToByteArray(0));                            //imaginary
                        buffer.AddRange(MathHelper.Int32ToByteArray(4));                            //name length
                        buffer.Add((byte)'w');
                        buffer.Add((byte)'z');
                        buffer.Add((byte)'p');
                        buffer.Add(0);
                        buffer.AddRange(pressuresVertBuff);

                        // WZR
                        // Vertical Beam Range Tracking
                        buffer.AddRange(MathHelper.Int32ToByteArray(10));                           //indicate floating point
                        buffer.AddRange(MathHelper.Int32ToByteArray(sampleCountVertBeam));          //rows - 1 for each sample
                        buffer.AddRange(MathHelper.Int32ToByteArray(1));                            //columns - 1 per sample
                        buffer.AddRange(MathHelper.Int32ToByteArray(0));                            //imaginary
                        buffer.AddRange(MathHelper.Int32ToByteArray(4));                            //name length
                        buffer.Add((byte)'w');
                        buffer.Add((byte)'z');
                        buffer.Add((byte)'r');
                        buffer.Add(0);
                        buffer.AddRange(rngTrkVertBuff);
                        


                        string fileName = "W" + record.RecordNumber.ToString("D07") + ".mat";

                        WriteRecord(buffer.ToArray(),                                               // Buffer 
                                            0,                                                      // Buffer offset
                                            buffer.Count,                                           // Buffer length
                                            FileDirectory,                                          // Dir
                                            fileName,                                               // File name
                                            true);                                                  // Create new

                        // Set the file path
                        record.FilePath = FileDirectory + @"\" + fileName;

                        // Add file name to list
                        fileList.Add(record.FilePath);
                    }
                }

                // Return the list of files created
                return fileList;
            }

            /// <summary>
            /// Write the buffer to the file.
            /// </summary>
            /// <param name="buf">Buffer to write.</param>
            /// <param name="offset">Offset within the buffer.</param>
            /// <param name="bytes">Number of bytes to write.</param>
            /// <param name="DirectoryName">Directory to write the file.</param>
            /// <param name="CaptureFileName">File name.</param>
            /// <param name="ForceCreateNew">Overwrite the file flag.</param>
            /// <returns>File path of the file created.</returns>
            string WriteRecord(byte[] buf, int offset, int bytes, string DirectoryName, string CaptureFileName, Boolean ForceCreateNew)
            {
                Boolean OK = true;
                DirectoryInfo di = new DirectoryInfo(DirectoryName);

                try
                {
                    // Determine whether the directory exists.
                    if (di.Exists)
                    {
                    }
                    else
                    {
                        // Try to create the directory.
                        di.Create();
                    }
                    // Delete the directory.
                    //di.Delete();
                }
                catch
                {
                    OK = false;
                    //WriteMessageTxtSerial("Can't create directory", true);
                }

                string Path = "Empty File";

                if (OK)
                {
                    Path = DirectoryName + @"\\" + CaptureFileName;
                    FileStream fs;

                    //if (ForceCreateNew)
                    //{
                    //    if (File.Exists(Path))
                    //    {
                    //        if (System.Windows.Forms.MessageBox.Show("Ok to overwrite " + Path + "?", "Erase File", System.Windows.Forms.MessageBoxButtons.OKCancel) != System.Windows.Forms.DialogResult.OK)
                    //        {
                    //            OK = false;
                    //        }
                    //        else
                    //        {
                                fs = new FileStream(Path, FileMode.Create);
                                fs.Close();
                        //    }
                        //}
                    //}

                    if (OK)
                    {
                        try
                        {
                            // Append or create a new file
                            if (File.Exists(Path))
                            {
                                fs = new FileStream(Path, FileMode.Append);
                            }
                            else
                            {
                                fs = new FileStream(Path, FileMode.Create);
                            }

                            // Write the data to the file
                            BinaryWriter w = new BinaryWriter(fs);
                            w.Write(buf, offset, bytes);
                            w.Close();
                            fs.Close();
                        }
                        catch (System.Exception ex)
                        {
                            string exceptionmessage = String.Format("caughtA: {0}", ex.GetType().ToString());
                            //WriteMessageTxtSerial(exceptionmessage, true);
                            Debug.WriteLine(exceptionmessage);
                            log.Error(exceptionmessage);
                        }
                    }
                }
                return Path;
            }

            ///// <summary>
            ///// Height Source.
            ///// </summary>
            ///// <returns>Return the Height source value.</returns>
            //private int Hss()
            //{
            //    return (int)Convert.ChangeType(BeamHeightSource, typeof(int));
            //}

            /// <summary>
            /// Convert RTI time to Julian day.
            /// </summary>
            /// <param name="year">Year.</param>
            /// <param name="month">Month.</param>
            /// <param name="day">Day.</param>
            /// <returns>Julian day.</returns>
            int rtitime_JulianDayNumber(int year, int month, int day)
            {
                int a = (14 - month) / 12;
                int y = year + 4800 - a;
                int m = month + 12 * a - 3;

                int JDN = day + (153 * m + 2) / 5 + (365 * y) + y / 4 - y / 100 + y / 400 - 32045;

                return JDN;
            }

            #endregion
        }
    }
}
