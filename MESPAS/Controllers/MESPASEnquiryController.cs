
using Helper.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using static Helper.Model.MESPASEnquiry;
using static Helper.Model.MESPASEnquiryClassDeclarations;
using static System.Net.Mime.MediaTypeNames;
using DuoVia.FuzzyStrings;
using System.Text.RegularExpressions;

namespace MESPAS.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MESPASEnquiryController : ControllerBase
    {
        static readonly log4net.ILog _log4net = log4net.LogManager.GetLogger(typeof(MESPASEnquiryController));
        private readonly ILogger<MESPASEnquiryController> _logger;
        private IConfiguration _Configuration;
        public MESPASEnquiryController(IConfiguration configuration)
        {
            _Configuration = configuration;
        }
        [HttpPost]
        [Route("GenerateJson")]
        public async Task<ActionResult> GenerateJson(Enquirydata inobjEnquirydata)
        {
            MESPASEnquiry objEnquiry = new MESPASEnquiry(_Configuration);
            Message obj = new Message();
            try
            {
                if (inobjEnquirydata.sourceType.ToUpper() == "MESPAS")
                {
                    string ResultMessage = string.Empty;
                    var result = Authenticate();
                    if (obj.result == "")
                    {
                        _log4net.Error("Error while Creating Enquiry");
                        return Ok(obj);
                    }
                    else
                    {
                        _log4net.Info("Data Converted into our json");
                        return Ok(obj);
                    }
                }
                return Ok(obj);
            }
            catch (Exception ex)
            {
                _log4net.Error(ex);
                return StatusCode(HttpContext.Response.StatusCode, ex.Message);
            }
        }
        public async Task<ActionResult<string>> Authenticate()
        {
            try
            {
                string usernameEmailAddress = string.Empty;
                string passwords = string.Empty;
                string apiBaseUrl = string.Empty;
                string apitoken = this._Configuration.GetSection("AuthenticateSettings")["Token"];
                _log4net.Info("Generated MESPAS Token" + apitoken);
                try
                {
                    await GetLatestEventId(apitoken);
                }
                catch (Exception ex)
                {
                    _log4net.Error("Error Occured while Passing MESPAS Token" + ex.Message);
                }
                return apitoken;
            }
            catch (Exception ex)
            {
                _log4net.Error("Error Occured while Generating MESPAS Token" + ex.Message);
                return (ex.Message);
                throw;
            }
        }
        public async Task<ActionResult<string>> GetLatestEventId(string token)
        {

            try
            {
                MESPASEnquiry objEnquiry = new MESPASEnquiry(_Configuration);
                string apiBaseUrl = string.Empty;
                apiBaseUrl = this._Configuration.GetSection("APIURL")["GetLatestEventId"];
                HttpClientHandler clientHandler = new HttpClientHandler();
                clientHandler.ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => { return true; };
                using (HttpClient client = new HttpClient(clientHandler))
                {
                    StringContent content = new StringContent("application/json");
                    string endpoint = apiBaseUrl;
                    using (var Response = await client.GetAsync(endpoint))
                    {
                        if (Response.StatusCode == System.Net.HttpStatusCode.OK)
                        {
                            var jsonObject = Response.Content.ReadAsStringAsync().Result;
                            _log4net.Info("Get latest Event Id" + jsonObject);
                            await GetMulitpleEventlist(token, jsonObject);
                            return jsonObject;
                        }
                        else
                        {
                            ModelState.Clear();
                            ModelState.AddModelError(string.Empty, "Bad Request");
                            objEnquiry.SendHTTPExceptionNotification(endpoint, Response.StatusCode);
                            return Ok("Bad Request");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _log4net.Error("Error Occured while getting latest event id" + ex.Message);
                return StatusCode(HttpContext.Response.StatusCode, ex.Message);
                throw;
            }
        }
        public async Task<ActionResult<string>> GetMulitpleEventlist(string token, string eventId)
        {

            try
            {
                MESPASEnquiry objEnquiry = new MESPASEnquiry(_Configuration);
                string apiBaseUrl = string.Empty;
                apiBaseUrl = this._Configuration.GetSection("APIURL")["EventCallByEventID"];
                HttpClientHandler clientHandler = new HttpClientHandler();
                clientHandler.ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => { return true; };
                using (HttpClient client = new HttpClient(clientHandler))
                {

                    string tokenValue = token;
                    client.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", token);
                    StringContent content = new StringContent("application/json");
                    long id = Convert.ToInt64(eventId);
                    Dictionary<string, long> query = new Dictionary<string, long>();
                    query.Add("last_received", id);
                    string endpoint = string.Format(apiBaseUrl, "last_received", id);
                    using (var Response = await client.GetAsync(endpoint))
                    {
                        if (Response.StatusCode == System.Net.HttpStatusCode.OK)
                        {
                            var jsonObject = Response.Content.ReadAsStringAsync().Result;
                            var jsonobj = System.Text.Json.JsonSerializer.Deserialize<IList<Enquirydata>>(jsonObject);
                            System.Diagnostics.Trace.WriteLine(jsonobj);
                            foreach (var data in jsonobj)
                            {
                                await UpdateLatestEventId(data.eventId);
                                if (data.eventType.ToUpper() == "REQUEST_CREATED")
                                {
                                    _log4net.Info("Get eventId respected of request created" + data.entityId);
                                    await GetEnquiryDtls(tokenValue, data.entityId, data.eventId);
                                }
                            }
                            return jsonObject;
                        }
                        else
                        {
                            ModelState.Clear();
                            ModelState.AddModelError(string.Empty, "Bad Request");
                            objEnquiry.SendMESPASHTTPExceptionNotification(endpoint, Response.StatusCode);
                            return Ok("Bad Request");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _log4net.Error("Error Occured while getting Request Created event list" + ex.Message);
                return StatusCode(HttpContext.Response.StatusCode, ex.Message);
                throw;
            }
        }
        public async Task<ContentResult> GetActiveCustomerlist(string objenquiry)
        {
            try
            {
                MESPASEnquiry objEnquiry = new MESPASEnquiry(_Configuration);
                string apiBaseUrl = string.Empty;
                apiBaseUrl = this._Configuration.GetSection("APIURL")["GetActiveCustomer"];
                HttpClientHandler clientHandler = new HttpClientHandler();
                clientHandler.ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => { return true; };
                using (HttpClient client = new HttpClient(clientHandler))
                {
                    string myJson = string.Empty;
                    var json = JsonConvert.SerializeObject(objenquiry);
                    HttpContent content = new StringContent(objenquiry, Encoding.UTF8, "application/json");
                    string name = objenquiry;
                    string endpoint = string.Format(apiBaseUrl, name);
                    using (var Response = await client.GetAsync(endpoint))
                    {
                        _log4net.Info("Get Active Customer " + objenquiry);
                        if (Response.StatusCode == System.Net.HttpStatusCode.OK)
                        {
                            var a = Response.Content.ReadAsStringAsync().Result;
                            _log4net.Info("After Get Active Customer " + JsonConvert.SerializeObject(a));
                            return Content(JsonConvert.SerializeObject(a));
                        }
                        else
                        {
                            ModelState.Clear();
                            ModelState.AddModelError(string.Empty, "Bad Request");
                            return Content("Bad Request");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _log4net.Error("Error Occured while getting active customer" + ex.Message);
                return Content(ex.Message);
                throw;
            }
        }
        public async Task<ActionResult<string>> GetEnquiryDtls(string token, string entityId, long eventId)
        {
            try
            {
                string defaultaccountCode = this._Configuration.GetSection("DefaultAccountCode")["AccountCode"];
                string PartNo = this._Configuration.GetSection("SpecificationDetails")["PartNo"];
                string ItemNo = this._Configuration.GetSection("SpecificationDetails")["ItemNo"];
                string DrawingNo = this._Configuration.GetSection("SpecificationDetails")["DrawingNo"];
                string KeyNo = this._Configuration.GetSection("SpecificationDetails")["KeyNo"];
                #region
                string rfqURL = this._Configuration.GetSection("RFQUrl")["rfqUrl"];
                #endregion
                string UpdatedPartNo = "";
                string UpdatedItemNo = "";
                string UpdatedDrawingNo = "";
                string UpdatedKeyNo = "";
                MESPASEnquiry objEnquiry = new MESPASEnquiry(_Configuration);
                string docpath = "";
                string apiBaseUrl = string.Empty;
                apiBaseUrl = this._Configuration.GetSection("APIURL")["EnquiryDtlsAPI"];
                HttpClientHandler clientHandler = new HttpClientHandler();
                clientHandler.ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => { return true; };
                using (HttpClient client = new HttpClient(clientHandler))
                {
                    string tokenValue = token;
                    client.DefaultRequestHeaders.Authorization =
                   new AuthenticationHeaderValue("Bearer", token);
                    string endpoint = (string.Format(apiBaseUrl, entityId));
                    using (var Response = await client.GetAsync(endpoint))
                    {
                        if (Response.StatusCode == System.Net.HttpStatusCode.OK)
                        {
                            var jsonObject = Response.Content.ReadAsStringAsync().Result;
                            var jsonobj = System.Text.Json.JsonSerializer.Deserialize<Enquirydata>(jsonObject);
                            if (jsonobj.status.ToUpper() == "NEW")
                            {
                                var jsondata = JObject.Parse(jsonObject);
                                if (jsondata != null)
                                {
                                    ContentResult customerlist = await GetActiveCustomerlist(jsondata["buyer"]["company"]["name"].ToString());
                                    string ownernamefromjson = jsondata["buyer"]["company"]["name"].ToString();
                                    _log4net.Info("Customer list" + customerlist.Content.Replace("\"", "").Trim().ToUpper()
                                        + ownernamefromjson.ToString());
                                    if (ownernamefromjson.ToUpper() == customerlist.Content.Replace("\"", "").Trim().ToUpper())
                                    {
                                        //create text file
                                        try
                                        {
                                            docpath = objEnquiry.createRFQFile(jsondata);
                                        }
                                        catch (Exception ex)
                                        {
                                            _log4net.Info("Error Occured while Saving attachment" + ex.Message);

                                        }
                                        _log4net.Info("Get enquiry details for respected event id" + jsondata);

                                        string enquiryDate = DateTime.Now.ToString("MM/dd/yyyy HH:mm:ss");
                                        string owner = jsondata["buyer"]["company"]["name"].ToString();
                                        int status = 1;
                                        string shipName = jsondata["vessel"]["name"].ToString();
                                        string enqrefNo = jsondata["requestTerms"]["referenceNumber"].ToString();
                                        string emailRecivedDt = jsondata["requestTerms"]["requestDate"].ToString();
                                        string sourceType = "MESPAS";
                                        string emailProcesseddt = DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss");
                                        string accountCode = defaultaccountCode.ToString();
                                        string equipment = jsondata["product"]["productInstance"]["label"].ToString();
                                        string type = jsondata["product"]["name"].ToString();
                                        string maker = jsondata["product"]["brand"]["name"].ToString();
                                        string serialNumber = jsondata["product"]["productInstance"]["serialNumber"].ToString();
                                        string itemdtls = jsondata["items"].ToString();
                                        string attachmentdtls = jsondata["attachments"].ToString();
                                        var jsonitemdtls = System.Text.Json.JsonSerializer.Deserialize<IList<EnquiryDetails>>(itemdtls);
                                        var jsonattachmentdtls = System.Text.Json.JsonSerializer.Deserialize<IList<EnquiryHeaderDocDetails>>(attachmentdtls);
                                        Enquiryheader objhdr = new Enquiryheader();
                                        {
                                            objhdr.enquiryDate = enquiryDate;
                                            objhdr.owner = owner;
                                            objhdr.status = status.ToString();
                                            objhdr.shipName = shipName;
                                            objhdr.enqrefNo = enqrefNo;
                                            objhdr.sourceType = sourceType;
                                            objhdr.maker = maker;
                                            objhdr.type = type;
                                            objhdr.equipment = equipment;
                                            objhdr.emailReceivedat = emailRecivedDt;
                                            objhdr.emailProcessedat = emailProcesseddt;
                                            objhdr.docPath = docpath;
                                            objhdr.serialNo = serialNumber;
                                            objhdr.rfqUrl = rfqURL + entityId;
                                            List<EnquiryDetails> lstdtl = new List<EnquiryDetails>();
                                            List<EnquiryHeaderDocDetails> lstdoc = new List<EnquiryHeaderDocDetails>();
                                            List<string> lstspecificationdtl = new List<string>();
                                            int SeqNo = 0;
                                            foreach (var data in jsonitemdtls)
                                            {
                                                SeqNo = SeqNo + 1;
                                                EnquiryDetails objdtl = new EnquiryDetails();
                                                {
                                                    objdtl.quantity = data.quantity;
                                                    objdtl.unit = data.unit;
                                                    objdtl.cost = "";
                                                    objdtl.status = status.ToString();
                                                    objdtl.seqNo = SeqNo.ToString();
                                                    objdtl.accountNo = accountCode;
                                                    objdtl.remark = data.remark;
                                                    objdtl.IsUpdatedMESPASItemsWithML = "1";
                                                    objdtl.name = data.component.name;
                                                    string execution = "";
                                                    if (!String.IsNullOrEmpty(data.component.execution))
                                                    {
                                                         execution = data.component.execution;
                                                    }
                                                    string allspecificationdetails = "";
                                                    foreach (var output in data.component.specificationDetails)
                                                    {
                                                        #region
                                                        if (maker.Contains("Customer Specific"))
                                                        {
                                                            if (output.name.Contains("Maker"))
                                                            {
                                                                objhdr.maker = output.value;
                                                            }
                                                        }
                                                        #endregion
                                                        #region
                                                        if (objhdr.serialNo == "")
                                                        {
                                                            if (output.name.Contains("Dwg. No."))
                                                            {
                                                                if (output.value != "" || output.value != null)
                                                                {
                                                                    _log4net.Info("serial number" + output.value);

                                                                    objhdr.serialNo = output.value;
                                                                }
                                                            }
                                                        }
                                                        #endregion
                                                        #region
                                                        if (output.name.Contains("Item"))
                                                        {
                                                            if (!String.IsNullOrEmpty(output.value))
                                                            {
                                                                UpdatedItemNo = ItemNo + " " + output.value;
                                                            }
                                                        }
                                                        #endregion
                                                        if (output.name.Contains("Part"))
                                                        {
                                                            if (!String.IsNullOrEmpty(output.value))
                                                            {
                                                                UpdatedPartNo = PartNo + " " + output.value;
                                                            }
                                                        }
                                                        if (output.name.Contains("Dwg"))
                                                        {
                                                            if (!String.IsNullOrEmpty(output.value))
                                                            {
                                                                UpdatedDrawingNo = DrawingNo + " " + output.value;
                                                            }
                                                        }
                                                        if (output.name.Contains("Key No"))
                                                        {
                                                            if (!String.IsNullOrEmpty(output.value))
                                                            {
                                                                UpdatedKeyNo = KeyNo + " " + output.value;
                                                            }
                                                        }
                                                        if ((output.value != "" || output.value != null) && !(output.name.Contains("Item") || output.name.Contains("Part") || output.name.Contains("Dwg") || output.name.Contains("Key No")))
                                                        {
                                                            allspecificationdetails = allspecificationdetails + " " + output.value;
                                                        }
                                                    }
                                                    string spedtls = allspecificationdetails;
                                                    if (!String.IsNullOrEmpty(objdtl.remark))
                                                    {
                                                        objdtl.partName = objdtl.name + " " + execution + " " + spedtls + " " + UpdatedItemNo + " " + UpdatedPartNo + " " + UpdatedDrawingNo + "Remark " + objdtl.remark + " " + UpdatedKeyNo;
                                                        objdtl.partName = Regex.Replace(objdtl.partName, @"\t|\n|\r", " ");
                                                    }
                                                    else if (!String.IsNullOrEmpty(objdtl.name))
                                                    {
                                                        objdtl.partName = objdtl.name + " " + execution + " " + spedtls + " " + UpdatedItemNo + " " + UpdatedPartNo + " " + UpdatedDrawingNo + " " + UpdatedKeyNo;
                                                        objdtl.partName = Regex.Replace(objdtl.partName, @"\t|\n|\r", " ");
                                                    }
                                                    else
                                                    {
                                                        objdtl.partName = objdtl.name + " " + execution + " " + spedtls;
                                                        objdtl.partName = Regex.Replace(objdtl.partName, @"\t|\n|\r", " ");
                                                    }
                                                    objdtl.partCode = "";
                                            }
                                                lstdtl.Add(objdtl);
                                            }

                                            objhdr.itemDetails = lstdtl;
                                            objhdr.serialNo = objhdr.serialNo;
                                            foreach (var data in jsonattachmentdtls)
                                            {
                                                ContentResult headerfilePath = null;
                                                if (jsonattachmentdtls != null)
                                                {
                                                    if (data.fileId != "" || data.fileId != null)
                                                    {
                                                        headerfilePath = await GetAttahcmentlist(token, data.fileId, data.fileName, enqrefNo, owner);
                                                    }
                                                    EnquiryHeaderDocDetails objDoc = new EnquiryHeaderDocDetails();
                                                    {
                                                        if (headerfilePath.Content.Contains(".pdf") ||
                                                            headerfilePath.Content.Contains(".jpeg") ||
                                                            headerfilePath.Content.Contains(".xlsx") ||
                                                            headerfilePath.Content.Contains(".xls") ||
                                                            headerfilePath.Content.Contains(".png") ||
                                                            headerfilePath.Content.Contains(".jpg"))
                                                        {
                                                            objDoc.docPath = headerfilePath.Content;
                                                        }
                                                        else
                                                        {
                                                            objDoc.errorDescription = headerfilePath.Content;
                                                        }

                                                        lstdoc.Add(objDoc);
                                                    }
                                                }
                                            }
                                            objhdr.docHdrDetails = lstdoc;

                                            string msdItemsresult = await GetMSDItemsList();
                                            string snqItemstresult = await GetSNQItemsList();
                                            string Makerresult = await GetMakerList();
                                            string Equipmentresult = await GetEquipmentList();
                                            var lstmsditemsdata = System.Text.Json.JsonSerializer.Deserialize<IList<MSDItemData>>(msdItemsresult);
                                            var lstsnqItemsData = System.Text.Json.JsonSerializer.Deserialize<IList<SNQItemData>>(snqItemstresult);
                                            var lstmakerData = System.Text.Json.JsonSerializer.Deserialize<IList<MakerData>>(Makerresult);
                                            var lstequipmentData = System.Text.Json.JsonSerializer.Deserialize<IList<EquipmentData>>(Equipmentresult);
                                            bool equipmentresult = false;
                                            bool makerresult = false;
                                            bool msditem = false;
                                            bool snqitem = false;
                                            string MLEquipmentLogic = this._Configuration.GetSection("MLLogic")["EquipmentMLAllow"];
                                            if (!String.IsNullOrEmpty(objhdr.maker) && !String.IsNullOrEmpty(objhdr.equipment))// || !equipment.Contains("Non Specific Equipment") || !maker.Contains("Customer Specific"))
                                            {
                                                // equipment checking 
                                                //if it is match then go to next steps
                                                if (!objhdr.equipment.Contains("Non Specific Equipment"))
                                                {
                                                        string equipmentName = objhdr.equipment;
                                                        if (MLEquipmentLogic == "YES")
                                                        {
                                                            if (!string.IsNullOrEmpty(equipmentName))
                                                            {
                                                                double percentage = 0;
                                                                string matchingPercent = this._Configuration.GetSection("MatchingPercentEquipment")["MATCHINGVALUE"];

                                                                for (int counter = 0; counter < lstequipmentData.Count; counter++)
                                                                {
                                                                    var lcs = equipmentName.ToUpper().LongestCommonSubsequence(lstequipmentData[counter].equipmentName.ToUpper());
                                                                  
                                                                    if (lcs.Item2 > double.Parse(matchingPercent) && percentage < lcs.Item2)
                                                                    {
                                                                        percentage = lcs.Item2;
                                                                        equipmentName = lstequipmentData[counter].equipmentName;
                                                                        objhdr.IsUpdatedEquipmentWithML = 1;
                                                                        equipmentresult = true;
                                                                    }
                                                                }

                                                            }
                                                    }

                                                }
                                                // maker checking 
                                                //  if match got to next steps

                                                string MakerMLLogic = this._Configuration.GetSection("MLLogic")["MakerMLAllow"];
                                                //ML Logic For Items
                                                #region ML Logic to find part name 
                                                if (!objhdr.maker.Contains("Customer Specific"))
                                                {
                                                        string makerName = objhdr.maker;
                                                        if (MakerMLLogic == "YES")
                                                        {
                                                            if (!string.IsNullOrEmpty(makerName))
                                                            {
                                                                double percentage = 0;
                                                                string matchingPercent = this._Configuration.GetSection("MatchingPercentMaker")["MATCHINGVALUE"];
                                                                for (int counter = 0; counter < lstmakerData.Count; counter++)
                                                                {
                                                                    var lcs = makerName.ToUpper().LongestCommonSubsequence(lstmakerData[counter].makerName.ToUpper());
                                                                    _log4net.Info("maker maching percent " + lcs.Item2);
                                                                    if (lcs.Item2 > double.Parse(matchingPercent) && percentage < lcs.Item2)
                                                                    {
                                                                      percentage = lcs.Item2;
                                                                    objhdr.IsUpdatedMakerWithML = 1;
                                                                    makerName = lstmakerData[counter].makerName;
                                                                        makerresult = true;
                                                                    }
                                                                }
                                                            }
                                                        }
                                                        #endregion ML Logic to find IMPA code
                                                        //ML Logic For PartCode/Impa Code
                                                    

                                                }
                                                string ItemsMLLogic = this._Configuration.GetSection("MLLogic")["ItemsMLAllow"];
                                                if (equipmentresult || makerresult)
                                                {
                                                    // MSD items checking 
                                                    // if match got to send to msd if it is not match go to next step

                                                    //ML Logic For Items
                                                    #region ML Logic to find part name 
                                                    foreach (var data in jsonitemdtls)
                                                    {
                                                        string itemName = data.component.name;
                                                        _log4net.Info("maching item name  " + itemName);
                                                        if (ItemsMLLogic == "YES")
                                                        {
                                                            if (!string.IsNullOrEmpty(itemName))
                                                            {
                                                                double percentage = 0;
                                                                string matchingPercent = this._Configuration.GetSection("MatchingPercent")["MATCHINGVALUE"];
                                                                for (int counter = 0; counter < lstmsditemsdata.Count; counter++)
                                                                {
                                                                    var lcs = itemName.ToUpper().LongestCommonSubsequence(lstmsditemsdata[counter].itemName.ToUpper());
                                                                    if (lcs.Item2 > double.Parse(matchingPercent))
                                                                    {
                                                                        percentage = lcs.Item2;
                                                                        itemName = lstmsditemsdata[counter].itemName;
                                                                        msditem = true;
                                                                    }

                                                                }
                                                            }

                                                        }
                                                        #endregion ML Logic to find IMPA code
                                                        //ML Logic For PartCode/Impa Code
                                                    }
                                                    // SNQ items checking 
                                                    // if match match send to snq if it is not match send to mespas
                                                    //ML Logic For Items
                                                    #region ML Logic to find IMPA code
                                                    if (!msditem)
                                                    {
                                                        foreach (var data in jsonitemdtls)
                                                        {
                                                            string itemName = data.component.name;

                                                            if (ItemsMLLogic == "YES")
                                                            {
                                                                if (!string.IsNullOrEmpty(itemName))
                                                                {
                                                                    double percentage = 0;
                                                                    string matchingPercent = this._Configuration.GetSection("MatchingPercent")["MATCHINGVALUE"];

                                                                    for (int counter = 0; counter < lstsnqItemsData.Count; counter++)
                                                                    {
                                                                        var lcs = itemName.ToUpper().LongestCommonSubsequence(lstsnqItemsData[counter].itemName.ToUpper());
                                                                        if (lcs.Item2 > double.Parse(matchingPercent))
                                                                        {
                                                                            percentage = lcs.Item2;
                                                                            itemName = lstsnqItemsData[counter].itemName;

                                                                            snqitem = true;
                                                                        }
                                                                    }

                                                                }

                                                            }
                                                            #endregion ML Logic to find IMPA code
                                                            //ML Logic For PartCode/Impa Code
                                                        }
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                _log4net.Info("SEND TO SNQ ");
                                                snqitem = true;
                                            }
                                            if (msditem)
                                            {
                                                objhdr.eventId = eventId;
                                                _log4net.Info("msd event id " + eventId);
                                                await SendMSDEnquiryData(objhdr);
                                            }
                                            else if (snqitem)
                                            {
                                                objhdr.eventId = eventId;
                                                _log4net.Info("snq event id " + eventId);
                                                await SendSNQEnquiryData(objhdr);
                                            }
                                            else
                                            {
                                                objhdr.eventId = eventId;
                                                _log4net.Info("mespas event id " + eventId);
                                                await SendEnquiryData(objhdr);
                                            }
                                        }
                                    }
                                    else
                                    {
                                       objEnquiry.CustomerNotMapped(ownernamefromjson);
                                    }
                                }
                            }
                        }
                        else
                        {
                            ModelState.Clear();
                            ModelState.AddModelError(string.Empty, "Bad Request");
                            objEnquiry.SendMESPASHTTPExceptionNotification(endpoint, Response.StatusCode);
                            return Ok("Bad Request");
                        }
                        return token;
                    }
                }
            }
            catch (Exception ex)
            {
                _log4net.Error("Error Occured while getting enquiry details for respected event id" + ex.Message);
                return StatusCode(HttpContext.Response.StatusCode, ex.Message);
                throw;
            }
        }
        public async Task<ActionResult<string>> SendEnquiryData(Object objEnqdtl)
        {
            MESPASEnquiry objEnquiry = new MESPASEnquiry(_Configuration);
            try
            {
                string apiBaseUrl = string.Empty;
                apiBaseUrl = this._Configuration.GetSection("APIURL")["SaveAPI"];
                HttpClientHandler clientHandler = new HttpClientHandler();
                clientHandler.ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => { return true; };
                using (HttpClient client = new HttpClient(clientHandler))
                {
                    string myJson = string.Empty;
                    var json = JsonConvert.SerializeObject(objEnqdtl);
                    HttpContent content = new StringContent(json, Encoding.UTF8, "application/json");
                    string endpoint = apiBaseUrl;
                    _log4net.Info("End point of MESPAS save api" + endpoint);
                    using (var Response = await client.PostAsync(endpoint, content))
                    {
                        _log4net.Info("Before Save Enquiry Data " + objEnqdtl);
                        if (Response.StatusCode == System.Net.HttpStatusCode.OK)
                        {
                            var a = Response.Content.ReadAsStringAsync().Result;
                            _log4net.Info("After Save Enquiry Data " + JsonConvert.SerializeObject(a));
                            return "OK";
                        }
                        else
                        {
                            ModelState.Clear();
                            ModelState.AddModelError(string.Empty, "Bad Request");
                            objEnquiry.SendHTTPExceptionNotification(endpoint, Response.StatusCode);
                            return Ok("Bad Request");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _log4net.Error("Error Occured while saving details for respected event id - MESPAS" + objEnqdtl);
                objEnquiry.SendNotProcessedEnquiryNotification(
                    objEnqdtl.GetType().GetProperty("").ToString(), objEnqdtl.GetType().GetProperty("").ToString(), ex.Message);
                return StatusCode(HttpContext.Response.StatusCode, ex.Message);
                throw;
            }
        }
        public async Task<ActionResult<string>> UpdateLatestEventId(long objenquiry)
        {
            try
            {
                MESPASEnquiry objEnquiry = new MESPASEnquiry(_Configuration);
                string apiBaseUrl = string.Empty;
                apiBaseUrl = this._Configuration.GetSection("APIURL")["UpdateEventId"];
                HttpClientHandler clientHandler = new HttpClientHandler();
                clientHandler.ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => { return true; };
                using (HttpClient client = new HttpClient(clientHandler))
                {
                    string myJson = string.Empty;
                    var json = JsonConvert.SerializeObject(objenquiry);
                    HttpContent content = new StringContent(json, Encoding.UTF8, "application/json");
                    string endpoint = string.Format(apiBaseUrl, objenquiry);
                    using (var Response = await client.PostAsync(endpoint, content))
                    {
                        _log4net.Info("Before Update latested Event ID " + objenquiry);
                        if (Response.StatusCode == System.Net.HttpStatusCode.OK)
                        {
                            var a = Response.Content.ReadAsStringAsync().Result;
                            _log4net.Info("After Update latested Event ID " + JsonConvert.SerializeObject(a));
                            return a;
                        }
                        else
                        {
                            ModelState.Clear();
                            ModelState.AddModelError(string.Empty, "Bad Request");
                            objEnquiry.SendHTTPExceptionNotification(endpoint, Response.StatusCode);
                            return Ok("Bad Request");

                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _log4net.Error("Error Occured while  updating latest event id" + ex.Message);
                return StatusCode(HttpContext.Response.StatusCode, ex.Message);
                throw;
            }
        }
        public async Task<ContentResult> GetAttahcmentlist(string token, string fileId, string fileName, string enqNo, string owner)
        {
            try
            {
                MESPASEnquiry objEnquiry = new MESPASEnquiry(_Configuration);
                string apiBaseUrl = string.Empty;
                apiBaseUrl = this._Configuration.GetSection("APIURL")["GetAttachmentDetails"];
                string ProcessedFilePath = this._Configuration.GetSection("RFQFile")["ProcessedHdrfilePath"];
                string NotProcessedFilePath = this._Configuration.GetSection("RFQFile")["NotProcessedfilePath"];
                HttpClientHandler clientHandler = new HttpClientHandler();
                clientHandler.ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => { return true; };
                using (HttpClient client = new HttpClient(clientHandler))
                {
                    string tokenValue = token;
                    client.DefaultRequestHeaders.Authorization =
                   new AuthenticationHeaderValue("Bearer", token);
                    StringContent content = new StringContent("application/json");
                    string id = fileId;
                    string endpoint = string.Format(apiBaseUrl, id);
                    using (var Response = await client.GetAsync(endpoint))
                    {
                        if (Response.StatusCode == System.Net.HttpStatusCode.OK)
                        {
                            var output = Response.Content.ReadAsByteArrayAsync().Result;
                            string path = "";
                            if (fileName.Contains(".pdf"))
                            {
                                var dt1 = DateTime.Now.ToString("dd-MM-yyyy");
                                string pathString = System.IO.Path.Combine(ProcessedFilePath, owner, enqNo.Replace("/", "-"));
                                Directory.CreateDirectory(pathString);
                                string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);
                                path = pathString + "\\" + fileNameWithoutExtension + "_" + dt1 + ".pdf";
                                var notprocessedpath = NotProcessedFilePath + fileNameWithoutExtension + "_" + dt1 + ".pdf";
                                if (!System.IO.File.Exists(path))
                                {
                                    System.IO.File.WriteAllBytes(path, output);
                                }
                                else
                                {
                                    System.IO.File.Move(path, notprocessedpath);
                                }
                                return Content(path);
                            }
                            else if (fileName.Contains("xls"))
                            {
                                var dt1 = DateTime.Now.ToString("dd-MM-yyyy");
                                path = System.IO.Path.Combine(ProcessedFilePath, owner, enqNo.Replace("/", "-"));
                                Directory.CreateDirectory(path);
                                string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);
                                path = path + "\\" + fileNameWithoutExtension + "_" + dt1 + ".xls";
                                var notprocessedpath = NotProcessedFilePath + fileNameWithoutExtension + "_" + dt1 + ".xls";
                                if (!System.IO.File.Exists(path))
                                {
                                    System.IO.File.WriteAllBytes(path, output);
                                }
                                else
                                {
                                    System.IO.File.Move(path, notprocessedpath);
                                }
                                return Content(path);
                            }
                            else
                            {
                                var dt1 = DateTime.Now.ToString("dd-MM-yyyy");
                                path = System.IO.Path.Combine(ProcessedFilePath, owner, enqNo.Replace("/", "-"));
                                Directory.CreateDirectory(path);
                                string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);
                                path = path + "\\" + fileNameWithoutExtension + "_" + dt1 + ".jpg";
                                var notprocessedpath = NotProcessedFilePath + fileNameWithoutExtension + "_" + dt1 + ".jpg";
                                if (!System.IO.File.Exists(path))
                                {
                                    System.IO.File.WriteAllBytes(path, output);
                                }
                                else
                                {
                                    System.IO.File.Move(path, notprocessedpath);
                                }
                                return Content(path);
                            }
                        }
                        else
                        {
                            ModelState.Clear();
                            ModelState.AddModelError(string.Empty, "Bad Request");
                            objEnquiry.SendMESPASHTTPExceptionNotification(endpoint, Response.StatusCode);
                            return Content("Bad Request");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _log4net.Error("Error Occured while getting document list" + ex.Message);
                return Content(ex.Message);
                throw;
            }
        }
        // now we are not considering below code
        public async Task<string> GetMSDItemsList()
        {
            try
            {
                MESPASEnquiry objEnquiry = new MESPASEnquiry(_Configuration);
                string apiBaseUrl = string.Empty;
                apiBaseUrl = this._Configuration.GetSection("APIURL")["GetMSDItemsList"];
                HttpClientHandler clientHandler = new HttpClientHandler();
                clientHandler.ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => { return true; };
                using (HttpClient client = new HttpClient(clientHandler))
                {
                    string myJson = string.Empty;
                    string endpoint = apiBaseUrl;
                    using (var Response = await client.GetAsync(endpoint))
                    {
                        if (Response.StatusCode == System.Net.HttpStatusCode.OK)
                        {
                            var a = Response.Content.ReadAsStringAsync().Result;
                            return a;
                        }
                        else
                        {
                            ModelState.Clear();
                            ModelState.AddModelError(string.Empty, "Bad Request");
                            objEnquiry.SendHTTPExceptionNotification(endpoint, Response.StatusCode);
                            return ("Bad Request");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _log4net.Error("Error Occured Getting spare part (msd items)categorisation" + ex.Message);
                return (ex.Message);
                throw;
            }
        }
        public async Task<string> GetSNQItemsList()
        {
            try
            {
                MESPASEnquiry objEnquiry = new MESPASEnquiry(_Configuration);
                string apiBaseUrl = string.Empty;
                apiBaseUrl = this._Configuration.GetSection("APIURL")["GetSNQItemsList"];
                HttpClientHandler clientHandler = new HttpClientHandler();
                clientHandler.ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => { return true; };
                using (HttpClient client = new HttpClient(clientHandler))
                {
                    string myJson = string.Empty;
                    string endpoint = apiBaseUrl;
                    using (var Response = await client.GetAsync(endpoint))
                    {
                        if (Response.StatusCode == System.Net.HttpStatusCode.OK)
                        {
                            var a = Response.Content.ReadAsStringAsync().Result;
                            return a;
                        }
                        else
                        {
                            ModelState.Clear();
                            ModelState.AddModelError(string.Empty, "Bad Request");
                            objEnquiry.SendHTTPExceptionNotification(endpoint, Response.StatusCode);
                            return ("Bad Request");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _log4net.Error("Error Occured Getting store part(snq items) categorisation" + ex.Message);
                return (ex.Message);
                throw;
            }
        }
        public async Task<string> GetEquipmentList()
        {
            try
            {
                MESPASEnquiry objEnquiry = new MESPASEnquiry(_Configuration);
                string apiBaseUrl = string.Empty;
                apiBaseUrl = this._Configuration.GetSection("APIURL")["GetEquipmentList"];
                HttpClientHandler clientHandler = new HttpClientHandler();
                clientHandler.ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => { return true; };
                using (HttpClient client = new HttpClient(clientHandler))
                {
                    string myJson = string.Empty;
                    string endpoint = apiBaseUrl;
                    using (var Response = await client.GetAsync(endpoint))
                    {
                        if (Response.StatusCode == System.Net.HttpStatusCode.OK)
                        {
                            var a = Response.Content.ReadAsStringAsync().Result;
                            return a;
                        }
                        else
                        {
                            ModelState.Clear();
                            ModelState.AddModelError(string.Empty, "Bad Request");
                            objEnquiry.SendHTTPExceptionNotification(endpoint, Response.StatusCode);
                            return ("Bad Request");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _log4net.Error("Error Occured Getting equipment list" + ex.Message);
                return (ex.Message);
                throw;
            }
        }
        public async Task<string> GetMakerList()
        {
            try
            {
                MESPASEnquiry objEnquiry = new MESPASEnquiry(_Configuration);
                string apiBaseUrl = string.Empty;
                apiBaseUrl = this._Configuration.GetSection("APIURL")["GetMakerList"];
                HttpClientHandler clientHandler = new HttpClientHandler();
                clientHandler.ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => { return true; };
                using (HttpClient client = new HttpClient(clientHandler))
                {
                    string myJson = string.Empty;
                    string endpoint = apiBaseUrl;
                    using (var Response = await client.GetAsync(endpoint))
                    {
                        if (Response.StatusCode == System.Net.HttpStatusCode.OK)
                        {
                            var a = Response.Content.ReadAsStringAsync().Result;
                            return a;
                        }
                        else
                        {
                            ModelState.Clear();
                            ModelState.AddModelError(string.Empty, "Bad Request");
                            objEnquiry.SendHTTPExceptionNotification(endpoint, Response.StatusCode);
                            return ("Bad Request");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _log4net.Error("Error Occured Getting maker list" + ex.Message);
                return (ex.Message);
                throw;
            }
        }
        public async Task<ActionResult<string>> SendMSDEnquiryData(Object objEnqdtl)
        {
            MESPASEnquiry objEnquiry = new MESPASEnquiry(_Configuration);
            try
            {
                string apiBaseUrl = string.Empty;
                apiBaseUrl = this._Configuration.GetSection("APIURL")["MSDSaveAPI"];
                HttpClientHandler clientHandler = new HttpClientHandler();
                clientHandler.ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => { return true; };
                using (HttpClient client = new HttpClient(clientHandler))
                {
                    string myJson = string.Empty;
                    var json = JsonConvert.SerializeObject(objEnqdtl);
                    HttpContent content = new StringContent(json, Encoding.UTF8, "application/json");
                    string endpoint = apiBaseUrl;
                    using (var Response = await client.PostAsync(endpoint, content))
                    {
                        _log4net.Info("Before Save Enquiry Data " + objEnqdtl);
                        if (Response.StatusCode == System.Net.HttpStatusCode.OK)
                        {
                            var a = Response.Content.ReadAsStringAsync().Result;
                            _log4net.Info("After Save Enquiry Data " + JsonConvert.SerializeObject(a));
                            return "OK";
                        }
                        else
                        {
                            ModelState.Clear();
                            ModelState.AddModelError(string.Empty, "Bad Request");
                            objEnquiry.SendHTTPExceptionNotification(endpoint, Response.StatusCode);
                            return Ok("Bad Request");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _log4net.Error("Error Occured while saving details for respected event id - MSD" + objEnqdtl);
                objEnquiry.SendNotProcessedEnquiryNotification(
                    objEnqdtl.GetType().GetProperty("").ToString(), objEnqdtl.GetType().GetProperty("").ToString(), ex.Message);
                return StatusCode(HttpContext.Response.StatusCode, ex.Message);
                throw;
            }
        }
        public async Task<ActionResult<string>> SendSNQEnquiryData(Object objEnqdtl)
        {
            MESPASEnquiry objEnquiry = new MESPASEnquiry(_Configuration);
            try
            {
                string apiBaseUrl = string.Empty;
                apiBaseUrl = this._Configuration.GetSection("APIURL")["SNQSaveAPI"];
                HttpClientHandler clientHandler = new HttpClientHandler();
                clientHandler.ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => { return true; };
                using (HttpClient client = new HttpClient(clientHandler))
                {
                    string myJson = string.Empty;
                    var json = JsonConvert.SerializeObject(objEnqdtl);
                    HttpContent content = new StringContent(json, Encoding.UTF8, "application/json");
                    string endpoint = apiBaseUrl;
                    using (var Response = await client.PostAsync(endpoint, content))
                    {
                        _log4net.Info("Before Save Enquiry Data " + objEnqdtl);
                        if (Response.StatusCode == System.Net.HttpStatusCode.OK)
                        {
                            var a = Response.Content.ReadAsStringAsync().Result;
                            _log4net.Info("After Save Enquiry Data " + JsonConvert.SerializeObject(a));
                            return "OK";
                        }
                        else
                        {
                            ModelState.Clear();
                            ModelState.AddModelError(string.Empty, "Bad Request");
                            objEnquiry.SendHTTPExceptionNotification(endpoint, Response.StatusCode);
                            return Ok("Bad Request");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _log4net.Error("Error Occured while saving details for respected event id - SNQ" + objEnqdtl);
                objEnquiry.SendNotProcessedEnquiryNotification(
                    objEnqdtl.GetType().GetProperty("").ToString(), objEnqdtl.GetType().GetProperty("").ToString(), ex.Message);
                return StatusCode(HttpContext.Response.StatusCode, ex.Message);
                throw;
            }
        }
    }
}
