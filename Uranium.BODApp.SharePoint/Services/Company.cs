using Microsoft.SharePoint;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Uranium.BODApp.SharePoint.Shared;

namespace Uranium.BODApp.SharePoint.Services
{
    public class Company
    {
        public static Response GetCompaniesName(string webSiteUrl)
        {
            Response response = new Response();
            List<string> companyNames = new List<string>();
            try
            {
                string siteUrl = ConfigurationManager.AppSettings[Constants.WebConfigSiteUrl];
                using (SPSite site = new SPSite(siteUrl))
                {
                    using (SPWeb web = site.OpenWeb(ConfigurationManager.AppSettings[webSiteUrl]))
                    {
                        if (web.Webs.Count > 0)
                        {
                            foreach (SPWeb subSite in web.Webs)
                            {
                                companyNames.Add(subSite.Title);
                            }
                        }
                    }
                    response.Result = companyNames;
                    response.IsSuccess = true;
                }
            }
            catch (Exception ex)
            {
                Shared.Issues.Log("Model/GetCompaniesName", ex.Message);
                response.Message = ex.Message;
            }
            return response;
        }
    }
}