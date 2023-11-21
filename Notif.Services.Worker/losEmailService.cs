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

namespace DMS.Combank.Cons.Worker
{
    partial class losEmailService : ServiceBase
    {
        private Thread _svcThread;
        private bool _keepRunning = true;
        private const string _MyName = "EmailService";

        public losEmailService()
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
                        DataTable dt = conn.GetDataTable("EXEC USP_BGPROC_EMAILLIST", null, AppVars.dbtimeout);
                        for (int i = 0; i < dt.Rows.Count; i++)
                        {
                            string AP_REGNO = null, ETYPE = null, ETRCODE = null,
                                EMAIL_ACCID = null, EMAIL_SVR = null, EMAIL_UID = null, EMAIL_PWD = null,
                                EMAIL_FROM = null, ETO = null,ECC=null, ESUBJECT = null, EBODY = null, EMAIL_STATUS = null,
                                EMAILKEY = null, STATUSBY = null;
                            int EMAIL_PORT = 0;
                            bool EMAIL_SSL = false;
                            try
                            {
                                AP_REGNO = dt.Rows[i]["AP_REGNO"].ToString();
                                EMAILKEY = dt.Rows[i]["EMAILKEY"].ToString();
                                ETYPE = dt.Rows[i]["ETYPE"].ToString();
                                ETRCODE = dt.Rows[i]["ETRCODE"].ToString();
                                EMAIL_ACCID = dt.Rows[i]["EMAIL_ACCID"].ToString();
                                EMAIL_SVR = dt.Rows[i]["EMAIL_SVR"].ToString();
                                EMAIL_UID = dt.Rows[i]["EMAIL_UID"].ToString();
                                EMAIL_PWD = dt.Rows[i]["EMAIL_PWD"].ToString();
                                EMAIL_FROM = dt.Rows[i]["EMAIL_FROM"].ToString();
                                ETO = dt.Rows[i]["ETO"].ToString();
                                ECC = dt.Rows[i]["ECC"].ToString();
                                STATUSBY = dt.Rows[i]["STATUSBY"].ToString();
                                ESUBJECT = dt.Rows[i]["ESUBJECT"].ToString();
                                EBODY = dt.Rows[i]["EBODY"].ToString();
                                EMAIL_STATUS = dt.Rows[i]["EMAIL_STATUS"].ToString();
                                EMAIL_PORT = 0;
                                EMAIL_SSL = false;

                                try { EMAIL_PORT = (int)dt.Rows[i]["EMAIL_PORT"]; }
                                catch { }
                                try { EMAIL_SSL = (bool)dt.Rows[i]["EMAIL_SSL"]; }
                                catch { }

                                //sending sta 20 - Sending 
                                object[] par = new object[] { EMAILKEY, 20, STATUSBY, null};
                                conn.ExecuteNonQuery("exec USP_BGPROC_EMAILSTA @1, @2, @3, @4", par, AppVars.dbtimeout);

                                Email2Sender.SendMail(EMAIL_FROM, ETO, ECC, ESUBJECT, EBODY, "", EMAIL_SVR, EMAIL_PORT, EMAIL_SSL, "", "");

                                //if success --> sta = 90 - Sent 
                                par = new object[] { EMAILKEY, 90, STATUSBY, "success" };
                                conn.ExecuteNonQuery("exec USP_BGPROC_EMAILSTA @1, @2, @3, @4", par, AppVars.dbtimeout);
                            }
                            catch (Exception ex1)
                            {
                                object[] par = new object[] { EMAILKEY, 80, STATUSBY, ex1.Message };
                                conn.ExecuteNonQuery("exec USP_BGPROC_EMAILSTA @1, @2, @3, @4", par, AppVars.dbtimeout);
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
    }
}
