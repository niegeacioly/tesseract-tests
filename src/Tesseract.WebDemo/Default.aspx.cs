using System;
using System.Collections;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Web;
using System.Web.SessionState;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.HtmlControls;
using iTextSharp.text.pdf;
using iTextSharp.text.pdf.parser;
using System.IO;
using System.Collections.Generic;
using Tesseract.WebDemo.Utils;
using System.Text;
using System.Net;
using OCRWebServiceREST.Client;
using Newtonsoft.Json;

namespace Tesseract.WebDemo
{
   /// <summary>
   /// Description of MainForm.
   /// </summary>
   public class DefaultPage : System.Web.UI.Page
   {
      #region Data
      // input panel controls
      protected Panel inputPanel;
      protected HtmlInputFile uploadedFile;
      protected HtmlButton submitFile;

      // result panel controls
      protected Panel resultPanel;
      protected HtmlGenericControl meanConfidenceLabel;
      protected HtmlTextArea resultText;
      protected HtmlTextArea tesseractResult;
      protected HtmlButton restartButton;

      protected TesseractEngine _tesseractEngine;

      protected StringBuilder _ocrResult;
      protected string _folderPath = @"~/TempFiles/";

      protected double DEFAULT_SCALE = 203 / 96;

      protected string USER_NAME = "NIEGECOSTA";
      protected string LICENSE_CODE = "18CB0BF6-AE7B-49CD-8690-5EB4B43FF4C0";

      #endregion

      #region Constructor
      public DefaultPage()
      {

         _tesseractEngine = new TesseractEngine(Server.MapPath(@"~/tessdata"), "por");
      }
      #endregion

      #region Event Handlers

      private void OnSubmitFileClicked(object sender, EventArgs args)
      {
         TesseractMethods();
         OCRWebServicesMethods();
      }

      private void OCRWebServicesMethods()
      {
         if (uploadedFile.PostedFile != null && uploadedFile.PostedFile.ContentLength > 0)
         {
            _ocrResult = new StringBuilder();

            string fileName = uploadedFile.PostedFile.FileName;
            string fullPath = Server.MapPath(_folderPath) + fileName;
            uploadedFile.PostedFile.SaveAs(fullPath);

            Console.WriteLine("Process document using OCRWebService.com (REST API)\n");


            // Process Document 
            ProcessDocument(USER_NAME, LICENSE_CODE, fullPath);

            // Get Account information
            PrintAccountInformation(USER_NAME, LICENSE_CODE);
         }
      }

      private void ProcessDocument(string user_name, string license_code, string fullPath)
      {
         // For SSL using
         // ServicePointManager.ServerCertificateValidationCallback = new RemoteCertificateValidationCallback(delegate { return true; });

         // Build your OCR:

         // Extraction text with English language
         string ocrURL = @"http://www.ocrwebservice.com/restservices/processDocument?gettext=true&language=portuguese";

         // Extraction text with English and German language using zonal OCR
         // ocrURL = @"http://www.ocrwebservice.com/restservices/processDocument?language=english,german&zone=0:0:600:400,500:1000:150:400";

         // Convert first 5 pages of multipage document into doc and txt
         // ocrURL = @"http://www.ocrwebservice.com/restservices/processDocument?language=english&pagerange=1-5&outputformat=doc,txt";


         byte[] uploadData = File.ReadAllBytes(fullPath);
         /*GetUploadedFile(file_path);*/
         //using (var binaryReader = new BinaryReader(uploadedFile.PostedFile.InputStream))
         //{
         //   uploadData = binaryReader.ReadBytes(uploadedFile.PostedFile.ContentLength);
         //}

         HttpWebRequest request = CreateHttpRequest(ocrURL, user_name, license_code, "POST");
         request.ContentLength = uploadData.Length;

         //  Send request
         using (Stream post = request.GetRequestStream())
         {
            post.Write(uploadData, 0, (int)uploadData.Length);
         }

         try
         {
            //  Get response
            using (HttpWebResponse response = request.GetResponse() as HttpWebResponse)
            {
               // Parse JSON response
               string strJSON = new StreamReader(response.GetResponseStream()).ReadToEnd();
               OCRResponseData ocrResponse = JsonConvert.DeserializeObject<OCRResponseData>(strJSON);

               PrintOCRData(ocrResponse);
            }
         }
         catch (WebException wex)
         {
            Console.WriteLine(string.Format("OCR API Error. HTTPCode:{0}", ((HttpWebResponse)wex.Response).StatusCode));
         }
      }

