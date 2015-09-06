using System;

namespace EtoTest.Model
{
    public class DataFileVersion
    {
        /// <summary>
        /// The name of this station - writen to name of conlict files when resyncing.
        /// </summary>
        public String StationName { get; set; }
        /// <summary>
        /// If this ID is different from the current on google drive then updates have occured on google
        /// </summary>
        public int FromVersionId { get; set; }
        /// <summary>
        /// If we have made some of our own updates when offline, then this value is incremented from 0.
        /// </summary>
        public int? CurrentVersionId { get; set; }

        /// <summary>
        /// A description of the operation that we are about to attempt.  If this value is set; then we crashed before the last operation completed.  
        /// Set to null immediately after completing an operation.
        /// </summary>
        public String BeforeOperation { get; set; }
    }
}