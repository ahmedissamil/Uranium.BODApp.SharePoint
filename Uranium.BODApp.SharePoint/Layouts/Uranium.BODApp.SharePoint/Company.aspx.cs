using Microsoft.SharePoint;
using Microsoft.SharePoint.WebControls;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web.Configuration;
using System.Web.Services;
using Uranium.BODApp.SharePoint.Models;
using Uranium.BODApp.SharePoint.Shared;

namespace Uranium.BODApp.SharePoint.Layouts.Uranium.BODApp.SharePoint
{
    public partial class Company : LayoutsPageBase
    {
        protected void Page_Load(object sender, EventArgs e)
        {
        }
        [WebMethod]
        public static Response GetCompanyNames()
        {
            Response response = new Response();
            List<string> portfoliocompanyNames = new List<string>();
            List<string> ekuityinvestmentscompanyNames = new List<string>();
            List<string> companyNames = new List<string>();
            try
            {
                response = Services.Company.GetCompaniesName(Constants.WebConfigEkuityinvestmentsSiteUrl);
                portfoliocompanyNames = (List<string>)response.Result;
                response = Services.Company.GetCompaniesName(Constants.WebConfigPortfolioSiteUrl);
                ekuityinvestmentscompanyNames = (List<string>)response.Result;
                companyNames = portfoliocompanyNames.Union(ekuityinvestmentscompanyNames).ToList();
                response.Result = companyNames;
                response.IsSuccess = true;
            }
            catch (Exception ex)
            {
                response.Message =ex.Message;
                Shared.Issues.Log("Company/GetCompanyNames", ex.Message);
            }
            return response;
        }
    }
}

