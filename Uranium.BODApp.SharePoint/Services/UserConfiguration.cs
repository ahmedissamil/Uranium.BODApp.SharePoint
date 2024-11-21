using Microsoft.SharePoint;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Services.Description;
using System.Web.UI.WebControls;
using Uranium.BODApp.SharePoint.Model;
using Uranium.BODApp.SharePoint.Shared;
using CamlBuilder;

namespace Uranium.BODApp.SharePoint.Service
{
    public static class UserConfiguration
    {
        public static Response Search(UserConfigurationModel userConfigurationModel)
        {
            Response response = new Response();
            string siteUrl = "";

            if (userConfigurationModel.IsFBAUser)
            {
                siteUrl = ConfigurationManager.AppSettings[Constants.WebConfigMeetingFBASiteUrl];
            }
            else
            {
                siteUrl = ConfigurationManager.AppSettings[Constants.WebConfigMeetingSiteUrl];
            }
            try
            {
                using (SPSite site = new SPSite(siteUrl))
                {
                    using (SPWeb web = site.OpenWeb())
                    {
                        var generator = LogicalJoin.And();
                        SPList userConfigurationList = web.Lists.TryGetList(Constants.ListNameUsersConfigurationsApp);
                        if (userConfigurationModel.UserId > 0)
                        {
                            generator.AddStatement(Operator.Equal(new FieldReference("UserName") { LookupId = true }, CamlBuilder.ValueType.User, userConfigurationModel.UserId));
                        }
                        if (!string.IsNullOrEmpty(userConfigurationModel.JobTitle))
                        {
                            generator.AddStatement(Operator.Equal("JobTitle", CamlBuilder.ValueType.Text, userConfigurationModel.JobTitle));
                        }
                        if (!string.IsNullOrEmpty(userConfigurationModel.Email))
                        {
                            generator.AddStatement(Operator.Equal("Email", CamlBuilder.ValueType.Text, userConfigurationModel.Email));
                        }
                        if (!string.IsNullOrEmpty(userConfigurationModel.ArabicName))
                        {
                            generator.AddStatement(Operator.Equal("ArabicName", CamlBuilder.ValueType.Text, userConfigurationModel.ArabicName));
                        }
                        if (!string.IsNullOrEmpty(userConfigurationModel.CompanyName))
                        {
                            generator.AddStatement(Operator.Equal("CompanyName", CamlBuilder.ValueType.Text, userConfigurationModel.CompanyName));
                        }
                        if (!string.IsNullOrEmpty(userConfigurationModel.Role))
                        {
                            generator.AddStatement(Operator.Equal("Role", CamlBuilder.ValueType.Text, userConfigurationModel.Role));
                        }
                        if (!string.IsNullOrEmpty(userConfigurationModel.UserType))
                        {
                            generator.AddStatement(Operator.Equal("UserType", CamlBuilder.ValueType.Text, userConfigurationModel.UserType));
                        }
                        SPQuery query = new SPQuery();
                        query.Query = Query.Build(generator).OrderBy(new FieldReference("Created") { Ascending = true }).GetCaml();
                        query.Query = query.Query.Replace("Query", "");
                        if (userConfigurationList != null)
                        {
                            SPListItemCollection itemCollection = userConfigurationList.GetItems(query);
                            List<SPListItem> sPListItems = itemCollection.Cast<SPListItem>().ToList();
                            List<UserConfigurationModel> listItems = sPListItems.Select(v => new UserConfigurationModel(v)).ToList();
                            listItems = listItems.Select(x =>
                            {
                                x.IsFBAUser = userConfigurationModel.IsFBAUser;
                                return x;
                            }).ToList();

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
                        response.IsSuccess = true;
                    }
                }
            }
            catch (Exception ex)
            {
                Issues.Log("Service/UserConfigureation.cs/Search", ex.Message);
                response.Message = ex.Message;
            }
            return response;
        }
    }
}
