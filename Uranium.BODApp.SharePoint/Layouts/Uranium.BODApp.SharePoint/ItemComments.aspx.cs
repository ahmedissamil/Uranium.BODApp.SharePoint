using Microsoft.SharePoint;
using Microsoft.SharePoint.WebControls;
using System;
using System.Web.Services;
using Uranium.BODApp.SharePoint.Models;
using Uranium.BODApp.SharePoint.Shared;

namespace Uranium.BODApp.SharePoint.Layouts.Uranium.BODApp.SharePoint
{
    public partial class ItemComments : LayoutsPageBase
    {
        protected void Page_Load(object sender, EventArgs e)
        {
        }
        [WebMethod]
        public static Response Add(ItemCommentModel itemcomment)
        {
            Response response = new Response();
            try
            {
                response = Services.ItemComment.Save(itemcomment);
            }
            catch (Exception ex)
            {
                response.Message = ex.Message;
                Shared.Issues.Log("ItemCmments/Add", ex.Message);
            }
            return response;
        }
        [WebMethod]
        public static Response GetCommentsByMeetingItemId(int meetingItemId)
        {
            Response response = new Response();
            ItemCommentModel itemCommentModel = new ItemCommentModel();
            try
            {
                itemCommentModel.MeetingItemId = meetingItemId;
                response = Services.ItemComment.Search(itemCommentModel);
            }
            catch (Exception ex)
            {
                response.Message = ex.Message;
                Shared.Issues.Log("GetCommentsByMeetingItemId/Add", ex.Message);
            }
            return response;
        }
    }
}
