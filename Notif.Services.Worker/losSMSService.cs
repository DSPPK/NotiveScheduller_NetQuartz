using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using DMS.Tools;
using DMS.Interface;
using System.Collections;
using DMS.Framework;
using System.Collections.Specialized;

namespace DMS.Combank.Cons.Worker
{
    partial class losSMSService : ServiceBase
    {
        private Thread _svcThread;
        private bool _keepRunning = true;
        private const string _MyName = "SMSService";

        public losSMSService()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            _svcThread = new Thread(new ThreadStart(mainProcess));
            _svcThread.Name = _MyName + "_Thread";
            _svcThread.Start();
            AppVars.putMessage(_MyName + " :: " + "Service started");
        }

        protected override void OnStop()
        {
            this._keepRunning = false;
            this._svcThread.Interrupt();
            this._svcThread.Join();
            AppVars.putMessage(_MyName + " :: " + "Service stopped");
        }

        private void mainProcess()
        {
            DateTime timemarker = DateTime.Now;
            while (_keepRunning)
            {
                try
                {
                    #region thread up log
                    if (AppVars.ThreadLoggingTime > 0)
                    {
                        TimeSpan span = DateTime.Now.Subtract(timemarker);
                        if (span.Minutes >= AppVars.ThreadLoggingTime)
                        {
                            AppVars.putMessage(_MyName + " :: " + "Thread Up");
                            timemarker = DateTime.Now;
                        }
                    }
                    #endregion

                    #region mainprocess
                    using (DbConnection conn = new DbConnection(AppVars.connstr))
                    {
                        DataTable dt = conn.GetDataTable("EXEC USP_BGPROC_SMSGATEWAY ", null, AppVars.dbtimeout);
                        for (int i = 0; i < dt.Rows.Count; i++)
                        {
                            string _REQUESTID = "", _RECEIVER = "", _PHONENBR = "", _MESSAGE = "", _IPSENDER = ""
                                    , _USERID = "", _APPNAME = "";
                            string _SMSSTATUS = "", _STATUSDESC = "";

                            object[] par;
                            try
                            {
                                _REQUESTID = CommonTools.StringNullSave(dt.Rows[0]["REQUEST_ID"]);
                                _RECEIVER = CommonTools.StringNullSave(dt.Rows[0]["RECEIVER_ID"]);
                                _PHONENBR = CommonTools.StringNullSave(dt.Rows[0]["PHONE_NBR"]);
                                _MESSAGE = CommonTools.StringNullSave(dt.Rows[0]["MESSAGE"]);
                                _IPSENDER = CommonTools.StringNullSave(dt.Rows[0]["SENDER_IP"]);
                                _USERID = CommonTools.StringNullSave(dt.Rows[0]["USERID"]);
                                _APPNAME = CommonTools.StringNullSave(dt.Rows[0]["APPLICATION_NAME"]);
                                var client = new PTBCSMSGateway();
                                var response = client.SendSMS(_REQUESTID, _RECEIVER, _PHONENBR, _MESSAGE, _IPSENDER, timemarker, _USERID, _APPNAME);
                                if (response.RESP_CD.ToString() == "01")
                                {
                                    _SMSSTATUS = CommonTools.StringNullSave(response.RESP_CD.ToString());
                                    _STATUSDESC = CommonTools.StringNullSave(response.RESP_MSG.ToString());
                                    
                                    par = new object[] { _REQUESTID, _SMSSTATUS, _STATUSDESC };
                                    conn.ExecuteNonQuery("EXEC USP_UPDATESMSGATEWAY @1, @2, @3", par, AppVars.dbtimeout);
                                }
                            }
                            catch
                            {
                                par = new object[] { _REQUESTID, "", "" };
                                conn.ExecuteNonQuery("EXEC USP_UPDATESMSGATEWAY @1, @2, @3", par, AppVars.dbtimeout);
                            }
                        }
                    }
                    #endregion
                }
                catch (Exception ex)
                {
                    AppVars.putErrMessage(_MyName + " :: " + "Error in mainProcess(). Msg: " + ex.ToString());
                    AppVars.putErrMessage(AppVars.connstr, _MyName, "Error in mainProcess(). Msg: " + ex.ToString());
                }

                try
                {
                    Thread.Sleep(AppVars.ThreadSleepMilliSeconds);
                }
                catch (ThreadInterruptedException) { }
            }
        }

