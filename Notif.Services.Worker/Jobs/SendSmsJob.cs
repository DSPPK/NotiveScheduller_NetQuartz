using log4net;
using Quartz;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;
using DMS.Infrastructure.Common;
using DMS.Infrastructure.Common.Extensions;
using DMS.Tools;
using DMS.Interface;
using System.Data;
using DMS.Framework;
using System.Collections.Specialized;
using System.Collections;
using DMS.Combank.Cons.Worker.Operations.Sms;

namespace DMS.Combank.Cons.Worker.Jobs
{
    [DisallowConcurrentExecution]
    public class SendSmsJob : BaseJob, IQueueEnabler
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(SendMailJob));

        public override int Checking
        {
            get { return 1; }
        }

        public override void Execute(IJobExecutionContext context)
        {
            log.Info("executing sms svc");
            DateTime timemarker = DateTime.Now;
            ModelReqData request = new ModelReqData();
            

            for (int c = 0; c < AppVars.connstr.Length; c++)
            {
                #region mainprocess
                using (DbConnection conn = new DbConnection(AppVars.connstr[c]))
                {
                    DataTable dt = conn.GetDataTable("EXEC USP_BGPROC_SMSGATEWAY ", null, AppVars.dbtimeout);

                    log.Info("SMS Engine SVC: " + dt.Rows.Count.ToString() + " ready to proceed");

                    for (int i = 0; i < dt.Rows.Count; i++)
                    {
                        string _REQUESTID = CommonTools.StringNullSave(dt.Rows[i]["REQUEST_ID"]);
                        object[] par = new object[] { _REQUESTID, "2", "", null };
                        conn.ExecuteNonQuery("EXEC USP_UPDATESMSGATEWAY @1, @2, @3, @4", par, AppVars.dbtimeout);
                    }

                    for (int i = 0; i < dt.Rows.Count; i++)
                    {
                        string _REQUESTID = CommonTools.StringNullSave(dt.Rows[i]["REQUEST_ID"]);

                        string _SMSSTATUS = "", _RECEIVER = "", _STATUSDESC = "", _IPSENDER = ""
                                , _USERID = "", _APPNAME = "", _QUERYKEY = "";

                        // new dhimas
                        string //_REQUESTID = "", 
                        _PHONENBR = "", _MESSAGE = "", _MASKING = "", _USERWEB = "", _PASSWORDWEB = "";

                        // add lutvi
                        string _proxyUser = CommonTools.StringNullSave(dt.Rows[i]["PROXY_USERID"]).Trim();
                        string _proxyPWD = CommonTools.StringNullSave(dt.Rows[i]["PROXY_PASSWORD"]).Trim();
                        string _proxyAddress = CommonTools.StringNullSave(dt.Rows[i]["PROXY_ADDRESS"]).Trim();

                        object[] param;
                        try
                        {
                            _REQUESTID = CommonTools.StringNullSave(dt.Rows[i]["REQUEST_ID"]).Trim();
                            _RECEIVER = CommonTools.StringNullSave(dt.Rows[i]["RECEIVER_ID"]).Trim();
                            _PHONENBR = CommonTools.StringNullSave(dt.Rows[i]["PHONE_NBR"]).Trim();
                            _IPSENDER = CommonTools.StringNullSave(dt.Rows[i]["SENDER_IP"]).Trim();
                            _USERID = CommonTools.StringNullSave(dt.Rows[i]["USERID"]).Trim();
                            _APPNAME = CommonTools.StringNullSave(dt.Rows[i]["SMSTPLID"]).Trim();
                            _QUERYKEY = dt.Rows[i]["SMS_QUERYKEY"].ToString().Trim();
                            _MESSAGE = CommonTools.StringNullSave(dt.Rows[i]["MESSAGE"]).Trim();
                            _MASKING = CommonTools.StringNullSave(dt.Rows[i]["MASKING"]).Trim();
                            _USERWEB = CommonTools.StringNullSave(dt.Rows[i]["USERWEB"]).Trim();
                            _PASSWORDWEB = CommonTools.StringNullSave(dt.Rows[i]["PASSWORDWEB"]).Trim();

                            log.Info("SMS Engine SVC: After set variable: " + " _REQUESTID= " + _REQUESTID + " _RECEIVER= " + 
                                _RECEIVER + " _PHONENBR= " + _PHONENBR + " _IPSENDER= " 
                                + _IPSENDER + " _USERID= " + _USERID + " _APPNAME= " + _APPNAME + " _QUERYKEY= " + _QUERYKEY + " _MESSAGE= " 
                                + _MESSAGE + " _MASKING= " + _MASKING + " _USERWEB= " + _USERWEB + " _PASSWORDWEB= " + _PASSWORDWEB
                                + " _proxyUser= " + _proxyUser
                                + " _proxyPWD= " + _proxyPWD
                                + " _proxyAddress= " + _proxyAddress);

                            if (_MESSAGE == "")
                            {
                                conn.ExecReader("SELECT * FROM RFSMSTEMPLATE WHERE SMSTPLID = @1 ", new object[] { _APPNAME }, AppVars.dbtimeout);
                                if (conn.hasRow())
                                {
                                    _MESSAGE = CommonTools.StringNullSave(conn.GetFieldValue("SMSCONTENT"));
                                }
                            }
                            _MESSAGE = CommonTools.UpdateSMSContent(_MESSAGE, _QUERYKEY, conn);
                            if (_MESSAGE.Length > 0 && _MESSAGE.Length > 450)
                            {
                                log.Info("SMS Engine SVC: _MESSAGE.Substring: " + _MESSAGE);
                                _MESSAGE = _MESSAGE.Substring(450);
                            }

                            log.Info("SMS Engine SVC: Save to table SMSGTWY: " + _REQUESTID + " Message: " + _MESSAGE);

                            NameValueCollection Keys = new NameValueCollection();
                            NameValueCollection Field = new NameValueCollection();
                            Keys.Add("REQUEST_ID", "'" + _REQUESTID + "'");
                            Field.Add("MESSAGE", "'" + _MESSAGE + "'");
                            CommonTools.Save(Field, Keys, "SMSGTWY", AppVars.connstr[c]);

                            log.Info("SMS Engine SVC: Set to ModelReqData: REFNO=" + _REQUESTID + " MSISDN= " + _PHONENBR + " MESSAGE= " + _MESSAGE + " MASKING= " + _MASKING + " USERID= " + _USERWEB + " PASSWORD= " + _PASSWORDWEB);

                            request.REFNO = _REQUESTID.Trim();
                            request.MSISDN = _PHONENBR.Trim();
                            request.MESSAGE = _MESSAGE.Trim();
                            request.MASKING = _MASKING.Trim();
                            request.USERID = _USERWEB.Trim();
                            request.PASSWORD = _PASSWORDWEB.Trim();

                            log.Info("SMS Engine SVC: " + (i + 1).ToString() + " sending sms to " + _RECEIVER);
                            var response = PTBCServiceRequest.SendSMS(request, _IPSENDER, _proxyAddress.Trim(), _proxyPWD.Trim(), _proxyUser.Trim());
                            log.Info("SMS Engine SVC: " + (i + 1).ToString() + " sending sms status : " + response.STATUS.ToString());

                            //var response = client.SendSMS(_REQUESTID, _RECEIVER, _PHONENBR, _MESSAGE, _IPSENDER, timemarker, _USERID, _APPNAME);
                            
                            _REQUESTID = CommonTools.StringNullSave(response.TRXID.ToString());
                            _SMSSTATUS = CommonTools.StringNullSave(response.STATUS.ToString());

                            log.Info("SMS Engine SVC: Response from svc: _REQUESTID=" + _REQUESTID + " _SMSSTATUS= " + _SMSSTATUS);

                            param = new object[] { _REQUESTID, _SMSSTATUS, _STATUSDESC, _MESSAGE };
                            conn.ExecuteNonQuery("EXEC USP_UPDATESMSGATEWAY @1, @2, @3, @4", param, AppVars.dbtimeout);
                        }
                        catch (Exception ex)
                        {
                            log.Error("SMS Engine SVC: Error in SMS Engine Service: " + ex.Message, ex);
                            //param = new object[] { _REQUESTID, "", "" };
                            //conn.ExecuteNonQuery("EXEC USP_UPDATESMSGATEWAY @1, @2, @3", par, AppVars.dbtimeout);
                        }
                    }
                }
                #endregion
            }
        }
    }
}