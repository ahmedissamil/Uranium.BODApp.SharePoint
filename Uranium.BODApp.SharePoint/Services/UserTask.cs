using CamlBuilder;
using Microsoft.SharePoint;
using Microsoft.Web.Hosting.Administration;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Configuration;
using Uranium.BODApp.SharePoint.Model;
using Uranium.BODApp.SharePoint.Models;
using Uranium.BODApp.SharePoint.Shared;


namespace Uranium.BODApp.SharePoint.Services
{
    public class UserTask
    {
        public static Response Search(UserTasksModel userTasksModel, Pagination pagination, DateTime? from, DateTime? to)
        {
            Response response = new Response();
            response.pagination = new Pagination();
            response.pagination = pagination;
            string siteUrl = ConfigurationManager.AppSettings[Constants.WebConfigMeetingSiteUrl];
            //string siteUrl = Constants.WebConfigMeetingSiteUrl;
            try
            {
                using (SPSite site = new SPSite(siteUrl))
                {
                    using (SPWeb web = site.OpenWeb())
                    {
                        var generator = LogicalJoin.And();
                        SPList userTasksList = web.Lists.TryGetList(Constants.ListNameWorkflowTasks);
                        if (!string.IsNullOrEmpty(userTasksModel.AssignedTo))
                        {
                            generator.AddStatement(Operator.Equal(new FieldReference("AssignedTo") { LookupId = true }, CamlBuilder.ValueType.User, userTasksModel.AssignedToId));
                        }
                        if (!string.IsNullOrEmpty(userTasksModel.Status) && userTasksModel.Status == "لم يتم البدء" || userTasksModel.Status == "قيد التقدم")
                        {
                            //generator.AddStatement(Operator.Equal("Status", CamlBuilder.ValueType.Text, "لم يتم البدء"));
                            //generator.AddStatement(Operator.Equal("Status", CamlBuilder.ValueType.Text, "لم يتم البدء"));

                            var orGenerator = LogicalJoin.Or(); // Use a separate generator for OR conditions
                            orGenerator.AddStatement(Operator.Equal("Status", CamlBuilder.ValueType.Text, "لم يتم البدء"));
                            orGenerator.AddStatement(Operator.Equal("Status", CamlBuilder.ValueType.Text, "قيد التقدم"));
                            generator.AddStatement(orGenerator); // Add the OR generator to the main generator


                        }
                        else if(!string.IsNullOrEmpty(userTasksModel.Status))
                        {
                            generator.AddStatement(Operator.Equal("Status", CamlBuilder.ValueType.Text, userTasksModel.Status));
                        }
                        if (userTasksModel.Created.HasValue)
                        {
                            // Format the date to ignore time (ISO 8601 date-only format)
                            string formattedDate = userTasksModel.Created.Value.ToString("yyyy-MM-dd");
                            generator.AddStatement(Operator.Equal("Created", CamlBuilder.ValueType.DateTime, formattedDate));
                        }
                        if (from.HasValue)
                        {
                            string formattedDate = from.Value.ToString("yyyy-MM-dd");
                            generator.AddStatement(Operator.GreaterThanOrEqualTo("Created", CamlBuilder.ValueType.DateTime, formattedDate));
                        }
                        if (to.HasValue)
                        {
                            string formattedDate = to.Value.ToString("yyyy-MM-dd");
                            generator.AddStatement(Operator.LowerThanOrEqualTo("Created", CamlBuilder.ValueType.DateTime, formattedDate));
                        }



                        SPQuery query = new SPQuery();

                        query.Query = Query.Build(generator).OrderBy(new FieldReference("Created") { Ascending = false }).GetCaml();

                        query.Query = query.Query.Replace("Query", "");

                        if (userTasksList != null)
                        {
                            SPListItemCollection itemCollection = userTasksList.GetItems(query);
                            List<SPListItem> sPListItems = itemCollection.Cast<SPListItem>().ToList();
                            response.pagination.TotalItems = sPListItems.Count;
                            sPListItems = sPListItems.Skip((pagination.PageNumber - 1) * pagination.PageSize).Take(pagination.PageSize).ToList();
                            List<UserTasksModel> listItems = sPListItems.Select(v => new UserTasksModel(v)).ToList();
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

                        response.IsSuccess = true;
                    }
                }
            }
            catch (Exception ex)
            {
                Issues.Log("Service/UserTasks.cs/Search", ex.Message);
                response.Message = ex.Message;
            }
            return response;

        }
        public static Response GetTaskById(int id)
        {
            Response response = new Response();
            Response MeetingItemResult = new Response();
            Response meetingReportsDocumentResult = new Response();

            MeetingItemModel temp = new MeetingItemModel();
            Response itemCommentResult = new Response();
            MeetingItemModel meetingItemtemp = new MeetingItemModel();
            ItemCommentModel commentTemp = new ItemCommentModel();
            MeetingReportsDocumentModel meetingReportsDocumentModel = new MeetingReportsDocumentModel();

            try
            {
                SPList tasksList = SPContext.Current.Web.Lists.TryGetList(Constants.ListNameWorkflowTasks);
                SPListItem task = tasksList.GetItemById(id);
                if (task != null)
                {
                    UserTasksModel model = new UserTasksModel(task);
                    temp.MeetingId = model.MeetingId;
                    MeetingItemResult = Services.MeetingItem.Search(temp);
                    if (MeetingItemResult.IsSuccess)
                    {
                        model.MeetingItems = (List<MeetingItemModel>)MeetingItemResult.Result;
                    }
                    if(model.MeetingItems != null)
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
                    meetingReportsDocumentModel.MeetingId = model.MeetingId;
                    meetingReportsDocumentResult = Services.MeetingReportDocument.Search(meetingReportsDocumentModel);
                    if(meetingReportsDocumentResult.IsSuccess)
                    {
                        model.MeetingReportsDocumentModel = (List<MeetingReportsDocumentModel>)meetingReportsDocumentResult.Result;

                    }
                    
                    response.Message = "Task Returned Successfully";
                    response.IsSuccess = true;
                    response.Result = model;
                }
            }
            catch (Exception ex)
            {
                response.Message = ex.Message;
                Issues.Log("Service/UserTasks.cs/GetTaskById", ex.Message);
            }
            return response;
        }
        public static Response Save(UserTasksModel userTasksModel)
        {
            Response response = new Response();
            string siteURL = WebConfigurationManager.AppSettings[Constants.WebConfigMeetingSiteUrl];
            //string siteURL = Constants.WebConfigMeetingSiteUrl;
            try
            {
                SPSecurity.RunWithElevatedPrivileges(delegate ()
                {
                    using (SPSite site = new SPSite(siteURL))
                    {
                        using (SPWeb web = site.OpenWeb())
                        {
                            web.AllowUnsafeUpdates = true;
                            SPList list = web.Lists.TryGetList(Constants.ListNameWorkflowTasks);
                            if (userTasksModel.Id > 0)
                            {
                                if (list != null)
                                {
                                    SPItem sPItem = null;
                                    SPListItem sPListItem = list.GetItemById(userTasksModel.Id);
                                    sPItem = userTasksModel.ToSPItem(sPListItem);
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

                                sPListItem = userTasksModel.ToSPItem(sPListItem);
                                sPListItem.Update();
                                response.Result = sPListItem.ID;
                                response.Message = $"Item Added successfully";
                            }
                            web.AllowUnsafeUpdates = false;
                        }
                    }
                });
                response.IsSuccess = true;
            }
            catch (Exception ex)
            {
                Issues.Log("Service/UserTasks.cs/GetTaskById", ex.Message);
                
                response.Message = ex.Message;
            }
            return response;
        }
        public static Response MakeTaskUnderProgress(int taskId)
        {
            Response response = new Response();
            UserTasksModel userTasksModel = new UserTasksModel();
            try
            {

                userTasksModel.Status = "قيد التقدم";
                string siteURL = WebConfigurationManager.AppSettings[Constants.WebConfigMeetingSiteUrl];
                using (SPSite site = new SPSite(siteURL))
                {
                    SPSecurity.RunWithElevatedPrivileges(delegate ()
                    {
                        using (SPWeb web = site.OpenWeb())
                        {
                            SPList list = web.Lists.TryGetList(Constants.ListNameWorkflowTasks);
                            if (list != null)
                            {
                                web.AllowUnsafeUpdates = true;
                                SPListItem sPListItem = list.GetItemById(taskId);
                                SPItem sPItem = userTasksModel.ToSPItem(sPListItem);
                                sPListItem.Update();
                                response.Result = sPListItem.ID;
                                response.IsSuccess = true;
                                response.Message = $"Task Changed !!";
                                web.AllowUnsafeUpdates = false;
                            }
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                Issues.Log("Service/UserTasks.cs/MakeTaskUnderProgress", ex.Message);
                
            }
            return response;
        }
        public static Response CheckFromMeetingItemStatus(int meetingId)
        {
            Response response = new Response();
            MeetingModel model = new MeetingModel();
            List<MeetingItemModel> items = new List<MeetingItemModel>();
            try
            {
                response = Services.Meeting.GetMeetingById(meetingId);
                model = (MeetingModel)response.Result;
                if (model != null && model.MeetingItems != null)
                {

                    items = model.MeetingItems.Where(x=> x.Status == "Rejected").ToList();
                }
                response.IsSuccess = true;
                response.Result = items.Count == 0 ? true : false;
            }
            catch(Exception ex)
            {
                response.Message =ex.Message;
                Issues.Log("Service/UserTasks.cs/CheckFromMeetingItemStatus", ex.Message);
            }
            return response;
        }
        public static Response AccepteTaskOutCome(int taskId)
        {
            Response response = new Response();
            UserTasksModel userTasksModel = new UserTasksModel();
            userTasksModel.TaskOutCome = "تمت الموافقة";
            userTasksModel.Status = "مكتملة";
            try
            {

            string siteURL = WebConfigurationManager.AppSettings[Constants.WebConfigMeetingSiteUrl];
            using (SPSite site = new SPSite(siteURL))
            {
                SPSecurity.RunWithElevatedPrivileges(delegate ()
                {
                    using (SPWeb web = site.OpenWeb())
                    {
                        SPList list = web.Lists.TryGetList(Constants.ListNameWorkflowTasks);
                        if (list != null)
                        {
                            web.AllowUnsafeUpdates = true;
                            SPListItem sPListItem = list.GetItemById(taskId);
                            SPItem sPItem = userTasksModel.ToSPItem(sPListItem);
                            sPListItem.Update();
                            response.Result = sPListItem.ID;
                            response.IsSuccess = true;
                            response.Message = $"Task Changed !!";
                            web.AllowUnsafeUpdates = false;
                        }
                    }
                });
            }
            }
            catch(Exception ex)
            {
                Issues.Log("Service/UserTasks.cs/AccepteTaskOutCome", ex.Message);
            }
            return response;
        }
        public static Response RejectTaskOutCome(int taskId)
        {
            Response response = new Response();
            UserTasksModel userTasksModel = new UserTasksModel();
            userTasksModel.TaskOutCome = "تم الرفض";
            userTasksModel.Status = "مكتملة";
            string siteURL = WebConfigurationManager.AppSettings[Constants.WebConfigMeetingSiteUrl];
            try
            {

            using (SPSite site = new SPSite(siteURL))
            {
                SPSecurity.RunWithElevatedPrivileges(delegate ()
                {
                    using (SPWeb web = site.OpenWeb())
                    {
                        SPList list = web.Lists.TryGetList(Constants.ListNameWorkflowTasks);
                        if (list != null)
                        {
                            web.AllowUnsafeUpdates = true;
                            SPListItem sPListItem = list.GetItemById(taskId);
                            SPItem sPItem = userTasksModel.ToSPItem(sPListItem);
                            sPListItem.Update();
                            response.Result = sPListItem.ID;
                            response.IsSuccess = true;
                            response.Message = $"Task Changed !!";
                            web.AllowUnsafeUpdates = false;
                        }
                    }


                });
            }
            }
            catch(Exception ex)
            {
                Issues.Log("Service/UserTasks.cs/RejectTaskOutCome", ex.Message);
            }
            return response;
        }
    }
}