      /// <summary>
      /// Print OCRWebService.com account information
      /// </summary>
      /// <param name="user_name"></param>
      /// <param name="license_code"></param>
      private static void PrintAccountInformation(string user_name, string license_code)
      {
         try
         {
            string address_get = @"http://www.ocrwebservice.com/restservices/getAccountInformation";

            HttpWebRequest request_get = CreateHttpRequest(address_get, user_name, license_code, "GET");

            using (HttpWebResponse response = request_get.GetResponse() as HttpWebResponse)
            {
               string strJSON = new StreamReader(response.GetResponseStream()).ReadToEnd();
               OCRResponseAccountInfo ocrResponse = JsonConvert.DeserializeObject<OCRResponseAccountInfo>(strJSON);

               Console.WriteLine(string.Format("Available pages:{0}", ocrResponse.AvailablePages));
               Console.WriteLine(string.Format("Max pages:{0}", ocrResponse.MaxPages));
               Console.WriteLine(string.Format("Expiration date:{0}", ocrResponse.ExpirationDate));
               Console.WriteLine(string.Format("Last processing time:{0}", ocrResponse.LastProcessingTime));
            }

         }
         catch (WebException wex)
         {
            Console.WriteLine(string.Format("OCR API Error. HTTPCode:{0}", ((HttpWebResponse)wex.Response).StatusCode));
         }
      }

      private static HttpWebRequest CreateHttpRequest(string addressUrl, string userName, string licenseCode, string httpMethod)
      {
         Uri address = new Uri(addressUrl);

         HttpWebRequest request = (HttpWebRequest)WebRequest.Create(address);

         byte[] authBytes = Encoding.UTF8.GetBytes(string.Format("{0}:{1}", userName, licenseCode).ToCharArray());
         request.Headers["Authorization"] = "Basic " + Convert.ToBase64String(authBytes);
         request.Method = httpMethod;
         request.Timeout = 600000;

         // Specify Response format to JSON or XML (application/json or application/xml)
         request.ContentType = "application/json";

         return request;
      }

      private void PrintOCRData(OCRResponseData ocrResponse)
      {
         _ocrResult = new StringBuilder();
         // Available pages
         Console.WriteLine("Available pages: " + ocrResponse.AvailablePages);

         for (int zone = 0; zone < ocrResponse.OCRText.Count; zone++)
         {
            for (int page = 0; page < ocrResponse.OCRText[zone].Count; page++)
            {
               _ocrResult.Append(ocrResponse.OCRText[zone][page]);
            }
         }

         resultText.InnerText = _ocrResult.ToString();

         inputPanel.Visible = false;
         resultPanel.Visible = true;
      }

      private static void DownloadConvertedFile(HttpWebResponse result, string file_name)
      {
         using (Stream response_stream = result.GetResponseStream())
         {
            using (Stream output_stream = File.OpenWrite(file_name))
            {
               response_stream.CopyTo(output_stream);
            }
         }
      }

      private void TesseractMethods()
      {
         if (uploadedFile.PostedFile != null && uploadedFile.PostedFile.ContentLength > 0)
         {
            _ocrResult = new StringBuilder();

            string fileName = uploadedFile.PostedFile.FileName;
            string fullPath = Server.MapPath(_folderPath) + fileName;
            uploadedFile.PostedFile.SaveAs(fullPath);
            Dictionary<string, System.Drawing.Image> images = PdfImageExtractor.ExtractImages(fullPath);
            if (images != null && images.Count > 0)
            {
               // for now just fail hard if there's any error however in a proper app I would expect a full demo.
               foreach (var image in images)
               {
                  Bitmap bitmap = new Bitmap(image.Value);
                  string imagePath = Server.MapPath(_folderPath) + "img_" + DateTime.Now.ToString("yyyyMMddHHmmss") + ".gif";
                  bitmap.Save(imagePath, System.Drawing.Imaging.ImageFormat.Gif);

                  using (var page = _tesseractEngine.Process(bitmap))
                  {
                     meanConfidenceLabel.InnerText = string.Format("{0:P}", page.GetMeanConfidence());
                     if (page.GetMeanConfidence() > 0.85f)
                     {
                        _ocrResult.Append(page.GetText());
                        _ocrResult.Append("\n");
                     }
                     else
                     {
                        int width = (int)(bitmap.Width * DEFAULT_SCALE);
                        int height = (int)(bitmap.Height * DEFAULT_SCALE);
                        Bitmap newBitmap = new Bitmap(bitmap, width, height);
                        bitmap.Dispose();
                        page.Dispose();
                        using (var newPageTransformed = _tesseractEngine.Process(newBitmap))
                        {
                           meanConfidenceLabel.InnerText = string.Format("{0:P}", page.GetMeanConfidence());
                           _ocrResult.Append(page.GetText());
                           _ocrResult.Append("\n");
                        }
                     }
                  }
               }
            }

            byte[] bytes = Encoding.Default.GetBytes(_ocrResult.ToString());
            string result = Encoding.Default.GetString(bytes);

            tesseractResult.InnerText = result;
            File.Delete(fullPath);
         }
         inputPanel.Visible = false;
         resultPanel.Visible = true;
      }

      private void OnRestartClicked(object sender, EventArgs args)
      {
         resultPanel.Visible = false;
         inputPanel.Visible = true;
      }

      #endregion

      #region Page Setup
      protected override void OnInit(EventArgs e)
      {
         InitializeComponent();
         base.OnInit(e);
      }

      //----------------------------------------------------------------------
      private void InitializeComponent()
      {
         this.restartButton.ServerClick += OnRestartClicked;
         this.submitFile.ServerClick += OnSubmitFileClicked;
      }

      #endregion
   }

}