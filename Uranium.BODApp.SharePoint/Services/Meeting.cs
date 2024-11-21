using CamlBuilder;
using Microsoft.SharePoint;
using Microsoft.SharePoint.Administration;
using Microsoft.SharePoint.Meetings;
using Microsoft.SharePoint.Utilities;
using Microsoft.Web.Hosting.Administration;
using Newtonsoft.Json.Linq;
using NReco.PdfGenerator;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Mail;
using System.Net.Security;
using System.Runtime.ConstrainedExecution;
using System.Runtime.Remoting.Contexts;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web.Configuration;
using System.Web.UI.WebControls;
using Uranium.BODApp.SharePoint.Layouts.Uranium.BODApp.SharePoint;
using Uranium.BODApp.SharePoint.Model;
using Uranium.BODApp.SharePoint.Models;
using Uranium.BODApp.SharePoint.Service;
using Uranium.BODApp.SharePoint.Shared;

namespace Uranium.BODApp.SharePoint.Services
{
    public class Meeting
    {
        public static Response GetMeetingById(int id)
        {
            Response response = new Response();
            Response meetingItemResult = new Response();
            Response itemCommentResult = new Response();
            Response meetingReportsDocumentResult = new Response();
            MeetingItemModel meetingItemtemp = new MeetingItemModel();
            ItemCommentModel commentTemp = new ItemCommentModel();
            MeetingReportsDocumentModel meetingReportsDocumentModel = new MeetingReportsDocumentModel();
            meetingReportsDocumentModel.MeetingId = id;
            string siteUrl = WebConfigurationManager.AppSettings[Constants.WebConfigMeetingSiteUrl];
            if (siteUrl == null && string.IsNullOrWhiteSpace(siteUrl))
            {
                siteUrl = WebConfigurationManager.AppSettings[Constants.WebConfigMeetingFBASiteUrl];
            }
            try
            {
                SPSecurity.RunWithElevatedPrivileges(delegate ()
                {
                    using (SPSite site = new SPSite(siteUrl))
                    {
                        using (SPWeb web = site.OpenWeb())
                        {
                            SPList MeetingsList = web.Lists.TryGetList(Constants.ListNameMeeting);
                            SPListItem meeting = MeetingsList.GetItemById(id);
                            if (meeting != null)
                            {
                                meetingReportsDocumentResult = Services.MeetingReportDocument.Search(meetingReportsDocumentModel);
                                MeetingModel model = new MeetingModel(meeting);
                                meetingItemtemp.MeetingId = id;
                                if (meetingReportsDocumentResult.IsSuccess)
                                {
                                    model.MeetingReportsDocumentModel = (List<MeetingReportsDocumentModel>)meetingReportsDocumentResult.Result;
                                }
                                meetingItemResult = Services.MeetingItem.Search(meetingItemtemp);
                                if (meetingItemResult.IsSuccess)
                                {
                                    model.MeetingItems = (List<MeetingItemModel>)meetingItemResult.Result;
                                }
                                if (model.MeetingItems != null)
                                {
                                    foreach (MeetingItemModel item in model.MeetingItems)
                                    {
                                        commentTemp.MeetingItemId = item.Id;
                                        itemCommentResult = Services.ItemComment.Search(commentTemp);
                                        if (itemCommentResult.IsSuccess)
                                        {
                                            item.Comments = (List<ItemCommentModel>)itemCommentResult.Result;
                                        }
                                    }
                                }
                                response.Message = "Meeting Returned Successfully";
                                response.IsSuccess = true;
                                response.Result = model;
                            }
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                response.Message = ex.Message;
                Shared.Issues.Log("Meeting.cs/GetMeetingById", ex.Message);
            }
            return response;
        }
        public static Response Search(MeetingModel meetingModel , Pagination pagination,DateTime? from,DateTime? to)
        {
            Response response = new Response();
            response.pagination = new Pagination();
            response.pagination = pagination;

            string siteUrl = WebConfigurationManager.AppSettings[Constants.WebConfigMeetingSiteUrl];
            if(siteUrl==null && string.IsNullOrWhiteSpace(siteUrl))
            {
                siteUrl = WebConfigurationManager.AppSettings[Constants.WebConfigMeetingFBASiteUrl];
            }
            try
            {
                SPSecurity.RunWithElevatedPrivileges(delegate ()
                {
                    using (SPSite site = new SPSite(siteUrl))
                    {
                        using (SPWeb web = site.OpenWeb())
                        {
                            var generator = LogicalJoin.And();
                            SPList meetingList = web.Lists.TryGetList(Constants.ListNameMeeting);
                            if (meetingModel.MeetingDate.HasValue)
                            {
                                string formattedDate = meetingModel.MeetingDate.Value.ToString("yyyy-MM-dd");
                                generator.AddStatement(Operator.Equal("MeetingDate", CamlBuilder.ValueType.DateTime, formattedDate));
                            }
                            if (from.HasValue)
                            {
                                string formattedDate = from.Value.ToString("yyyy-MM-dd");
                                generator.AddStatement(Operator.GreaterThanOrEqualTo("MeetingDate", CamlBuilder.ValueType.DateTime, formattedDate));
                            }
                            if (to.HasValue)
                            {
                                string formattedDate = to.Value.ToString("yyyy-MM-dd");
                                generator.AddStatement(Operator.LowerThanOrEqualTo("MeetingDate", CamlBuilder.ValueType.DateTime, formattedDate));
                            }
                            if (meetingModel.IsComingMeeting.HasValue)
                            {
                                if (meetingModel.IsComingMeeting == true)
                                {
                                    string formattedDate = DateTime.Now.ToString("yyyy-MM-dd");
                                    generator.AddStatement(Operator.GreaterThanOrEqualTo("MeetingDate", CamlBuilder.ValueType.DateTime, formattedDate));
                                }
                                else
                                {
                                    string formattedDate = DateTime.Now.ToString("yyyy-MM-dd");
                                    generator.AddStatement(Operator.LowerThanOrEqualTo("MeetingDate", CamlBuilder.ValueType.DateTime, formattedDate));
                                }
                            }
                            SPQuery query = new SPQuery();
                            query.Query = Query.Build(generator).OrderBy(new FieldReference("Created") { Ascending = false }).GetCaml();
                            query.Query = query.Query.Replace("Query", "");
                            if (meetingList != null)
                            {
                                SPListItemCollection itemCollection = meetingList.GetItems(query);
                                if (itemCollection.Count > 0)
                                {
                                    List<SPListItem> sPListItems = itemCollection.Cast<SPListItem>().ToList();
                                    response.pagination.TotalItems = sPListItems.Count;
                                    sPListItems = sPListItems.Skip((pagination.PageNumber - 1) * pagination.PageSize).Take(pagination.PageSize).ToList();
                                    List<MeetingModel> listItems = sPListItems.Select(v => new MeetingModel(v)).ToList();
                                    response.Result = listItems;
                                    if (listItems.Count > 0)
                                    {
                                        response.Message = "Result Returned Successfully";
                                    }
                                    else
                                    {
                                        response.Message = "Not Foundeed";
                                    }
                                }
                            }
                            response.IsSuccess = true;
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                Issues.Log("Service/Meeting.cs/Search", ex.Message);
                response.Message = ex.Message;
            }
            return response;

        }
        public static Response Save(MeetingModel meetingModel)
        {
            Response response = new Response();
            string siteURL = WebConfigurationManager.AppSettings[Constants.WebConfigMeetingSiteUrl];
            try
            {
                SPSecurity.RunWithElevatedPrivileges(delegate ()
                {
                    using (SPSite site = new SPSite(siteURL))
                    {
                        using (SPWeb web = site.OpenWeb())
                        {
                            web.AllowUnsafeUpdates = true;
                            SPList list = web.Lists.TryGetList(Constants.ListNameMeeting);
                            if (meetingModel.Id > 0)
                            {
                                if (list != null)
                                {
                                    SPItem sPItem = null;
                                    SPListItem sPListItem = list.GetItemById(meetingModel.Id);
                                    sPItem = meetingModel.ToSPItem(sPListItem);
                                    sPListItem.Update();
                                    response.Result = sPListItem.ID;
                                    response.Message = $"Item Edited successfully";
                                }
                                else
                                {
                                    response.Message = $"List Not Founded founded";
                                }
                            }
                            else
                            {
                                web.AllowUnsafeUpdates = true;
                                SPListItem sPListItem = list.Items.Add();
                                sPListItem = meetingModel.ToSPItem(sPListItem);
                                sPListItem.Update();
                                response.Result = sPListItem.ID;
                                response.Message = $"Item Added successfully";
                                ExecuteWorkFlow(sPListItem.ID, site, "MeetingProcessAutomation");
                            }
                            web.AllowUnsafeUpdates = false;
                        }
                    }
                });
                response.IsSuccess = true;
            }
            catch (Exception ex)
            {
                Shared.Issues.Log("Meeting.cs/Save", ex.Message);
                response.Message = ex.Message;
            }
            return response;
        }
        public static Response ValidateMeetingModel(MeetingModel meetingModel)
        {
            Response response = new Response();
            try
            {

                if (meetingModel.MeetingDate >= DateTime.Now)
                {
                    response.IsSuccess = true;
                }
                else
                {
                    response.Message = "Meeting date not Valid";
                    response.IsSuccess = false;
                    return response;
                }
                meetingModel.From = Regex.Replace(meetingModel.From, @"\s*:\s*", ":");
                meetingModel.To = Regex.Replace(meetingModel.To, @"\s*:\s*", ":");
                if (!string.IsNullOrWhiteSpace(meetingModel.From) && !string.IsNullOrWhiteSpace(meetingModel.To))
                {
                    DateTime fromDateTime = DateTime.ParseExact(meetingModel.From, "hh:mm tt", CultureInfo.InvariantCulture);
                    DateTime toDateTime = DateTime.ParseExact(meetingModel.To, "hh:mm tt", CultureInfo.InvariantCulture);
                    if (fromDateTime < toDateTime)
                    {
                        response.IsSuccess = true;
                    }
                    else
                    {
                        response.Message = "From || To field not valid required";
                        response.IsSuccess = false;
                        return response;
                    }
                }
                else
                {
                    response.Message = "From || To field required";
                    response.IsSuccess = false;
                    return response;
                }
            }
            catch (Exception ex)
            {
                Shared.Issues.Log("Meeting.cs/ValidateMeetingModel", ex.Message);
            }
            return response;
        }
        private static void ExecuteWorkFlow(int id, SPSite _site, string workflowName)
        {
            SPUser user = _site.SystemAccount;
            SPUserToken token = user.UserToken;
            try
            {
                using (SPSite site = new SPSite(_site.Url, token))
                {
                    using (SPWeb web = site.OpenWeb("/ar-eg/Meeting/"))
                    {
                        SPList list = web.Lists.TryGetList("Meetings");
                        if (list != null)
                        {
                            SPListItem item = list.GetItemById(id);
                            var workflowServiceManager = new Microsoft.SharePoint.WorkflowServices.WorkflowServicesManager(item.Web);
                            var workflowSubscriptionService = workflowServiceManager.GetWorkflowSubscriptionService();
                            var subscriptions = workflowSubscriptionService.EnumerateSubscriptionsByList(item.ParentList.ID);
                            foreach (var workflowSubscription in subscriptions)
                            {
                                if (workflowSubscription.Name.Equals(workflowName))
                                {
                                    var inputParameters = new Dictionary<string, object>();
                                    workflowServiceManager.GetWorkflowInstanceService().StartWorkflowOnListItem(workflowSubscription, item.ID, inputParameters);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Shared.Issues.Log("Meetings/ExecuteWorkFlow", ex.Message);
            }
        }
        public static Response DeleteAttachment(int id,string name)
        {
            Response response = new Response();
            string siteURL = WebConfigurationManager.AppSettings[Constants.WebConfigMeetingSiteUrl];
            try
            {                
                SPSecurity.RunWithElevatedPrivileges(delegate ()
                {
                    using (SPSite site = new SPSite(siteURL))
                    {
                        using (SPWeb web = site.OpenWeb())
                        {
                            SPList MeetingsList = web.Lists.TryGetList(Constants.ListNameMeeting);
                            SPListItem meeting = MeetingsList.GetItemById(id);
                            if (meeting.Attachments.Count > 0)
                            {
                                string attachmentUrl = meeting.Attachments.Cast<string>()
                                                        .FirstOrDefault(url => System.IO.Path.GetFileName(url).Equals(name, StringComparison.OrdinalIgnoreCase));
                                if (attachmentUrl != null)
                                {
                                    web.AllowUnsafeUpdates = true;
                                    meeting.Attachments.Delete(name);
                                    meeting.Update();
                                    web.AllowUnsafeUpdates = false;
                                    response.Message = "Attachment Deleted!!";
                                    response.IsSuccess = true;
                                }
                                else
                                {
                                    response.Message = "Attachment not found.";
                                }
                            }
                        }
                    }
                });
            }
            catch(Exception ex)
            {
                response.Message = ex.Message;
                Shared.Issues.Log("Meetings/DeleteAttachment", ex.Message);
            }
            return response;
        }
        public static bool SendInvitationToExternalAttendees(int meetingId, string[] externalAttendeesEmails)
        {
            Response response = new Response();
            
            string siteURL = WebConfigurationManager.AppSettings[Constants.WebConfigMeetingSiteUrl];
            try
            {
                using (SPSite site = new SPSite(siteURL))
                {
                    using (SPWeb web = site.OpenWeb())
                    {
                        string webUR = web.Url;
                        web.AllowUnsafeUpdates = true;
                        SPList meetingList = web.Lists.TryGetList("Meetings");
                        if (meetingId != 0 && externalAttendeesEmails != null && externalAttendeesEmails.Length > 0)
                        {
                            response = GetMeetingById(meetingId);
                            if (response.IsSuccess)
                            {
                                response.IsSuccess = false;
                                MeetingModel meeting = (MeetingModel)response.Result;
                                var meetingStartTime = meeting.From;
                                var meetingEndTime = meeting.To;
                                StringBuilder mailbuddy = new StringBuilder();
                                mailbuddy.AppendLine("السادة الحضور،");
                                mailbuddy.AppendLine("<br/><br/>");
                                mailbuddy.AppendLine("نتشرف بدعوتكم لحضور الاجتماع " + meeting.Title + " الذي سيعقد في " + meeting.Location + " بتاريخ " + meeting.MeetingDate + " من الساعة " + meetingStartTime + " حتى الساعة " + meetingEndTime + ".");
                                mailbuddy.AppendLine("<br/><br/>");
                                mailbuddy.AppendLine("وتفضلوا بقبول فائق الاحترام والتقدير.");
                                List<string> emails = new List<string>(externalAttendeesEmails);
                                response.Result = SendEmail(emails, "دعوة لحضور اجتماع", mailbuddy.ToString());
                            }
                            
                            response.Result ="";
                        }
                        web.AllowUnsafeUpdates = false;
                    }
                }
            }
            catch (Exception ex)
            {
                response.Message = ex.Message;
                Shared.Issues.Log("Meetings/SendInvitationToExternalAttendees", ex.Message);
            }
            return response.IsSuccess;
        }
        public static bool SendInvitationToInternalAttendees(int meetingId, int[] attendeesIds)
        {
            Response response = new Response();
            string siteURL = WebConfigurationManager.AppSettings[Constants.WebConfigMeetingSiteUrl];
            List<string> emails = new List<string>();
            try
            {
                using (SPSite site = new SPSite(siteURL))
                {
                    using (SPWeb web = site.OpenWeb())
                    {
                        string webURL = web.Url;
                        web.AllowUnsafeUpdates = true;
                        SPList meetingList = web.Lists.TryGetList("Meetings");
                        if (meetingId != 0 && attendeesIds.Length > 0)
                        {
                            response = GetMeetingById(meetingId);
                            if (response.IsSuccess)
                            {
                                response.IsSuccess = false;
                                MeetingModel meetingModel = (MeetingModel)response.Result;
                                var meetingStartTime = meetingModel.From;
                                var meetingEndTime = meetingModel.To;
                                StringBuilder mailbuddy = new StringBuilder();
                                mailbuddy.AppendLine("السادة الحضور،");
                                mailbuddy.AppendLine("<br/><br/>");
                                mailbuddy.AppendLine("نود إعلامكم بدعوتكم لحضور الاجتماع " + meetingModel.Title + " الذي سيعقد في " + meetingModel.Location + " بتاريخ " + meetingModel.MeetingDate + " من الساعة " + meetingStartTime + " حتى الساعة " + meetingEndTime + ".");
                                mailbuddy.AppendLine("<br/><br/>");
                                mailbuddy.AppendLine("وتفضلوا بقبول فائق الاحترام والتقدير.");

                                foreach (var id in attendeesIds)
                                {
                                    SPUser attendeeUser = web.AllUsers.GetByID(id);
                                    emails.Add(attendeeUser.Email);
                                }
                                response.Result = SendEmail(emails, "An invitation to attend a meeting", mailbuddy.ToString());
                                response.IsSuccess = true;
                            }
                        }
                        web.AllowUnsafeUpdates = false;
                    }
                }
                return response.IsSuccess;
            }
            catch (Exception ex)
            {
                Shared.Issues.Log("Meetings/SendInvitationToInternalAttendees", ex.Message);
                throw;
            }
        }
        public static bool SendEmail(List<string> emails, string subject, string body)
        {
            try
            {
                using (MailMessage mail = new MailMessage())
                {
                    using (SmtpClient SmtpServer = new SmtpClient())
                    {
                        //Get the Sharepoint SMTP information from the SPAdministrationWebApplication
                        string smtpServer = SPAdministrationWebApplication.Local.OutboundMailServiceInstance.Server.Address;
                        string smtpFrom = SPAdministrationWebApplication.Local.OutboundMailSenderAddress ;
                        int smtpPort = SPAdministrationWebApplication.Local.OutboundMailPort;
                        string UserName = SPAdministrationWebApplication.Local.OutboundMailUserName;
                        var securedPassword = SPAdministrationWebApplication.Local.OutboundMailPassword;
                        IntPtr securedStringPassword = IntPtr.Zero;
                        securedStringPassword = System.Runtime.InteropServices.Marshal.SecureStringToGlobalAllocUnicode(securedPassword);
                        string Password = System.Runtime.InteropServices.Marshal.PtrToStringUni(securedStringPassword);
                        mail.From = new MailAddress(smtpFrom);
                        mail.Subject = subject;
                        StringBuilder builder = new StringBuilder();
                        builder.Append(body);
                        
                        mail.Body = builder.ToString();
                        mail.IsBodyHtml = true;
                        SmtpServer.Host = smtpServer;
                        SmtpServer.Port = Convert.ToInt32(smtpPort);
                        SmtpServer.TargetName = "STARTTLS/smtp.office365.com";
                        SmtpServer.DeliveryMethod = SmtpDeliveryMethod.Network;
                        SmtpServer.UseDefaultCredentials = false;
                        SmtpServer.Credentials = new NetworkCredential(UserName, Password);
                        SmtpServer.EnableSsl = true;
                        ServicePointManager.ServerCertificateValidationCallback = delegate (object s, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors) { return true; };
                        foreach (string email in emails)
                        {
                            if (!string.IsNullOrEmpty(email))
                            {
                                
                                mail.To.Add(email);
                                SmtpServer.Send(mail);
                                mail.To.Remove(new MailAddress(email));

                            }
                        }
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                Shared.Issues.Log("SendMail/SendEmail", ex.Message);
                return false;
            }
        }


        public static string GetPdfSiteUrl()
        {
            string webURL = "";
            try
            {
                string siteURL = "";
                using (SPSite site = new SPSite(SPContext.Current.Site.Url))
                {
                    using (SPWeb web = site.OpenWeb("/ar-eg/Meeting/"))
                    {
                        webURL = web.Url;
                        SPList PdfSiteURLConfigurationsList = web.Lists.TryGetList("PdfSiteURLConfigurations");
                        if (PdfSiteURLConfigurationsList != null)
                        {
                            SPListItemCollection usersConfigurationsItems = PdfSiteURLConfigurationsList.GetItems();
                            if (usersConfigurationsItems.Count > 0)
                            {
                                SPListItem item = usersConfigurationsItems[0];
                                siteURL = item.Title != null ? item.Title : "";
                            }
                        }
                    }
                }
                return siteURL;
            }
            catch (Exception ex)
            {
                Shared.Issues.Log("Meetings/GetPdfSiteUrl", ex.Message);
                throw ex;
            }
        }


        public static Response PDFGenerate(string id, string reportTitle, string reportName)
        {
            Response response = new Response();
            try
            {
                SPSite sit = SPContext.Current.Site;
                SPSecurity.RunWithElevatedPrivileges(delegate ()
                {
                    List<string> sPUsersRecievePdf = new List<string>();
                    using (SPSite site = new SPSite(sit.Url))
                    {
                        using (SPWeb web = site.OpenWeb("/ar-eg/Meeting/"))
                        {
                            var siteURL = GetPdfSiteUrl();
                            if (!string.IsNullOrEmpty(id))
                            {
                                int meetingId = Convert.ToInt32(id);
                                SPList meetingsList = web.Lists.TryGetList("Meetings");
                                if (meetingsList != null)
                                {
                                    Response meetingResponse = GetMeetingById(meetingId);
                                    if(meetingResponse.Result != null)
                                    {
                                        MeetingModel meeting = (MeetingModel)meetingResponse.Result;
                                        string finalReportTitle = "", CIO = "", CEO = "", cioJobTitle = "", ceoJobTitle = "", meetingNotice = "", reportPreparer = "", reportPrerarerJobTitle = "", reportDate = "";
                                        UserConfigurationModel userConfigurationModel = new UserConfigurationModel();
                                        userConfigurationModel.Role = "CIO";
                                        Response cioModelResponse = UserConfiguration.Search(userConfigurationModel);
                                        if (cioModelResponse.Result != null)
                                        {
                                            UserConfigurationModel cioModel = ((List<UserConfigurationModel>)cioModelResponse.Result).FirstOrDefault();
                                            CIO = cioModel.ArabicName?? string.Empty;
                                            cioJobTitle = cioModel.JobTitle?? string.Empty;
                                            userConfigurationModel.Role = "CEO";    
                                            Response ceoModelResponse = UserConfiguration.Search(userConfigurationModel);
                                            if (ceoModelResponse.Result != null)
                                            {
                                                UserConfigurationModel ceoModel = ((List<UserConfigurationModel>)ceoModelResponse.Result).FirstOrDefault();
                                                CIO = cioModel.ArabicName ?? string.Empty;
                                                cioJobTitle = cioModel.JobTitle ?? string.Empty;
                                                meetingNotice = meeting.Notice??string.Empty;
                                                reportDate = meeting.Created.ToString();
                                                UserConfigurationModel owner = new UserConfigurationModel();
                                                UserConfigurationModel responsable = new UserConfigurationModel();
                                                if (meeting.PostReportResponsibleOwnerId > 0)
                                                {
                                                    UserConfigurationModel ownerConfigurationModel = new UserConfigurationModel();
                                                    ownerConfigurationModel.UserId = meeting.PostReportResponsibleOwnerId;
                                                    Response ownerResponse = UserConfiguration.Search(ownerConfigurationModel);
                                                    if (ownerResponse.Result != null)
                                                    {
                                                        owner = ((List<UserConfigurationModel>)ownerResponse.Result).FirstOrDefault();
                                                        if (meeting.PostReportResponsiblesId[0] > 0)
                                                        {
                                                            responsable.UserId = meeting.MeetingResponsibleId;
                                                            responsable = ((List<UserConfigurationModel>)UserConfiguration.Search(responsable).Result).FirstOrDefault();
                                                        }

                                                    }
                                                }
                                                else
                                                {
                                                    response.Message = "PostReportResponsibleOwnerId Not Founded";
                                                }
                                                StringBuilder allMeetingItems = new StringBuilder();
                                                allMeetingItems.Append("<div class='itemsBlock'>");

                                                if (reportName == "post-report")
                                                {
                                                    finalReportTitle = "تقرير بعد حضور " + (meeting.Title ?? string.Empty);
                                                    reportPreparer = owner != null ? owner.ArabicName : "";
                                                    reportPrerarerJobTitle = owner != null ? owner.JobTitle : "";
                                                    meetingNotice = new StringBuilder().Append(" <div class='editableBlock Notice BorderMe'> <h5>تنويه</h5> <p>")
                                                                                        .Append(meeting.Notice ?? string.Empty)
                                                                                        .Append("</p> </div>").ToString();
                                                    reportDate = DateTime.Now.Date.ToString("yyyy/MM/dd");
                                                }
                                                else
                                                {
                                                    finalReportTitle = "تقرير قبل حضور " + (meeting.Title ?? string.Empty);
                                                    reportPreparer = responsable != null ? responsable.ArabicName : "";
                                                    reportPrerarerJobTitle = responsable != null ? responsable.JobTitle : "";
                                                    reportDate = meeting.MeetingDate.ToString();

                                                    if (!string.IsNullOrEmpty(meeting.Notice))
                                                    {
                                                        meetingNotice = new StringBuilder().Append(" <div class='editableBlock Notice BorderMe'> <h5>تنويه</h5> <p>")
                                                                                            .Append(meeting.Notice ?? string.Empty)
                                                                                            .Append("</p> </div>").ToString();
                                                    }
                                                }

                                                int count = 0;
                                                if (meeting.MeetingItems != null)
                                                {
                                                    foreach (MeetingItemModel item in meeting.MeetingItems)
                                                    {
                                                        // Initializing variables
                                                        var itemRecommendationStatus = item.RecommendationStatus ?? string.Empty;
                                                        var itemRecommendationStatusHTML = string.Empty;
                                                        StringBuilder itemRecommendationTxt = new StringBuilder();
                                                        StringBuilder BODComment = new StringBuilder();
                                                        StringBuilder bodMemberCommentBuilder = new StringBuilder();
                                                        StringBuilder postReportMeetingStatus = new StringBuilder();
                                                        StringBuilder decissionApprovedWith = new StringBuilder();
                                                                

                                                        // Building the 'decissionApprovedWith' block if applicable
                                                        if (item != null && !string.IsNullOrWhiteSpace(item.DecissionApprovedWith))
                                                        {
                                                                decissionApprovedWith
                                                                .Append("<div class='ActionTake'>")
                                                                .Append(" <i class='fas fa-users'></i> ")
                                                                .Append("<p class='ActionTakeText'>تم اتخاذ القرار  ")
                                                                .Append(item.DecissionApprovedWith)
                                                                .Append("</p>")
                                                                .Append(" </div>"); // No need to call ToString here

                                                            string result = decissionApprovedWith.ToString(); // Now convert to string after the build is complete

                                                        }

                                                        // Building post-report content
                                                        if (reportName == "post-report")
                                                        {
                                                            if (!string.IsNullOrWhiteSpace(itemRecommendationStatus))
                                                            {
                                                                var statusTextBuilder = new StringBuilder();

                                                                if (itemRecommendationStatus == "لم يتم العمل بالتوصية")
                                                                {
                                                                    statusTextBuilder.Append("<p class='statusText'>لم يتم العمل بالتوصية</p>");
                                                                    itemRecommendationStatusHTML = statusTextBuilder.ToString();
                                                                    postReportMeetingStatus 
                                                                    .Append("<div class='StatusBox'> ")
                                                                    .Append("<div class='DoneOrNot'>")
                                                                    .Append(" <i class='fas fa-times-circle'></i> ")
                                                                    .Append(itemRecommendationStatusHTML)
                                                                    .Append(" </div> ")
                                                                    .Append(decissionApprovedWith)
                                                                    .Append("</div>")
                                                                    .ToString(); // Call ToString() at the end to get the final string

                                                                }
                                                                else
                                                                {
                                                                    statusTextBuilder.Append("<p class='statusText'> تم العمل بالتوصية</p>");
                                                                    itemRecommendationStatusHTML = statusTextBuilder.ToString();
                                                                    postReportMeetingStatus 
                                                                        .Append("<div class='StatusBox'> ")
                                                                        .Append("<div class='DoneOrNot'>")
                                                                        .Append(" <i class='fas fa-check-circle'></i> ")
                                                                        .Append(itemRecommendationStatusHTML)
                                                                        .Append(" </div> ")
                                                                        .Append(decissionApprovedWith)
                                                                        .Append("</div>")
                                                                        .ToString();
                                                                }
                                                            }
                                                        }

                                                        if (meeting.Title == "اجتماع مجلس الإدارة")
                                                        {
                                                            itemRecommendationTxt.Append("التوجيه");
                                                            BODComment.Append("تعليق عضو مجلس الإدارة");

                                                            if (item != null && !string.IsNullOrWhiteSpace(item.BODMemberComment))
                                                            {

                                                                bodMemberCommentBuilder.Append("<div class='form-group MangerComment'>");
                                                                bodMemberCommentBuilder.Append("<label for='comment'>");
                                                                bodMemberCommentBuilder.Append(BODComment);
                                                                bodMemberCommentBuilder.Append(" / <span class='ManagerName'>");
                                                                bodMemberCommentBuilder.Append(item.ModifiedBy ?? string.Empty);
                                                                bodMemberCommentBuilder.Append("</span></label>");
                                                                bodMemberCommentBuilder.Append("<div class='editableBlock'>");
                                                                bodMemberCommentBuilder.Append("<p>");
                                                                bodMemberCommentBuilder.Append(item.BODMemberComment ?? string.Empty);
                                                                bodMemberCommentBuilder.Append("</p></div></div>");

                                                                string bodMemberComment = bodMemberCommentBuilder.ToString();
                                                            }
                                                        }
                                                        else
                                                        {
                                                            itemRecommendationTxt.Append("التوصية");
                                                            BODComment.Append("تعليق المفوض بالحضور");
                                                            if (item != null && !string.IsNullOrWhiteSpace(item.BODMemberComment))
                                                            {

                                                                bodMemberCommentBuilder.Append("<div class='form-group MangerComment'> ")
                                                                    .Append("<label for='comment'>")
                                                                    .Append(BODComment)
                                                                    .Append(" / ")
                                                                    .Append("<span class='ManagerName'>")
                                                                    .Append(item.ModifiedBy)
                                                                    .Append("</span>")
                                                                    .Append("</label> ")
                                                                    .Append("<div class='editableBlock'> ")
                                                                    .Append("<p> ")
                                                                    .Append(item.BODMemberComment)
                                                                    .Append(" </p> </div> </div>");

                                                            }
                                                        }
                                                        count++;
                                                        allMeetingItems.Append("<div class='itemBlock' id='itemBlock1'>");
                                                        allMeetingItems.Append(postReportMeetingStatus);
                                                        allMeetingItems.Append(
                                                            "<div class='form-group itemnumber'>" +
                                                            "<div class='headingitem'>" +
                                                            "<h5 id='itemtitle'> <label data-itemid='57' for='itemtitle'> " +
                                                            (item.ItemOrder) + ". &nbsp;</label> " +
                                                            (item.Title ?? string.Empty) + "</h5>" +
                                                            "</div>" +
                                                            "<div class='eventitem'>" +
                                                            "</div>" +
                                                            "</div>" +
                                                            "<div class='basicData'>" +
                                                            "<div class='form-group'>" +
                                                            "<label for='comment'>التعليق</label> " +
                                                            "<div class='editableBlock' id='returnComment'>" +
                                                            "<p>" + (item.Comment ?? string.Empty) + "</p>" +
                                                            "</div>" +
                                                            "</div>" +
                                                            "<div class='form-group'>" +
                                                            "<label for='comment'>" + (itemRecommendationTxt) + "</label> " +
                                                            "<div class='editableBlock' id='returnComment'>" +
                                                            "<p>" + (item.Recommendation ?? string.Empty) + "</p>" +
                                                            "</div>" +
                                                            "</div>" +
                                                            (bodMemberCommentBuilder) +
                                                            "</div>");
                                                    }
                                                }
                                                else
                                                {
                                                    response.Message = "No meeting Item founded";
                                                }
                                                var htmlToPdf = new HtmlToPdfConverter();
                                                htmlToPdf.Orientation = PageOrientation.Portrait;
                                                int attCount = 0;
                                                if (meeting.AttachmentModels != null)
                                                {
                                                    attCount = meeting.AttachmentModels.Count();
                                                }
                                                byte[] ReportBytes = null;
                                                StringBuilder newhtml = new StringBuilder();

                                                newhtml.Append("<html><head><meta http-equiv='Content-Type' content='text/html; charset=utf-8' x-undefined='' />")
                                                    .Append(" <link rel='stylesheet' href='https://cdnjs.cloudflare.com/ajax/libs/font-awesome/5.15.4/css/all.min.css'> ")
                                                    .Append("<link rel='stylesheet' href='").Append(siteURL).Append("_layouts/15/Uranium.BODApp.SharePoint/Style/bootstrap.min.css' />")
                                                    .Append(" <link rel='stylesheet' href='").Append(siteURL).Append("_layouts/15/Uranium.BODApp.SharePoint/Style/jquery-ui.css' /> ")
                                                    .Append(" <link rel='stylesheet' href='").Append(siteURL).Append("_layouts/15/Uranium.BODApp.SharePoint/Style/Report.css' /> ")
                                                    .Append(" <link rel='stylesheet' href='").Append(siteURL).Append("_layouts/15/Uranium.BODApp.SharePoint/Style/mainStyle.css' />")
                                                    .Append(" <link rel='stylesheet' href='").Append(siteURL).Append("_layouts/15/Uranium.BODApp.SharePoint/Style/BODStyle.css'>")
                                                    .Append(" <link rel='stylesheet' href='").Append(siteURL).Append("_layouts/15/Uranium.BODApp.SharePoint/Style/Responsive.css' />")
                                                    .Append(" </head> <body > ")
                                                    .Append("<div class='WhiteBox'> ")
                                                    .Append("<div class='PDFBox'>  <div class=' ContentBox' >")
                                                    .Append("<div class='itemsMeetingBlock'> ")
                                                    .Append("<div class='reportTitlesBox'> ")
                                                    .Append("<h5 class='firstTitle'>").Append(finalReportTitle ?? string.Empty).Append("</h5> ")
                                                    .Append("<h5 class='BlueTitle'>").Append(meeting.CompanyName ?? string.Empty).Append("</h5> ")
                                                    .Append("</div>")
                                                    .Append(" <table class='Introtable'> ")
                                                    .Append("<tr> <th>تاريخ الجلسة</th> <td>").Append(meeting.MeetingDate).Append("</td> ")
                                                    .Append("<th>تاريخ استلام الدعوة ومرفقاتها</th> <td >").Append(reportDate ?? string.Empty).Append("</td> </tr> <tr> ")
                                                    .Append("<th>تاريخ إعداد التقرير</th> <td >").Append(reportDate ?? string.Empty).Append("</td> <th>عدد المرفقات</th>")
                                                    .Append(" <td>").Append(attCount).Append("</td> </tr> ")
                                                    .Append("</table>")
                                                    .Append(meetingNotice ?? string.Empty)
                                                    .Append(" <h5 class='blueHeading'>بنود جدول الأعمال</h5>")
                                                    .Append(" <div class='itemsCreated'> ")
                                                    .Append("<div class='itemMeeting'>")
                                                    .Append(allMeetingItems)
                                                    .Append(" </div>")
                                                    .Append(" </div>")
                                                    .Append(" <table class='Infotable'> ")
                                                    .Append("<tr> <th>إعداد</th> ")
                                                    .Append("<th>مراجعة</th>")
                                                    .Append(" <th>اعتماد</th> ")
                                                    .Append("</tr> ")
                                                    .Append("<tr> ")
                                                    .Append("<td>").Append(reportPreparer ?? string.Empty).Append("</td>")
                                                    .Append(" <td>").Append(CIO).Append("</td> ")
                                                    .Append("<td>").Append(CEO).Append("</td> ")
                                                    .Append(" </tr> ")
                                                    .Append("<tr> <td>").Append(reportPrerarerJobTitle).Append("</td> <td>").Append(cioJobTitle).Append("</td> <td>").Append(ceoJobTitle).Append("</td>")
                                                    .Append(" </tr> </table> ")
                                                    .Append("</div> </div></div> </div></div></div></div></body> </html>");


                                                Shared.Issues.Log("PDFGenerate", "Pass the Links");
                                                string finalHtml = newhtml.ToString();


                                                var mem = MeetingPDF(site, "", "", finalReportTitle, false, false, finalHtml, meeting.CompanyName, meeting.MeetingDate.ToString());
                                                using (var memoryStream = new MemoryStream(mem))
                                                {

                                                    using (BinaryReader br = new BinaryReader(memoryStream))
                                                    {
                                                        ReportBytes = br.ReadBytes(mem.Length);
                                                    }
                                                    web.AllowUnsafeUpdates = true;
                                                    SPFieldUserValueCollection PDFUsers = new SPFieldUserValueCollection();

                                                    SPList ReportsDocument = web.Lists.TryGetList("MeetingReportsDocument");
                                                    if (ReportsDocument != null)
                                                    {
                                                        SPFolder spLibrary = ReportsDocument.RootFolder;
                                                        bool replaceExistingFile = true;
                                                        SPFile spfile = spLibrary.Files.Add(reportName + meetingId + ".pdf", ReportBytes, replaceExistingFile);
                                                        //spfile.Item["MeetingID"] = meetingId
                                                        //if (meeting.MeetingAttendee != null&& meeting.PostReportResponsiblesId != null && meeting.ExternalAttendeesEmails != null)
                                                        //{
                                                        var meetingAttendees = meeting.MeetingAttendee;
                                                        var meetingResponibles = meeting.PostReportResponsiblesId;
                                                        var meetingExternalAttendees = meeting.ExternalAttendeesEmails;
                                                        if(meetingAttendees != null)
                                                        {
                                                            foreach (var attendee in meetingAttendees)
                                                            {
                                                                SPUser ReportUser = web.AllUsers.GetByID(Convert.ToInt32(attendee.UserId));
                                                                if (ReportUser != null)
                                                                {
                                                                    SPFieldUserValue userValue = new SPFieldUserValue(web, ReportUser.ID, ReportUser.LoginName);
                                                                    PDFUsers.Add(userValue);
                                                                }
                                                            }
                                                                
                                                            foreach (var responible in meetingAttendees)
                                                            {
                                                                SPUser ReportUser = web.AllUsers.GetByID(Convert.ToInt32(responible));
                                                                if (ReportUser != null)
                                                                {
                                                                    SPFieldUserValue userValue = new SPFieldUserValue(web, ReportUser.ID, ReportUser.LoginName);
                                                                    PDFUsers.Add(userValue);
                                                                }
                                                            }

                                                            if (cioModel.UserId > 0)
                                                            {
                                                                SPUser ReportUser = web.AllUsers.GetByID(Convert.ToInt32(cioModel.UserId));
                                                                if (ReportUser != null)
                                                                {
                                                                    SPFieldUserValue userValue = new SPFieldUserValue(web, ReportUser.ID, ReportUser.LoginName);
                                                                    if (!sPUsersRecievePdf.Contains(ReportUser.Email))
                                                                    {
                                                                        sPUsersRecievePdf.Add(ReportUser.Email);
                                                                    }
                                                                    PDFUsers.Add(userValue);
                                                                }
                                                            }

                                                            if (ceoModel.UserId > 0)
                                                            {
                                                                SPUser ReportUser = web.AllUsers.GetByID(Convert.ToInt32(ceoModel.UserId));
                                                                if (ReportUser != null)
                                                                {
                                                                    SPFieldUserValue userValue = new SPFieldUserValue(web, ReportUser.ID, ReportUser.LoginName);
                                                                    if (!sPUsersRecievePdf.Contains(ReportUser.Email))
                                                                    {
                                                                        sPUsersRecievePdf.Add(ReportUser.Email);
                                                                    }
                                                                    PDFUsers.Add(userValue);
                                                                }
                                                            }
                                                            if (meetingResponibles != null && meetingResponibles[0] > 0)
                                                            {
                                                                SPUser ReportUser = web.AllUsers.GetByID(Convert.ToInt32(meetingResponibles[0]));
                                                                if (ReportUser != null)
                                                                {
                                                                    SPFieldUserValue userValue = new SPFieldUserValue(web, ReportUser.ID, ReportUser.LoginName);
                                                                    PDFUsers.Add(userValue);
                                                                }
                                                            }
                                                            spfile.Item["PDFToUsers"] = PDFUsers;
                                                        }
                                                        response.Result = web.Url + "/" + spfile.Url;
                                                        spfile.Item["ReportUrl"] = response.Result.ToString();
                                                        spfile.Item["Title"] = reportName?? string.Empty;
                                                        spfile.Item["MeetingID"] = new SPFieldLookupValue(meetingId, meetingId.ToString());
                                                        spfile.Item.Update();
                                                        spLibrary.Update();
                                                        ReportsDocument.Update();
                                                        if (spfile.Item["Title"] != null)
                                                        {
                                                            if (spfile.Item["Title"].ToString() == "pre-report")
                                                            {
                                                                //bool isReportSend = SendPreReportAsAttachmentToExternalAttendees(meetingId, meetingExternalAttendees, mem, reportName + ".pdf");
                                                            }
                                                        }
                                                        //}
                                                        ExecuteWorkFlow(meetingId, site, "SendPDF");
                                                        web.AllowUnsafeUpdates = false;
                                                        response.IsSuccess = true;
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                response.Message = "Ceo Not founded";

                                            }
                                        }
                                        else
                                        {
                                            response.Message = "Cio Not founded";

                                        }
                                    }
                                    else
                                    {
                                        response.Message = "meeting with this id not founded";

                                    }
                                }
                                else
                                {
                                    response.Message = "Meeting List equal null";
                                }
                            }
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                Shared.Issues.Log("PDFGenerate", ex.Message);
                response.Message =ex.Message;
            }
            return response;
        }
        public static object GetReportImage(string imageName)
        {
            string webURL = "";
            string name = "";
            string imageURL = "";
            try
            {
                if (!string.IsNullOrEmpty(imageName))
                {
                    using (SPSite site = new SPSite(SPContext.Current.Site.Url))
                    {
                        using (SPWeb web = site.OpenWeb("/ar-eg/Meeting/"))
                        {
                            webURL = web.Url;
                            SPList imagesList = web.Lists["الصور"];
                            if (imagesList != null)
                            {
                                SPQuery query = new SPQuery();
                                query.Query = "<Where><Eq><FieldRef Name='FileLeafRef' /><Value Type='File'>" + imageName + "</Value></Eq></Where>";
                                SPListItemCollection Images = imagesList.GetItems(query);
                                if (Images.Count > 0)
                                {
                                    SPListItem image = Images[0];
                                    if (image != null)
                                    {
                                        name = image.Name;
                                        imageURL = webURL + "/" + image.Url;
                                    }

                                }
                            }
                            else
                            {
                                Shared.Issues.Log("Meetings/GetReportImage", "list does not exist");
                            }
                        }
                    }
                }
                return new { name = name, url = imageURL };
            }
            catch (Exception ex)
            {
                Shared.Issues.Log("Meetings/GetReportImage", ex.Message);
                throw;
            }
        }
        public static string ConvertURLTobase64(string url)
        {
            try
            {
                byte[] bytes = null;
                string imageBytes = "";
                SPSecurity.RunWithElevatedPrivileges(delegate ()
                {
                    using (SPSite site = new SPSite(SPContext.Current.Site.Url))
                    {
                        using (SPWeb web = site.OpenWeb("/ar-eg/Meeting/"))
                        {
                            string[] image = url.Split('.');
                            if (image.Length > 1)
                            {
                                string extention = image[1];
                                SPFile spfile = web.GetFile(url);
                                Stream fs = spfile.OpenBinaryStream();
                                BinaryReader br = new BinaryReader(fs);
                                bytes = br.ReadBytes((Int32)fs.Length);
                                imageBytes = Convert.ToBase64String(bytes);
                            }
                        }
                    }
                });
                return imageBytes;
            }
            catch (Exception ex)
            {
                Shared.Issues.Log("Meetings/ConvertURLTobase64", ex.Message);
                throw;
            }

        }
        public static byte[] MeetingPDF(SPSite site, string titleNaView = "", string CustomSwitches = "", string titlePDF = "", bool grayScale = false, bool convertToImage = false, string html = "", string companyName = "", string meetindate = "")
        {
            var siteURL = GetPdfSiteUrl();
            string logoURL = JObject.FromObject(GetReportImage("ekuityLogo.PNG"))["url"].ToString();
            string logoBytes = ConvertURLTobase64(logoURL);
            string reportURL = JObject.FromObject(GetReportImage("backArrow.png"))["url"].ToString();
            string reportBytes = ConvertURLTobase64(reportURL);
            HtmlToPdfConverter converter = null;
            StringBuilder htmlSB = new StringBuilder();
            try
            {
                SPSecurity.RunWithElevatedPrivileges(delegate ()
                {
                    htmlSB.Append(html);
                    CustomSwitches = "--enable-local-file-access";
                    converter = new HtmlToPdfConverter()
                    {
                        CustomWkHtmlArgs = CustomSwitches,
                        Grayscale = grayScale,
                    };
                    converter.PageHeaderHtml = "<html>" +
                    "<body style='padding-top:20px;'> " +
                     "<img src='data:image/png;base64," + logoBytes + "'  class='ribbon'   style='display: block; direction: ltr;text-align: left; background-color: transparent !important; position:relative !important; top:10px !important; margin-bottom:50px !important ; padding-bottom:50px !important'/>" +
                     "<img class='reportImg' src='data:image/png;base64," + reportBytes + " ' style='opacity:0.8; direction: rtl;text-align: right; width: 10% !important; background-color: transparent !important; display: inline; position: absolute;top:45px; !Important; right:0;  padding-left: 80rem; !important' />" +
                    " </body> </html>";

                    converter.PageFooterHtml = "<html><head><meta http-equiv='Content-Type' content='text/html; charset=utf-8' x-undefined='' />" +
                       "<body><div class='footer' style='position:relative; direction:rtl; border-top: 1px solid rgb(71, 70, 70); width:93%; margin:auto '> <p style='width:100%;'>" + titlePDF + " | " + companyName + " | " + meetindate + " <span class='page' style='position:absolute;left:0;'></span></p></div></body></html> ";
                    converter.Quiet = false;

                    converter.LogReceived += (sender, e) =>
                    {
                        Shared.Issues.Log("GeneratePDF/WkHtmlToPdf Log:", e.Data);
                    };
                });
                return converter.GeneratePdf(htmlSB.ToString());
            }
            catch (Exception ex)
            {
                Shared.Issues.Log("Meeting.aspx/MeetingPDF", ex.Message);
                throw ;
            }
        }
       
    }
}