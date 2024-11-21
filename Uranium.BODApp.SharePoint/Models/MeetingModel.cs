using Microsoft.SharePoint;
using Microsoft.SharePoint.Workflow;
using Microsoft.Web.Hosting.Administration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using Uranium.BODApp.SharePoint.Model;
using Uranium.BODApp.SharePoint.Service;

namespace Uranium.BODApp.SharePoint.Models
{
    public class MeetingModel
    {
        public MeetingModel() { }
        public MeetingModel(SPListItem item)
        {
            try
            {
                if (item.Attachments != null && item.Attachments.Count > 0)
                {
                    string itemUrl = item.ParentList.ParentWeb.Url + "/" + item.ParentList.RootFolder.Url;
                    foreach (string attachmentName in item.Attachments)
                    {
                        var attachment = new AttachmentModel
                        {
                            AttachmentName = attachmentName,
                            AttachmentLink = itemUrl + "/Attachments/" + item.ID + "/" + attachmentName
                        };

                        this.AttachmentModels.Add(attachment);
                    }
                }
                if (item["ID"] != null)
                {
                    this.Id = int.Parse(item["ID"].ToString());
                }
                if (item["ISPostReportResponsible"] != null)
                {

                    this.ISPostReportResponsble = bool.Parse(item["ISPostReportResponsible"].ToString());
                }
                if (item["Title"] != null)
                {
                    this.Title = !string.IsNullOrWhiteSpace(item["Title"].ToString()) ? item["Title"].ToString() : "";
                }
                if (item["CompanyName"] != null)
                {
                    this.CompanyName = !string.IsNullOrWhiteSpace(item["CompanyName"].ToString()) ? item["CompanyName"].ToString() : "";
                }
                
                if (item["PostReportRejectReason"] != null && !string.IsNullOrWhiteSpace(item["PostReportRejectReason"].ToString()))
                {
                    this.PostReportRejectReason = item["PostReportRejectReason"].ToString();
                }

                if (item["Location1"] != null)
                {
                    this.Location = !string.IsNullOrWhiteSpace(item["Location1"].ToString()) ? item["Location1"].ToString() : "";
                }
                if (item["MeetingDate"] != null && !string.IsNullOrWhiteSpace(item["MeetingDate"].ToString()))
                {
                    this.MeetingDate = DateTime.Parse(item["MeetingDate"].ToString());
                    if (MeetingDate > DateTime.Now)
                    {
                        IsComingMeeting = true;
                    }
                    else
                    {
                        IsComingMeeting = false;
                    }
                }
                if (item["PostReportResponsibles"] != null)
                {
                    SPFieldUserValueCollection userValues = (SPFieldUserValueCollection)item["PostReportResponsibles"];
                    foreach (SPFieldUserValue userValue in userValues)
                    {
                        this.PostReportResponsiblesId.Add(userValue.LookupId);
                    }
                }

                if (item["From1"] != null)
                {
                    this.From = !string.IsNullOrWhiteSpace(item["From1"].ToString()) ? item["From1"].ToString() : "";
                }
                if (item["To"] != null)
                {
                    this.To = !string.IsNullOrWhiteSpace(item["To"].ToString()) ? item["To"].ToString() : "";
                }
                if (item["Created"] != null && !string.IsNullOrWhiteSpace(item["Created"].ToString()))
                {
                    this.Created = DateTime.Parse(item["Created"].ToString());
                }
                if (item["Author"] != null && !string.IsNullOrEmpty(item["Author"].ToString()))
                {
                    SPFieldLookupValue user = new SPFieldLookupValue(item["Author"].ToString());
                    this.CreatedBy = user.LookupValue;
                    this.CreatedById = user.LookupId;
                }
                if (item["PostReportConfirmationResponsible"] != null && !string.IsNullOrEmpty(item["PostReportConfirmationResponsible"].ToString()))
                {
                    SPFieldLookupValue user = new SPFieldLookupValue(item["PostReportConfirmationResponsible"].ToString());
                    this.PostReportConfirmationId = user.LookupId;
                    this.PostReportConfirmation = user.LookupValue;
                }
                if (item["Notice"] != null && !string.IsNullOrWhiteSpace(item["Notice"].ToString()))
                {
                    this.Notice = item["Notice"].ToString();
                }
                if (item["ISMeetingItemRejected"] != null)
                {
                    this.IsMeetingItemRejected = !string.IsNullOrWhiteSpace(item["ISMeetingItemRejected"].ToString()) ? bool.Parse(item["ISMeetingItemRejected"].ToString()) : (bool?)null;
                }
                if (item["ISPreReportConfirmed"] != null)
                {
                    this.IsPreReportConfirmed = !string.IsNullOrWhiteSpace(item["ISPreReportConfirmed"].ToString()) ? bool.Parse(item["ISPreReportConfirmed"].ToString()) : (bool?)null;
                }
                if (item["ISPostReportConfirmed"] != null)
                {
                    this.ISPostReportConfirmed = !string.IsNullOrWhiteSpace(item["ISPostReportConfirmed"].ToString()) ? bool.Parse(item["ISPostReportConfirmed"].ToString()) : (bool?)null;
                }
                if (item["ISPostReportConfirmed"] != null)
                {
                    this.AttachmentUploaded = !string.IsNullOrWhiteSpace(item["AttachmentUploaded"].ToString()) ? item["AttachmentUploaded"].ToString() : "";
                }
                if (item["MeetingProcessAutomation"] != null)
                {
                    SPFieldUrlValue meetingProcessAutomation = new SPFieldUrlValue(item["MeetingProcessAutomation"].ToString());
                    //this.MeetingStatus = !string.IsNullOrWhiteSpace(item["MeetingProcessAutomation"].ToString()) ? item["MeetingProcessAutomation"].ToString() : "";
                    if (meetingProcessAutomation != null)
                    {
                        this.MeetingStatus = meetingProcessAutomation.Description;
                        this.MeetingStatusLink = meetingProcessAutomation.Url;
                    }
                }
                if (item["AttendancePrecentage"] != null)
                {
                    this.AttendancePrecentage = !string.IsNullOrWhiteSpace(item["AttendancePrecentage"].ToString()) ? item["AttendancePrecentage"].ToString() : "";
                }
                if (item["PostReportResponsiblesCount"] != null)
                {
                    this.PostReportResposiblesCount = !string.IsNullOrWhiteSpace(item["PostReportResponsiblesCount"].ToString()) ? int.Parse(item["PostReportResponsiblesCount"].ToString()) : 0;
                }
                if (item["externalAttendeesEmails"] != null && !string.IsNullOrWhiteSpace(item["externalAttendeesEmails"].ToString()))
                {
                    string[] list = item["externalAttendeesEmails"].ToString()
                    .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                    this.ExternalAttendeesEmails = list;
                }
                if (item["MeetingResponsible"] != null)
                {
                    SPFieldLookupValue user = new SPFieldLookupValue(item["MeetingResponsible"].ToString());

                    this.MeetingResponsible = user.LookupValue;
                    this.MeetingResponsibleId = user.LookupId;
                }
                if (item["MeetingAttendees"] != null)
                {
                    SPFieldLookupValueCollection users = new SPFieldLookupValueCollection(item["MeetingAttendees"].ToString());
                    List<UserConfigurationModel> temp = new List<UserConfigurationModel>();
                    foreach (SPFieldLookupValue user in users)
                    {
                        UserConfigurationModel userConfigurationModel = new UserConfigurationModel();
                        userConfigurationModel.UserId = user.LookupId;
                    }
                    this.MeetingAttendee = temp;
                }
                if (item["PostReportOwner"] != null)
                {
                    SPFieldLookupValue user = new SPFieldLookupValue(item["PostReportOwner"].ToString());

                    this.PostReportResponsibleOwner = user.LookupValue;
                    this.PostReportResponsibleOwnerId = user.LookupId;
                }
            }
            catch (Exception ex)
            {
                Shared.Issues.Log("Model/MeetingModel()", ex.Message);
            }
        }
        public SPListItem ToSPItem(SPListItem item)
        {
            try
            {
                if ( !string.IsNullOrWhiteSpace(this.PostReportRejectReason))
                {
                    item["PostReportRejectReason"] = this.PostReportRejectReason;
                }
                if (this.PostReportResponsibleOwnerId > 0)
                {
                    SPUser user = SPContext.Current.Web.SiteUsers.GetByID(this.PostReportConfirmationId);
                    item["PostReportOwner"] = user;
                }
                if(this.ISPostReportResponsble.HasValue)
                {
                    item["ISPostReportResponsible"] = this.ISPostReportResponsble.ToString();

                }
                foreach (AttachmentModel attach in this.AttachmentModels)
                {
                    if (attach.Attachment != null)
                    {
                        item.Attachments.Add(System.IO.Path.GetFileName($"{attach.AttachmentName}"), attach.Attachment);
                    }
                }
                if (!string.IsNullOrWhiteSpace(this.Title))
                {
                    item["Title"] = this.Title;
                }
                if (!string.IsNullOrWhiteSpace(this.CompanyName))
                {
                    item["CompanyName"] = this.CompanyName;
                }
                if (!string.IsNullOrWhiteSpace(this.Location))
                {
                    item["Location1"] = this.Location;
                }
                if (this.MeetingDate != null)
                {
                    item["MeetingDate"] = this.MeetingDate;
                }
                if (!string.IsNullOrWhiteSpace(this.From))
                {
                    item["From1"] = this.From;
                }
                if (!string.IsNullOrWhiteSpace(this.To))
                {
                    item["To"] = this.To;
                }
                item["Created"] = DateTime.Now;
                if (this.CurrentUser!= null)
                {
                    item["Author"] = this.CurrentUser;
                    item["Editor"] = this.CurrentUser;
                }
                if (!string.IsNullOrWhiteSpace(this.Notice))
                {
                    item["Notice"] = this.Notice;
                }
                if (this.IsMeetingItemRejected.HasValue)
                {
                    item["ISMeetingItemRejected"] = this.IsMeetingItemRejected.Value.ToString();
                }
                if (this.IsPreReportConfirmed.HasValue)
                {
                    item["ISPreReportConfirmed"] = this.IsPreReportConfirmed.Value.ToString();
                }
                if (this.ISPostReportConfirmed.HasValue)
                {
                    item["ISPostReportConfirmed"] = this.ISPostReportConfirmed.Value.ToString();
                }
                if (!string.IsNullOrWhiteSpace(this.AttachmentUploaded))
                {
                    item["AttachmentUploaded"] = this.AttachmentUploaded;
                }
                if (!string.IsNullOrWhiteSpace(this.MeetingStatus))
                {
                    SPFieldUrlValue urlValue = new SPFieldUrlValue
                    {
                        Url = this.MeetingStatusLink,
                        Description = this.MeetingStatus
                    };
                    item["MeetingProcessAutomation"] = urlValue;
                }
                if (!string.IsNullOrWhiteSpace(this.AttendancePrecentage))
                {
                    item["AttendancePrecentage"] = this.AttendancePrecentage;
                }
                if (this.PostReportResposiblesCount > 0)
                {
                    item["PostReportResponsiblesCount"] = this.PostReportResposiblesCount.ToString();
                }
                if (this.ExternalAttendeesEmailsIds != null && this.ExternalAttendeesEmailsIds.Count>0)
                {
                    string result =string.Empty;
                    for(int i = 0; this.ExternalAttendeesEmailsIds.Count() > i;i++)
                    {
                        SPUser user = SPContext.Current.Web.SiteUsers.GetByID(int.Parse(this.ExternalAttendeesEmailsIds[i].ToString()));
                        if(!string.IsNullOrWhiteSpace(user.Email))
                        { 
                            result += $"{user.Email},";
                        }
                    }
                    item["externalAttendeesEmails"] = result;
                }
                if (this.MeetingResponsible != null)
                {
                    SPUser user = SPContext.Current.Web.SiteUsers.GetByID(this.MeetingResponsibleId);
                    item["MeetingResponsible"] = user;
                }
                if (this.PostReportConfirmationId > 0)
                {
                    SPUser user = SPContext.Current.Web.SiteUsers.GetByID(this.PostReportConfirmationId);
                    item["PostReportConfirmationResponsible"] = user;
                }
                if (this.PostReportResponsiblesId != null && PostReportResponsiblesId.Count > 0)
                {
                    SPFieldUserValueCollection userValues = new SPFieldUserValueCollection();
                    for (int i = 0; i < this.PostReportResponsiblesId.Count(); i++)
                    {
                        int userId = int.Parse(this.PostReportResponsiblesId[i].ToString());
                        SPUser spUser = SPContext.Current.Web.SiteUsers.GetByID(userId);
                        if (spUser != null)
                        {
                            SPFieldUserValue userValue = new SPFieldUserValue(SPContext.Current.Web, spUser.ID, spUser.Name);
                            userValues.Add(userValue);
                        }
                    }
                    item["PostReportResponsibles"] = userValues;
                }
                if (this.MeetingAttendees != null && this.MeetingAttendees.Any())
                {
                    SPFieldUserValueCollection userValues = new SPFieldUserValueCollection();
                    for (int i = 0; i < this.MeetingAttendees.Count(); i++)
                    {
                        int userId = int.Parse(this.MeetingAttendees[i].ToString());
                        SPUser spUser = SPContext.Current.Web.SiteUsers.GetByID(userId);
                        if (spUser != null)
                        {
                            SPFieldUserValue userValue = new SPFieldUserValue(SPContext.Current.Web, spUser.ID, spUser.Name);
                            userValues.Add(userValue);
                        }
                    }
                    item["MeetingAttendees"] = userValues;
                }
            }
            catch (Exception ex)
            {
                Shared.Issues.Log("Model/MeetingModel/SPItem", ex.Message);
            }
            return item;
        }
        public int Id { get; set; }
        public string Title { get; set; }
        public string CompanyName { get; set; }
        public string Location { get; set; }
        public DateTime? MeetingDate { get; set; }
        public string From { get; set; }
        public string To { get; set; }
        public DateTime Created { get; set; }
        public string CreatedBy { get; set; }
        public int CreatedById { get; set; }
        public bool? IsMeetingItemRejected { get; set; }
        public bool? IsPreReportConfirmed { get; set; }
        public bool? ISPostReportConfirmed { get; set; }
        public int PostReportConfirmationId { get; set; }
        public string PostReportConfirmation { get; set; }
        public bool? ISPostReportResponsble { get; set; }
        public string MeetingStatus { get; set; }
        public string AttachmentUploaded { get; set; }
        public string Notice { get; set; }
        public int PostReportResposiblesCount { get; set; }
        public string AttendancePrecentage { get; set; }
        public List<int> ExternalAttendeesEmailsIds { get; set; }
        public string[] ExternalAttendeesEmails { get; set; }
        public bool? IsComingMeeting { get; set; }
        public SPUser CurrentUser { get; set; }
        public string MeetingStatusLink { get; set; }
        public string PostReportResponsibles {  get; set; }
        public List<int> PostReportResponsiblesId =new List<int>();
        public string MeetingResponsible { get; set; }
        public int MeetingResponsibleId { get; set; }
        public List<int> MeetingAttendees { get; set; }
        public string PostReportResponsibleOwner {  get; set; }
        public int PostReportResponsibleOwnerId {  get; set; }
        public string PostReportRejectReason {  get; set; }
        public List<UserConfigurationModel> MeetingAttendee = new List<UserConfigurationModel>();
        public List<AttachmentModel> AttachmentModels = new List<AttachmentModel>();
        public List<MeetingItemModel> MeetingItems = new List<MeetingItemModel>();
        public List<MeetingReportsDocumentModel> MeetingReportsDocumentModel = new List<MeetingReportsDocumentModel>();
    }
    
}