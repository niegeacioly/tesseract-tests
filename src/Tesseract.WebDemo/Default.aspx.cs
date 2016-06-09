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
using System.IO;
using System.Collections.Generic;
using Tesseract.WebDemo.Utils;
using System.Text;
using System.Net;
using OCRWebServiceREST.Client;
using Newtonsoft.Json;
using Google.Apis.Vision.v1;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Vision.v1.Data;
using Ghostscript.NET.Rasterizer;
using System.Text.RegularExpressions;

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
      protected FileUpload uploadedFile;
      protected HtmlButton submitFile;

      // result panel controls
      protected Panel resultPanel;
      protected HtmlGenericControl meanConfidenceLabel;
      protected HtmlTextArea resultText;
      protected HtmlTextArea tesseractResult;
      protected HtmlTextArea googleOCRResult;
      protected HtmlButton restartButton;
      protected HtmlButton downloadButton;

      protected TesseractEngine _tesseractEngine;

      protected VisionService _visionService;

      protected string _ocrResult;
      protected string _folderPath = @"~/TempFiles/";

      protected double DEFAULT_SCALE = 203 / 96;

      protected string USER_NAME = "NIEGECOSTA";
      protected string LICENSE_CODE = "18CB0BF6-AE7B-49CD-8690-5EB4B43FF4C0";

      protected string _outputFile = string.Empty;

      #endregion

      #region Constructor
      public DefaultPage()
      {
         _visionService = CreateAuthorizedClient();
         _tesseractEngine = new TesseractEngine(Server.MapPath(@"~/tessdata"), "por");
      }
      #endregion

      #region Event Handlers

      private void OnSubmitFileClicked(object sender, EventArgs args)
      {
         //TesseractMethods();
         //OCRWebServicesMethods();
         GoogleOCRMethods();
      }

      private void OCRWebServicesMethods()
      {
         if (uploadedFile.PostedFile != null && uploadedFile.PostedFile.ContentLength > 0)
         {
            _ocrResult = string.Empty;

            string fileName = uploadedFile.PostedFile.FileName;
            string fullPath = Server.MapPath(_folderPath) + fileName;
            uploadedFile.PostedFile.SaveAs(fullPath);

            Console.WriteLine("Process document using OCRWebService.com (REST API)\n");

            // Process Document 
            ProcessDocument(USER_NAME, LICENSE_CODE, fullPath);

            File.Delete(fullPath);
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
         _ocrResult = string.Empty;
         // Available pages
         Console.WriteLine("Available pages: " + ocrResponse.AvailablePages);

         for (int zone = 0; zone < ocrResponse.OCRText.Count; zone++)
         {
            for (int page = 0; page < ocrResponse.OCRText[zone].Count; page++)
            {
               _ocrResult += ocrResponse.OCRText[zone][page];
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
            _ocrResult = string.Empty;

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
                        _ocrResult += page.GetText();
                        _ocrResult += "\n";
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
                           _ocrResult += page.GetText();
                           _ocrResult += "\n";
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

      public Dictionary<string, System.Drawing.Image> ConvertPDFToImage(string fullPath)
      {

         Dictionary<string, System.Drawing.Image> capturedImages = new Dictionary<string, System.Drawing.Image>();
         int desired_x_dpi = 200;
         int desired_y_dpi = 200;

         string outputPath = Server.MapPath(_folderPath);

         using (var rasterizer = new GhostscriptRasterizer())
         {
            rasterizer.Open(fullPath);

            for (var pageNumber = 1; pageNumber <= rasterizer.PageCount; pageNumber++)
            {
               var pageFilePath = Path.Combine(outputPath, string.Format("Page-{0}.png", pageNumber));

               var img = rasterizer.GetPage(desired_x_dpi, desired_y_dpi, pageNumber);
               img.Save(pageFilePath, System.Drawing.Imaging.ImageFormat.Png);
               capturedImages.Add(pageFilePath, img);
            }
         }

         return capturedImages;
      }

      public void GoogleOCRMethods()
      {
         if (uploadedFile.PostedFile != null && uploadedFile.PostedFile.ContentLength > 0)
         {
            _ocrResult = string.Empty;
            Dictionary<string, System.Drawing.Image> images = null;
            string contentType = uploadedFile.PostedFile.ContentType;
            string fileName = uploadedFile.PostedFile.FileName;
            string fullPath = Server.MapPath(_folderPath) + fileName;
            uploadedFile.PostedFile.SaveAs(fullPath);
            if (contentType.Equals("application/pdf"))
            {
               images = ConvertPDFToImage(fullPath);
            }
            else
            {
               System.Drawing.Image catchedImage = System.Drawing.Image.FromFile(fullPath);
               images = new Dictionary<string, System.Drawing.Image>(1);
               images.Add(fullPath, catchedImage);
            }
            if (images != null && images.Count > 0)
            {
               // for now just fail hard if there's any error however in a proper app I would expect a full demo.
               foreach (var image in images)
               {
                  Bitmap bitmap = new Bitmap(image.Value);
                  IList<AnnotateImageResponse> result = ExtractTextsByGoogleOCR(_visionService, image.Key);

                  if (result != null)
                  {
                     // Loop through and output label annotations for the image
                     foreach (var response in result)
                     {
                        //foreach (var text in response.TextAnnotations)
                        //{
                        if (response != null && response.TextAnnotations != null)
                           _ocrResult += (response.TextAnnotations[0].Description);
                        //}
                     }
                  }
                  File.Delete(image.Key);
                  image.Value.Dispose();
               }
               googleOCRResult.InnerText = _ocrResult;
               fileName = fileName.Substring(0, fileName.IndexOf(".")) + ".txt";
               _outputFile = Server.MapPath(_folderPath) + fileName;
               File.WriteAllText(_outputFile, _ocrResult);
               HttpContext.Current.Session["output"] = fileName;
            }
            File.Delete(fullPath);
            inputPanel.Visible = false;
            resultPanel.Visible = true;
         }
      }

      private void OnRestartClicked(object sender, EventArgs args)
      {
         resultPanel.Visible = false;
         inputPanel.Visible = true;
         string fileName = HttpContext.Current.Session["output"].ToString();
         _outputFile = Server.MapPath(_folderPath) + fileName;
         File.Delete(_outputFile);
      }

      public void DownloadFile(object sender, EventArgs args)
      {
         string fileName = HttpContext.Current.Session["output"].ToString();
         _outputFile = Server.MapPath(_folderPath) + fileName;
         string text = string.Empty;
         using (StreamReader sr = new StreamReader(_outputFile))
         {
            text = sr.ReadToEndAsync().Result;
         }

         Response.Clear();
         Response.ClearHeaders();

         Response.AddHeader("Content-Length", text.Length.ToString());
         Response.ContentType = "text/plain";
         Response.AppendHeader("content-disposition", "attachment;filename=\"" + fileName + "\"");

         Response.Write(text);
         Response.End();
      }

      public VisionService CreateAuthorizedClient()
      {
         GoogleCredential credential = GoogleCredential.GetApplicationDefaultAsync().Result;
         // Inject the Cloud Vision scopes
         if (credential.IsCreateScopedRequired)
         {
            credential = credential.CreateScoped(new[]
            {
                    VisionService.Scope.CloudPlatform
                });
         }
         return new VisionService(new BaseClientService.Initializer
         {
            HttpClientInitializer = credential,
            GZipEnabled = false
         });
      }

      public IList<AnnotateImageResponse> ExtractTextsByGoogleOCR(VisionService vision, string imagePath)
      {
         Console.WriteLine("Detecting Texts...");
         // Convert image to Base64 encoded for JSON ASCII text based request   
         byte[] imageArray = File.ReadAllBytes(imagePath);
         string imageContent = Convert.ToBase64String(imageArray);
         // Post label detection request to the Vision API
         // [START construct_request]
         var responses = vision.Images.Annotate(
             new BatchAnnotateImagesRequest()
             {
                Requests = new[] {
                    new AnnotateImageRequest() {
                        Features = new [] { new Feature() { Type =
                          "TEXT_DETECTION"}},
                        Image = new Google.Apis.Vision.v1.Data.Image() { Content = imageContent }
                    }
            }
             }).Execute();
         return responses.Responses;
         // [END construct_request]
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
         this.downloadButton.ServerClick += DownloadFile;
      }

      #endregion
   }

}