using Microsoft.SharePoint;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Configuration;
using static System.Net.WebRequestMethods;

namespace Uranium.BODApp.SharePoint.Shared
{
    public class Issues
    {
        public static Response Log(string title, string description)
        {
            Response response = new Response();
            try
            {
                string siteUrl = WebConfigurationManager.AppSettings[Constants.WebConfigMeetingSiteUrl];
                if (!string.IsNullOrEmpty(siteUrl))
                {
                    using (SPSite site = new SPSite(siteUrl))
                    {
                        using (SPWeb web = site.OpenWeb())
                        {
                            SPList list = web.Lists.TryGetList(Constants.ListNameIssuesLog);
                            if (list == null)
                            {
                                web.AllowUnsafeUpdates = true;
                                SPListCollection lists = web.Lists;
                                lists.Add(Constants.ListNameIssuesLog, "Issues log list", SPListTemplateType.GenericList);
                                SPList newList = web.Lists[Constants.ListNameIssuesLog];
                                newList.Fields.Add("Description", SPFieldType.Note, true);
                                newList.Update();
                                SPView view = newList.DefaultView;
                                view.ViewFields.Add("Description");
                                view.ViewFields.Add("Created");
                                view.Query = "<OrderBy><FieldRef Name='Created' Ascending='FALSE'></FieldRef></OrderBy>";
                                view.Update();
                                web.Update();
                                SPListItem issueItem = newList.Items.Add();
                                issueItem["Title"] = title;
                                issueItem["Description"] = description;
                                issueItem.Update();
                                web.AllowUnsafeUpdates = false;
                            }
                            else
                            {
                                web.AllowUnsafeUpdates = true;
                                SPListItem issueItem = list.Items.Add();
                                issueItem["Title"] = title;
                                issueItem["Description"] = description;
                                issueItem.Update();
                                web.AllowUnsafeUpdates = false;
                            }
                        }
                    }
                }
                else
                {
                    response.Message = "site url is empty";
                }
            }
            catch (Exception ex)
            {
                response.Message = ex.Message;
                response.Result = WebConfigurationManager.AppSettings[Constants.WebConfigMeetingSiteUrl];
                throw;
            }
            return response;
        }
    }
}
