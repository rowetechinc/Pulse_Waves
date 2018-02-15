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
 * 10/30/2014      RC          3.0.2      Initial coding
 * 
 */


using MathNet.Numerics.IntegralTransforms;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace RTI
{
    namespace Waves
    {
        class WavesCalc
        {
            /// <summary>
            /// Combine the real and imagainary data and
            /// do an FFT on the data.  Set the FFT data
            /// to the dataout.
            /// The column is the selected bin.
            /// The row is the samples (ensembles).
            /// </summary>
            /// <param name="rows">Number of samples in a burst.</param>
            /// <param name="datainreal">Data input Real.</param>
            /// <param name="datainimag">Data input Imaginary.</param>
            /// <param name="badData">Data to flag as bad.</param>
            /// <returns>Complex number spectrum.</returns>
            public static Complex[] ComplexSpectrum(int rows, float[] datainreal, float[] datainimag, float badData)
            {
                Complex[] dataout = new Complex[rows / 2];

                Complex[] FFTdata = new Complex[rows];
                for (int i = 0; i < rows; i++)
                {
                    float real = 0.0f;
                    float imag = 0.0f;
                    if(datainreal[i] != badData)
                    {
                        real = datainreal[i];
                    }
                    if(datainimag[i] != badData)
                    {
                        imag = datainimag[i];
                    }

                    FFTdata[i] = new Complex(real, imag);
                }

                // Compute spectrum 
                Fourier.BluesteinForward(FFTdata, FourierOptions.Matlab);

                // Set value
                for (int i = 0; i < rows / 2; i++)
                {
                    dataout[i] = new Complex(FFTdata[i].Real, FFTdata[i].Imaginary);
                }

                return dataout;
            }

            /// <summary>
            /// Take the real data and
            /// do an FFT on the data.  Set the FFT data
            /// to the dataout.
            /// The column is the selected bin.
            /// The row is the samples (ensembles).
            /// </summary>
            /// <param name="rows">Number of samples in a burst.</param>
            /// <param name="datain">Data input Real.</param>
            /// <param name="badData">Data to flag as bad.</param>
            /// <returns>Real number spectrum.</returns>
            public static Complex[] RealSpectrum(int rows, float[] datain, float badData)
            {
                Complex[] dataout = new Complex[rows / 2];

                Complex[] FFTdata = new Complex[rows];
                for (int i = 0; i < rows; i++)
                {
                    if (datain[i] != badData)
                    {
                        FFTdata[i] = new Complex(datain[i], 0.0);
                    }
                    else
                    {
                        // Bad data so put in 0
                        FFTdata[i] = new Complex(0.0, 0.0);
                    }
                }

                // Compute spectrum 
                Fourier.BluesteinForward(FFTdata, FourierOptions.Matlab);

                // Set the value
                for (int i = 0; i < rows / 2; i++)
                {
                    dataout[i] = new Complex(FFTdata[i].Real, FFTdata[i].Imaginary);
                }

                return dataout;
            }

            /// <summary>
            /// Average the array and return the averaged value.
            /// The row is the samples (ensembles).
            /// </summary>
            /// <param name="rows">Number of samples in a burst.</param>
            /// <param name="data">Data to average.</param>
            /// <returns>Average of the data.</returns>
            public static float AverageSeries(int rows, float[] data)
            {
                float mp = 0;
                int goodCount = 0;
                for (int i = 0; i < rows; i++)
                {
                    if (data[i] != DataSet.Ensemble.BAD_VELOCITY)
                    {
                        mp += data[i];
                        goodCount++;
                    }
                }
                if (goodCount > 0)
                {
                    mp /= goodCount;
                    return mp;
                }
                
                return 0.0f;
            }

            /// <summary>
            /// Average the array and return the averaged value.
            /// The row is the samples (ensembles).
            /// </summary>
            /// <param name="rows">Number of samples in a burst.</param>
            /// <param name="data">Data to average.</param>
            /// <returns>Average of the data.</returns>
            public static double AverageArray(int rows, double[] data)
            {
                if(data == null)
                {
                    return 0;
                }

                double mp = 0;
                for (int i = 0; i < rows; i++)
                {
                    mp += data[i];
                }
                mp /= rows;
                return mp;
            }

            /// <summary>
            /// Band Average Spectrum.
            /// </summary>
            /// <param name="rows">Number of samples.</param>
            /// <param name="column">Selected bin.</param>
            /// <param name="Bands">Number of bands.</param>
            /// <param name="BandEnd"></param>
            /// <param name="Frequency">Frequency.</param>
            /// <param name="Denominator">Denominator</param>
            /// <param name="datain">Data In. Magnitude.</param>
            /// <returns>Band Average Spectrum calculated.</returns>
            public static double[] BandAverageSpectrum(int rows, int Bands, int[] BandEnd, double Frequency, double[] Denominator, Complex[] datain)
            {
                //scale the power spectrum
                double[] spectrum = new double[rows];
                spectrum[0] = 0;
                for (int i = 1; i < rows / 2; i++)
                    spectrum[i] = datain[i].Magnitude * datain[i].Magnitude * 2.0 / (rows * rows) / Frequency;// WavesFrequency[0];
                //sum the spectrum
                double[] Cs = new double[rows];
                Cs[0] = spectrum[0];
                for (int i = 1; i < rows / 2; i++)
                    Cs[i] = Cs[i - 1] + spectrum[i];

                //sum spectrum band
                double[] Snum = new double[rows];
                Snum[0] = Cs[(int)BandEnd[0]];
                for (int i = 1; i < Bands; i++)
                    Snum[i] = Cs[(int)BandEnd[i]] - Cs[(int)BandEnd[i - 1]];
                //average spectrum band
                double[] dataout = new double[Bands];
                for (int i = 0; i < Bands; i++)
                    dataout[i] = Snum[i] / Denominator[i];

                return dataout;
            }

            /// <summary>
            /// Band Average Cross Spectrum.
            /// </summary>
            /// <param name="rows">Number of samples.</param>
            /// <param name="Bands">Number of bands.</param>
            /// <param name="BandEnd"></param>
            /// <param name="Frequency">Frequency.</param>
            /// <param name="Denominator">Denominator.</param>
            /// <param name="datainA">Data Input A. Real.</param>
            /// <param name="datainB">Data Input B. Imaginary.</param>
            /// <returns>Band Average Cross Spectrum results.</returns>
            public static double[] BandAverageCrossSpectrum(int rows, int Bands, int[] BandEnd, double Frequency, double[] Denominator, Complex[] datainA, Complex[] datainB)
            {
                //scale the power spectrum
                Complex[] CrossSpectrum = new Complex[rows];
                double[] CSpec = new double[rows];
                CSpec[0] = 0;
                for (int i = 1; i < rows / 2; i++)
                {
                    //Complex Conjegate
                    //CrossSpectrum[i].Re = datainB[i].Re;
                    //CrossSpectrum[i].Im = -datainB[i].Im;
                    CrossSpectrum[i] = new Complex(datainB[i].Real, -datainB[i].Imaginary);

                    CrossSpectrum[i] *= datainA[i];
                    //scale the real part
                    CSpec[i] = CrossSpectrum[i].Real * 2.0 / (rows * rows) / Frequency;// WavesFrequency[0];
                }

                //sum the spectrum
                double[] Cs = new double[rows];
                Cs[0] = CSpec[0];
                for (int i = 1; i < rows / 2; i++)
                    Cs[i] = Cs[i - 1] + CSpec[i];

                //sum spectrum band
                double[] Snum = new double[rows];
                Snum[0] = Cs[(int)BandEnd[0]];
                for (int i = 1; i < Bands; i++)
                    Snum[i] = Cs[(int)BandEnd[i]] - Cs[(int)BandEnd[i - 1]];
                //average spectrum band
                double[] dataout = new double[Bands];
                for (int i = 0; i < Bands; i++)
                    dataout[i] = Snum[i] / Denominator[i];

                return dataout;
            }

            /// <summary>
            /// Find K.
            /// </summary>
            /// <param name="WavesBands"></param>
            /// <param name="F"></param>
            /// <param name="H"></param>
            /// <param name="k"></param>
            public static void Find_k(int WavesBands, double[] F, double H, double[] k)
            {
                //Dispersion Relation
                //Wave frequency ω is related to wave number k by the dispersion relation
                //(2*pi*f)^2 = (gk) * tanh(kd)

                /*
                From Matlab
                  For infinite depth, kfromw inverts the cubic polynomial. 
                  For finite depth, a zero-finding method is used, starting from the infinite depth solution.
              
                  omega(k) = sqrt ( tanh(k*h0) * (g*k + gamma*k^3/rho))
             
                  where: 
                        omega is the pulsation (in rad/s), k the wavenumber (in 1/m), 
                        h0 the depth (in m), g the gravity (in m^2/s), 
                        gamma the surface tension (in N/m) and 
                        rho the fluid density (in kg/m^3).
             
                  defaults:       
                        g: 9.8100
                        gamma: 0.0720
                        h0: Inf
                        rho: 1000
                */

                //zero finding method            

                double g = 9.80171;
                double K = 0.0;
                double O;
                double P;

                for (int i = 0; i < WavesBands; i++)
                {
                    O = Math.Pow(2 * Math.PI * F[i], 2);
                    K = 0.0;
                    double kk = 0.0;
                    double min = 1000.0;
                    int j;
                    bool gotit = false;
                    for (j = 0; j < 10000; j++)
                    {
                        P = Math.Abs(O - g * K * Math.Tanh(K * H));
                        if (min > P)
                        {
                            gotit = true;
                            min = P;
                            kk = K;
                        }
                        else
                        {
                            if (gotit)
                                break;
                        }
                        K += 0.01;
                    }
                    gotit = false;
                    K = kk - 0.01;
                    min = 1000.0;
                    for (j = 0; j < 10000; j++)
                    {
                        P = Math.Abs(O - g * K * Math.Tanh(K * H));
                        if (min > P)
                        {
                            gotit = true;
                            min = P;
                            kk = K;
                        }
                        else
                        {
                            if (gotit)
                                break;
                        }
                        K += 0.00001;
                    }

                    k[i] = kk;
                }
            }

            /// <summary>
            /// Calculate the directional spectrum.
            /// </summary>
            /// <param name="Bands">Wave Bands</param>
            /// <param name="F">Frequency.</param>
            /// <param name="ap">Pressure Average.</param>
            /// <param name="hp">Pressure Sensor Height</param>
            /// <param name="hv">Waves Cell Depth.</param>
            /// <param name="minWaveHeight">Min Waves Height.</param>
            /// <param name="maxScale">Max Scale Factor.</param>
            /// <param name="HSpectrum">Height spectrum from pressure or beam.</param>
            /// <param name="uSpectrum">East Velocity spectrum.</param>
            /// <param name="vSpectrum">North velocity spectrum.</param>
            /// <param name="Frequency">Frequency.</param>
            /// <param name="HuCrossSpectrum">East Velocity Banded Cross Spectrum.</param>
            /// <param name="HvCrossSpectrum">North Velocity Banded Cross Spectrum.</param>
            /// <param name="uvCrossSpectrum">East/North Velocity Banded Cross Spectrum.</param>
            /// <param name="Su"></param>
            /// <param name="Sp"></param>
            /// <param name="Dir"></param>
            /// <param name="Spread"></param>
            /// <param name="SpreadR1"></param>
            /// <param name="UseBeam"></param>
            public static void DirectionalSpectrum(int Bands, double[] F, double ap, double hp, double hv, double minWaveHeight, double maxScale,
                                             double[] HSpectrum, double[] uSpectrum, double[] vSpectrum, double[] Frequency,
                                             double[] HuCrossSpectrum, double[] HvCrossSpectrum, double[] uvCrossSpectrum,
                                             double[] Su, double[] Sp, double[] Dir, double[] Spread, double[] SpreadR1, bool UseBeam)
            {
                int i;
                double[] K = new double[Bands];

                Find_k(Bands, F, ap + hp, K);

                double sinhkh;
                double coshkh;
                double coshkhz;
                double coshkhZ;

                for (i = 0; i < Bands; i++)
                {
                    sinhkh = Math.Sinh(K[i] * (hp + ap));
                    coshkh = Math.Cosh(K[i] * (hp + ap));
                    coshkhz = Math.Cosh(K[i] * hp);
                    coshkhZ = Math.Cosh(K[i] * (hp + hv));

                    if (!UseBeam && coshkh * coshkh > maxScale)
                    {
                        Su[i] = Double.NaN;
                        Sp[i] = Double.NaN;
                        Dir[i] = Double.NaN;
                        Spread[i] = Double.NaN;
                        SpreadR1[i] = Double.NaN;
                    }
                    else
                    {
                        //calculate free surface wave height from subsurface wave height
                        Su[i] = (uSpectrum[i] + vSpectrum[i])
                                     * Math.Pow(sinhkh / (2.0 * Math.PI * Frequency[i]) / coshkhZ, 2.0);
                        if (!UseBeam)
                            Sp[i] = HSpectrum[i] * Math.Pow(coshkh / coshkhz, 2.0);
                        else
                            Sp[i] = HSpectrum[i];

                        if (Sp[i] < minWaveHeight)
                        {
                            Dir[i] = Double.NaN;
                            Spread[i] = Double.NaN;
                            SpreadR1[i] = Double.NaN;
                        }
                        else
                        {
                            Dir[i] = 57.296 * Math.Atan2(HuCrossSpectrum[i], HvCrossSpectrum[i]);
                            Dir[i] = (Dir[i] + 180.0) % 360.0;
                            //if (Dir[i] > 180.0)
                            //    Dir[i] = Dir[i] - 360;

                            double R2 = Math.Sqrt(Math.Pow(uSpectrum[i] - vSpectrum[i], 2.0) + 4.0 * Math.Pow(uvCrossSpectrum[i], 2.0)) / (uSpectrum[i] + vSpectrum[i]);

                            double denom = Math.Sqrt(HSpectrum[i] * (uSpectrum[i] + vSpectrum[i]));
                            double a1 = HuCrossSpectrum[i] / denom;
                            double b1 = HvCrossSpectrum[i] / denom;
                            double r1 = Math.Sqrt(Math.Pow(a1, 2.0) + Math.Pow(b1, 2.0));

                            Spread[i] = 57.296 * Math.Sqrt((1.0 - R2) / 2.0);
                            SpreadR1[i] = 57.296 * Math.Sqrt(2.0 * (1.0 - r1));

                            //ω = (gk tanh(kH))^0.5 + kU cosα 
                        }
                    }
                }
            }

            /// <summary>
            /// Cut off the low frequencies.
            /// Find all the frequencies that meet
            /// the cut off threshold.  Store the spectrum
            /// values only for the good wave bands found.
            /// </summary>
            /// <param name="bands">Number of wave bands.</param>
            /// <param name="lowfreqcutoffHz">Frequency threshold.</param>
            /// <param name="FrequencyIn">Frequency values to check against threshold.</param>
            /// <param name="pSpectrum">Good pressure spectrum values.</param>
            /// <param name="hSpectrum">Good Height spectrum values.</param>
            /// <param name="uSpectrum">Good East Velocity spectrum values.</param>
            /// <param name="vSpectrum">Good North Velocity spectrum values.</param>
            /// <param name="uvSpectrum">Good East/North Velocity spectrum values.</param>
            /// <param name="puCrossSpectrum">Good spectrum values.</param>
            /// <param name="huCrossSpectrum">Good spectrum values.</param>
            /// <param name="pvCrossSpectrum">Good spectrum values.</param>
            /// <param name="hvCrossSpectrum">Good spectrum values.</param>
            /// <param name="uvCrossSpectrum">Good spectrum values.</param>
            /// <param name="Bandwidth">Good bandwidth values.</param>
            /// <param name="Frequency">Good frquency values.</param>
            /// <param name="Start">Good start values.</param>
            /// <param name="End">Good end values.</param>
            /// <returns>Number of good bands found.</returns>
            public static int CutoffLowFreqs(int bands, double lowfreqcutoffHz, double[] FrequencyIn,
                double[] pSpectrum, double[] hSpectrum, double[] uSpectrum, double[] vSpectrum, double[] uvSpectrum,
                double[] puCrossSpectrum, double[] huCrossSpectrum, double[] pvCrossSpectrum, double[] hvCrossSpectrum, double[] uvCrossSpectrum,
                double[] Bandwidth, double[] Frequency, int[] Start, int[] End)
            {
                int i;
                // Low frequency cutoff
                // Find all the good wave bands
                int[] index = new int[2048];
                int newbands = 0;
                for (i = 0; i < bands; i++)
                {
                    if (FrequencyIn[i] > lowfreqcutoffHz)
                    {
                        index[newbands] = i;
                        newbands++;
                    }
                }

                // Set the spectrums based off the good wave bands found
                for (i = 0; i < newbands; i++)
                {
                    uSpectrum[i] = uSpectrum[index[i]];
                    vSpectrum[i] = vSpectrum[index[i]];
                    pSpectrum[i] = pSpectrum[index[i]];
                    hSpectrum[i] = hSpectrum[index[i]];
                    uvSpectrum[i] = uvSpectrum[index[i]];

                    puCrossSpectrum[i] = puCrossSpectrum[index[i]];
                    huCrossSpectrum[i] = huCrossSpectrum[index[i]];
                    pvCrossSpectrum[i] = pvCrossSpectrum[index[i]];
                    hvCrossSpectrum[i] = hvCrossSpectrum[index[i]];
                    uvCrossSpectrum[i] = uvCrossSpectrum[index[i]];

                    Bandwidth[i] = Bandwidth[index[i]];
                    Frequency[i] = Frequency[index[i]];

                    End[i] = End[index[i]];
                    Start[i] = Start[index[i]];
                    //DegreesOfFreedom[i] = 2 * (WavesBandEnd[i] - WavesBandStart[i] + 1);
                }


                return newbands;
            }

            /// <summary>
            /// Initialize some values.
            /// </summary>
            /// <param name="rows">Number of samples in a burst.</param>
            /// <param name="dt">Time between samples.</param>
            /// <param name="desiredbands"></param>
            /// <param name="FrequencyIn"></param>
            /// <param name="Start"></param>
            /// <param name="End"></param>
            /// <param name="Denominator"></param>
            /// <param name="Bandwidth"></param>
            /// <param name="Frequency">Frquency.</param>
            /// <returns>Number of bands.</returns>
            public static int Init(int rows, double dt, double desiredbands,
                double[] FrequencyIn, int[] Start, int[] End, double[] Denominator,
                double[] Bandwidth, double[] Frequency)
            {
                double Dt = rows * dt;

                double[] logfrequency = new double[rows];//log of frequency array
                for (int i = 0; i < rows / 2; i++)
                {
                    FrequencyIn[i] = (i + 1.0) / Dt;
                    logfrequency[i] = Math.Log(FrequencyIn[i]);
                }

                //create the band indices and determine final number of bands
                double logfrequencyspan = 1.000000001 * (logfrequency[rows / 2 - 1] - logfrequency[0]) / desiredbands;
                double[] step = new double[rows];
                for (int i = 0; i < rows / 2; i++)
                    step[i] = 1.0 + Math.Floor((logfrequency[i] - logfrequency[0]) / logfrequencyspan);

                int Bands = 0;
                for (int i = 0; i < rows / 2; i++)
                {
                    double a = step[i + 1] - step[i];
                    if (a > 0.0)
                    {
                        End[Bands] = i;
                        Bands++;
                    }
                    else
                    {
                        End[i] = 0;
                    }
                }
                End[Bands] = rows / 2 - 1;
                Bands++;
                Start[0] = 0;
                for (int i = 0; i < Bands; i++)
                    Start[i + 1] = End[i] + 1;//last index of band


                Denominator[0] = 1;
                Bandwidth[0] = (End[1] - End[0]) * (FrequencyIn[10] - FrequencyIn[9]);
                for (int i = 1; i < Bands; i++)
                {
                    Denominator[i] = End[i] - End[i - 1];
                    Bandwidth[i] = Denominator[i] * (FrequencyIn[10] - FrequencyIn[9]);
                }
                //sum the frequency
                double[] Cf = new double[rows];
                Cf[0] = FrequencyIn[0];
                for (int i = 1; i < rows / 2; i++)
                    Cf[i] = Cf[i - 1] + FrequencyIn[i];

                //sum frequency band
                double[] Fnum = new double[rows];
                Fnum[0] = Cf[(int)End[0]];
                for (int i = 1; i < Bands; i++)
                    Fnum[i] = Cf[End[i]] - Cf[End[i - 1]];
                
                //average frequency band
                for (int i = 0; i < Bands; i++)
                    Frequency[i] = Fnum[i] / Denominator[i];

                return Bands;
            }

            /// <summary>
            /// Calculate the parameters.
            /// </summary>
            /// <param name="column">Selected bin.</param>
            /// <param name="bands">Number of bands.</param>
            /// <param name="Dir">Direction.</param>
            /// <param name="Spread">Speard.</param>
            /// <param name="Sp"></param>
            /// <param name="Bandwidth">Bandwidth.</param>
            /// <param name="Frequency">Frequency.</param>
            /// <param name="MeanPeriod">Mean Period.</param>
            /// <param name="MeanFreq">Mean Frequency.</param>
            /// <param name="Hs"></param>
            /// <param name="PeakFreq">Peak Frequency.</param>
            /// <param name="PeakPeriod">Peak Period.</param>
            /// <param name="PeakDir">Peak Direction.</param>
            /// <param name="PeakSpread">Peak Spread.</param>
            public static void Parameters(int column, int bands, double[] Dir, double[] Spread,
                                     double[] Sp, double[] Bandwidth, double[] Frequency,
                double[] MeanPeriod, double[] MeanFreq, double[] Hs, double[] PeakFreq, double[] PeakPeriod,
                double[] PeakDir, double[] PeakSpread)
            {
                double m2 = 0.0;
                double m0 = 0.0;
                double maxSp = -999999.0;
                int nP = 0;
                for (int i = 0; i < bands; i++)
                {
                    if (!Double.IsNaN(Sp[i]))
                    {
                        m0 += Sp[i] * Bandwidth[i];//first moment of the power spectrum
                        m2 += Math.Pow(Frequency[i], 2.0) * Sp[i] * Bandwidth[i];//second moment of the power spectrum

                        if (maxSp < Sp[i])
                        {
                            maxSp = Sp[i];//value and location of peak 
                            nP = i;
                        }
                    }
                }
                if (m2 > 0)
                {
                    MeanPeriod[column] = Math.Sqrt(m0 / m2);
                    MeanFreq[column] = 1.0 / MeanPeriod[column];
                }
                else
                {
                    MeanPeriod[column] = 0.0;
                    MeanFreq[column] = 0.0;
                }

                Hs[column] = 3.8 * Math.Sqrt(m0);
                double A = double.NaN, B = double.NaN, C = double.NaN;
                if (nP > 0)
                {
                    A = Sp[nP - 1];
                    B = Sp[nP];
                    C = Sp[nP + 1];
                }
                //extrapolate to the peak frequency
                if (!(double.IsNaN(A) && double.IsNaN(B) && double.IsNaN(C)))
                {
                    PeakFreq[column] = Math.Log(Frequency[nP + 1]) - Math.Log(Frequency[nP - 1]);
                    PeakFreq[column] *= (-(C - A)) / (2.0 * (A - 2.0 * B + C));
                    PeakFreq[column] /= 2.0;
                    PeakFreq[column] = Math.Exp(Math.Log(Frequency[nP]) + PeakFreq[column]);
                    PeakPeriod[column] = 1.0 / PeakFreq[column];
                }
                else
                {
                    PeakFreq[column] = 0.0;
                    PeakPeriod[column] = 0.0;
                }

                PeakDir[column] = Dir[nP];
                PeakSpread[column] = Spread[nP];
            }

        }
    }
}