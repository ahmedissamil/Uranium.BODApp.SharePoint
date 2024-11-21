using CamlBuilder;
using Microsoft.SharePoint;
using Microsoft.SharePoint.WebControls;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Script.Serialization;
using System.Web.Services;
using System.Web.Services.Description;
using Uranium.BODApp.SharePoint.Models;
using Uranium.BODApp.SharePoint.Services;
using Uranium.BODApp.SharePoint.Shared;

namespace Uranium.BODApp.SharePoint.Layouts.Uranium.BODApp.SharePoint
{
    public partial class Meeting : LayoutsPageBase
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            var currentRequest = HttpContext.Current.Request;
            var meetingObject = currentRequest.Form["meetingModel"];
            object result = new object();

            if (meetingObject != null)
            {
                
                var outerObject = JObject.Parse(meetingObject);
                var innerObject = outerObject["meetingModel"]?.ToString();

                MeetingModel meetingModel = JsonConvert.DeserializeObject<MeetingModel>(innerObject);


                if(meetingModel.Id > 0 )
                {
                    result = CreatePreMeetingReport(currentRequest);
                }
                else
                {
                    result = AddMeeting(currentRequest);
                }
            }
            HttpContext.Current.Response.ContentType = "application/json; charset=utf-8";
            HttpContext.Current.Response.Write(new JavaScriptSerializer().Serialize(result));
        }
        [WebMethod]
        public static Response AddMeeting(HttpRequest request)
        {
            Response response = new Response();
            try
            {
                var meetingObject = request.Form["meetingModel"];
                var outerObject = JObject.Parse(meetingObject);
                var innerObject = outerObject["meetingModel"]?.ToString();
                MeetingModel meetingModel = JsonConvert.DeserializeObject<MeetingModel>(innerObject);
                meetingModel.CurrentUser = SPContext.Current.Web.CurrentUser;
                AttachmentModel attachmentModel = null;
                response = Services.Meeting.ValidateMeetingModel(meetingModel);
                int i = 0;
                if(response.IsSuccess)
                {
                    while (HttpContext.Current.Request.Files[$"file{i}"] != null)
                    {
                        HttpPostedFile file = HttpContext.Current.Request.Files[$"file{i}"];
                        if (file != null)
                        {
                            attachmentModel = new AttachmentModel();
                            using (var binaryReader = new System.IO.BinaryReader(file.InputStream))
                            {
                                attachmentModel.Attachment = binaryReader.ReadBytes(file.ContentLength);
                                attachmentModel.AttachmentName = System.IO.Path.GetFileName(file.FileName);
                                meetingModel.AttachmentUploaded = "True";
                                meetingModel.AttachmentModels.Add(attachmentModel);
                            }
                        }
                        i++;
                    }
                    if (response.IsSuccess)
                        response = Services.Meeting.Save(meetingModel);
                }
            }
            catch (Exception ex)
            {
                response.Message = ex.Message;
                Shared.Issues.Log("Meeting/GetCommentsByMeetingItemId",ex.Message);
            }
            return response;
        }
        [WebMethod]  
        public static Response GetMeetingById(int id)
        {
            Response response = new Response();
            try
            {
                response = Services.Meeting.GetMeetingById(id);
                
            }
            catch (Exception ex)
            {
                response.Message = ex.Message;
                Issues.Log("Meeting.cs/GetMeetingById", ex.Message);
            }
            return response;
        }
        [WebMethod]
        public static Response GetMeeting(MeetingModel meetingModel, Pagination pagination, DateTime? from, DateTime? to)
        {
            Response response = new Response();
            try
            {
                if (pagination != null && pagination.PageNumber > 0 && pagination.PageSize > 0)
                {
                    response = Services.Meeting.Search(meetingModel, pagination, from, to);
                }
                else
                {
                    response.Message = "Enter valid Page Number & Page Size";
                }
            }
            catch (Exception ex)
            {
                response.Message = ex.Message;
                Issues.Log("Meeting.cs/GetMeeting", ex.Message);
            }
            return response;
        }
        [WebMethod]
        public static Response DeleteAttachment(int id,string name)
        {
            Response response = new Response();
            try
            {
                response = Services.Meeting.DeleteAttachment(id, name);
            }
            catch(Exception ex)
            {
                response.Message = ex.Message;
                Issues.Log("Meeting.cs/DeleteAttachment", ex.Message);
            }
            return response;
        }
        [WebMethod]
        public static Response CreatePreMeetingReport(HttpRequest request)
        {
            Response response = new Response();
            try
            {
                var meetingObject = request.Form["meetingModel"];
                var outerObject = JObject.Parse(meetingObject);
                var innerObject = outerObject["meetingModel"]?.ToString();
                bool isDraft = bool.Parse(request["isDraft"]);
                int taskId = int.TryParse(request["taskId"], out int idResult) ? idResult : 0;
                MeetingModel meetingModel = JsonConvert.DeserializeObject<MeetingModel>(innerObject);
                if(meetingModel.PostReportResponsiblesId.Count== 1|| meetingModel.PostReportResponsiblesId.Count == 2)
                {
                    meetingModel.PostReportResposiblesCount = meetingModel.PostReportResponsiblesId.Count;
                    AttachmentModel attachmentModel = null;
                    if(meetingModel.Id > 0)
                    {
                        int i = 0;
                        while (HttpContext.Current.Request.Files[$"file{i}"] != null)
                        {
                            HttpPostedFile file = HttpContext.Current.Request.Files[$"file{i}"];
                            if (file != null)
                            {
                                attachmentModel = new AttachmentModel();
                                using (var binaryReader = new System.IO.BinaryReader(file.InputStream))
                                {
                                    attachmentModel.Attachment = binaryReader.ReadBytes(file.ContentLength);
                                    attachmentModel.AttachmentName = System.IO.Path.GetFileName(file.FileName);
                                    meetingModel.AttachmentUploaded = "True";
                                    meetingModel.AttachmentModels.Add(attachmentModel);
                                }
                            }
                            i++;
                        }
                        response = Services.Meeting.Save(meetingModel);
                        if(response.IsSuccess)
                        {
                            if (isDraft)
                            {
                                response = Services.UserTask.MakeTaskUnderProgress(taskId);
                            }    
                            else
                            {
                                response = Services.UserTask.CheckFromMeetingItemStatus(meetingModel.Id);
                                if (response.IsSuccess)
                                {
                                    if ((bool)response.Result)
                                    {
                                        response = Services.UserTask.AccepteTaskOutCome(taskId);
                                        response = Services.Meeting.GetMeetingById(meetingModel.Id);
                                        if (response.IsSuccess)
                                        {
                                            MeetingModel meeting = (MeetingModel)response.Result;
                                            response.Result = Services.Meeting.SendInvitationToExternalAttendees(meeting.Id, meeting.ExternalAttendeesEmails);
                                            int[] ids = meeting.MeetingAttendee.Select(x => x.UserId).ToArray();
                                            response.Result = Services.Meeting.SendInvitationToInternalAttendees(meeting.Id, ids);
                                            SPSite currentSite = SPContext.Current.Site;
                                            string pdfUrl = string.Empty;
                                            SPSecurity.RunWithElevatedPrivileges(() =>
                                            {
                                                response = Services.Meeting.PDFGenerate(meeting.Id.ToString(), "تقرير قبل حضور الاجتماع", "pre-report"); 
                                            });
                                        }
                                    }
                                    else
                                    {
                                        response = Services.UserTask.RejectTaskOutCome(taskId);
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        response.Message = "Id required";
                    }
                }
                else
                {
                    response.Message = "PostReportResponsibles Should be 1 or 2 users !!";
                }
            }
            catch(Exception ex)
            {
                response.Message = ex.Message;
                Shared.Issues.Log("Meeting/CreatePreMeetingReport", ex.Message);
            }
            return response;
        }
    }
}
