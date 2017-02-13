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
 * 11/11/2011      RC                     Initial coding
 * 11/16/2011      RC                     Added Error Log name.
 * 11/17/2011      RC                     Added Version number.
 * 12/02/2011      RC          1.07       Changed to version 1.07.
 * 12/05/2011      RC          1.08       Changed to version 1.08.  
 *                                         Added VERSION_ADDITIONAL to version number to denote beta or special builds.
 * 12/07/2011      RC          1.09       Changed to version 1.09.
 * 12/29/2011      RC          1.11       Changed to version 1.11.  Changed namespace to RTI.Pulse
 * 01/10/2012      RC          1.12       Changed to version 1.12.
 * 01/16/2012      RC          1.13       Changed to version 1.13.
 * 01/17/2012      RC          1.14       Changed to version 1.14.
 * 02/02/2012      RC          2.00       Changed to version 2.00.
 * 02/10/2012      RC          2.02       Changed to version 2.02.
 * 02/13/2012      RC          2.03       Changed to version 2.03.
 * 02/24/2012      RC          2.04       Changed to version 2.04.
 * 03/02/2012      RC          2.04       Added GetProjectDefaultFolderPath() to get a default folder path for projects.
 *                                         Added a default project name.
 * 03/05/2012      RC          2.05       Changed to version 2.05.
 * 03/06/2012      RC          2.06       Changed to version 2.06.
 * 03/20/2012      RC          2.07       Changed to version 2.07.
 * 04/02/2012      RC          2.08       Changed to version 2.08.
 * 04/12/2012      RC          2.09       Changed to version 2.09.
 * 04/23/2012      RC          2.10       Changed to version 2.10.
 * 04/30/2012      RC          2.11       Changed to version 2.11.
 * 06/18/2012      RC          2.12       Changed to version 2.12.
 * 07/24/2012      RC          2.13       Changed to version 2.13.
 * 08/28/2012      RC          2.14       Changed to version 2.14.
 * 08/29/2012      RC          2.15       Changed to version 2.15.
 * 09/04/2012      RC          2.15       Add an admin check.
 * 09/11/2012      RC          2.15       Made version number get retrieved from AssemblyInfo.cs.
 * 10/23/2012      RC          2.16       Changed to version 2.16.
 * 08/22/2014      RC          4.0.2      Added DEFAULT_RECORD_DIR_TANK and DEFAULT_RECORD_DIR.
 * 10/21/2014      RC          0.0.1      Added GetPulseOptionsPath() to get the JSON file.
 * 
 */
using System;
using System.IO;
using System.Windows.Media;

namespace RTI
{
    namespace Pulse_Waves
    {
        /// <summary>
        /// Common values used in the application.
        /// This inclues the version number and company name.
        /// </summary>
        public class Commons
        {
            #region Version Number

            /// <summary>
            /// Pulse version number.
            /// Version number is set in AssembleInfo.cs.
            /// </summary>
            public static string VERSION
            {
                get
                {
                    return System.Reflection.Assembly.GetEntryAssembly().GetName().Version.ToString();
                }
            }

            /// <summary>
            /// Used to denote Beta or Alpha builds.  Or any
            /// special branches of the application.
            /// </summary>
            public const string VERSION_ADDITIONAL = " Beta";

            #endregion

            #region Storage

            /// <summary>
            /// Directory under User folder to create the project database file 
            /// and errorlog.
            /// </summary>
            public const string PULSE_COMPANY_DIR = "RTI";

            /// <summary>
            /// Directory under User/APP_DIR to create the project database file
            /// and errorlog.
            /// </summary>
            public const string PULSE_APP_DIR = "Pulse_Waves";

            /// <summary>
            /// Error Log for application.
            /// </summary>
            public const string ERROR_LOG_FILENAME = "PulseErrorLog.log";

            /// <summary>
            /// Pulse DVL options JSON file.
            /// </summary>
            public const string PULSE_OPTIONS = "PulseOptions.json";

            /// <summary>
            /// Default project name.
            /// </summary>
            public const string DEFAULT_PROJECT_NAME = "Project";

            /// <summary>
            /// Default recording directory.
            /// </summary>
            public const string DEFAULT_RECORD_DIR = @"C:\RTI_Waves";

            /// <summary>
            /// Default recording directory for tank testing.
            /// </summary>
            public const string DEFAULT_RECORD_DIR_TANK = @"C:\RTI_Capture\tank";

            /// <summary>
            /// Enum to use to create directory for storage path.
            /// CommonApplicationData to share the projects between users on a single machine.
            /// Environment.GetFolderPath(PULSE_STORAGE_PATH);
            /// </summary>
            public const Environment.SpecialFolder PULSE_STORAGE_PATH = Environment.SpecialFolder.CommonApplicationData;


