using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace LightSwitchApplication
{
    /// <summary>
    /// Summary description for ManageAlerts
    /// </summary>
    public class ManageAlerts : IHttpHandler
    {

        public void ProcessRequest(HttpContext context)
        {

            try
            {
                using (var ctx = ServerApplicationContext.CreateContext())
                {
                    ctx.DataWorkspace.ApplicationData.RefreshProjectAlerts();

                    context.Response.Clear();
                    context.Response.Flush();
                }

            }
            catch (Exception ex)
            {
                context.Response.ContentType = "text/plain";
                context.Response.Write(ex.Message);
            }
            finally
            {
                context.Response.End();
            }


        }

        public bool IsReusable
        {
            get
            {
                return false;
            }
        }
    }
}