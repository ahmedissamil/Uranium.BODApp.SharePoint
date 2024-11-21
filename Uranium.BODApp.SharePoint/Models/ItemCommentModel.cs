using Microsoft.SharePoint;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Uranium.BODApp.SharePoint.Models
{
    public class ItemCommentModel
    {
        public ItemCommentModel(){}
        public ItemCommentModel(SPListItem item)
        {
            try
            {

                this.Id = int.Parse(item["ID"].ToString());
                this.Comment = item["Comment1"].ToString() ?? string.Empty;
                if (item["Modified"] != null && !string.IsNullOrEmpty(item["Modified"].ToString()))
                {
                    this.Modified = DateTime.Parse(item["Modified"].ToString());
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
                if (item["Editor"] != null && !string.IsNullOrWhiteSpace(item["Editor"].ToString()))
                {
                    SPFieldLookupValue user = new SPFieldLookupValue(item["Author"].ToString());
                    this.ModifiedBy = user.LookupValue;
                    this.ModifiedById = user.LookupId;
                }
                if (item["ItemId"] != null)
                {
                    var lookupValue = new SPFieldLookupValue(item["ItemId"].ToString());
                    this.MeetingItemId = lookupValue.LookupId; 
                }
            }
            catch(Exception ex)
            {
                Shared.Issues.Log("Model/ItemCommentModel()", ex.Message);
            }
        }
        public SPListItem ToSPItem(SPListItem item)
        {
            try
            {

                item["Comment"] = this.Comment;
                if (this.MeetingItemId != 0)
                {
                    item["ItemId"] = new SPFieldLookupValue(this.MeetingItemId, "").ToString();
                }
            }
            catch (Exception ex)
            {
                Shared.Issues.Log("Model/AttachmentModel/ToSPItem()", ex.Message);
            }
            return item;
        }
        public int Id { get; set; }
        public string Comment { get; set; }
        public string CreatedBy { get; set; }
        public int CreatedById { get; set; }
        public string ModifiedBy { get; set; }
        public int ModifiedById { get; set; }
        public int MeetingItemId { get; set; }
        public DateTime Created { get; set; }
        public DateTime Modified { get; set; }
    }
}