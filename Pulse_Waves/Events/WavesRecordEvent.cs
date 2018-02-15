using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RTI
{
    /// <summary>
    /// Record to hold waves records.
    /// </summary>
    public class WavesRecordEvent
    {
        #region Properties

        /// <summary>
        /// Latest waves record.
        /// </summary>
        public Waves.WavesRecord Record { get; set; }

        #endregion

        /// <summary>
        /// Send an event to handle the next waves record.
        /// </summary>
        /// <param name="record">Waves Record.</param>
        public WavesRecordEvent(Waves.WavesRecord record)
        {
            Record = record; 
        }
    }

    /// <summary>
    /// Load the wave record file.
    /// </summary>
    public class WavesRecordFileEvent
    {
        #region Properties

        /// <summary>
        /// Latest waves record file path.
        /// </summary>
        public string[] RecordFilePath{ get; set; }

        #endregion

        /// <summary>
        /// Send an event to handle the next waves record.
        /// </summary>
        /// <param name="recordFilePaths">Waves Record file path.</param>
        public WavesRecordFileEvent(string[] recordFilePaths)
        {
            RecordFilePath = recordFilePaths;
        }
    }
}
