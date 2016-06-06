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
      protected HtmlButton restartButton;

      protected TesseractEngine _tesseractEngine;

      protected StringBuilder _ocrResult;
      protected string _folderPath = @"~/TempFiles/";
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
         if (uploadedFile.PostedFile != null && uploadedFile.PostedFile.ContentLength > 0)
         {
            _ocrResult = new StringBuilder();

            string fileName = uploadedFile.PostedFile.FileName;
            string fullPath = Server.MapPath(_folderPath) + fileName;
            uploadedFile.PostedFile.SaveAs(fullPath);
            Dictionary<string, System.Drawing.Image> images = PdfImageExtractor.ExtractImages(fullPath);

            // for now just fail hard if there's any error however in a proper app I would expect a full demo.
            foreach (var image in images)
            {
               Bitmap newBitmap = new Bitmap(image.Value);
               using (var page = _tesseractEngine.Process(newBitmap))
               {
                  meanConfidenceLabel.InnerText = string.Format("{0:P}", page.GetMeanConfidence());
                  _ocrResult.Append(page.GetText());
                  _ocrResult.Append("\n");
               }
            }

            resultText.InnerText = _ocrResult.ToString();
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