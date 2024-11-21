using Microsoft.SharePoint;
using Microsoft.SharePoint.WebControls;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System;
using System.Web;
using System.Web.Services;
using Uranium.BODApp.SharePoint.Models;
using Uranium.BODApp.SharePoint.Shared;
using System.Collections.Generic;
using Uranium.BODApp.SharePoint.Services;
using System.Web.Script.Serialization;

namespace Uranium.BODApp.SharePoint.Layouts.Uranium.BODApp.SharePoint
{
    public partial class MeetingItems : LayoutsPageBase
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            var currentRequest = HttpContext.Current.Request;
            object result = new object();
            if (currentRequest != null)
            {
                result = Add(currentRequest);
            }
            HttpContext.Current.Response.ContentType = "application/json; charset=utf-8";
            HttpContext.Current.Response.Write(new JavaScriptSerializer().Serialize(result));
        }
        [WebMethod]
        public static Response Search(MeetingItemModel meetingItemModel)
        {
            Response response = new Response();
            try
            {
                response = Services.MeetingItem.Search(meetingItemModel);
            }
            catch (Exception ex)
            {
                response.Message = ex.Message;
                Shared.Issues.Log("MeetingItem.aspx/Search", ex.Message);
            }
            return response;
        }
        [WebMethod]
        public static Response Add(HttpRequest request)
        {
            Response response = new Response();
            Response result = new Response();
            UserTasksModel model = new UserTasksModel();
            MeetingItemModel meetingItemModel = new MeetingItemModel();
            try
            {
                bool isDraft = bool.TryParse(request["isDraft"], out bool draftResult) ? draftResult : false;
                int taskId = int.TryParse(request["taskId"], out int idResult) ? idResult : 0;
                var meetingItemObject = request.Form["meetingItemModel"];
                if (!string.IsNullOrWhiteSpace(meetingItemObject))
                {
                    var outerObject = JObject.Parse(meetingItemObject);
                    var innerObject = outerObject["meetingItemModel"]?.ToString();            
                    meetingItemModel = JsonConvert.DeserializeObject<MeetingItemModel>(innerObject);
                    MeetingModel meetingModel = new MeetingModel();
                    meetingModel.Id = meetingItemModel.MeetingId;
                    AttachmentModel attachmentModel = null;
                    response = Services.MeetingItem.ValidateMeetingIteemsModel(meetingItemModel);
                    if(response.IsSuccess)
                    {
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
                        if (response.IsSuccess)
                        {
                            response = Services.MeetingItem.Save(meetingItemModel);
                            if (response.IsSuccess)
                            {
                                result = Services.UserTask.GetTaskById(taskId);
                                if(result.IsSuccess)
                                {
                                    model = (UserTasksModel)result.Result;
                                    if (isDraft)
                                        result = Services.UserTask.MakeTaskUnderProgress(taskId);
                                    else
                                        result = Services.UserTask.AccepteTaskOutCome(taskId);
                                    if (result.IsSuccess)
                                    {
                                        result = Services.UserTask.Save(model);
                                        result.Message = "Task Updated Successfully";
                                    }
                                }
                            }    
                        }    
                    }
                }
                else
                {
                    result = Services.UserTask.GetTaskById(taskId);
                    if (result.IsSuccess)
                    {
                        model = (UserTasksModel)result.Result;
                        if (isDraft)
                            result = Services.UserTask.MakeTaskUnderProgress(taskId);
                        else
                            result = Services.UserTask.AccepteTaskOutCome(taskId);
                    }
                }
            }
            catch (Exception ex)
            {
                response.Message = ex.Message;
                Shared.Issues.Log("MeetingItem.aspx/Add", ex.Message);
            }
            return response;
        }
        [WebMethod]
        public static Response Edit(MeetingItemModel meetingItemModel)
        {
            Response response = new Response();
            try
            {
                if (meetingItemModel.Id > 0)
                {
                    response = Services.MeetingItem.Save(meetingItemModel);
                }
                else
                {
                    response.Message = " MeetingItemId required !!";
                }
                response.IsSuccess = true;
            }
            catch (Exception ex)
            {
                response.Message = ex.Message;
                Shared.Issues.Log("MeetingItem.aspx/Edit", ex.Message);
            }
            return response;
        }
        [WebMethod]
        public static Response Delete(int id)
        {
            Response response = new Response();
            try
            {
                if (id > 0)
                {
                    response = Services.MeetingItem.Delete(id);
                }
                else
                {
                    response.Message = " MeetingItemId required !!";
                }

                response.IsSuccess = true;
            }
            catch (Exception ex)
            {
                response.Message = ex.Message;
                Shared.Issues.Log("MeetingItem.aspx/Delete", ex.Message);
            }
            return response;
        }
    }
}