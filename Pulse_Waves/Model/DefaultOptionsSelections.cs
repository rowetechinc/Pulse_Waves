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
 * 12/30/2014      RC          0.0.1       Initial coding
 * 
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RTI
{
    /// <summary>
    /// Give the subsystem codes and a description for the default options.
    /// </summary>
    public class DefaultOptionsSelections
    {
        #region Properties

        /// <summary>
        /// Subsystem code 1.
        /// </summary>
        public byte Code1 { get; set; }

        /// <summary>
        /// Subsystem code 2.
        /// </summary>
        public byte Code2 { get; set; }

        /// <summary>
        /// Subsystem Descriptions.
        /// </summary>
        public byte Desc { get; set; }

        #endregion

        /// <summary>
        /// Initialize the values.
        /// If it is not a dual frequncy system, set the code 2 to 0.
        /// </summary>
        /// <param name="code1">Subsystem code 1.</param>
        /// <param name="code2">Subsystem code 2.</param>
        public DefaultOptionsSelections(byte code1, byte code2 = 0)
        {
            Code1 = code1;
            Code2 = code2;
        }

        /// <summary>
        /// Set the string description for the codes given.
        /// </summary>
        /// <returns>String descriptions.</returns>
        public override string ToString()
        {
            string desc = "";

            // Code 1
            desc = Convert.ToString(Convert.ToChar(Code1));

            // Code 2
            if (Code2 != 0)
            {
                desc += Convert.ToString(Convert.ToChar(Code2));
            }

            // Tab
            desc += "\t";

            // Code 1 desc
            desc += Subsystem.DescString(Code1);

            // Code 2 desc
            if(Code2 != 0)
            {
                desc += ", ";
                desc += Subsystem.DescString(Code2);
            }


            return desc;
        }

    }
}
