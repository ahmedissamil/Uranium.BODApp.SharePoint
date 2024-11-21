using Microsoft.SharePoint;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Uranium.BODApp.SharePoint.Models
{
    public class AttachmentModel
    {
        public AttachmentModel() { }
        public AttachmentModel(SPListItem item)
        {
            try
            {
                if (item.Attachments != null && item.Attachments.Count > 0)
                {
                    string itemUrl = item.ParentList.ParentWeb.Url + "/" + item.ParentList.RootFolder.Url;
                    this.AttachmentName = item.Attachments[0];
                    this.AttachmentLink = itemUrl + "/Attachments/" + item.ID + "/" + this.AttachmentName;
                }
            }
            catch(Exception ex)
            {
                Shared.Issues.Log("Model/AttachmentModel()", ex.Message);
            }
        }
        public SPItem ToSPItem(SPListItem item)
        {
            try
            {
                if (this.Attachment != null)
                {
                    item.Attachments.Add(System.IO.Path.GetFileName($"{this.AttachmentName}"), this.Attachment);
                }
            }
            catch(Exception ex)
            {
                Shared.Issues.Log("Model/AttachmentModel/ToSPItem()", ex.Message);
            }
            return item;
        }
        public byte[] Attachment { get; set; }
        public string AttachmentName { get; set; }
        public string AttachmentLink { get; set; }
    }
}
