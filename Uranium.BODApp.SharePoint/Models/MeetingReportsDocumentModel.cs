using Microsoft.SharePoint;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Uranium.BODApp.SharePoint.Models
{
    public class MeetingReportsDocumentModel
    {
        public MeetingReportsDocumentModel()
        {

        }

        public MeetingReportsDocumentModel(SPListItem item)
        {
            if (item["ID"] != null && !string.IsNullOrWhiteSpace(item["ID"].ToString()))
            {
                this.Id = int.Parse(item["ID"].ToString());
            }

            if (item["MeetingID"] != null)
            {
                var lookupValue = new SPFieldLookupValue(item["MeetingID"].ToString());
                this.MeetingId = lookupValue.LookupId;
            }

            if (item["Title"] != null && string.IsNullOrWhiteSpace(item["Title"].ToString()))
            {
                this.Title = item["Title"].ToString();
            }
            if (item["ReportUrl"] != null && string.IsNullOrWhiteSpace(item["ReportUrl"].ToString()))
            {
                // Retrieve the old URL from the SharePoint list item
                string oldUrl = item["ReportUrl"].ToString();

                // Get the dynamic root URL from the current SharePoint context
                string newRootUrl = SPContext.Current.Web.Url; // E.g., "http://dev-issmail-sp9:47037/ar-eg/Meeting"

                // Find the position of the first '/' after "http://" or "https://"
                int indexOfPath = oldUrl.IndexOf("/", oldUrl.IndexOf("//") + 2);

                // Extract the path from the original URL
                string urlPath = oldUrl.Substring(indexOfPath); // E.g., "/Lists/MeetingReportsDocument/post-report500.pdf"

                // Check if the root URL already contains the relative path
                if (newRootUrl.EndsWith("/ar-eg/Meeting", StringComparison.OrdinalIgnoreCase))
                {
                    // Remove the relative path from the root URL to avoid duplication
                    newRootUrl = newRootUrl.Substring(0, newRootUrl.Length - "/ar-eg/Meeting".Length);
                }

                // Construct the new URL by appending the path to the corrected root URL
                string newUrl = newRootUrl + urlPath;

                // Assign the updated URL to the ReportPDF property
                this.ReportPDF = newUrl;
            }
        }

        public SPFolder ToSPFolder(SPFolder spLibrary)
        {
            if (spLibrary != null)
            {
                spLibrary.Item["ID"] = this.Id;
                spLibrary.Item["MeetingId"] = this.MeetingId;
                spLibrary.Item["Title"] = this.Title;
                spLibrary.Item["ReportUrl"] = this.ReportPDF;
            }
            return spLibrary;
        }



        public int Id { get; set; }
        public int MeetingId { get; set; }
        public string ReportPDF { get; set; }
        public string Title { get; set; }
    }
}
