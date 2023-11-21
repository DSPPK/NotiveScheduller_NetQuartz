using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Data;
using Notif.Services.Worker.classes;

namespace Notif.Services.Worker
{
    public class CommonTools
    {
        public static string setValue(string value)
        {
            string valsr = null;
            if (value.ToString().Trim() != "?")
            {
                valsr = value.ToString().ToUpper();
            }
            return valsr;
        }

        public static string StringNullDoc(Object value)
        {
            if (value != null && value.ToString() != "")
            {
                return value.ToString() + "/";
            }
            else
            {
                return "";
            }
        }

        public static string StringNull(Object value)
        {
            if (value != null && value.ToString() != "")
            {
                return value.ToString();
            }
            else
            {
                return "";
            }
        }

        public static string StringNullSave(Object value)
        {
            if (value != null && value.ToString() != "")
            {
                return value.ToString().Replace("'", "''");
            }
            else
            {
                return "";
            }
        }

        public static string IntegerNull(Object value)
        {
            if (value != null && value.ToString() != "")
            {
                return value.ToString();
            }
            else
            {
                return "0";
            }
        }

        public static string floatNull(Object value)
        {
            if (value != null && value.ToString() != "")
            {
                try
                {

                    return Double.Parse(value.ToString()).ToString();
                }
                catch
                {
                    return "0";
                }
            }
            else
            {
                return "0";
            }
        }

