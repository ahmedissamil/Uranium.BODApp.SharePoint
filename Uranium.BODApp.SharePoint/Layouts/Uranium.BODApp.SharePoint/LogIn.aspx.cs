using Microsoft.SharePoint;
using Microsoft.SharePoint.WebControls;
using Microsoft.Web.Hosting.Administration;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Runtime.Remoting.Contexts;
using System.Security.Cryptography;
using System.Security.Policy;
using System.Web.Security;
using System.Web.Services;
using Uranium.BODApp.SharePoint.Model;
using Uranium.BODApp.SharePoint.Shared;

namespace Uranium.BODApp.SharePoint.Layouts.Uranium.BODApp.SharePoint
{
    public partial class LogIn : LayoutsPageBase
    {
        protected void Page_Load(object sender, EventArgs e)
        {


        }
        [WebMethod]
        public static Response SignIn(string userName, string password)
        {
            Response response = new Response();
            UserConfigurationModel userConfigurationModel = new UserConfigurationModel();
            try
            {
                if (string.IsNullOrWhiteSpace(userName) && string.IsNullOrWhiteSpace(password))
                {
                    bool hasReadPermission = SPContext.Current.Web.DoesUserHavePermissions(SPBasePermissions.ViewListItems);
                    bool hasWritePermission = SPContext.Current.Web.DoesUserHavePermissions(SPBasePermissions.AddListItems);

                    if (hasReadPermission || hasWritePermission)
                    {
                        SPUser sPUser = SPContext.Current.Web.CurrentUser;
                        SPSecurity.RunWithElevatedPrivileges(delegate ()
                        {
                            userConfigurationModel.UserName = sPUser.Name;
                            userConfigurationModel.UserId = sPUser.ID;
                            response = Service.UserConfiguration.Search(userConfigurationModel);
                            if (((List<UserConfigurationModel>)response.Result).Count() > 0)
                            {
                                response.Result = ((List<UserConfigurationModel>)response.Result).FirstOrDefault();
                                response.Message = "User logged in successfully";
                            }
                            else
                            {
                                response.Message = "User logged in successfully but does not exist in configurations list";
                            }
                        });
                    }
                    else
                    {
                        response.Message = "This user has no permission on this site";
                    }
                }
                else
                {
                    bool isAuthenticated = AuthenticateFBAUser(userName, password);
                    if (isAuthenticated)
                    {
                        userConfigurationModel.IsFBAUser = true;
                        string siteUrl = ConfigurationManager.AppSettings[Constants.WebConfigMeetingFBASiteUrl];
                        SPSecurity.RunWithElevatedPrivileges(delegate ()
                        {
                            using (SPSite site = new SPSite(siteUrl))
                            {
                                using (SPWeb web = site.OpenWeb())
                                {
                                    SPUser user = web.AllUsers[$"i:0#.f|fbamembershipprovider|{userName}"];
                                    userConfigurationModel.UserId = user.ID;
                                    response = Service.UserConfiguration.Search(userConfigurationModel);
                                    if (((List<UserConfigurationModel>)response.Result).Count() > 0)
                                    {
                                        response.Result = ((List<UserConfigurationModel>)response.Result).FirstOrDefault();
                                        response.Message = "User logged in successfully";
                                    }
                                    else
                                    {
                                        response.Message = "User logged in successfully but does not exist in configurations list";
                                    }
                                }
                            }
                        });
                    }
                    else
                    {
                        response.Message = "UserName or Password InCorrect";
                    }
                }
            }
            catch (Exception ex)
            {
                response.Message = ex.Message;
                Issues.Log("LogIn/SignIn", ex.Message);
            }
            return response;
        }
        private static bool AuthenticateFBAUser(string username, string password)
        {
            try
            {
                return Membership.ValidateUser(username, password);
            }
            catch (Exception ex)
            {
                Issues.Log("AuthenticateFBAUser", ex.Message);
                return false;
            }
        }
    }
}