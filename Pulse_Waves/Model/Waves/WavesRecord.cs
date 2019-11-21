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
 * 11/20/2019      RC          1.7.0      Added VertPressure and VertRangeTracking.
 * 
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RTI
{
    namespace Waves
    {
        /// <summary>
        /// All the values accumulated for a waves sample.
        /// </summary>
        public class WavesSample
        {
            /// <summary>
            /// Set a flag if this data is a vertical sample or
            /// a 4 beam sample.  TRUE = vertical sample.
            /// </summary>
            public bool IsVerticalSample { get; set; }

            /// <summary>
            /// WZBM
            /// Vertical Beam Height in meters.
            /// [beam]
            /// </summary>
            public float[] VertBeam { get; set; }

            /// <summary>
            /// WZHS
            /// Vertical beam Height in meters.
            /// This is the average of all the range tracking values.
            /// </summary>
            public float VertBeamHeight { get; set; }

            /// <summary>
            /// WPS
            /// Pressure in meters.
            /// </summary>
            public float Pressure { get; set; }

            /// <summary>
            /// WZP 
            /// Vertical Beam Pressure in meters.
            /// </summary>
            public float VertPressure { get; set; }

            /// <summary>
            /// WTS
            /// Water Temperature degree farenheit.
            /// </summary>
            public float WaterTemp { get; set; }

            /// <summary>
            /// WHG
            /// Heading degrees.
            /// </summary>
            public float Heading { get; set; }

            /// <summary>
            /// WPH
            /// Pitch degrees.
            /// </summary>
            public float Pitch { get; set; }

            /// <summary>
            /// WRL
            /// Roll degrees.
            /// </summary>
            public float Roll { get; set; }

            /// <summary>
            /// WBM
            /// Beam Velocity in m/s.
            /// [bin,beam]
            /// </summary>
            public float[,] BeamVel { get; set; }

            /// <summary>
            /// Wts
            /// Time stamp in seconds.
            /// </summary>
            public double TimeStampSeconds { get; set; }

            ///// <summary>
            ///// WFT
            ///// Sample Time.
            ///// </summary>
            //public double SampleTime { get; set; }

            /// <summary>
            /// WSHS
            /// Range Tracking in meters.
            /// [beam]
            /// </summary>
            public float[] RangeTracking { get; set; }

            /// <summary>
            /// WZR 
            /// Vertical Beam Range Tracking in meters.
            /// This is a single vertical beam value for the range compared to the 4 beam value. 
            /// </summary>
            public float VertRangeTracking { get; set; }

            /// <summary>
            /// Height source which is derived from the
            /// selected height source in meters.
            /// </summary>
            public float Height { get; set; }

            /// <summary>
            /// WaveSampleNumber.
            /// Number of samples.
            /// </summary>
            public int NumSamples { get; set; }

            /// <summary>
            /// Ensemble number.
            /// Ensemble number from the sample..
            /// </summary>
            public int EnsembleNumber { get; set; }

            /// <summary>
            /// List of selected bins.
            /// </summary>
            //public List<int> SelectedBins { get; set; }

            /// <summary>
            /// WUS
            /// East velocity data for given selected bins in m/s.
            /// [bins]
            /// </summary>
            public float[] EastTransformData { get; set; }

            /// <summary>
            /// WVS
            /// North velocity data for given selected bins in m/s.
            /// [bins]
            /// </summary>
            public float[] NorthTransformData { get; set; }

            /// <summary>
            /// WZS
            /// Vertical velocity data for given selected bins in m/s.
            /// [bins]
            /// </summary>
            public float[] VerticalTransformData { get; set; }

            #region Cleanup Data

            ///// <summary>
            ///// Vertical Velocity Beam 0 in meters per second.
            ///// [bins]
            ///// </summary>
            //public float[] VertVelB0 { get; set; }

            ///// <summary>
            ///// Vertical Velocity Beam 1 in meters per second.
            ///// [bins]
            ///// </summary>
            //public float[] VertVelB1 { get; set; }
            ///// <summary>
            ///// Vertical Velocity Beam 2 in meters per second.
            ///// [bins]
            ///// </summary>
            //public float[] VertVelB2 { get; set; }
            ///// <summary>
            ///// Vertical Velocity Beam 3 in meters per second.
            ///// [bins]
            ///// </summary>
            //public float[] VertVelB3 { get; set; }

            ///// <summary>
            ///// Velocity Beam 0 in meters per second.
            ///// [bins]
            ///// </summary>
            //public float[] VelB0 { get; set; }

            ///// <summary>
            ///// Velocity Beam 1 in meters per second.
            ///// [bins]
            ///// </summary>
            //public float[] VelB1 { get; set; }

            ///// <summary>
            ///// Velocity Beam 2 in meters per second.
            ///// [bins]
            ///// </summary>
            //public float[] VelB2 { get; set; }

            ///// <summary>
            ///// Velocity Beam 3 in meters per second.
            ///// [bins]
            ///// </summary>
            //public float[] VelB3 { get; set; }



            #endregion

            /// <summary>
            /// Initialize the values.
            /// The user can select multple bins.
            /// </summary>
            /// <param name="beams">Number of beams.</param>
            /// <param name="SelectedBins">Selected bins.</param>
            public WavesSample(int beams, List<int> SelectedBins)
            {
                Init(beams, SelectedBins.Count);
            }

            /// <summary>
            /// Initialize the values.
            /// The user can select multple bins.
            /// </summary>
            /// <param name="beams">Number of beams.</param>
            /// <param name="SelectedBinCount">Number of selected bin.</param>
            public WavesSample(int beams, int SelectedBinCount)
            {
                Init(beams, SelectedBinCount);
            }

            /// <summary>
            /// Initialize the values.
            /// </summary>
            /// <param name="beams">Number of beams.</param>
            /// <param name="bins">Number of bins.</param>
            public void Init(int beams, int bins)
            {
                if (beams == 1)
                {
                    IsVerticalSample = true;
                }
                else
                {
                    IsVerticalSample = false;
                }

                VertBeam = new float[bins];                    // beams
                VertBeamHeight = 0.0f;
                Pressure = 0.0f;
                VertPressure = 0.0f;
                WaterTemp = 0.0f;
                Heading = 0.0f;
                Pitch = 0.0f;
                Roll = 0.0f;
                BeamVel = new float[bins, beams];               // [bins,beam]
                TimeStampSeconds = 0.0;
                //SampleTime = 0.0;
                RangeTracking = new float[beams];               // [beams]
                VertRangeTracking = 0.0f;
                Height = 0;
                NumSamples = 0;
                EnsembleNumber = 0;
                //VertVelB0 = new float[bins];                    // [bins]
                //VertVelB1 = new float[bins];                    // [bins]
                //VertVelB2 = new float[bins];                    // [bins]
                //VertVelB3 = new float[bins];                    // [bins]
                //VelB0 = new float[bins];                        // [bins]
                //VelB1 = new float[bins];                        // [bins]
                //VelB2 = new float[bins];                        // [bins]
                //VelB3 = new float[bins];                        // [bins]
                EastTransformData = new float[bins];            // [bins]
                NorthTransformData = new float[bins];           // [bins]
                VerticalTransformData = new float[bins];        // [bins]
            }
        }

        /// <summary>
        /// A wave record.  This will will contain all the
        /// data for a burst.  Multiple ensembles will be combined
        /// to contain all the information from the burst.
        /// </summary>
        public class WavesRecord
        {

            /// <summary>
            /// List of all the wave samples.
            /// </summary>
            public List<WavesSample> WaveSamples { get; set; }

            /// <summary>
            /// Set flag if the first sample has been processed to
            /// get the environmental and time data.
            /// </summary>
            public bool IsFirstSampleSet { get; set; }

            /// <summary>
            /// Height source.
            /// </summary>
            public RTI.RecoverDataOptions.HeightSource HeightSrc { get; set; }

            /// <summary>
            /// First sample time in hours of a day.
            /// FirstSampleTime * 24 = Hours
            /// </summary>
            public double FirstSampleTime { get; set; }

            /// <summary>
            /// Date string.
            /// </summary>
            public string DateStr { get; set; }

            /// <summary>
            /// Serial number and wave record string.
            /// </summary>
            public string SnStr { get; set; }

            /// <summary>
            /// Latitude.
            /// Lat and Long should be in degrees (convert minutes and seconds).
            /// Use negative degrees for west and south.
            /// </summary>
            public double Latitude { get; set; }

            /// <summary>
            /// Longitude.
            /// Lat and Long should be in degrees (convert minutes and seconds).
            /// Use negative degrees for west and south.
            /// </summary>
            public double Longitude { get; set; }

            /// <summary>
            /// Wave cell depth in meters for each selected bin.
            /// [bins]
            /// </summary>
            public float[] WaveCellDepth { get; set; }

            /// <summary>
            /// Pressure Sensor height.
            /// </summary>
            public float PressureSensorHeight { get; set; }

            /// <summary>
            /// List of all the bins to get data from.
            /// </summary>
            public List<int> SelectedBins { get; set; }

            /// <summary>
            /// Info text.  Contains the serial number, date and time and record 
            /// number.
            /// </summary>
            public string InfoTxt { get; set; }

            /// <summary>
            /// Time between samples.
            /// </summary>
            public float TimeBetweenSamples { get; set; }

            /// <summary>
            /// File path for the wave record.
            /// </summary>
            public string FilePath { get; set; }

            /// <summary>
            /// Waves Record number.
            /// </summary>
            public int RecordNumber { get; set; }

            /// <summary>
            /// Initialize the wave record.
            /// </summary>
            public WavesRecord()
            {
                Init();
            }

            /// <summary>
            /// Initialize the values.
            /// </summary>
            public void Init()
            {
                WaveSamples = new List<WavesSample>();
                HeightSrc = RTI.RecoverDataOptions.HeightSource.Vertical;
                SnStr = "";
                FirstSampleTime = 0.0;
                //Latitude = 32.0 + 51.901 / 60.0;
                //Longitude = -(117.0 + 15.571 / 60.0);
                Latitude = 0.0;
                Longitude = 0.0;
                PressureSensorHeight = 0.65f;
                SelectedBins = new List<int>();
                WaveCellDepth = new float[3];               // By default there are 3 selected bins
                DateStr = "";
                InfoTxt = "";
                TimeBetweenSamples = 0.0f;
                FilePath = "";
                RecordNumber = 0;
                IsFirstSampleSet = false;
            }

            /// <summary>
            /// Set the Info Text as the string for this object.
            /// </summary>
            /// <returns></returns>
            public override string ToString()
            {
                return InfoTxt;
            }

            /// <summary>
            /// This will validate the data.
            /// It will check if any of the ensembles are missing.
            /// If any ensembles are missing, it will add it in and
            /// put some data in, based off the previous and future ensemble.
            /// 
            /// </summary>
            public void Validate()
            {
                var samples = new List<WavesSample>();
                int prevEnsNum = 0;

                foreach(var samp in WaveSamples)
                {
                    // Initialize if never initialized
                    if(prevEnsNum == 0)
                    {
                        prevEnsNum = samp.EnsembleNumber - 1;
                    }

                    // Check if the next record is sequential
                    if(samp.EnsembleNumber != prevEnsNum+2)     // Add 2 to account for the vertical beam.
                    {
                        // Add in a sample to replace missing sample
                        Debug.WriteLine("Missing Sample: " + samp.EnsembleNumber + " prev: " + prevEnsNum);

                        // Set the prev sample number
                        prevEnsNum = samp.EnsembleNumber;
                    }
                    else
                    {
                        // Keep the sample
                        samples.Add(samp);

                        // Set the prev sample number
                        prevEnsNum = samp.EnsembleNumber;
                    }
                }

                // Replace the hold samples with the new one
                this.WaveSamples = samples;


            }
        }
    }
}
