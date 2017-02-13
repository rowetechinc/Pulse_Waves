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
 * Date            Initials    Vertion    Comments
 * -----------------------------------------------------------------
 * 04/04/2012      RC          2.07       Initial coding
 * 04/10/2012      RC          2.08       Made a NotificationObject so can update in list.
 *                                         Added DownloadProgress to see the progress bar.
 * 07/26/2012      RC          2.13       Moved file information in DownloadFile to AdcpSerialPort to reuse the decoding of the file info.
 * 08/28/2012      RC          2.13       Added properties for the file information so they can be binded to the list.
 * 08/08/2014      RC          4.0.0      Updated RaisePropertyChanged with RaiseAndSetIfChanged().
 * 
 */

using System;
using System.Globalization;
using System.Diagnostics;
using ReactiveUI;

namespace RTI
{

    /// <summary>
    /// An object to represent a file
    /// that can be downloaded from the
    /// ADCP.  This will include the file
    /// name, size, date and time of last modification
    /// and the number of bytes currently
    /// downloaded.
    /// </summary>
    public class DownloadFile : ReactiveObject
    {
        #region Properties

        /// <summary>
        /// File to monitor download process.
        /// </summary>
        private RTI.Commands.AdcpEnsFileInfo _fileInfo;
        /// <summary>
        /// File to monitor download process.
        /// </summary>
        public RTI.Commands.AdcpEnsFileInfo FileInfo
        {
            get { return _fileInfo; }
            set
            {
                this.RaiseAndSetIfChanged(ref _fileInfo, value);
                this.RaisePropertyChanged("FileName");
            }
        }

        /// <summary>
        /// File Name.
        /// </summary>
        public string FileName
        {
            get { return _fileInfo.FileName; }
        }

        /// <summary>
        /// Date and time the file was last modified.
        /// </summary>
        public string ModificationDateTime
        {
            get { return _fileInfo.ModificationDateTime.ToString(); }
        }

        /// <summary>
        /// File Size.
        /// </summary>
        public string FileSize
        {
            get { return _fileInfo.FileSize.ToString(); }
        }

        /// <summary>
        /// File size in bytes.
        /// </summary>
        private long _downloadFileSize;
        /// <summary>
        /// File size in bytes.
        /// </summary>
        public long DownloadFileSize
        {
            get { return _downloadFileSize; }
            set
            {
                this.RaiseAndSetIfChanged(ref _downloadFileSize, value);
            }
        }

        /// <summary>
        /// Number of bytes currently downloaded.
        /// </summary>
        private long _downloadProgress;
        /// <summary>
        /// Number of bytes currently downloaded.
        /// </summary>
        public long DownloadProgress
        {
            get { return _downloadProgress; }
            set
            {
                this.RaiseAndSetIfChanged(ref _downloadProgress, value);
            }
        }

        /// <summary>
        /// Set flag whether the file is selected.
        /// This is used in the listview to determine
        /// which files are selected for download.
        /// </summary>
        private bool _isSelected;
        /// <summary>
        /// Set flag whether the file is selected.
        /// This is used in the listview to determine
        /// which files are selected for download.
        /// </summary>
        public bool IsSelected
        {
            get { return _isSelected; }
            set
            {
                this.RaiseAndSetIfChanged(ref _isSelected, value);
            }
        }

        #endregion

        /// <summary>
        /// Empty Constructor.
        /// Default values used.
        /// </summary>
        public DownloadFile()
        {
            // Default values
            _fileInfo = new RTI.Commands.AdcpEnsFileInfo();
            IsSelected = true;
            DownloadProgress = 0;
            DownloadFileSize = (long)(_fileInfo.FileSize * MathHelper.MB_TO_BYTES);
        }

        /// <summary>
        /// Get the file info and initialize the file info.
        /// </summary>
        /// <param name="fileInfo">Information about the file to download.</param>
        public DownloadFile(RTI.Commands.AdcpEnsFileInfo fileInfo)
        {
            _fileInfo = fileInfo;
            IsSelected = true;
            DownloadProgress = 0;
            DownloadFileSize = (long)(_fileInfo.FileSize * MathHelper.MB_TO_BYTES);
        }

    }

}