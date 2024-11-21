using CamlBuilder;
using Microsoft.SharePoint;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web.Configuration;
using Uranium.BODApp.SharePoint.Model;
using Uranium.BODApp.SharePoint.Models;
using Uranium.BODApp.SharePoint.Shared;

namespace Uranium.BODApp.SharePoint.Services
{
    public class MeetingItem
    {
        public static Response Search(MeetingItemModel meetingItemModel)
        {
            Response response = new Response();
            string siteUrl = WebConfigurationManager.AppSettings[Constants.WebConfigMeetingSiteUrl];
            try
            {
                using (SPSite site = new SPSite(siteUrl))
                {
                    using (SPWeb web = site.OpenWeb())
                    {
                        var generator = LogicalJoin.And();
                        SPList meetingList = web.Lists.TryGetList(Constants.ListNameMeetingItems);
                        if (meetingItemModel.MeetingId > 0)
                        {
                            generator.AddStatement(Operator.Equal(new FieldReference("MeetingId") { LookupId = true }, CamlBuilder.ValueType.Lookup, meetingItemModel.MeetingId));
                        }
                        // Format the date to ignore time (ISO 8601 date-only format)

                        SPQuery query = new SPQuery();

                        query.Query = Query.Build(generator).OrderBy(new FieldReference("ItemOrder") { Ascending = true }).GetCaml();

                        query.Query = query.Query.Replace("Query", "");

                        if (meetingList != null)
                        {
                            SPListItemCollection itemCollection = meetingList.GetItems(query);
                            if (itemCollection.Count > 0)
                            {
                                List<SPListItem> sPListItems = itemCollection.Cast<SPListItem>().ToList();
                                List<MeetingItemModel> listItems = sPListItems.Select(v => new MeetingItemModel(v)).ToList();
                                
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
                        }
                        response.IsSuccess = true;

                    }
                }
            }
            catch (Exception ex)
            {
                Issues.Log("Service/MeetingItems.cs/Search", ex.Message);
                response.Message = ex.Message;
            }
            return response;

        }
        public static Response SaveList(List<MeetingItemModel> meetingItemModelList)
        {
            Response response = new Response();
            string siteURL = WebConfigurationManager.AppSettings[Constants.WebConfigMeetingSiteUrl];
            //string siteURL = Constants.WebConfigMeetingSiteUrl;
            try
            {
                
                using (SPSite site = new SPSite(siteURL))
                {
                    using (SPWeb web = site.OpenWeb())
                    {
                        web.AllowUnsafeUpdates = true;
                        SPList list = web.Lists.TryGetList(Constants.ListNameMeetingItems);
                        foreach(MeetingItemModel item in meetingItemModelList)
                        {
                            if (item.Id > 0)
                            {
                                if (list != null)
                                {
                                    SPItem sPItem = null;
                                    SPListItem sPListItem = list.GetItemById(item.Id);
                                    sPItem = item.ToSPItem(sPListItem);
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
                                sPListItem = item.ToSPItem(sPListItem);
                                sPListItem.Update();
                                response.Result = sPListItem.ID;
                                response.Message = $"Item Added successfully";
                            }
                            web.AllowUnsafeUpdates = false;
                        }
                    }
                }
                
                response.IsSuccess = true;
            }
            catch (Exception ex)
            {
                Issues.Log("Service/MeetingItems.cs/SaveList", ex.Message);
                response.Message = ex.Message;
            }
            return response;
        }
        public static Response Save(MeetingItemModel meetingItemModel)
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
                            SPList list = web.Lists.TryGetList(Constants.ListNameMeetingItems);
                                if (meetingItemModel.Id > 0)
                                {
                                    if (list != null)
                                    {
                                        SPItem sPItem = null;
                                        SPListItem sPListItem = list.GetItemById(meetingItemModel.Id);
                                        sPItem = meetingItemModel.ToSPItem(sPListItem);
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
                                    sPListItem = meetingItemModel.ToSPItem(sPListItem);
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
                Issues.Log("Service/MeetingItems.cs/Save", ex.Message);
                response.Message = ex.Message;
            }
            return response;
        }
        public static Response ValidateMeetingIteemsModel(MeetingItemModel meetingItemModel)
        {
            Response response = new Response();
            try
            {

                if (!string.IsNullOrWhiteSpace(meetingItemModel.Title))
                {
                    response.IsSuccess = true;
                }
                else
                {
                    response.Message =  $"Meeting Title required";
                    response.IsSuccess = false;
                    return response;
                }

                if (!string.IsNullOrWhiteSpace(meetingItemModel.Comment))
                {
                    response.IsSuccess = true;
                }
                else
                {
                    response.Message = $"Meeting Comment required";
                    response.IsSuccess = false;
                    return response;
                }

                if (!string.IsNullOrWhiteSpace(meetingItemModel.Recommendation))
                {
                    response.IsSuccess = true;
                }
                else
                {
                    response.Message = $"Meeting Recommendation required";
                    response.IsSuccess = false;
                    return response;
                }
            }
            catch (Exception ex)
            {
                Issues.Log("Service/MeetingItems.cs/ValidateMeetingIteemsModel", ex.Message);
            }
            
            return response;
        }
        public static Response Delete(int id)
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
                            SPList list = web.Lists.TryGetList(Constants.ListNameMeetingItems);
                            if (id > 0)
                            {
                                if (list != null)
                                {
                                    SPListItem sPListItem = list.GetItemById(id);
                                    sPListItem.Delete();
                                    response.Result = sPListItem.ID;
                                    response.Message = $"Item Deleted successfully";
                                    web.AllowUnsafeUpdates = false;
                                }
                                else
                                {
                                    response.Message = $"List Not Founded founded";
                                }
                            }
                            
                        }
                    }
                });
                response.IsSuccess = true;
            }
            catch (Exception ex)
            {
                Shared.Issues.Log("MeetingItem.sc/ ValidateMeetingIteemsModel", ex.Message);
                response.Message = ex.Message;
            }
            return response;
        }

    }
}
