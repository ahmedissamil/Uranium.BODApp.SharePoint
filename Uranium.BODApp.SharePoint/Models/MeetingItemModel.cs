using Microsoft.SharePoint;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Uranium.BODApp.SharePoint.Models
{
    public class MeetingItemModel
    {
        public MeetingItemModel() { }
        public MeetingItemModel(SPListItem item)
        {
            try
            {

                this.Title = item["ItemTitle"]?.ToString() ?? string.Empty;
                this.Comment = item["Comment"]?.ToString() ?? string.Empty;
                this.Recommendation = item["Recommendation"]?.ToString() ?? string.Empty;
                this.Status = item["Status"]?.ToString() ?? string.Empty;
                if (item["MeetingId"] != null)
                {
                    var lookupValue = new SPFieldLookupValue(item["MeetingId"].ToString());
                    MeetingId = lookupValue.LookupId; // Use LookupId for the integer ID, or LookupValue for the string value
                }
                this.BODMemberComment = item["BODMemberComment"]?.ToString() ?? string.Empty;
                this.CIOComment = item["CIOComment"]?.ToString() ?? string.Empty;
                this.CEOApproval = item["CEOApproval"]?.ToString() ?? string.Empty;
                this.CEOComment = item["CEOComment"]?.ToString() ?? string.Empty;
                this.BODApprovalType = item["BODApprovalType"]?.ToString() ?? string.Empty;
                this.CIOApproval = item["CIOApproval"]?.ToString() ?? string.Empty;
                this.BODApproval = item["BODApproval"]?.ToString() ?? string.Empty;
                this.ItemOrder = item["ItemOrder"] != null ? int.Parse(item["ItemOrder"].ToString()) : 0;
                this.RejectionReason = item["RejectionReason"]?.ToString() ?? string.Empty;
                this.RecommendationStatus = item["RecommendationStatus"]?.ToString() ?? string.Empty;
                this.DecissionApprovedWith = item["DecissionApprovedWith"]?.ToString() ?? string.Empty;
                this.ItemApproval = item["ItemApproval"] as List<SPUser> ?? new List<SPUser>();
                this.Modified = item["Modified"] != null ? (DateTime)item["Modified"] : DateTime.MinValue;
                this.Created = item["Created"] != null ? (DateTime)item["Created"] : DateTime.MinValue;
                if (item["Author"] != null && !string.IsNullOrEmpty(item["Author"].ToString()))
                {
                    SPFieldLookupValue user = new SPFieldLookupValue(item["Author"].ToString());
                    this.CreatedBy = user.LookupValue;
                }
                if (item["Editor"] != null && !string.IsNullOrEmpty(item["Editor"].ToString()))
                {
                    SPFieldLookupValue user = new SPFieldLookupValue(item["Editor"].ToString());
                    this.CreatedBy = user.LookupValue;
                }
                this.Id = int.Parse(item["ID"].ToString());
            }
            catch (Exception ex)
            {
                Shared.Issues.Log("Model/MeetingItemModel()", ex.Message);
            }
        }
        public SPListItem ToSPItem(SPListItem item)
        {
            try
            {

                item["ItemTitle"] = this.Title;
                item["Comment"] = this.Comment;
                item["Recommendation"] = this.Recommendation;
                item["Status"] = this.Status;
                if (this.MeetingId > 0)
                {
                    item["MeetingId"] = new SPFieldLookupValue(this.MeetingId, string.Empty); // Set LookupId; display value is optional here
                }
                item["BODMemberComment"] = this.BODMemberComment;
                item["CIOComment"] = this.CIOComment;
                item["CEOApproval"] = this.CEOApproval;
                item["CEOComment"] = this.CEOComment;
                item["BODApprovalType"] = this.BODApprovalType;
                item["CIOApproval"] = this.CIOApproval;
                item["BODApproval"] = this.BODApproval;
                item["ItemOrder"] = this.ItemOrder;
                item["RejectionReason"] = this.RejectionReason;
                item["RecommendationStatus"] = this.RecommendationStatus;
                item["DecissionApprovedWith"] = this.DecissionApprovedWith;
                item["ItemApproval"] = this.ItemApproval;
            }
            catch (Exception ex)
            {
                Shared.Issues.Log("Model/MeetingItemModel/ToSPItem", ex.Message);
            }
            return item;
        }
        public int Id { get; set; }
        public string Title { get; set; }
        public string Comment {  get; set; }
        public string Recommendation {  get; set; }
        public string Status {  get; set; }
        public int MeetingId {  get; set; }
        public string BODMemberComment {  get; set; }
        public string CIOComment {  get; set; }
        public string CEOApproval {  get; set; }
        public string CEOComment {  get; set; }
        public string BODApprovalType {  get; set; }
        public string CIOApproval {  get; set; }
        public string BODApproval {  get; set; }
        public int ItemOrder {  get; set; }
        public string RejectionReason {  get; set; }
        public string RecommendationStatus {  get; set; }
        public string DecissionApprovedWith {  get; set; }
        public List<SPUser> ItemApproval {  get; set; }
        public DateTime Modified {  get; set; }
        public DateTime Created { get; set; }
        public string CreatedBy { get; set; }
        public string ModifiedBy { get; set; }
        public List<ItemCommentModel> Comments = new List<ItemCommentModel>();
    }
}