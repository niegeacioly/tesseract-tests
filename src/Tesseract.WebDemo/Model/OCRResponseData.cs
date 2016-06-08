using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OCRWebServiceREST.Client
{
    /// <summary>
    /// OCR Response data
    /// </summary>
    internal class OCRResponseData
    {
        /// <summary>
        /// Error message
        /// </summary>
        public string ErrorMessage { get; set; }

        /// <summary>
        /// Available pages
        /// </summary>
        public int AvailablePages { get; set; }

        /// <summary>
        /// OCRed text
        /// </summary>
        public List<List<string>> OCRText { get; set; }

        /// <summary>
        /// Output file1 URL
        /// </summary>
        public string OutputFileUrl { get; set; }

        /// <summary>
        /// Output file2 URL
        /// </summary>
        public string OutputFileUrl2 { get; set; }

        /// <summary>
        /// Output file3 URL
        /// </summary>
        public string OutputFileUrl3 { get; set; }
        
        /// <summary>
        /// Reserved
        /// </summary>
        public List<List<string>> Reserved { get; set; }

        /// <summary>
        /// OCRWords
        /// </summary>
        public List<List<OCRWSWord>> OCRWords { get; set; }

        /// <summary>
        /// Task description
        /// </summary>
        public string TaskDescription { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        public OCRResponseData()
        {
            OCRText = new List<List<string>>();
            Reserved = new List<List<string>>();
            OCRWords = new List<List<OCRWSWord>>();
        }
    }

    internal class OCRWSWord
    {
        public int Top;
        public int Left;
        public int Height;
        public int Width;
        public string OCRWord;
    }
}