            /// <summary>
            /// Generate a directory to store application data.
            /// Verify the folder exist when creating the path.
            /// </summary>
            /// <returns>Path to store application data.</returns>
            public static string GetAppStorageDir()
            {
                // Get the current working directory
                string dir = Environment.GetFolderPath(PULSE_STORAGE_PATH);

                // Check if the Company folders exist
                if (!Directory.Exists(dir + @"\" + PULSE_COMPANY_DIR))
                {
                    Directory.CreateDirectory(dir + @"\" + PULSE_COMPANY_DIR);
                }

                // Check if Application folder exist
                if (!Directory.Exists(dir + @"\" + PULSE_COMPANY_DIR + @"\" + PULSE_APP_DIR))
                {
                    Directory.CreateDirectory(dir + @"\" + PULSE_COMPANY_DIR + @"\" + PULSE_APP_DIR);
                }

                // Return full path to application directory
                return dir + @"\" + PULSE_COMPANY_DIR + @"\" + PULSE_APP_DIR;
            }

            /// <summary>
            /// Create a default folder path for the projects.
            /// This will be a folder in MyDocuments in a folder
            /// named RTI.
            /// </summary>
            /// <returns>Default folder path for projects.</returns>
            public static string GetProjectDefaultFolderPath()
            {
                string myDoc = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                return string.Format(@"{0}\RTI", myDoc);
            }

            /// <summary>
            /// Return the Full path to the
            /// error log file.
            /// </summary>
            /// <returns></returns>
            public static string GetErrorLogPath()
            {
                return Commons.GetAppStorageDir() + @"\" + Commons.ERROR_LOG_FILENAME;
            }

            /// <summary>
            /// Get the file path to the Pulse Options JSON file.
            /// </summary>
            /// <returns>File path to the JSON options file.</returns>
            public static string GetPulseOptionsPath()
            {
                return Commons.GetAppStorageDir() + @"\" + Commons.PULSE_OPTIONS; ;
            }

            #endregion

            #region Images

            /// <summary>
            /// Image for record ON.
            /// </summary>
            public const string RECORD_IMAGE_ON = "../Images/record.png";

            /// <summary>
            /// Image for record OFF.
            /// </summary>
            public const string RECORD_IMAGE_OFF = "../Images/record_off.png";

            /// <summary>
            /// Image for record Blink.
            /// </summary>
            public const string RECORD_IMAGE_BLINK = "../Images/record_blink.png";

            /// <summary>
            /// Image for record Blink.
            /// </summary>
            public const string PLAYBACK_IMAGE_PLAYBACK = "../Images/playback.png";

            /// <summary>
            /// Image for record Blink.
            /// </summary>
            public const string PLAYBACK_IMAGE_PAUSE = "../Images/pause.png";

            /// <summary>
            /// Image for indicator at 0 position.  (Empty)
            /// </summary>
            public const string INDICATOR_0 = "../Images/indicator_0.png";

            /// <summary>
            /// Image for indicator at 1 position.
            /// </summary>
            public const string INDICATOR_1 = "../Images/indicator_1.png";

            /// <summary>
            /// Image for indicator at 2 position.
            /// </summary>
            public const string INDICATOR_2 = "../Images/indicator_2.png";

            /// <summary>
            /// Image for indicator at 3 position.
            /// </summary>
            public const string INDICATOR_3 = "../Images/indicator_3.png";


            /// <summary>
            /// Image for indicator at 4 position. (Max)
            /// </summary>
            public const string INDICATOR_4 = "../Images/indicator_4.png";
            #endregion

            #region UserControl Dimensions

            /// <summary>
            /// Width of the user control.
            /// The user control will contain the
            /// view and the button.
            /// Dimensions used in Text and Plot viewmodel
            /// buttons.
            /// </summary>
            public const int USERCONTROL_WIDTH = 1400;

            /// <summary>
            /// Height of the user control.
            /// The user control will contain the
            /// view and the button.
            /// Dimensions used in Text and Plot viewmodel
            /// buttons.
            /// </summary>
            public const int USERCONTROL_HEIGHT = 300;

            /// <summary>
            /// Button Width for each view.
            /// </summary>
            public const int BUTTON_WIDTH = 24;

            /// <summary>
            /// Button Height for each view.
            /// </summary>
            public const int BUTTON_HEIGHT = 32;

            /// <summary>
            ///  Width of the text to display
            ///  the Subsystem in the button.
            /// </summary>
            public const int TEXT_WIDTH = 15;

            /// <summary>
            /// Location for the arrow image.
            /// </summary>
            public const string BUTTON_LOC = @"../Images/right_arrow.png";

            /// <summary>
            /// Convert the given hex string to a solid color brush.
            /// If a bad string is given, it will return the color white.
            /// Found at: http://wrb.home.xs4all.nl/Articles_2011/Article_WPFBrush_01.htm
            /// </summary>
            /// <param name="sHexColor">Hex color string.  #AARRGGBB</param>
            /// <returns></returns>
            public static SolidColorBrush HexColorToBrush(string sHexColor)
            {
                if (sHexColor.Length != 9)
                {
                    return new SolidColorBrush(Colors.White);
                }
                byte A = Convert.ToByte(sHexColor.Substring(1, 2), 16);
                byte R = Convert.ToByte(sHexColor.Substring(3, 2), 16);
                byte G = Convert.ToByte(sHexColor.Substring(5, 2), 16);
                byte B = Convert.ToByte(sHexColor.Substring(7, 2), 16);
                SolidColorBrush sb =
                    new SolidColorBrush(Color.FromArgb(A, R, G, B));
                return sb;
            }

            #endregion

            #region Admin Functions


            /// <summary>
            /// Check if the file pulse_admin.txt exist in the path.
            /// If the file exist, the user knows how to activate
            /// the admin section.  The user will then get access to additional 
            /// feature.
            /// </summary>
            /// <returns>TRUE = Admin Access / FALSE = No Admin Access.</returns>
            public static bool IsAdmin()
            {
                // Check if the file exist in the path
                if (File.Exists("pulse_admin.txt"))
                {
                    return true;
                }

                return false;
            }

            #endregion
        }
    }

}