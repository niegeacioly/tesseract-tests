using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OCRWebServiceREST.Client
{
    /// <summary>
    /// User account information class
    /// </summary>
    internal class OCRResponseAccountInfo
    {
        /// <summary>
        /// Available pages
        /// </summary>
        public int AvailablePages { get; set; }

        /// <summary>
        /// Max pages
        /// </summary>
        public int MaxPages { get; set; }

        /// <summary>
        /// Expiration Date
        /// </summary>
        public string ExpirationDate { get; set; }

        /// <summary>
        /// Last processing date
        /// </summary>
        public string LastProcessingTime { get; set; }

        /// <summary>
        /// Subscription plan
        /// </summary>
        public string SubcriptionPlan { get; set; }

        /// <summary>
        /// Error message
        /// </summary>
        public string ErrorMessage { get; set; }
    }
}
