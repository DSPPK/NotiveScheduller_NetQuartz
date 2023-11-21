using System;
using System.Configuration;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using NPOI.SS.UserModel;
using System.Data;
using System.Collections.Specialized;

namespace Notif.Services.Worker.Db
{
    public class SYS_Connection : Page
    {
        private const string localhost = "127.0.0.1";
        private static string connstr = GetConnectionString();
        private static int dbtimeout = int.Parse(ConfigurationManager.AppSettings["DbTimeOut"]);
        private static DBConnect conn = new DBConnect(connstr);

        public static string GetIP(Page Pages)
        {
            List<string> listHost = Pages.Request.ServerVariables.GetValues("REMOTE_HOST").ToList<string>();
            string retValue = (listHost.Count > 0) ? listHost.ElementAt(0) : localhost;
            return retValue;
        }

        public static string GetClientIP()
        {
            string ClientIP = string.Empty;
            try
            {
                ClientIP = HttpContext.Current.Request.ServerVariables["HTTP_X_FORWARDED_FOR"];
                if (string.IsNullOrEmpty(ClientIP))
                {
                    ClientIP = HttpContext.Current.Request.ServerVariables["REMOTE_ADDR"];
                }
            }
            catch (Exception exception)
            {
                throw new Exception(string.Format("An error occurred: {0}", exception.Message));
            }
            return ClientIP;
        }

        public static string GetConnectionString()
        {
            return ConfigurationManager.AppSettings["connstr"].ToString();
        }

        public static DataTable GetDataCC()
        {
            return conn.GetDataTable("EXEC SP_START", null, dbtimeout, connstr);
        }

        public static string UpdateProcess(string key, int status)
        {
            string result = UpdateProcess(key, status, null, null, null, null);

            return result;
        }

        public static string UpdateProcess(string key, int status, string responseCd, string reponseMsg, string reponseMsgEn, string reponseParam)
        {

            NameValueCollection Field = new NameValueCollection();
            NameValueCollection Keys = new NameValueCollection();
            string result = "";

            try
            {
                Field.Add("STATUS", CommonTools.NullSave(status, "int"));

                if (responseCd != null)
                    Field.Add("RESPONSE", CommonTools.NullSave(responseCd, "str"));

                if (reponseMsg != null)
                    Field.Add("MESSAGE", CommonTools.NullSave(reponseMsg, "str"));

                if (reponseMsgEn != null)
                    Field.Add("MESSAGE_EN", CommonTools.NullSave(reponseMsgEn, "str"));

                if (reponseParam != null)
                    Field.Add("PARAM", CommonTools.NullSave(reponseParam, "str"));

                if (status == 90)
                    Field.Add("DATE", CommonTools.NullSave(DateTime.Now, "dtt"));


                Keys.Add("KEY", CommonTools.NullSave(key, "str"));
                result = conn.Save(Field, Keys, "TABLE_SENDER", dbtimeout, connstr);

                if (result != "")
                {
                    throw new Exception(result);
                }

            }
            catch (Exception ex)
            {
                if (ex.Message.IndexOf("Last Query:") > 0)
                    result = ex.Message.Substring(0, ex.Message.IndexOf("Last Query:"));
                else
                    result = ex.Message;
            }

            return result;

        }

        private static object readAsObject(ICell cell)
        {
            object retVal = null;

            CellType celltype = cell.CellType;
            switch (celltype)
            {
                case CellType.Blank:
                    retVal = string.Empty;
                    break;
                case CellType.Boolean:
                    retVal = cell.BooleanCellValue;
                    break;
                case CellType.Error:
                    retVal = cell.ErrorCellValue;
                    break;
                case CellType.Formula:
                    IFormulaEvaluator evaluator = cell.Sheet.Workbook.GetCreationHelper().CreateFormulaEvaluator();
                    string evaluatorValue = evaluator.Evaluate(cell).FormatAsString();
                    retVal = evaluatorValue;
                    break;
                case CellType.Numeric:
                    if (DateUtil.IsCellDateFormatted(cell))
                    {
                        DateTime date = cell.DateCellValue;
                        ICellStyle style = cell.CellStyle;
                        string format = style.GetDataFormatString().Replace('m', 'M');
                        if (format == "M/d/yy")
                        {
                            format = "M/dd/yyyy";
                        }
                        retVal = date.ToString(format);
                    }
                    else
                    {
                        retVal = cell.NumericCellValue.ToString();
                    }
                    break;
                case CellType.String:
                    retVal = cell.StringCellValue;
                    break;
                case CellType.Unknown:
                    IFormulaEvaluator evaluator2 = cell.Sheet.Workbook.GetCreationHelper().CreateFormulaEvaluator();
                    string evaluatorValue2 = evaluator2.Evaluate(cell).FormatAsString();
                    retVal = evaluatorValue2;
                    break;
            }
            return retVal;
        }
    }
}