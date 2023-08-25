using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Drawing;
using System;
using System.Net.Mail;
using MigraDoc.DocumentObjectModel;
using MigraDoc.Rendering;
using static Helper.Model.MESPASEnquiryClassDeclarations;
using System.Collections.Generic;
using System.IO;
using PdfSharp.Pdf;
using System.Text;
using System.Runtime.Serialization.Formatters.Binary;
namespace Helper.Model
{
    public class MESPASEnquiry
    {
        private IConfiguration _Configuration;
        public MESPASEnquiry(IConfiguration configuration)
        {
            _Configuration = configuration;
        }
        public void SendMESPASHTTPExceptionNotification(string apiURL, System.Net.HttpStatusCode statuscode)
        {
            try
            {
                dynamic dtlBody = "";
                MailMessage mail = new MailMessage();
                SmtpClient SmtpServer = new SmtpClient("smtp.gmail.com");
                mail.From = new MailAddress(this._Configuration.GetSection("HTTPExceptionNotification")["FromMail"]);
                mail.To.Add(this._Configuration.GetSection("HTTPExceptionNotification")["ToMail"]);
                mail.CC.Add(this._Configuration.GetSection("HTTPExceptionNotification")["CCMail"]);
                if (apiURL != "")
                {
                    mail.Subject = "HTTP ERROR";
                    mail.Body = "<p>Hello Team,<br /><br />This API URL : " + apiURL + "  and API Status Code : " + statuscode + " has some errors .<br/><br/>" +
                        "<span style='color: red;'>This is a system generated email, do not reply to this email id.</span><br/><br/>" +
                        "Thanks & Regards,<br />" +
                        "RPA BOT</p>";
                }
                mail.IsBodyHtml = true;
                SmtpServer.Port = 587;
                SmtpServer.UseDefaultCredentials = true;
                SmtpServer.Credentials = new System.Net.NetworkCredential(this._Configuration.GetSection("HTTPExceptionNotification")["Username"], this._Configuration.GetSection("HTTPExceptionNotification")["Password"]);
                SmtpServer.EnableSsl = true;
                SmtpServer.Send(mail);
            }
            catch (Exception ex)
            {
            }
        }
        public void SendHTTPExceptionNotification(string apiURL, System.Net.HttpStatusCode statuscode)
        {
            try
            {
                dynamic dtlBody = "";
                MailMessage mail = new MailMessage();
                SmtpClient SmtpServer = new SmtpClient("smtp.gmail.com");
                mail.From = new MailAddress(this._Configuration.GetSection("HTTPOurExceptionNotification")["FromMail"]);
                mail.To.Add(this._Configuration.GetSection("HTTPOurExceptionNotification")["ToMail"]);
                mail.CC.Add(this._Configuration.GetSection("HTTPOurExceptionNotification")["CCMail"]);
                if (apiURL != "")
                {
                    mail.Subject = "HTTP ERROR";
                    mail.Body = "<p>Hello Team,<br /><br />This API URL : " + apiURL + "  and API Status Code : " + statuscode + " has some errors .<br/><br/>" +
                        "<span style='color: red;'>This is a system generated email, do not reply to this email id.</span><br/><br/>" +
                        "Thanks & Regards,<br />" +
                        "RPA BOT</p>";
                }
                mail.IsBodyHtml = true;
                SmtpServer.Port = 587;
                SmtpServer.UseDefaultCredentials = true;
                SmtpServer.Credentials = new System.Net.NetworkCredential(this._Configuration.GetSection("HTTPOurExceptionNotification")["Username"], this._Configuration.GetSection("HTTPExceptionNotification")["Password"]);
                SmtpServer.EnableSsl = true;
                SmtpServer.Send(mail);
            }
            catch (Exception ex)
            {
            }
        }
        public void SendNotProcessedEnquiryNotification(string customerName, string enqNo, string exceptionmsg)
        {
            try
            {
                dynamic dtlBody = "";
                MailMessage mail = new MailMessage();
                SmtpClient SmtpServer = new SmtpClient("smtp.gmail.com");
                mail.From = new MailAddress(this._Configuration.GetSection("NotProcessedEnquiryNotification")["FromMail"]);
                mail.To.Add(this._Configuration.GetSection("NotProcessedEnquiryNotification")["ToMail"]);
                mail.CC.Add(this._Configuration.GetSection("NotProcessedEnquiryNotification")["CCMail"]);
                if (customerName != "" && enqNo != "")
                {
                    mail.Subject = "Enquiry Not Processed";
                    mail.Body = "<p>Hello Team,<br /><br />Enquiry Not Processed due to : " + exceptionmsg + "<br/><br/>" +
                        "Enquiry Details" + "<br/><br/>" +
                        "Customer Name : " + customerName +
                        "Enquiry Number :" + enqNo +
                        "<span style='color: red;'>This is a system generated email, do not reply to this email id.</span><br/><br/>" +
                        "Thanks & Regards,<br />" +
                        "RPA BOT</p>";
                }
                mail.IsBodyHtml = true;
                SmtpServer.Port = 587;
                SmtpServer.UseDefaultCredentials = true;
                SmtpServer.Credentials = new System.Net.NetworkCredential(this._Configuration.GetSection("NotProcessedEnquiryNotification")["Username"], this._Configuration.GetSection("NotProcessedEnquiryNotification")["Password"]);
                SmtpServer.EnableSsl = true;
                SmtpServer.Send(mail);
            }
            catch (Exception ex)
            {
            }
        }
        public void CustomerNotMapped(string customerName)
        {
            try
            {
                dynamic dtlBody = "";
                MailMessage mail = new MailMessage();
                SmtpClient SmtpServer = new SmtpClient("smtp.gmail.com");
                mail.From = new MailAddress(this._Configuration.GetSection("NotProcessedEnquiryNotification")["FromMail"]);
                mail.To.Add(this._Configuration.GetSection("NotProcessedEnquiryNotification")["ToMail"]);
                mail.CC.Add(this._Configuration.GetSection("NotProcessedEnquiryNotification")["CCMail"]);
                if (customerName != "")
                {
                    mail.Subject = "Customer not found";
                    mail.Body = "<p>Hello Team,<br /><br />Customer " + customerName + " not found in our database. <br/><br/>" +
                        "<span style='color: red;'>This is a system generated email, do not reply to this email id.</span><br/><br/>" +
                        "Thanks & Regards,<br />" +
                        "RPA BOT</p>";
                }
                mail.IsBodyHtml = true;
                SmtpServer.Port = 587;
                SmtpServer.UseDefaultCredentials = true;
                SmtpServer.Credentials = new System.Net.NetworkCredential(this._Configuration.GetSection("NotProcessedEnquiryNotification")["Username"], this._Configuration.GetSection("NotProcessedEnquiryNotification")["Password"]);
                SmtpServer.EnableSsl = true;
                SmtpServer.Send(mail);
            }
            catch (Exception ex)
            {
            }
        }
        public string createRFQFile(JObject data)
        {
            string pdffilepath = "";
            string folderName = this._Configuration.GetSection("RFQFile")["ProcessedJsonfilePath"];
            string NotProcessedfilePath = this._Configuration.GetSection("RFQFile")["NotProcessedfilePath"];
            var dt1 = DateTime.Now.ToString("dd-MM-yyyy");
            string rfqNo = data["id"].ToString();
            System.IO.Directory.CreateDirectory(folderName);
            string fileName = "EventId" + "_" + rfqNo + "_" + dt1 + ".txt";
            string pathString = System.IO.Path.Combine(folderName, fileName);
            string notProcessedfile = System.IO.Path.Combine(NotProcessedfilePath, fileName);
            if (!File.Exists(pathString))
            {
                using (StreamWriter fileStr = File.CreateText(pathString))
                {
                    fileStr.WriteLine(data);
                    fileStr.Close();
                    pdffilepath = CreatePDFFileFromTxtFile(pathString);
                    fileStr.Close();
                }
            }
            else
            {
                File.Move(pathString, notProcessedfile);
            }
            return pdffilepath;
        }
       public string CreatePDFFileFromBinarydata(string binarydata)
        {
            var base64EncodedBytes = System.Convert.FromBase64String(binarydata);
            string inputStr = Encoding.UTF8.GetString(Convert.FromBase64String(binarydata));
            return "ok";
        }
        public string  CreatePDFFileFromTxtFile(string textfilefullpath)
        {
            string NotProcessedfilePath = this._Configuration.GetSection("RFQFile")["NotProcessedfilePath"];
            string ProcessedPdffilePath = this._Configuration.GetSection("RFQFile")["ProcessedPdffilePath"];
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            List<string> textFileLines = new List<string>();
            using (StreamReader sr = new StreamReader(textfilefullpath))
            {
                while (!sr.EndOfStream)
                {
                    textFileLines.Add(sr.ReadLine());
                }
            }
            Document doc = new Document();
            Section section = doc.AddSection();
            //just font arrangements as you wish
            MigraDoc.DocumentObjectModel.Font font = new MigraDoc.DocumentObjectModel.Font("Verdana", 8);
            foreach (string line in textFileLines)
            {
                Paragraph paragraph = section.AddParagraph();
                paragraph.AddFormattedText(line, font);
            }
            PdfDocumentRenderer renderer = new PdfDocumentRenderer();
            renderer.Document = doc;
            renderer.RenderDocument();
            string pdffileName = Path.GetFileNameWithoutExtension(textfilefullpath);
            string pdffilepath = ProcessedPdffilePath + pdffileName + ".pdf";
            string notprocessedfileName = Path.GetFileNameWithoutExtension(textfilefullpath);
            string notprocessedfilepath = NotProcessedfilePath + notprocessedfileName + ".pdf";
            if (!File.Exists(pdffilepath))
            {
                renderer.Save(pdffilepath);
            }
            else
            {
                File.Move(pdffilepath,notprocessedfilepath);
            }
            return pdffilepath;
        }
    }
}

