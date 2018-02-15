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
 * 10/15/2014      RC          0.0.1       Initial coding
 * 
 */

using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace RTI
{
    using log4net;
    using System.Windows;

    /// <summary>
    /// Application.
    /// </summary>
    public partial class App : Application
    {
        // Setup logger
        private static readonly ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        /// <summary>
        /// Initialize the app.
        /// </summary>
        public App()
        {
            InitializeComponent();
        }

        /// <summary>
        /// This method is called when an unhandled exception is called.  This will display a message box, then close the application.
        /// </summary>
        /// <param name="sender">Not used.</param>
        /// <param name="args">Get the exception message.</param>
        void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs args)
        {
            // Log the error
            log.Fatal("An unexpected application exception occurred", args.Exception);

            // Display a message box
            // If the user presses OK, the application will shutdown.
            // If the user presses Cancel, the application will continue to run.
            MessageBoxResult result = MessageBox.Show("An unexpected exception has occurred. Shutting down the application. Please check the log file for more details." + args.Exception, "Pulse Exception", MessageBoxButton.OKCancel);

            // Prevent default unhandled exception processing
            args.Handled = true;

            // If you press cancel, the program will continue to try and run.
            if (result == MessageBoxResult.OK)
            {
                System.Environment.Exit(0);
            }
        }
    }

}
