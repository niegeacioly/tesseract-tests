<%@ Page
   Language="C#"
   AutoEventWireup="false"
   Inherits="Tesseract.WebDemo.DefaultPage"
   ValidateRequest="false"
   EnableSessionState="true" %>


<!DOCTYPE html>
<html lang="en">
<head>
   <title>NF Reader</title>

   <meta http-equiv="content-type" content="text/html; charset=utf-8" />
   <%--<meta http-equiv="CACHE-CONTROL" content="NO-CACHE" />--%>
   <%--<meta http-equiv="PRAGMA" content="NO-CACHE" />--%>
   <meta name="viewport" content="width=device-width, initial-scale=1.0">
   <!-- Bootstrap -->
   <link href="Content/bootstrap.min.css" rel="stylesheet">

   <!-- HTML5 Shim and Respond.js IE8 support of HTML5 elements and media queries -->
   <!-- WARNING: Respond.js doesn't work if you view the page via file:// -->
   <!--[if lt IE 9]>
	      <script src="https://oss.maxcdn.com/libs/html5shiv/3.7.0/html5shiv.js"></script>
	      <script src="https://oss.maxcdn.com/libs/respond.js/1.3.0/respond.min.js"></script>
   	 	<![endif]-->

</head>
<body>
   <div class="container">
      <form id="Form1" runat="server" enctype="multipart/form-data" method="post">
         <asp:Panel  ID="inputPanel" runat="server">
            <fieldset>
               <legend>File Upload</legend>
               <div class="form-group">
                  <label for="imageFile" runat="server">File:</label>
                  <asp:FileUpload class="form-control" id="uploadedFile" runat="server" />
                  <span class="help-block">The file to be processed. (pdf, gif, png, jpg, jpeg, bmp, tiff) </span>
               </div>
               <button id="submitFile" type="submit" class="btn btn-default" runat="server">Submit</button>
            </fieldset>
            <Triggers>
               <asp:PostBackTrigger ControlID="submitFile" />
            </Triggers>
         </asp:Panel>
         <asp:Panel ID="resultPanel" Visible="False" runat="server">
            <fieldset>
               <legend>OCR Results</legend>
               <div class="form-group">
                  <%--<label for="result" runat="server">Mean Confidence of Tesseract:</label>
                  <label class="form-control" id="meanConfidenceLabel" runat="server" />--%>
                  <button id="downloadButton" type="button" class="btn btn-default" runat="server">Download txt file</button>
               </div>
               <div class="form-group">
                  <%--<label for="result" runat="server">Result: OCR Web Service</label>
                  <textarea class="form-control" rows="10" id="resultText" readonly="readonly" runat="server"></textarea>
                  <label for="result" runat="server">Tesseract</label>
                  <textarea class="form-control" rows="10" id="tesseractResult" readonly="readonly" runat="server"></textarea>--%>
                  <label for="result" runat="server">Google OCR</label>
                  <textarea class="form-control" rows="10" id="googleOCRResult" readonly="readonly" runat="server"></textarea>
               </div>
               <button id="restartButton" type="submit" class="btn btn-default" runat="server">Restart</button>
            </fieldset>
         </asp:Panel>
      </form>
   </div>
</body>
</html>
