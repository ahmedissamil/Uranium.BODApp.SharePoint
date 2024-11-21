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
    public class MeetingReportDocument
    {
       
        public static Response Search(MeetingReportsDocumentModel meetingReportModel)
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
                        SPList meetingList = web.Lists.TryGetList(Constants.ListNameMeetingReportsDocument);

                        if (meetingReportModel.MeetingId > 0)
                        {
                            generator.AddStatement(Operator.Equal(new FieldReference("MeetingID") { LookupId = true }, CamlBuilder.ValueType.Lookup, meetingReportModel.MeetingId));
                        }


                        SPQuery query = new SPQuery();
                        query.Query = Query.Build(generator).OrderBy(new FieldReference("Created") { Ascending = false }).GetCaml();
                        query.Query = query.Query.Replace("Query", "");
                        if (meetingList != null)
                        {
                            SPListItemCollection itemCollection = meetingList.GetItems(query);
                            if (itemCollection.Count > 0)
                            {
                                List<SPListItem> sPListItems = itemCollection.Cast<SPListItem>().ToList();
                                List<MeetingReportsDocumentModel> listItems = sPListItems.Select(v => new MeetingReportsDocumentModel(v)).ToList();
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
                Issues.Log("Service/MeetingReportDocument.cs/Search", ex.Message);
                response.Message = ex.Message;
            }
            return response;

        }
    }
}
