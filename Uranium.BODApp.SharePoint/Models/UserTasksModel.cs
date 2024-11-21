using Microsoft.SharePoint;
using Microsoft.SharePoint.ApplicationPages.Calendar.Exchange;
using Microsoft.SharePoint.BusinessData.Administration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Uranium.BODApp.SharePoint.Layouts.Uranium.BODApp.SharePoint;
using Uranium.BODApp.SharePoint.Services;
using Uranium.BODApp.SharePoint.Shared;

namespace Uranium.BODApp.SharePoint.Models
{
    public class UserTasksModel
    {
        public UserTasksModel() { }
        public UserTasksModel(SPItem item)
        {
            try
            {

                this.Id = int.Parse(item["ID"].ToString());
                if (item["AssignedTo"] != null && !string.IsNullOrEmpty(item["AssignedTo"].ToString()))
                {
                    SPFieldLookupValue user = new SPFieldLookupValue(item["AssignedTo"].ToString());
                    this.AssignedTo = user.LookupValue;
                    this.AssignedToId = user.LookupId;
                }
                if (item["Title"] != null && !string.IsNullOrWhiteSpace(item["Title"].ToString()))
                {
                    this.Title = item["Title"].ToString();
                }
                if (item["Status"] != null && !string.IsNullOrWhiteSpace(item["Status"].ToString()))
                {
                    this.Status = item["Status"].ToString();
                }
                if (item["Created"] != null && !string.IsNullOrWhiteSpace(item["Created"].ToString()))
                {
                    this.Created = DateTime.Parse(item["Created"].ToString());
                }
                if (item["Body"] != null && !string.IsNullOrWhiteSpace(item["Body"].ToString()))
                {
                    Response response = new Response();
                    this.MeetingId = int.Parse(item["Body"].ToString());
                    response = Services.Meeting.GetMeetingById(this.MeetingId);
                    if (response.IsSuccess)
                    {
                        this.MeetingTitle = ((MeetingModel)response.Result).Title;
                    }
                }
                if (item["TaskOutcome"] != null && string.IsNullOrWhiteSpace(item["TaskOutcome"].ToString()))
                {
                    this.TaskOutCome = item["TaskOutcome"].ToString();
                }
            }
            catch (Exception ex)
            {
                Shared.Issues.Log("Model/UserTasksModel", ex.Message);
            }
        }
        public SPListItem ToSPItem(SPListItem item)
        {
            try
            {

            if (!string.IsNullOrEmpty(this.AssignedTo))
            {
                SPFieldLookupValue user = new SPFieldLookupValue(this.AssignedToId, this.AssignedTo);
                item["AssignedTo"] = user.ToString();
            }
            if (!string.IsNullOrWhiteSpace(this.Title))
            {
                item["Title"] = this.Title;
            }
            if (!string.IsNullOrWhiteSpace(this.Status))
            {
                item["Status"] = this.Status;
            }
            if (this.MeetingId > 0)
            {
                item["Body"] = this.MeetingId.ToString();
            }
            if (!string.IsNullOrWhiteSpace(this.TaskOutCome))
            {
                item["TaskOutcome"] = this.TaskOutCome;
            }
            }
            catch(Exception ex)
            {
                Shared.Issues.Log("Model/UserTasksModel/ToSPItem", ex.Message);
            }
            return item;
        }
        public int Id { get; set; }
        public string AssignedTo { get; set; }
        public int AssignedToId { get; set; }
        public string Title { get; set; }
        public string Status { get; set; }
        public DateTime? Created { get; set; }
        public string MeetingTitle { get; set; }
        public int MeetingId { get; set; }
        public string TaskOutCome { get; set; }
        public MeetingModel MeetingModel { get; set; }
        public List<MeetingItemModel> MeetingItems = new List<MeetingItemModel>();
        public List<MeetingReportsDocumentModel> MeetingReportsDocumentModel = new List<MeetingReportsDocumentModel>();
    }
}