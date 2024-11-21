using Microsoft.SharePoint;
using Microsoft.SharePoint.Meetings;
using Microsoft.SharePoint.WebControls;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Script.Serialization;
using System.Web.Services;
using Uranium.BODApp.SharePoint.Models;
using Uranium.BODApp.SharePoint.Services;
using Uranium.BODApp.SharePoint.Shared;

namespace Uranium.BODApp.SharePoint.Layouts.Uranium.BODApp.SharePoint
{
    public partial class UserTasks : LayoutsPageBase
    {
        public static bool FullDetails { set; get; }
        protected void Page_Load(object sender, EventArgs e)
        {
            var currentRequest = HttpContext.Current.Request;
            object result = new object();

            if (currentRequest != null)
            {
                var requestedDate = currentRequest.Form["APIName"];
                string json = requestedDate.ToString();
                if (json == "CloseTask")
                {
                    result = CloseTask(currentRequest);
                }
                else if (json == "CreatePostReport")
                {
                    result = CreatePostReport(currentRequest);
                }
                else
                {
                    result = ReviewMeetingItem(currentRequest);
                }
               

            }
            HttpContext.Current.Response.ContentType = "application/json; charset=utf-8";
            HttpContext.Current.Response.Write(new JavaScriptSerializer().Serialize(result));
        }
        [WebMethod]
        public static Response GetUserTasks(UserTasksModel userTasksModel, Pagination pagination, DateTime? from, DateTime? to)
        {
            FullDetails = false;
            Response response = new Response();
            try
            {
                if (pagination != null && pagination.PageNumber > 0 && pagination.PageSize > 0)
                {
                    SPUser currentUser = SPContext.Current.Web.CurrentUser;
                    userTasksModel.AssignedTo = currentUser.Name;
                    userTasksModel.AssignedToId = currentUser.ID;
                    response = UserTask.Search(userTasksModel, pagination, from, to);
                }
                else
                {
                    response.Message = "Enter valid Page Number & Page Size";
                }
            }
            catch (Exception ex)
            {
                response.Message = ex.Message;
                Issues.Log("UserTasks/GetUserTasks", ex.Message);
            }
            return response;
        }
        [WebMethod]
        public static Response AssignPreReportResponsible(int userId, int taskId, int meetingId)
        {
            Response response = new Response();
            try
            {
                SPUser user = SPContext.Current.Web.SiteUsers.GetByID(userId);
                if (user != null)
                {
                    response = Services.UserTask.GetTaskById(taskId);
                    UserTasksModel userTask = (UserTasksModel)response.Result;
                    if (userTask != null)
                    {
                        userTask.Status = "مكتملة";
                        response = Services.UserTask.Save(userTask);
                        if (response.IsSuccess)
                        {
                            response = Services.Meeting.GetMeetingById(meetingId);
                            MeetingModel meeting = (MeetingModel)response.Result;
                            if (meeting != null)
                            {
                                meeting.MeetingResponsible = user.Name;
                                meeting.MeetingResponsibleId = user.ID;
                                response = Services.Meeting.Save(meeting);
                                if (response.IsSuccess)
                                {
                                    response.Message = "Task Closed Successfully";
                                }
                            }
                            else
                            {
                                response.Message = "Task Does not Returned";
                            }
                        }
                    }
                    else
                    {
                        response.Message = "Task Does not Returned";
                    }
                }
                else
                {
                    response.Message = "User Not Founded !!";
                }
            }
            catch (Exception ex)
            {
                Shared.Issues.Log("UserTasks.aspx/AssignPreReportResponsible", ex.Message);
                response.Message = ex.Message;
            }
            return response;
        }
        [WebMethod]
        public static Response GetTaskById(int id)
        {
            Response response = new Response();
            try
            {
                response = Services.UserTask.GetTaskById(id);
                response.IsSuccess = true;
            }
            catch (Exception ex)
            {
                response.Message = ex.Message;
                Shared.Issues.Log("UserTasks.aspx/GetTaskById", ex.Message);
            }
            return response;
        }
        [WebMethod]
        public static Response ReviewMeetingItem(HttpRequest request)
        {
            Response response = new Response();
            try
            {
                var meetingObject = request.Form["request"];
                var json = JObject.Parse(meetingObject);
                int meetingId = json["meetingId"].Value<int>();
                bool isDraft = json["isDraft"].Value<bool>();
                int taskId = json["taskId"].Value<int>();
                AttachmentModel attachmentModel = null;
                MeetingModel meetingModel = new MeetingModel();
                meetingModel.Id = meetingId;
                int i = 1;
                if (HttpContext.Current.Request.Files[$"file1"] != null)
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
                    response = Services.Meeting.Save(meetingModel);
                }
                if (isDraft)
                {
                    response = Services.UserTask.MakeTaskUnderProgress(taskId);
                }
                else
                {
                    response = Services.UserTask.CheckFromMeetingItemStatus(meetingId);
                    if (response.IsSuccess)
                    {
                        if ((bool)response.Result)
                        {
                            response = Services.UserTask.AccepteTaskOutCome(taskId);
                        }
                        else
                        {
                            response = Services.UserTask.RejectTaskOutCome(taskId);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                response.Message += ex.Message;
                Shared.Issues.Log("UserTasks.aspx/GetTaskById",ex.Message);
            }
            return response;
        }
        [WebMethod]
        public static Response CloseTask(HttpRequest request)
        {
            Response response = new Response();
            try
            {
                var requestedDate = request.Form["request"];
                var json = JObject.Parse(requestedDate);
                bool isDraft = json["isDraft"].Value<bool>();
                int taskId = json["taskId"].Value<int>();
                int meetingId = json["meetingId"].Value<int>();
                AttachmentModel attachmentModel = null;
                MeetingModel meetingModel = new MeetingModel();
                meetingModel.Id = meetingId;
                int i = 1;
                if (HttpContext.Current.Request.Files[$"file1"] != null)
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
                    response = Services.Meeting.Save(meetingModel);
                }
                if (isDraft)
                {
                    response = Services.UserTask.MakeTaskUnderProgress(taskId);
                }
                else
                {
                    response = Services.UserTask.AccepteTaskOutCome(taskId);
                }
            }
            catch (Exception ex)
            {
                response.Message = ex.Message;
                Shared.Issues.Log("UserTask.aspx/CloseTask", ex.Message);
            }
            return response;
        }
        [WebMethod]
        public static Response CreatePostReport(HttpRequest request)
        {
            AttachmentModel attachmentModel =new AttachmentModel();
            Response response = new Response();
            var meetingObject = request.Form["meetingModel"];
            var outerObject = JObject.Parse(meetingObject);
            var innerObject = outerObject["meetingModel"]?.ToString();
            bool isDraft = bool.Parse(request["isDraft"]);
            int taskId = int.TryParse(request["taskId"], out int idResult) ? idResult : 0;
            MeetingModel meeting = JsonConvert.DeserializeObject<MeetingModel>(innerObject);

            MeetingModel meetingModel = new MeetingModel();
            meeting.PostReportResponsibleOwnerId = SPContext.Current.Web.CurrentUser.ID;
            meeting.ISPostReportResponsble = true;
            response = Services.Meeting.GetMeetingById(meeting.Id);
            meetingModel = (MeetingModel)response.Result;
            int i = 0;
            while (HttpContext.Current.Request.Files[$"file{i}"] != null)
            {
                HttpPostedFile file = HttpContext.Current.Request.Files[$"file{i}"];
                if (file != null)
                {
                    using (var binaryReader = new System.IO.BinaryReader(file.InputStream))
                    {
                        attachmentModel.Attachment = binaryReader.ReadBytes(file.ContentLength);
                        attachmentModel.AttachmentName = System.IO.Path.GetFileName(file.FileName);
                        meeting.AttachmentUploaded = "True";
                        meeting.AttachmentModels.Add(attachmentModel);
                    }
                }
                i++;
            }
            if (response.IsSuccess)
            {
                if (meetingModel.PostReportResponsiblesId.Count == 1)
                {
                    if (isDraft)
                    {
                        response = Services.UserTask.MakeTaskUnderProgress(taskId);
                    }
                    else
                    {
                        response = Services.UserTask.AccepteTaskOutCome(taskId);
                        SPSecurity.RunWithElevatedPrivileges(() =>
                        {
                            response.Result = Services.Meeting.PDFGenerate(meeting.Id.ToString(), "تقرير بعد حضور الاجتماع", "post-report"); ;
                        });
                    }
                }
                else if (meetingModel.PostReportResponsiblesId.Count == 2)
                {
                    if (isDraft)
                    {
                        response = Services.UserTask.MakeTaskUnderProgress(taskId);
                    }
                    else
                    {
                        response = Services.UserTask.AccepteTaskOutCome(taskId);
                     
                    }
                    meeting.PostReportConfirmationId = meetingModel.PostReportResponsiblesId[1];
                }
            }

            response = Services.Meeting.Save(meeting);
            return response;
        }

        [WebMethod]
        public static Response ConfirmPretReport(bool isConfermed,int taskId,int meetingId,string rejectReason)
        {
            Response response = new Response();
            MeetingModel meetingModel = new MeetingModel();
            try
            {

                response = Services.Meeting.GetMeetingById(meetingId);
                if(response.IsSuccess)
                {
                    if (isConfermed)
                    {
                        response = Services.UserTask.AccepteTaskOutCome(taskId);
                        SPSecurity.RunWithElevatedPrivileges(() =>
                        {
                            response.Result = Services.Meeting.PDFGenerate(meetingId.ToString(), "تقرير قبل حضور الاجتماع", "pre-report"); ;
                        });
                    }
                    else
                    {
                        meetingModel = (MeetingModel)response.Result;
                        response = Services.UserTask.RejectTaskOutCome(taskId);
                        meetingModel.PostReportRejectReason = rejectReason;
                        response = Services.Meeting.Save(meetingModel);
                    }
                }
            }
            catch(Exception ex)
            {
                response.Message = ex.Message;
            }
            return response;
        }
    }
}