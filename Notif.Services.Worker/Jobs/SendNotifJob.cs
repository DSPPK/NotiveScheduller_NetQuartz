using log4net;
using Quartz;
using System;
using System.Text;
using System.Configuration;
using System.Data;
using Notif.Services.Worker.Model;
using System.Web.Script.Serialization;
using System.Net;
using Notif.Services.Worker.Db;
using System.Reflection;

namespace Notif.Services.Worker.Jobs
{
    [DisallowConcurrentExecution]
    public class SendNotifJob : BaseJob
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(SendNotifJob));

        public override int Checking
        {
            get { return 1; }
        }

        public override void Execute(IJobExecutionContext context)
        {
            try
            {
                startSendNotif();
            }
            catch (Exception ex)
            {
                log.Error("STP Notif Engine SVC: Error in Notif Engine Service: " + ex.Message, ex);
            }
        }

        private bool checkSchedulerActive(int step, string filetp)
        {
            Boolean runing = CommonTools.ConvertBoolean(ConfigurationManager.AppSettings["process_" + step]);
            if (runing)
            {
                if (CommonTools.processScheduler(step))
                    switch (step)
                    {
                        case 0:
                            return true;
                        case 1:
                            return true;
                        case 2:
                            return true;
                        case 3:
                            return true;
                    }
            }
            return false;
        }

        #region schedule send email
        private static void startSendNotif()
        {
            #region mainprocess send firebase on board

            DataTable dt = SYS_Connection.GetDataCC();
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                string result = "";
                string _KEY = CommonTools.StringNullSave(dt.Rows[i]["KEY"]);
                string _ID = CommonTools.StringNullSave(dt.Rows[i]["ID"]);
                int _STATUS = int.Parse(CommonTools.IntegerNull(dt.Rows[i]["STATUS"]));
                double _LIMIT = int.Parse(CommonTools.IntegerNull(dt.Rows[i]["LIMIT"]));
                result = SYS_Connection.UpdateProcess(_KEY, 10);

                string responseCd = "";
                string reponseMsg = "", reponseMsgEn = "";
                string reponseParam = "";

                try
                {
                    string apiUrl = ConfigurationManager.AppSettings["UrlNotification"];
                    ModelRequest req = new ModelRequest
                    {
                        appNo = _ID, /* same LosId (mandatory) */
                        appStatus = CommonTools.StringNullSave(_STATUS), /* same LosId (mandatory) */
                        limit = CommonTools.floatNull(_LIMIT), /* same LosId (mandatory) */
                    };

                    ModelResponse resp = new ModelResponse();
                    result = SYS_Connection.UpdateProcess(_KEY, 20);

                    try
                    {
                        var str = new JavaScriptSerializer().Serialize(req);
                        WebClient client = new WebClient();
                        client.Headers["Content-type"] = "application/json";
                        client.Encoding = Encoding.UTF8;

                        string json = client.UploadString(apiUrl, str);
                        resp = (new JavaScriptSerializer()).Deserialize<ModelResponse>(json);
                        if (resp != null)
                        {
                            responseCd = resp.responseCode;
                            reponseMsg = resp.descErrorCode;
                            reponseMsgEn = resp.descErrorCodeEN;
                            if (resp.descErrorCode != null)
                            {
                                try
                                {
                                    reponseParam = new JavaScriptSerializer().Serialize(resp.responseData);
                                }
                                catch { }
                            }
                        }
                    }
                    catch
                    {
                        throw;
                    }
                    result = SYS_Connection.UpdateProcess(_KEY, 90, responseCd, reponseMsg, reponseMsgEn, reponseParam);
                }
                catch (Exception ex)
                {
                    string errmsg = "Alert " + ReNameMethod(MethodBase.GetCurrentMethod().Name);
                    if (ex.Message.IndexOf("Last Query:") > 0)
                        errmsg = errmsg + " (apps) " + ex.Message.Substring(0, ex.Message.IndexOf("Last Query:"));
                    else
                        errmsg = errmsg + " (apps) " + ex.Message;

                    reponseMsg = errmsg;

                    result = SYS_Connection.UpdateProcess(_KEY, 80, responseCd, reponseMsg, reponseMsgEn, reponseParam);
                }
            }
            #endregion
        }

        #endregion

    }
}