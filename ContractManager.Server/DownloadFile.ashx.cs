using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace LightSwitchApplication
{
    /// <summary>
    /// Summary description for DownloadFile
    /// </summary>
    public class DownloadFile : IHttpHandler
    {

        public void ProcessRequest(HttpContext context)
        {
            int fileId = -1;

            // get the file name from the querystring
            if (context.Request.QueryString["fileId"] != null)
            {
                fileId = Convert.ToInt32(context.Request.QueryString["fileId"].ToString());
            }


            try
            {
                using (var ctx = ServerApplicationContext.CreateContext())
                {
                    var myFileStoreEntry = ctx.DataWorkspace.ApplicationData.ProjectFilesSet_SingleOrDefault(fileId);

                    if (myFileStoreEntry != null)
                    {
                        byte[] myBytes = myFileStoreEntry.BinaryContent;
                        string fileName = myFileStoreEntry.FileName;

                        context.Response.Clear();
                        context.Response.AddHeader("Content-Disposition", "attachment;filename=\"" + fileName + "\"");
                        //context.Response.AddHeader("Content-Length", fileInfo.Length.ToString());
                        context.Response.ContentType = "application/octet-stream";
                        context.Response.BinaryWrite(myBytes);
                        context.Response.Flush();
                    }
                    else
                    {
                        throw new Exception("File not found");
                    }

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