        public static string SaveDateNull(Object value, bool time)
        {
            if (value != null && value.ToString() != "")
            {
                try
                {
                    if (time)
                        return DateTime.ParseExact(value.ToString(), "yyyyMMdd-HHmmss", null).ToString("dd MMM yyyy HH:mm:ss");
                    else
                        return DateTime.ParseExact(value.ToString(), "yyyyMMdd", null).ToString("dd MMM yyyy");
                }
                catch
                {
                    try
                    {
                        if (time)
                            return DateTime.ParseExact(value.ToString(), "yyyyMMdd-HH:mm:ss", null).ToString("dd MMM yyyy HH:mm:ss");
                        else
                            return DateTime.ParseExact(value.ToString(), "yyyyMMdd", null).ToString("dd MMM yyyy");
                    }
                    catch
                    {
                        try
                        {
                            if (time)
                                return DateTime.ParseExact(value.ToString(), "dd-MM-yy HH:mm:ss", null).ToString("dd MMM yyyy HH:mm:ss");
                            else
                                return DateTime.ParseExact(value.ToString(), "dd-MM-yy", null).ToString("dd MMM yyyy");
                        }
                        catch
                        {
                            try
                            {
                                if (time)
                                    return DateTime.ParseExact(value.ToString(), "dd-MM-yyyy HH:mm:ss", null).ToString("dd MMM yyyy HH:mm:ss");
                                else
                                    return DateTime.ParseExact(value.ToString(), "dd-MM-yyyy", null).ToString("dd MMM yyyy");
                            }
                            catch
                            {
                                try
                                {
                                    if (time)
                                        return DateTime.ParseExact(value.ToString(), "dd/MM/yyyy HH:mm:ss", null).ToString("dd MMM yyyy HH:mm:ss");
                                    else
                                        return DateTime.ParseExact(value.ToString(), "dd/MM/yyyy", null).ToString("dd MMM yyyy");
                                }
                                catch
                                {
                                    return value.ToString();
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                return null;
            }
        }

        public static string GetterDateNull(Object value, bool time)
        {

            if (value != null && value.ToString() != "")
            {
                if (time)
                    return DateTime.Parse(value.ToString()).ToString("yyyyMMdd-HHmmss");
                else
                    return DateTime.Parse(value.ToString()).ToString("yyyyMMdd");
            }
            else
            {
                return "";
            }
        }

        public static string GetterBooleanNull(Object value)
        {
            string values = "0";
            if (value != null && value.ToString() != "")
            {
                try
                {
                    if (value.ToString().ToUpper() == "TRUE" || value.ToString() == "1")
                    {
                        values = "1";
                    }
                }
                catch { }
            }

            return values;
        }

        public static Boolean ConvertBoolean(Object value)
        {
            Boolean values = false;
            if (value != null && value.ToString() != "")
            {
                try
                {
                    if (value.ToString().ToUpper() == "TRUE" || value.ToString() == "1")
                    {
                        values = true;
                    }
                }
                catch { }
            }

            return values;
        }

        public static string NullSave(System.Object value, string tipe)
        {

            return NullSave(value, tipe, true, true);
        }

        public static string NullSave(System.Object value, string tipe, Boolean IsStr)
        {

            return NullSave(value, tipe, IsStr, true);
        }

        public static string NullSave(System.Object value, string tipe, Boolean IsStr, Boolean IsNull)
        {

            if (value != null && value.ToString() != "")
            {
                string vals = "";
                if (tipe.ToLower() == "dtt") // 5
                    vals = SaveDateNull(value, true);
                else if (tipe.ToLower() == "dt") // 4
                    vals = SaveDateNull(value, false);
                else if (tipe.ToLower() == "bit") //3
                    vals = GetterBooleanNull(value);
                else if (tipe.ToLower() == "flt") // 2
                    vals = floatNull(value);
                else if (tipe.ToLower() == "int") // 1
                    vals = IntegerNull(value);
                else // "str" 0 
                    vals = StringNullSave(value);

                if (IsStr && tipe.ToLower() != "flt")
                    return "'" + vals + "'";
                else
                    return vals;
            }
            else
            {
                if (IsNull)
                    return "NULL";
                else
                    return null;
            }
        }

        public static bool processScheduler(int sch)
        {
            try
            {

                string[] schedule = StringNull(System.Configuration.ConfigurationManager.AppSettings["ScheduleGenerate"]).Split(new char[]
                        {
                        ' '
                        });

                string y = DateTime.Now.ToString("yyyy");
                string m = DateTime.Now.ToString("MM");
                string d = DateTime.Now.ToString("dd");

                int startDate = 0;

                string[] dtschdl = schedule[0].Split(new char[]
                {
                        '-'
                });

                if (dtschdl.Length > 0)
                {
                    if (int.Parse(IntegerNull(dtschdl[sch])) <= int.Parse(d))
                        startDate = DateTime.DaysInMonth(int.Parse(y), int.Parse(m));

                    TimeSpan ts1 = TimeSpan.Parse(schedule[1]);

                    if (DateTime.Now.TimeOfDay > ts1)
                        if (startDate >= int.Parse(d))
                            return true;
                }
            }
            catch { }

            return false;
        }

        public static string generateHtml(string TemplateHtml, DataSet ds, string NewLineChar)
        {
            if (ds.Tables.Count > 0)
            {
                string strTable = "[TABLE";
                string strTableEnd = "]";
                string strStart = "[%=";
                string strEnd = "%]";
                string strLoopStart = "[%loop=";
                string strLoopEnd = "%]";
                string strLoopStart2 = "[%loop2=";
                string strLoopEnd2 = "%]";

                List<string> lsLoop = new List<string>();
                List<string> lsLoop2 = new List<string>();
                List<string> lstable = new List<string>();
                List<string> lstable2 = new List<string>();

                int idxLoopStart = TemplateHtml.IndexOf(strLoopStart);
                while (idxLoopStart > 0)
                {
                    int idxLoopEnd = TemplateHtml.IndexOf(strLoopEnd, idxLoopStart);
                    if (idxLoopEnd < 0)
                        throw new Exception("Index Out Of Bound! End Loop Tag does not set.");

                    string strLoop = TemplateHtml.Substring(idxLoopStart + strLoopStart.Length, idxLoopEnd - (idxLoopStart + strLoopStart.Length));
                    lsLoop.Add(strLoop);

                    idxLoopStart = TemplateHtml.IndexOf(strLoopStart, idxLoopEnd);
                }

                int idxLoopStart2 = TemplateHtml.IndexOf(strLoopStart2);
                while (idxLoopStart2 > 0)
                {
                    int idxLoopEnd2 = TemplateHtml.IndexOf(strLoopEnd2, idxLoopStart2);
                    if (idxLoopEnd2 < 0)
                        throw new Exception("Index Out Of Bound! End Loop Tag does not set.");

                    string strLoop2 = TemplateHtml.Substring(idxLoopStart2 + strLoopStart2.Length, idxLoopEnd2 - (idxLoopStart2 + strLoopStart2.Length));
                    lsLoop2.Add(strLoop2);

                    idxLoopStart2 = TemplateHtml.IndexOf(strLoopStart2, idxLoopEnd2);
                }

                for (int t = 0; t < ds.Tables.Count; t++)
                {
                    string table = strTable + t + strTableEnd;
                    lstable.Add(table);
                    DataTable dtData = ds.Tables[t];
                    for (int i = 0; i < dtData.Rows.Count; i++)
                        for (int j = 0; j < dtData.Columns.Count; j++)
                        {
                            int k = lsLoop.Count;
                            for (k = 0; k < lsLoop.Count; k++)
                            {
                                if (lsLoop[k] == dtData.Columns[j].ColumnName)          //columnname exists in loop columns..
                                    break;
                            }
                            if (k == lsLoop.Count)                                      //columnname does not exists in loop columns
                                TemplateHtml = TemplateHtml.Replace(table + strStart + dtData.Columns[j].ColumnName + strEnd, toValDesc(dtData.Rows[i][j]));
                            else
                                TemplateHtml = TemplateHtml.Replace(table + strLoopStart + dtData.Columns[j].ColumnName + strLoopEnd, toValDesc(dtData.Rows[i][j]) + NewLineChar + table + strLoopStart + dtData.Columns[j].ColumnName + strLoopEnd);       //pad more loop code for repeatition
                        }
                }
                for (int t = 0; t < ds.Tables.Count; t++)
                {
                    string table = strTable + t + strTableEnd;
                    lstable2.Add(table);
                    DataTable dtData = ds.Tables[t];
                    for (int i = 0; i < dtData.Rows.Count; i++)
                        for (int j = 0; j < dtData.Columns.Count; j++)
                        {
                            int k = lsLoop2.Count;
                            for (k = 0; k < lsLoop2.Count; k++)
                            {
                                if (lsLoop2[k] == dtData.Columns[j].ColumnName)          //columnname exists in loop columns..
                                    break;
                            }
                            if (k == lsLoop2.Count)                                      //columnname does not exists in loop columns
                                TemplateHtml = TemplateHtml.Replace(table + strStart + dtData.Columns[j].ColumnName + strEnd, toValDesc(dtData.Rows[i][j]));
                            else
                                TemplateHtml = TemplateHtml.Replace(table + strLoopStart2 + dtData.Columns[j].ColumnName + strLoopEnd2, toValDesc(dtData.Rows[i][j]) + "         " + "       " + table + strLoopStart2 + dtData.Columns[j].ColumnName + strLoopEnd2);       //pad more loop code for repeatition
                        }
                }

                for (int p = 0; p < lstable.Count; p++)
                    for (int l = 0; l < lsLoop.Count; l++)
                    {
                        TemplateHtml = TemplateHtml.Replace(NewLineChar + lstable[p] + strLoopStart + lsLoop[l] + strLoopEnd, "");       //clear loop code 
                        TemplateHtml = TemplateHtml.Replace(lstable[p] + strLoopStart + lsLoop[l] + strLoopEnd, "");       //clear loop code (second run, with no prefixed newline)
                    }

                for (int p = 0; p < lstable2.Count; p++)
                    for (int l = 0; l < lsLoop2.Count; l++)
                    {
                        TemplateHtml = TemplateHtml.Replace("       " + "       " + lstable2[p] + strLoopStart2 + lsLoop2[l] + strLoopEnd2, "");       //clear loop code 
                        TemplateHtml = TemplateHtml.Replace(lstable2[p] + strLoopStart2 + lsLoop2[l] + strLoopEnd2, "");       //clear loop code (second run, with no prefixed newline)
                    }
            }

            return TemplateHtml;
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

        public static string HtmlToPlainText(string html)
        {
            const string tagWhiteSpace = @"(>|$)(\W|\n|\r)+<";//matches one or more (white space or line breaks) between '>' and '<'
            const string stripFormatting = @"<[^>]*(>|$)";//match any character between '<' and '>', even when end tag is missing
            const string lineBreak = @"<(br|BR)\s{0,1}\/{0,1}>";//matches: <br>,<br/>,<br />,<BR>,<BR/>,<BR />
            var lineBreakRegex = new Regex(lineBreak, RegexOptions.Multiline);
            var stripFormattingRegex = new Regex(stripFormatting, RegexOptions.Multiline);
            var tagWhiteSpaceRegex = new Regex(tagWhiteSpace, RegexOptions.Multiline);

            var text = html;
            //Decode html specific characters
            text = System.Net.WebUtility.HtmlDecode(text);
            //Remove tag whitespace/line breaks
            text = tagWhiteSpaceRegex.Replace(text, "><");
            //Replace <br /> with line breaks
            text = lineBreakRegex.Replace(text, Environment.NewLine);
            //Strip formatting
            text = stripFormattingRegex.Replace(text, string.Empty);
            text = text.Replace(">", "");

            return text;
        }
    }
}