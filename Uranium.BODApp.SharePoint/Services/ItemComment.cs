using CamlBuilder;
using Microsoft.SharePoint;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Configuration;
using Uranium.BODApp.SharePoint.Models;
using Uranium.BODApp.SharePoint.Shared;

namespace Uranium.BODApp.SharePoint.Services
{
    public class ItemComment
    {
        public static Response Save(ItemCommentModel itemCommentModel)
        {
            Response response = new Response();
            string siteURL = WebConfigurationManager.AppSettings[Constants.WebConfigMeetingSiteUrl];
            try
            {
                using (SPSite site = new SPSite(siteURL))
                {
                    using (SPWeb web = site.OpenWeb())
                    {
                        web.AllowUnsafeUpdates = true;
                        SPList list = web.Lists.TryGetList(Constants.ListNameItemComments);
                        if (itemCommentModel.Id > 0)
                        {
                            if (list != null)
                            {
                                SPListItem sPListItem = list.GetItemById(itemCommentModel.Id);
                                SPItem sPItem = itemCommentModel.ToSPItem(sPListItem);
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
                            sPListItem = itemCommentModel.ToSPItem(sPListItem);
                            sPListItem.Update();
                            response.Result = sPListItem.ID;
                            response.Message = $"Item Added successfully";
                        }
                        web.AllowUnsafeUpdates = false;
                    }
                }
                response.IsSuccess = true;
            }
            catch (Exception ex)
            {
                Shared.Issues.Log("ItemComment.cs/Save", ex.Message);
                response.Message = ex.Message;
            }
            return response;
        }
        public static Response Search(ItemCommentModel itemCommentModel)
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
                        SPList meetingList = web.Lists.TryGetList(Constants.ListNameItemComments);
                        if (itemCommentModel.MeetingItemId > 0)
                        {
                            generator.AddStatement(Operator.Equal("ItemId", CamlBuilder.ValueType.Number, itemCommentModel.MeetingItemId));
                        }
                        SPQuery query = new SPQuery();
                        query.Query = Query.Build(generator).OrderBy(new FieldReference("Created") { Ascending = true }).GetCaml();
                        query.Query = query.Query.Replace("Query", "");
                        if (meetingList != null)
                        {
                            SPListItemCollection itemCollection = meetingList.GetItems(query);
                            if (itemCollection.Count > 0)
                            {
                                List<SPListItem> sPListItems = itemCollection.Cast<SPListItem>().ToList();
                                List<ItemCommentModel> listItems = sPListItems.Select(v => new ItemCommentModel(v)).ToList();
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
                Issues.Log("Service/ItemComments.cs/Search", ex.Message);
                response.Message = ex.Message;
            }
            return response;
        }
    }
}