using Microsoft.SharePoint;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;
using Uranium.BODApp.SharePoint.Models;

namespace Uranium.BODApp.SharePoint.Model
{
    public class UserConfigurationModel
    {
        public UserConfigurationModel() { }

        public UserConfigurationModel(SPListItem item)
        {
            try
            {

                if (item.Attachments != null && item.Attachments.Count > 0)
                {
                    string itemUrl = item.ParentList.ParentWeb.Url + "/" + item.ParentList.RootFolder.Url;
                    this.Picture = new AttachmentModel();
                    this.Picture.AttachmentLink = itemUrl + "/Attachments/" + item.ID + "/" + item.Attachments[0];
                    this.Picture.AttachmentName = "ProfilePic";
                }
                if (item["UserName"] != null && !string.IsNullOrEmpty(item["UserName"].ToString()))
                {
                    SPFieldLookupValue user = new SPFieldLookupValue(item["UserName"].ToString());
                    this.UserName = user.LookupValue;
                    this.UserId = user.LookupId;
                }
                if (item["JobTitle"] != null && !string.IsNullOrWhiteSpace(item["JobTitle"].ToString()))
                {
                    this.JobTitle = item["JobTitle"].ToString();
                }
                if (item["Email"] != null && !string.IsNullOrWhiteSpace(item["Email"].ToString()))
                {
                    this.Email = item["Email"].ToString();
                }
                if (item["ArabicName"] != null && !string.IsNullOrWhiteSpace(item["ArabicName"].ToString()))
                {
                    this.ArabicName = item["ArabicName"].ToString();
                }
                if (item["CompanyName"] != null && !string.IsNullOrWhiteSpace(item["CompanyName"].ToString()))
                {
                    this.CompanyName = item["CompanyName"].ToString();
                }
                if (item["Role"] != null && !string.IsNullOrWhiteSpace(item["Role"].ToString()))
                {
                    this.Role = item["Role"].ToString();
                }
                if (item["UserType"] != null && !string.IsNullOrWhiteSpace(item["UserType"].ToString()))
                {
                    this.UserType = item["UserType"].ToString();
                }
            }
            catch (Exception ex)
            {
                Shared.Issues.Log("Model/UserConfigurationModel", ex.Message);
            }
        }
        public string UserName { get; set; }
        public bool IsFBAUser { get; set; }
        public int UserId { get; set; }
        public string JobTitle { get; set; }
        public string Email { get; set; }
        public string ArabicName { get; set; }
        public string CompanyName { get; set; }
        public string Role { get; set; }
        public string UserType { get; set; }
        public AttachmentModel Picture { get; set; }
    }
}