        private string UpdateSMSContent(string SMSContent, string TableQueryKey)
        {
            string sSMSContent = SMSContent;

            List<string> lsLoop = new List<string>();

            string sFields = "";

            string TagStart = "@[", TagEnd = "]";
            string[] sFormulaField;
            string[] sFormulaLoopField;

            int StartCharPos = 0, EndCharPos = 0;

            #region SingleField
            for (int x = 0; x < sSMSContent.Length; x++)
            {
                if (sSMSContent.Length > (x + TagStart.Length) && sSMSContent.Substring(x, TagStart.Length) == TagStart)
                    StartCharPos = x;

                if (sSMSContent.Length > (x + TagEnd.Length) && sSMSContent.Substring(x, TagEnd.Length) == TagEnd)
                    EndCharPos = x;

                if (StartCharPos > 0 && EndCharPos > StartCharPos)
                {
                    string FieldName = sSMSContent.Substring(StartCharPos, EndCharPos - StartCharPos + 1);
                    sFields += FieldName + ";";
                    StartCharPos = EndCharPos = 0;
                }
            }
            System.Diagnostics.Debug.Print(sFields);
            sFormulaField = sFields.Split(';');
            DataTable dtx = TemplateDataSource(TableQueryKey, sFormulaField);

            if (dtx != null)
            {
                for (int i = 0; i < dtx.Columns.Count; i++)
                {
                    string sFieldName = TagStart + dtx.Columns[i].Caption + TagEnd;
                    sSMSContent = sSMSContent.Replace(sFieldName, toValDesc(dtx.Rows[0][i]));
                }
            }

            #endregion

            #region LoopField

            StartCharPos = 0;
            EndCharPos = 0;
            sFields = "";


            ArrayList ArrayLoop = new ArrayList();
            string TagLoopStart1 = "@Loop", TagLoopEnd1 = "[";

            for (int x = 0; x < sSMSContent.Length; x++)
            {
                if (sSMSContent.Length > (x + TagLoopStart1.Length) && sSMSContent.Substring(x, TagLoopStart1.Length) == TagLoopStart1)
                    StartCharPos = x;

                if (sSMSContent.Length > (x + TagLoopEnd1.Length) && sSMSContent.Substring(x, TagLoopEnd1.Length) == TagLoopEnd1)
                    EndCharPos = x;

                if (StartCharPos > 0 && EndCharPos > StartCharPos)
                {
                    string LoopTag = sSMSContent.Substring(StartCharPos, EndCharPos - StartCharPos + 1);

                    if (!ArrayLoop.Contains(LoopTag)) ArrayLoop.Add(LoopTag);

                    StartCharPos = EndCharPos = 0;
                }
            }

            for (int z = 0; z < ArrayLoop.Count; z++)
            {

                string TagLoopStart = ArrayLoop[z].ToString(), TagLoopEnd = "]";
                sFields = "";

                for (int x = 0; x < sSMSContent.Length; x++)
                {
                    if (sSMSContent.Length > (x + TagLoopStart.Length) && sSMSContent.Substring(x, TagLoopStart.Length) == TagLoopStart)
                        StartCharPos = x;

                    if (sSMSContent.Length > (x + TagLoopEnd.Length) && sSMSContent.Substring(x, TagLoopEnd.Length) == TagLoopEnd)
                        EndCharPos = x;

                    if (StartCharPos > 0 && EndCharPos > StartCharPos)
                    {
                        string FieldName = sSMSContent.Substring(StartCharPos, EndCharPos - StartCharPos + 1);
                        sFields += FieldName.Replace(TagLoopStart, "@[") + ";";
                        StartCharPos = EndCharPos = 0;
                    }
                }
                System.Diagnostics.Debug.Print(sFields);
                sFormulaField = sFields.Split(';');
                dtx = TemplateDataSource(TableQueryKey, sFormulaField);

                if (dtx != null)
                {
                    for (int i = 0; i < dtx.Columns.Count; i++)
                    {
                        string sFieldName = TagLoopStart + dtx.Columns[i].Caption + TagLoopEnd;

                        for (int j = 0; j < dtx.Rows.Count; j++)
                        {
                            sSMSContent = sSMSContent.Replace(sFieldName, toValDesc(dtx.Rows[j][i]) + (j + 1 < dtx.Rows.Count ? " \\par " + sFieldName : ""));
                        }
                    }
                }
            }

            #endregion

            return sSMSContent;
        }

        private DataTable TemplateDataSource(string TableQueryKey, string[] ListField)
        {
            DataTable dt = null;

            using (DbConnection conn = new DbConnection(AppVars.connstr))
            {
                string strSql = "";
                dynamicFramework dynFramework = new dynamicFramework(conn);
                Hashtable hashFieldFW = dynFramework.hashFieldFW;

                NameValueCollection dv = null;
                string strField = "", strFilter = "", strCond = "";

                strCond += " AND " + TableQueryKey;

                for (int i = 0; i < ListField.Length; i++)
                {
                    try
                    {
                        string FieldId = dynFramework.retrvFieldId(ListField[i].Trim());
                        if (FieldId != "")
                        {
                            dv = (NameValueCollection)hashFieldFW[FieldId];
                            strField += "," + dv["FieldFW"].ToString() + " as " + "[" + FieldId + "]";
                        }
                    }
                    catch (Exception ex) { }
                };
                if (strField != "")
                {
                    strField = strField.Substring(1);
                    strSql = dynFramework.Retrieve(strField, strFilter + strCond);
                    //object[] par = new object[] { TableKeyContent };
                    dt = conn.GetDataTable(strSql, null, AppVars.dbtimeout);
                }
            }
            return dt;
        }

        static private string toValDesc(object value)
        {
            value = staticFramework.getvalue(value);
            switch (value.GetType().ToString())
            {
                case "System.Decimal":
                case "System.Double":
                    return ((double)value).ToString("###,##0.00");
                case "System.Int16":
                case "System.Int32":
                case "System.Int64":
                case "System.int":
                    return ((int)value).ToString("###,##0");
                case "System.DateTime":
                    return ((DateTime)value).ToString("dd MMMM yyyy");
                default:
                    return value.ToString();
            }
        }
    }
}
