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
 * 10/30/2014      RC          4.1.0      Initial coding
 * 
 * 
 * 
 */

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace RTI
{
    namespace Waves
    {
        /// <summary>
        /// RTI Waves processing.
        /// </summary>
        public class RtiWaves
        {

            #region Enums and classes

            ///// <summary>
            ///// Height source.
            ///// </summary>
            //public enum HeightSource
            //{
            //    /// <summary>
            //    /// Beam 0.
            //    /// </summary>
            //    Beam0 = 0,

            //    /// <summary>
            //    /// Beam 1.
            //    /// </summary>
            //    Beam1 = 1,

            //    /// <summary>
            //    /// Beam 2.
            //    /// </summary>
            //    Beam2 = 2,

            //    /// <summary>
            //    /// Beam 3.
            //    /// </summary>
            //    Beam3 = 3,

            //    /// <summary>
            //    /// Vertical Beam.
            //    /// </summary>
            //    Vertical = 4
            //}

            ///// <summary>
            ///// Store the incoming ensemble data.
            ///// </summary>
            //public class EnsembleData
            //{
            //    /// <summary>
            //    /// Ensemble data.
            //    /// </summary>
            //    public DataSet.Ensemble Ensemble { get; set; }

            //    /// <summary>
            //    /// Correlation Threshold.
            //    /// </summary>
            //    public float CorrelationThreshold { get; set; }

            //    /// <summary>
            //    /// Pressure offset.
            //    /// </summary>
            //    public float PressureOffset { get; set; }

            //    /// <summary>
            //    /// File directory.
            //    /// </summary>
            //    public string FileDirectory { get; set; }

            //    /// <summary>
            //    /// Initialize the values.
            //    /// </summary>
            //    /// <param name="ens">Ensemble.</param>
            //    /// <param name="CorThres">Correlation Threshold.</param>
            //    /// <param name="Poffset">Pressure Offset.</param>
            //    /// <param name="fDir">File Directory.</param>
            //    public EnsembleData(DataSet.Ensemble ens, float CorThres, float Poffset, string fDir)
            //    {
            //        Ensemble = ens;
            //        CorrelationThreshold = CorThres;
            //        PressureOffset = Poffset;
            //        FileDirectory = fDir;
            //    }
            //}


            #endregion

            #region Variables

            /// <summary>
            /// Default number of wave bands.
            /// </summary>
            public const int DEFAULT_WAVES_MAX_BANDS = 100;

            /// <summary>
            /// Default minimum Frequency.
            /// </summary>
            public const double DEFAULT_MIN_FREQ = 0.035;

            /// <summary>
            /// Default Max scale factor.
            /// </summary>
            public const double DEFAULT_MAX_SCALE_FACTOR = 200;

            /// <summary>
            /// Default minimum height.
            /// </summary>
            public const double DEFAULT_MIN_HEIGHT = 0.1;

            /// <summary>
            /// Default is Height sensor beam.
            /// </summary>
            public const bool DEFAULT_IS_HEIGHT_SENSOR_BEAM = true;

            /// <summary>
            /// Setup logger to report errors.
            /// </summary>
            private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

            //int WavesProws = 0;
            //int WavesPcols = 0;
            //int WavesUrows = 0;
            //int WavesUcols = 0;
            //int WavesVrows = 0;
            //int WavesVcols = 0;
            //int WavesZrows = 0;
            //int WavesZcols = 0;
            //int WavesTrows = 0;
            //int WavesTcols = 0;
            //int WavesHrows = 0;
            //int WavesHcols = 0;

            const int WavesMaxRows = 8192;
            const int WavesMaxColumns = 101;

            double[] WavesFrequency = new double[WavesMaxRows];//frequency array

            const int WavesMaxBands = 1000;
            double WavesLowFreqCutOff = 0.035;//28.6 seconds
            double WavesMaximumScaleFactor = 200;
            double WavesMinimumHeight = 0.1;

            double[] WavesDemeanedAveragePressure = new double[WavesMaxColumns];
            double[] WavesDemeanedAverageU = new double[WavesMaxColumns];
            double[] WavesDemeanedAverageV = new double[WavesMaxColumns];
            public double[] WavesAverageUVmag = new double[WavesMaxColumns];
            public double[] WavesAverageUVdir = new double[WavesMaxColumns];


            public double[] WavesHs = new double[WavesMaxColumns];
            double[] WavesPeakFreq = new double[WavesMaxColumns];
            public double[] WavesPeakPeriod = new double[WavesMaxColumns];
            public double[] WavesMeanPeriod = new double[WavesMaxColumns];
            double[] WavesMeanFreq = new double[WavesMaxColumns];
            public double[] WavesPeakDir = new double[WavesMaxColumns];
            public double[] WavesPeakSpread = new double[WavesMaxColumns];

            double[] WavesSu = new double[WavesMaxBands];
            public double[] WavesSp = new double[WavesMaxBands];
            public double[] WavesDir = new double[WavesMaxBands];
            public double[] WavesSpread = new double[WavesMaxBands];
            public double[] WavesSpreadR1 = new double[WavesMaxBands];

            public double[] uvWavesBandedSpectrum = new double[WavesMaxBands];
            public double[][] uWavesBandedSpectrum = new double[WavesMaxColumns][];
            public double[][] vWavesBandedSpectrum = new double[WavesMaxColumns][];
            public double[] pWavesBandedSpectrum = new double[WavesMaxBands];
            public double[] hWavesBandedSpectrum = new double[WavesMaxBands];

            double[] puWavesBandedCrossSpectrum = new double[WavesMaxBands];
            double[] pvWavesBandedCrossSpectrum = new double[WavesMaxBands];

            double[] huWavesBandedCrossSpectrum = new double[WavesMaxBands];
            double[] hvWavesBandedCrossSpectrum = new double[WavesMaxBands];
            double[] uvWavesBandedCrossSpectrum = new double[WavesMaxBands];

            public double[] WavesBandedFrequency = new double[WavesMaxBands];
            public double[] WavesBandedPeriod = new double[WavesMaxBands];
            double[] WavesBandedBandwidth = new double[WavesMaxBands];

            int[] WavesBandStart = new int[WavesMaxRows];
            int[] WavesBandEnd = new int[WavesMaxRows];

            double[] WavesDenominator = new double[WavesMaxBands];

            const int WAVES_RAW_SERIES = 1;
            const int WAVES_PERIOD = 2;
            const int WAVES_FREQUENCY = 3;
            const int WAVES_WAV_SET = 4;
            const int WAVES_VEL_SET = 5;
            const int WAVES_SEN_SET = 6;
            const int WAVES_FFT = 7;
            const int WAVES_SPECTRUM = 8;

            string WavesFileNameStr = "";

            /// <summary>
            /// Keep track of all the wave bands for each selected bin.
            /// </summary>
            public int[] WavesBands = new int[WavesMaxColumns];

            /// <summary>
            /// Acumulated Pressure values as an array.
            /// Pressure [Samples]
            /// </summary>
            float[] WavesRecordPressure;

            /// <summary>
            /// Pressure spectrum.
            /// [Samples/2]
            /// </summary>
            public Complex[] pSpectrum;

            /// <summary>
            /// Accumulated Temperature values as an array.
            /// Temperature [Samples]
            /// </summary>
            float[] WavesRecordTemperature;

            /// <summary>
            /// Accumulated Wave Height values as an array.
            /// Height [Samples]
            /// </summary>
            float[] WavesRecordHeightSource;

            /// <summary>
            /// Height source spectrum.
            /// [Samples/2]
            /// </summary>
            public Complex[] hSpectrum;

            /// <summary>
            /// Accumulated East Velocities for each selected bin as an array.
            /// East Velocity [SelectedBins][Samples]
            /// </summary>
            float[][] WavesEastVelocitySeries;

            /// <summary>
            /// East Spectrum values.
            /// [SelectedBins][Samples/2]
            /// </summary>
            public Complex[][] eastSpectrum;

            /// <summary>
            /// Average east velocity for each selected bin.
            /// [SelectedBins]
            /// </summary>
            double[] WavesRecordAverageEastVelocity;

            /// <summary>
            /// Accumulated North Velocities for each selected bin as an array.
            /// North Velocity [SelectedBins][Samples]
            /// </summary>
            float[][] WavesNorthVelocitySeries;

            /// <summary>
            /// North Spectrum values.
            /// [SelectedBins][Samples/2]
            /// </summary>
            public Complex[][] northSpectrum;

            /// <summary>
            /// Averaged North velocity for each selected bin.
            /// [SelectedBin]
            /// </summary>
            double[] WavesRecordAverageNorthVelocity;

            /// <summary>
            /// East and North spectrum.
            /// [Samples/2]
            /// </summary>
            public Complex[][] uvSpectrum;

            /// <summary>
            /// Accumulated Vertical Velocities for each selected bin as an array.
            /// Vertical Velocity [SelectedBins, Samples]
            /// </summary>
            float[,] WavesZseries;

            /// <summary>
            /// Accumulated Average Height for each selected bin as an array.
            /// Average Height [SelectedBins]
            /// </summary>
            public double[] WavesRecordAverageHeight;

            /// <summary>
            /// Accumulated Average Pressure for each selected bin as an array.
            /// Average Height [SelectedBins]
            /// </summary>
            public double[] WavesRecordAveragePressure;

            /// <summary>
            /// Accumulated Average Temperature for each selected bin as an array.
            /// Average Temperature [SelectedBins]
            /// </summary>
            public double[] WavesRecordAverageTemperature;

            #endregion

            #region Properties

            /// <summary>
            /// Waves Record read in.
            /// </summary>
            public WavesRecord Record { get; set; }

            /// <summary>
            /// Number of Waves bands to try and display.
            /// </summary>
            public int NumWavesBands { get; set; }

            /// <summary>
            /// Waves minimum frequency.
            /// </summary>
            public double WavesMinFreq { get; set; }

            /// <summary>
            /// Waves Max Scale Factor.
            /// </summary>
            public double WavesMaxScaleFactor { get; set; }

            /// <summary>
            /// Waves minimum height.
            /// </summary>
            public double WavesMinHeight { get; set; }

            /// <summary>
            /// Waves Record number.
            /// </summary>
            public int WavesRecordNumber { get; set; }

            /// <summary>
            /// String on the results of waves processing the data.
            /// </summary>
            public string WavesProc { get; set; }

            /// <summary>
            /// Set a flag if using Height sensor beam.
            /// </summary>
            public bool IsHeightSensorBeam { get; set; }

            #region Averages

            /// <summary>
            /// Average Pressure.
            /// </summary>
            public double WavesMeanPressure = 0;

            /// <summary>
            /// Average East Velocity for all the selected bins.
            /// </summary>
            public double WavesMeanU = 0;

            /// <summary>
            /// Average Vertical velocity for all the selected bins.
            /// </summary>
            public double WavesMeanV = 0;

            /// <summary>
            /// Average temperature.
            /// </summary>
            public double WavesMeanTemperature = 0;

            #endregion

            #endregion

            /// <summary>
            /// Initialize the values.
            /// </summary>
            public RtiWaves()
            {
                //WavesBin = 1;
                NumWavesBands = DEFAULT_WAVES_MAX_BANDS;
                WavesMinFreq = DEFAULT_MIN_FREQ;
                WavesMaxScaleFactor = DEFAULT_MAX_SCALE_FACTOR;
                WavesMinHeight = DEFAULT_MIN_HEIGHT;
                WavesRecordNumber = 0;
                WavesProc = "";
                IsHeightSensorBeam = DEFAULT_IS_HEIGHT_SENSOR_BEAM;

                Record = new WavesRecord();
            }

            #region Decode Matlab file

            /// <summary>
            /// Read in the waves data.
            /// 
            /// Rows = Number of samples
            /// Columns = Number of Selected bins
            /// Some values are 1 value per record like Lat, Lon, and Wave Cell Depth
            /// Some values are 1 value per sample like, pressure, temp, ...
            /// Some values are 1 value per sample and selected bin [Selected bin, Sample] like Velocities
            /// </summary>
            /// <param name="forceread">Flag to force reading.</param>
            /// <param name="filePath">File path to file to read.</param>
            public void ImportMatlabWaves(bool forceread, string filePath)
            {
                bool UseBeam = false;
                if (IsHeightSensorBeam)
                    UseBeam = true;

                //buttonWavesExport.Enabled = false;

                if (forceread || Record.TimeBetweenSamples <= 0)
                {
                    if (ReadInFile(filePath))
                    {
                        // Process all the Selected bins
                        for (int i = 0; i < Record.WaveCellDepth.Length; i++)
                        {
                            Waves_Calc(Record.TimeBetweenSamples, Record.WaveSamples.Count, i, WavesMaxBands, UseBeam);
                        }

                        // Get the average for all the selected bins averages
                        WavesMeanU = RTI.Waves.WavesCalc.AverageArray(Record.WaveCellDepth.Length, WavesRecordAverageEastVelocity);
                        WavesMeanV = RTI.Waves.WavesCalc.AverageArray(Record.WaveCellDepth.Length, WavesRecordAverageNorthVelocity);
                        WavesMeanPressure = WavesRecordPressure.Average();
                        WavesMeanTemperature = WavesRecordTemperature.Average();

                        for (int x = 0; x < Record.WaveCellDepth.Length; x++)
                        {
                            //WavesDemeanedAveragePressure[i] = WavesRecordAveragePressure[i] - WavesMeanPressure;
                            WavesDemeanedAverageU[x] = WavesRecordAverageEastVelocity[x] - WavesMeanU;
                            WavesDemeanedAverageV[x] = WavesRecordAverageNorthVelocity[x] - WavesMeanV;
                            WavesAverageUVmag[x] = Math.Sqrt(WavesRecordAverageEastVelocity[x] * WavesRecordAverageEastVelocity[x] + WavesRecordAverageNorthVelocity[x] * WavesRecordAverageNorthVelocity[x]);
                            WavesAverageUVdir[x] = (180.0 / Math.PI) * Math.Atan2(WavesRecordAverageEastVelocity[x], WavesRecordAverageNorthVelocity[x]);
                        }

                        // Show the content of the file
                        Wave_ShowFileContents();
                    }
                }
            }

            /// <summary>
            /// Read in the file information from the given stream.
            /// </summary>
            /// <param name="filePath">File path to read.</param>
            private bool ReadInFile(string filePath)
            {
                if(!File.Exists(filePath))
                {
                    return false;
                }

                bool OK = false;

                // Set the file path
                Record.FilePath = filePath;

                // Open the file
                using (FileStream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                {
                    using (BufferedStream bs = new BufferedStream(fileStream))
                    {
                        //long nBytesRead = 0;
                        long nBytes = 0;

                        try
                        {
                            if (bs != null)
                            {

                                WavesFileNameStr = filePath;
                                try
                                {
                                    nBytes = fileStream.Length;
                                    byte[] wBuff = new byte[nBytes];
                                    var nBytesRead = bs.ReadAsync(wBuff, 0, (int)nBytes);

                                    //decode the mat file
                                    int type;
                                    int nrows;
                                    int ncols;
                                    int imag;
                                    int namelen;
                                    string name;

                                    WavesProc = "";
                                    //PacketPointer = 0;
                                    int index = 0;

                                    while (index < nBytesRead.Result)
                                    {
                                        type = MathHelper.ByteArrayToInt32(wBuff, index); index += 4;                           // Value Types (float, int, ...)
                                        nrows = MathHelper.ByteArrayToInt32(wBuff, index); index += 4;                          // Number of Rows
                                        ncols = MathHelper.ByteArrayToInt32(wBuff, index); index += 4;                          // Number of columns
                                        imag = MathHelper.ByteArrayToInt32(wBuff, index); index += 4;                           // Imaginary
                                        namelen = MathHelper.ByteArrayToInt32(wBuff, index); index += 4;                        // Name length
                                        name = MathHelper.ByteArrayToString(wBuff, namelen - 1, index); index += namelen;       // Name

                                        switch (name)
                                        {
                                            case "txt":
                                                {
                                                    string s = "";
                                                    for (int j = 0; j < ncols; j++)
                                                    {
                                                        s += (char)MathHelper.ByteArrayToFloat(wBuff, index);
                                                        index += 4;
                                                    }
                                                    Record.InfoTxt = s;
                                                    break;
                                                }
                                            case "wdt":
                                                {
                                                    Record.TimeBetweenSamples = MathHelper.ByteArrayToFloat(wBuff, index);
                                                    index += 4;
                                                    break;
                                                }
                                            case "whp":
                                                {
                                                    Record.PressureSensorHeight = MathHelper.ByteArrayToFloat(wBuff, index);
                                                    index += 4;
                                                    break;
                                                }
                                            case "whv":
                                                {
                                                    // ncols = Number of selected bins
                                                    Record.WaveCellDepth = new float[ncols];
                                                    for (int x = 0; x < ncols; x++)
                                                    {
                                                        Record.WaveCellDepth[x] = MathHelper.ByteArrayToFloat(wBuff, index);
                                                        index += 4;
                                                    }
                                                    break;
                                                }
                                            case "lat":
                                                {
                                                    Record.Latitude = MathHelper.ByteArrayToDouble(wBuff, index);
                                                    index += 8;
                                                    break;
                                                }
                                            case "lon":
                                                {
                                                    Record.Longitude = MathHelper.ByteArrayToDouble(wBuff, index);
                                                    index += 8;
                                                    break;
                                                }
                                            case "wft":
                                                {
                                                    Record.FirstSampleTime = MathHelper.ByteArrayToDouble(wBuff, index);
                                                    index += 8;
                                                    break;
                                                }
                                            case "wps":
                                                {
                                                    // Add the samples if the list isn't created
                                                    AddSamples(nrows, 3);

                                                    // Update the array size if it does not match the number of samples
                                                    if (WavesRecordPressure == null || WavesRecordPressure.Length != nrows)
                                                    {
                                                        WavesRecordPressure = new float[nrows];
                                                        pSpectrum = new Complex[nrows / 2];
                                                    }

                                                    for (int samp = 0; samp < nrows; samp++ )
                                                    {
                                                        Record.WaveSamples[samp].Pressure = MathHelper.ByteArrayToFloat(wBuff, index);
                                                        WavesRecordPressure[samp] = Record.WaveSamples[samp].Pressure;
                                                        index += 4;
                                                    }
                                                    break;
                                                }
                                            case "wus":
                                                {
                                                    OK = true;

                                                    // nrows = samples
                                                    // ncols = selected bins

                                                    // Add the samples if the list isn't created
                                                    AddSamples(nrows, ncols);

                                                    // Update the array size if it does not match the number of samples and selected bin
                                                    if (WavesEastVelocitySeries == null || WavesEastVelocitySeries.GetLength(1) != nrows)
                                                    {
                                                        WavesEastVelocitySeries = new float[ncols][];
                                                        eastSpectrum = new Complex[ncols][];
                                                        WavesRecordAverageEastVelocity = new double[ncols];
                                                        WavesRecordAveragePressure = new double[ncols];
                                                        WavesRecordAverageHeight = new double[ncols];
                                                        WavesRecordAverageTemperature = new double[ncols];

                                                        if(uvSpectrum == null || uvSpectrum.GetLength(0) != ncols)
                                                        {
                                                            uvSpectrum = new Complex[ncols][];
                                                        }
                                                    }

                                                    // Add the data to array and record
                                                    for (int j = 0; j < ncols; j++)
                                                    {
                                                        WavesEastVelocitySeries[j] = new float[nrows];
                                                        for (int i = 0; i < nrows; i++)
                                                        {
                                                            float eastVel = MathHelper.ByteArrayToFloat(wBuff, index);
                                                            Record.WaveSamples[i].EastTransformData[j] = eastVel;
                                                            WavesEastVelocitySeries[j][i] = eastVel;
                                                            index += 4;
                                                        }
                                                    }
                                                    break;
                                                }
                                            case "wvs":
                                                {
                                                    // nrows = samples
                                                    // ncols = selected bins

                                                    // Add the samples if the list isn't created
                                                    AddSamples(nrows, ncols);

                                                    // Update the array size if it does not match the number of samples and selected bin
                                                    // nrows = samples
                                                    // ncols = selected bins
                                                    if (WavesNorthVelocitySeries == null || WavesNorthVelocitySeries.GetLength(1) != nrows)
                                                    {
                                                        WavesNorthVelocitySeries = new float[ncols][];
                                                        northSpectrum = new Complex[ncols][];
                                                        WavesRecordAverageNorthVelocity = new double[ncols];

                                                        if (uvSpectrum == null || uvSpectrum.GetLength(0) != ncols)
                                                        {
                                                            uvSpectrum = new Complex[ncols][];
                                                        }
                                                    }

                                                    for (int j = 0; j < ncols; j++)
                                                    {
                                                        WavesNorthVelocitySeries[j] = new float[nrows];
                                                        for (int i = 0; i < nrows; i++)
                                                        {
                                                            float northVel = MathHelper.ByteArrayToFloat(wBuff, index);
                                                            Record.WaveSamples[i].NorthTransformData[j] = northVel;
                                                            WavesNorthVelocitySeries[j][i] = northVel;
                                                            index += 4;
                                                        }
                                                    }
                                                    break;
                                                }
                                            case "wzs":
                                                {

                                                    // nrows = samples
                                                    // ncols = selected bins

                                                    // Add the samples if the list isn't created
                                                    AddSamples(nrows, ncols);

                                                    // Update the array size if it does not match the number of samples and selected bin
                                                    if (WavesZseries == null || WavesZseries.Length != nrows)
                                                    {
                                                        WavesZseries = new float[ncols, nrows];
                                                    }

                                                    for (int j = 0; j < ncols; j++)
                                                    {
                                                        for (int i = 0; i < nrows; i++)
                                                        {
                                                            float vertVel = MathHelper.ByteArrayToFloat(wBuff, index);
                                                            Record.WaveSamples[i].VerticalTransformData[j] = vertVel;
                                                            WavesZseries[j, i] = vertVel;
                                                            index += 4;
                                                        }
                                                    }
                                                    break;
                                                }
                                            case "wts":
                                                {
                                                    // Add the samples if the list isn't created
                                                    AddSamples(nrows, 3);

                                                    // Update the array size if it does not match the number of samples
                                                    if (WavesRecordTemperature == null || WavesRecordTemperature.Length != nrows)
                                                    {
                                                        WavesRecordTemperature = new float[nrows];
                                                    }

                                                    for (int samp = 0; samp < nrows; samp++)
                                                    {
                                                        float temp = MathHelper.ByteArrayToFloat(wBuff, index);
                                                        Record.WaveSamples[samp].WaterTemp = temp;
                                                        WavesRecordTemperature[samp] = temp;
                                                        index += 4;
                                                    }
                                                    break;
                                                }
                                            case "whs":
                                                {
                                                    // Wave Height source
                                                    // Add the samples if the list isn't created
                                                    AddSamples(nrows, 3);

                                                    // Update the array size if it does not match the number of samples
                                                    if (WavesRecordHeightSource == null || WavesRecordHeightSource.Length != nrows)
                                                    {
                                                        WavesRecordHeightSource = new float[nrows];
                                                        hSpectrum = new Complex[nrows / 2];
                                                    }


                                                    for (int samp = 0; samp < nrows; samp++)
                                                    {
                                                        float height = MathHelper.ByteArrayToFloat(wBuff, index);
                                                        Record.WaveSamples[samp].Height = height;
                                                        WavesRecordHeightSource[samp] = height;
                                                        index += 4;
                                                    }
                                                    break;
                                                }
                                            default:
                                                {
                                                    if (type == 10 | type == 11)
                                                        index += nrows * ncols * 4;
                                                    else
                                                        index += nrows * ncols * 8;
                                                    break;
                                                }
                                        }
                                    }
                                }
                                catch (Exception ex)
                                {
                                    //System.Windows.Forms.MessageBox.Show(String.Format("caughtD: {0}", ex.GetType().ToString()));
                                    string exceptionMessage = String.Format("caughtD: {0}\n{1}", ex.GetType().ToString(), ex);
                                    Debug.WriteLine(exceptionMessage);
                                    log.Error(exceptionMessage);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            //System.Windows.Forms.MessageBox.Show(String.Format("caughtE: {0}", ex.GetType().ToString()));
                            string exceptionMessage = String.Format("caughtE: {0}", ex.GetType().ToString());
                            Debug.WriteLine(exceptionMessage);
                            log.Error(exceptionMessage, ex);
                        }
                    }
                }

                return OK;
            }

            /// <summary>
            /// Add the list of samples if it doesn't exist
            /// or if the number of samples does not match.
            /// </summary>
            /// <param name="sampleCount">Number of samples to add.</param>
            /// <param name="numSelectedBins">Number of selected bins.</param>
            private void AddSamples(int sampleCount, int numSelectedBins)
            {
                // Create the list if it doesn't exist
                if(Record.WaveSamples == null)
                {
                    Record.WaveSamples = new List<WavesSample>();
                }

                // Get the current number of samples
                int total = Record.WaveSamples.Count;

                // Add the missing samples
                if( total < sampleCount)
                {
                    int add = sampleCount - total;
                    for(int x = 0; x < add; x++)
                    {
                        Record.WaveSamples.Add(new WavesSample(DataSet.Ensemble.DEFAULT_NUM_BEAMS_BEAM, numSelectedBins));
                    }
                }
            }

            /// <summary>
            /// Show the results of reading in the waves file.
            /// </summary>
            private void Wave_ShowFileContents()
            {
                WavesProc = WavesFileNameStr + "\r\n";
                WavesProc += "Record #:                " + Waves_ConvertSampleTime(0) + "\r\n";
                //WavesProc += AddSpaces(str2, 38) + "\r\n";
                //WavesProc += "interval         " + AddSpaces(((int)days).ToString() + " days " + ((int)hours).ToString("D2") + ":" + ((int)mins).ToString("D2") + ":" + ((int)secs).ToString("D2"), 21) + "\r\n";
                WavesProc += "Latitude:                " + Record.Latitude.ToString("0.0000000000000") + " degrees\r\n";
                WavesProc += "Longitude:               " + Record.Longitude.ToString("0.0000000000000") + " degrees\r\n";
                WavesProc += "Samples per record:      " + Record.WaveSamples.Count.ToString() + "\r\n";
                WavesProc += "Sample rate:             " + Record.TimeBetweenSamples.ToString("0.00") + " seconds\r\n";
                WavesProc += "Pressure sens height:    " + Record.PressureSensorHeight.ToString("0.000") + " meters\r\n";
                WavesProc += "Number of Selected Bins: " + Record.WaveCellDepth.Length.ToString() + "\r\n";
                for (int x = 0; x < Record.WaveCellDepth.Length; x++ )
                {
                    WavesProc += "     Wave bin height[" + x + "]: " + Record.WaveCellDepth[x].ToString("0.000") + " meters\r\n";
                }
            }

            /// <summary>
            /// Calculate the waves values.
            /// </summary>
            /// <param name="dt">Time between samples.</param>
            /// <param name="rows">Number of samples in the record.</param>
            /// <param name="column">Selected bin.</param>
            /// <param name="maxbands">Maximum number of wave bands.</param>
            /// <param name="UseBeam">Flag if can use beam data.</param>
            public void Waves_Calc(double dt, int rows, int column, int maxbands, bool UseBeam)
            {
                /*
                   NORTEK output:
             
                   Time Stamp (Month, day, year, hour, minute, second)
                   Significant Wave Height (Meters)
                   Peak Period (Seconds)
                   Mean Period (Seconds)
                   Direction at Peak (Degrees)
                   Spreading (Degrees)
                   Mean Direction (Degrees)
                   Unidirectivity Index (Dimensionless)
                   Error Code (See text on Error Messages)
             
                   The quality checks include the following with the corresponding error code. A value of 0 indicates no detected data errors.
                
                 * No Errors (0)
                    Processing of burst was successful and had no detectable errors.
               
                 * No Pressure (-1)
                    This would appear if the pressure is too low, and indicates that the instrument was most likely
                    out of the water or unreasonably close to the surface.
                    Low Pressure (-2)
                    This error suggests that there was no dynamic pressure detected in the time series, and means
                    that the waves are not measurable (i.e. a constant pressure). This would occur if the instrument
                    was deployed at a depth that is too deep to measure the waves or simply that there were no
                    measurable waves.
                
                 * Low Amplitude (-3)
                    This indicates that the amplitude of the Doppler signal was too low to measure the orbital
                    velocity.
                
                 * White Noise Test (-4)
                    This test is to determine if the estimated spectrum is purely white noise. Such a spectrum does
                    not contain information about the surface waves.
                
                 * Unreasonable Estimate (-5)
                    If it appears that there is an unreasonable estimate then the burst is flagged as bad.
                
                 * Never Processed (-6)
                    The wave processing cleans up some corrupt data (ie checksum errors) however if number of
                    bad points exceeds 10% of the burst data, then the data is not processed and it is tagged with
                    this error message. Usually this error message suggests that the wave burst was incomplete or
                    damaged
                */

                /*
                    Significant Wave Height. A practical definition that is often
                    used is the height of the highest 1/3 of the waves, H1/3. The height is computed
                    as follows: measure wave height for a few minutes, pick out say 120 wave crests
                    and record their heights. Pick the 40 largest waves and calculate the average
                    height of the 40 values. This is H1/3 for the wave record.
             
                    More recently, significant wave height is calculated from measured wave displacement.
                    If the sea contains a narrow range of wave frequencies, H1/3 is related to the 
                    standard deviation of sea-surface displacement (NAS, 1963: 22; Hoffman and Karst, 1975)
                
                    H1/3 = 4*(sd^2)^0.5
             
                    Where (sd^2)^0.5 is the standard deviation of surface displacement.
             
                    This relationship is much more useful, and it is now the accepted way to calculate wave
                    height from wave measurements. 
              
                   From Shore Protection Manual:
                   Hs = H1/3
                   mean of the top 10%, H10 = 1.27Hs
                   the mean of the top 1%, H1 = 1.67*Hs              
                */
                /* 
                 Waves processing steps:
                 1. Transform the Pressure and Velocity time series from time domain to frequency domain.
                 2. Calculate the Auto and Cross Spectra for the pressure and two velocities.
                 3. Apply transfer functions to the Auto Spectra to arrive at the Power Spectra for the free surface.
                 4. Apply quality control to the spectra (Determine a cutoff frequency and extrapolate).
                 5. Estimate wave statistics for height and period using the moments calculations.
                 6. Calculate the Fourier arguments to be used for the directional estimates.
                */
                /*
                From SONTEK:
            
                Accounting for Mean Current:
                In the presence of strong ambient currents U, the general wave dispersion relation needs to be
                modified to account for the Doppler shift in the frequency of the waves propagating through a moving
                medium:
            
                ω = (gk tanh(kH))^0.5 + kU cosα 
            
                where U is the current magnitude and α is the angle between the wave and the current direction. This
                correction becomes significant when the Doppler term is comparable with the general dispersion term. 
                Therefore the criterion for taking the mean current into account is
            
                U cos(α) ≥ 0.14(g/k * tanh(kH)^0.5
                */
                try
                {
                    if (rows > 0 && dt > 0)
                    {
                        int bands = NumWavesBands;
                        if (bands < 0)
                            bands = 0;
                        if (bands > maxbands)
                            bands = maxbands;

                        WavesLowFreqCutOff = WavesMinFreq;
                        WavesMinimumHeight = WavesMinHeight;
                        WavesMaximumScaleFactor = WavesMaxScaleFactor;

                        WavesBands[column] = RTI.Waves.WavesCalc.Init(rows, dt, bands,
                                                WavesFrequency, WavesBandStart, WavesBandEnd, WavesDenominator,
                                                WavesBandedBandwidth, WavesBandedFrequency);


                        // Average the velocity data for each selected bin
                        WavesRecordAverageEastVelocity[column] = RTI.Waves.WavesCalc.AverageSeries(WavesEastVelocitySeries[column].Length, WavesEastVelocitySeries[column]);
                        WavesRecordAverageNorthVelocity[column] = RTI.Waves.WavesCalc.AverageSeries(WavesNorthVelocitySeries[column].Length, WavesNorthVelocitySeries[column]);
                        WavesRecordAverageHeight[column] = RTI.Waves.WavesCalc.AverageSeries(WavesRecordHeightSource.Length, WavesRecordHeightSource);
                        WavesRecordAveragePressure[column] = RTI.Waves.WavesCalc.AverageSeries(WavesRecordPressure.Length, WavesRecordPressure);
                        WavesRecordAverageTemperature[column] = RTI.Waves.WavesCalc.AverageSeries(WavesRecordTemperature.Length, WavesRecordTemperature);

                        //The spectrum is in units of meter^2/Hz.
                        hSpectrum = RTI.Waves.WavesCalc.RealSpectrum(rows, WavesRecordHeightSource, 0);
                        pSpectrum = RTI.Waves.WavesCalc.RealSpectrum(rows, WavesRecordPressure, 0);
                        eastSpectrum[column] = RTI.Waves.WavesCalc.RealSpectrum(rows, WavesEastVelocitySeries[column], DataSet.Ensemble.BAD_VELOCITY);
                        northSpectrum[column] = RTI.Waves.WavesCalc.RealSpectrum(rows, WavesNorthVelocitySeries[column], DataSet.Ensemble.BAD_VELOCITY);
                        uvSpectrum[column] = RTI.Waves.WavesCalc.ComplexSpectrum(rows, WavesEastVelocitySeries[column], WavesNorthVelocitySeries[column], DataSet.Ensemble.BAD_VELOCITY);

                        uvWavesBandedSpectrum = RTI.Waves.WavesCalc.BandAverageSpectrum(rows, WavesBands[column], WavesBandEnd, WavesFrequency[0], WavesDenominator, uvSpectrum[column]);
                        uWavesBandedSpectrum[column] = RTI.Waves.WavesCalc.BandAverageSpectrum(rows, WavesBands[column], WavesBandEnd, WavesFrequency[0], WavesDenominator, eastSpectrum[column]);
                        vWavesBandedSpectrum[column] = RTI.Waves.WavesCalc.BandAverageSpectrum(rows, WavesBands[column], WavesBandEnd, WavesFrequency[0], WavesDenominator, northSpectrum[column]);
                        pWavesBandedSpectrum = RTI.Waves.WavesCalc.BandAverageSpectrum(rows, WavesBands[column], WavesBandEnd, WavesFrequency[0], WavesDenominator, pSpectrum);
                        hWavesBandedSpectrum = RTI.Waves.WavesCalc.BandAverageSpectrum(rows, WavesBands[column], WavesBandEnd, WavesFrequency[0], WavesDenominator, hSpectrum);

                        huWavesBandedCrossSpectrum = RTI.Waves.WavesCalc.BandAverageCrossSpectrum(rows, WavesBands[column], WavesBandEnd, WavesFrequency[0], WavesDenominator, hSpectrum, eastSpectrum[column]);
                        hvWavesBandedCrossSpectrum = RTI.Waves.WavesCalc.BandAverageCrossSpectrum(rows, WavesBands[column], WavesBandEnd, WavesFrequency[0], WavesDenominator, hSpectrum, northSpectrum[column]);
                        puWavesBandedCrossSpectrum = RTI.Waves.WavesCalc.BandAverageCrossSpectrum(rows, WavesBands[column], WavesBandEnd, WavesFrequency[0], WavesDenominator, pSpectrum, eastSpectrum[column]);
                        pvWavesBandedCrossSpectrum = RTI.Waves.WavesCalc.BandAverageCrossSpectrum(rows, WavesBands[column], WavesBandEnd, WavesFrequency[0], WavesDenominator, pSpectrum, northSpectrum[column]);
                        uvWavesBandedCrossSpectrum = RTI.Waves.WavesCalc.BandAverageCrossSpectrum(rows, WavesBands[column], WavesBandEnd, WavesFrequency[0], WavesDenominator, eastSpectrum[column], northSpectrum[column]);

                        WavesBands[column] = RTI.Waves.WavesCalc.CutoffLowFreqs(WavesBands[column], WavesLowFreqCutOff, WavesBandedFrequency,
                                           pWavesBandedSpectrum, hWavesBandedSpectrum, uWavesBandedSpectrum[column], vWavesBandedSpectrum[column], uvWavesBandedSpectrum,
                                           puWavesBandedCrossSpectrum, huWavesBandedCrossSpectrum, pvWavesBandedCrossSpectrum, hvWavesBandedCrossSpectrum, uvWavesBandedCrossSpectrum,
                                           WavesBandedBandwidth, WavesBandedFrequency, WavesBandStart, WavesBandEnd);

                        for (int i = 0; i < WavesBands[column]; i++)
                        {
                            WavesBandedPeriod[i] = 1.0 / WavesBandedFrequency[i];
                        }

                        if (UseBeam)
                        {
                            RTI.Waves.WavesCalc.DirectionalSpectrum(WavesBands[column],
                                                                    WavesBandedFrequency,
                                                                    WavesRecordAverageHeight[column],          // Average Height source
                                                                    Record.PressureSensorHeight,               // Pressure Sensor Height
                                                                    Record.WaveCellDepth[column],              // Wave Cell Depth for selected bin 
                                                                    WavesMinimumHeight,
                                                                    WavesMaximumScaleFactor,
                                                                    hWavesBandedSpectrum,
                                                                    uWavesBandedSpectrum[column],
                                                                    vWavesBandedSpectrum[column],
                                                                    WavesBandedFrequency,
                                                                    huWavesBandedCrossSpectrum,
                                                                    hvWavesBandedCrossSpectrum,
                                                                    uvWavesBandedCrossSpectrum,
                                                                    WavesSu,
                                                                    WavesSp,
                                                                    WavesDir,
                                                                    WavesSpread,
                                                                    WavesSpreadR1,
                                                                    UseBeam);
                        }
                        else
                        {
                            RTI.Waves.WavesCalc.DirectionalSpectrum(WavesBands[column],
                                                                    WavesBandedFrequency,
                                                                    WavesRecordAveragePressure[column],         // Averaged Pressure
                                                                    Record.PressureSensorHeight,                // Pressure Sensor Height
                                                                    Record.WaveCellDepth[column],               // Wave Cell Depth for selected bin 
                                                                    WavesMinimumHeight,
                                                                    WavesMaximumScaleFactor,
                                                                    pWavesBandedSpectrum,
                                                                    uWavesBandedSpectrum[column],
                                                                    vWavesBandedSpectrum[column],
                                                                    WavesBandedFrequency,
                                                                    puWavesBandedCrossSpectrum,
                                                                    pvWavesBandedCrossSpectrum,
                                                                    uvWavesBandedCrossSpectrum,
                                                                    WavesSu,
                                                                    WavesSp,
                                                                    WavesDir,
                                                                    WavesSpread,
                                                                    WavesSpreadR1,
                                                                    UseBeam);
                        }


                        RTI.Waves.WavesCalc.Parameters(column, WavesBands[column], WavesDir, WavesSpread,
                                                         WavesSp, WavesBandedBandwidth, WavesBandedFrequency,
                                                         WavesMeanPeriod, WavesMeanFreq, WavesHs,
                                                         WavesPeakFreq, WavesPeakPeriod, WavesPeakDir, WavesPeakSpread);

                        //for (int i = 0; i < WavesBands; i++)
                        //    if (WavesDir[i] > 180.0)
                        //        WavesDir[i] = WavesDir[i] - 360;
                    }
                }
                catch(Exception e)
                {
                    log.Error("Error with waves calculation.", e);
                }
            }

            ///// <summary>
            ///// Get the waves column.
            ///// </summary>
            ///// <returns></returns>
            //private int Waves_GetColumn()
            //{
            //    int column = WavesRecordNumber;
            //    if (column > WavesUcols - 1)
            //        column = WavesUcols - 1;
            //    if (column < 0)
            //        column = 0;
            //    if (column > WavesMaxColumns - 1)
            //        column = WavesMaxColumns - 1;

            //    WavesRecordNumber = column;

            //    return column;
            //}

            

            #endregion

            #region Date and Time

            /// <summary>
            /// Convert the sample time.
            /// </summary>
            /// <param name="column">Column.</param>
            /// <returns>Date and time.</returns>
            string Waves_ConvertSampleTime(int column)
            {
                int yy, mm, dd;
                double h, m, s;
                double ST = Record.FirstSampleTime;
                ST += 1721059.0;
                jdnl_to_ymd((long)ST, out yy, out mm, out dd, 0);
                ST -= (long)ST;
                ST *= (24.0 * 3600.0);//seconds
                ST = Math.Round(ST);
                h = (int)(ST / 3600.0);
                m = (int)((ST - 3600.00 * h) / 60.0);
                s = (int)(ST - 3600.0 * h - 60.0 * m);

                string str = yy.ToString("D4") + "/" + mm.ToString("D2") + "/" + dd.ToString("D2") + ", " + ((int)h).ToString("D2") + ":" + ((int)m).ToString("D2") + ":" + ((int)s).ToString("D2");
                return str;
            }

            /// <summary>
            /// Gregorian date to juliene date.
            /// </summary>
            /// <param name="y">Year.</param>
            /// <param name="m">Month.</param>
            /// <param name="d">Day.</param>
            /// <returns>Julian date.</returns>
            long gregorian_calendar_to_jd(int y, int m, int d)
            {
                y += 8000;
                if (m < 3)
                {
                    y--;
                    m += 12;
                }
                return (y * 365) + (y / 4) - (y / 100) + (y / 400) - 1200820 + (m * 153 + 3) / 5 - 92 + d - 1;
            }

            /// <summary>
            /// Convert year, month, day to julian time.
            /// </summary>
            /// <param name="y">Year.</param>
            /// <param name="m">Month.</param>
            /// <param name="d">Day</param>
            /// <param name="julian">Julian.</param>
            /// <returns>Time.</returns>
            private long ymd_to_jdnl(int y, int m, int d, int julian)
            {
                long jdn;
                if (y < 0) // adjust BC year
                    y++;
                if (julian > 0)
                    jdn = 367L * y - 7 * (y + 5001L + (m - 9) / 7) / 4 + 275 * m / 9 + d + 1729777L;
                else
                    jdn = (long)(d - 32076) + 1461L * (y + 4800L + (m - 14) / 12) / 4 + 367 * (m - 2 - (m - 14) / 12 * 12) / 12
                                - 3 * ((y + 4900L + (m - 14) / 12) / 100) / 4 + 1;/* correction by rdg */
                return jdn;
            }

            /// <summary>
            /// Convert the julian time year, month and day.
            /// </summary>
            /// <param name="jdn"></param>
            /// <param name="yy">Year.</param>
            /// <param name="mm">Month.</param>
            /// <param name="dd">Day.</param>
            /// <param name="julian"></param>
            private void jdnl_to_ymd(long jdn, out int yy, out int mm, out int dd, int julian)
            {
                long x, z, m, d, y;
                long daysPer400Years = 146097;
                long fudgedDaysPer4000Years = 1460970 + 31;

                x = jdn + 68569;
                if (julian > 0)
                {
                    x += 38;
                    daysPer400Years = 146100;
                    fudgedDaysPer4000Years = 1461000 + 1;
                }
                z = 4 * x / daysPer400Years;
                x = x - (daysPer400Years * z + 3) / 4;
                y = 4000 * (x + 1) / fudgedDaysPer4000Years;
                x = x - 1461 * y / 4 + 31;
                m = 80 * x / 2447;
                d = x - 2447 * m / 80;
                x = m / 11;
                m = m + 2 - 12 * x;
                y = 100 * (z - 49) + y + x;
                yy = (int)y;
                mm = (int)m;
                dd = (int)d;
                if (yy <= 0)// adjust BC years
                    (yy)--;
            }

            #endregion

            #region Override

            /// <summary>
            /// Override ToString with Record's toString().
            /// </summary>
            /// <returns></returns>
            public override string ToString()
            {
                return Record.ToString();
            }

            #endregion
        }
    }
}
