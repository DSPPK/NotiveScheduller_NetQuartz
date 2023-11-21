using System;
using System.Data;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Reflection;
using Notif.Services.Worker.Db;

namespace Notif.Services.Worker.classes
{
    #region staticFramework
    public class staticFramework
    {
        public static bool reffsorted = false;

        #region schema controls
        public static void retrieveschema(DataTable dt, string columnname, Control ctrl)
        {
            retrieveschema(dt.Columns[columnname], ctrl);
        }
        public static void retrieveschema(DataTable dt, int columnidx, Control ctrl)
        {
            retrieveschema(dt.Columns[columnidx], ctrl);
        }
        public static void retrieveschema(DataTable dt, Control ctrl, string prefix)
        {
            retrieveschema(dt.Columns[ctrl.ID.Substring(prefix.Length)], ctrl);
        }
        public static void retrieveschema(DataTable dt, Control ctrl)
        {
            retrieveschema(dt.Columns[ctrl.ID], ctrl);
        }
        private static void retrieveschema(DataColumn dc, Control ctrl)
        {
            if (dc != null)
            {
                if (ctrl is RadioButtonList)
                {
                    (ctrl as RadioButtonList).RepeatDirection = RepeatDirection.Horizontal;
                }
                else
                {
                    if (ctrl is TextBox)
                    {
                        TextBox textBox = (TextBox)ctrl;
                        if (dc.MaxLength > 0)
                        {
                            textBox.MaxLength = dc.MaxLength;
                        }
                        if (textBox.Columns == 0 && textBox.Width.Value == 0.0)
                        {
                            double num = 1.2;
                            if (dc.MaxLength >= 500)
                            {
                                textBox.TextMode = TextBoxMode.MultiLine;
                                textBox.Width = Unit.Percentage(100.0);
                                textBox.Rows = 7;
                            }
                            else if (dc.MaxLength >= 200)
                            {
                                textBox.TextMode = TextBoxMode.MultiLine;
                                textBox.Columns = (int)(50.0 * num);
                                textBox.Rows = 3;
                            }
                            else if (dc.MaxLength >= 50)
                            {
                                textBox.Columns = (int)(50.0 * num);
                            }
                            else if (dc.MaxLength >= 3)
                            {
                                textBox.Columns = (int)((double)dc.MaxLength * num);
                            }
                            else
                            {
                                textBox.Columns = (int)(3.0 * num);
                            }
                        }
                    }
                }
            }
        }
        #endregion

        public static string toSql(object value)
        {
            value = getvalue(value);
            if (value == null || (value is string && value.ToString() == "") || value is DBNull)
            {
                return "NULL";
            }
            if (value is bool)
            {
                if (!(bool)value)
                {
                    return "0";
                }
                return "1";
            }
            else
            {
                if (value is double || value is float || value is decimal)
                {
                    return value.ToString().Replace(",", ".");
                }
                if (value is int)
                {
                    return value.ToString();
                }
                if (value is DateTime)
                {
                    return "'" + ((DateTime)value).ToString("yyyy/MM/dd HH:mm:ss") + "'";
                }
                return "'" + value.ToString().Replace("'", "''") + "'";
            }
        }

        public static void reff(Control ctrl, string query, object[] param, DBConnect conn, int dbtimeout)
        {
            reff(ctrl, query, param, conn, dbtimeout, true, reffsorted);
        }

        public static void reff(Control ctrl, string query, object[] param, DBConnect conn, int dbtimeout, bool hasactive)
        {
            reff(ctrl, query, param, conn, dbtimeout, hasactive, reffsorted);
        }

        public static void reff(Control ctrl, string query, object[] param, DBConnect conn, int dbtimeout, bool hasactive, bool sort)
        {
            if (hasactive && ctrl.Page.Request.QueryString["readonly"] == null && query.Trim().ToLower().StartsWith("select"))
            {
                try
                {
                    string text = "select top 0 " + query.Trim().Substring(6);
                    if (text.ToLower().IndexOf(" where ") > 0)
                    {
                        text += " and active = '1'";
                    }
                    else
                    {
                        text += " where active = '1'";
                    }
                    conn.ExecNonQuery(text, param, dbtimeout);
                    if (query.ToUpper().IndexOf(" WHERE ") > 0)
                    {
                        query += " AND ACTIVE ='1'";
                    }
                    else
                    {
                        query += " WHERE ACTIVE ='1'";
                    }
                }
                catch
                {
                }
            }
            DataTable dataTable = conn.GetDataTable(query, param, dbtimeout);
            DataView dataView = new DataView(dataTable);
            if (sort)
            {
                dataView.Sort = dataTable.Columns[1].ColumnName;
            }
            if (ctrl is RadioButtonList)
            {
                ((RadioButtonList)ctrl).RepeatDirection = RepeatDirection.Horizontal;
            }
            else if (ctrl is CheckBoxList)
            {
                ((CheckBoxList)ctrl).RepeatDirection = RepeatDirection.Horizontal;
            }
            if (ctrl is ListControl)
            {
                ListControl listControl = (ListControl)ctrl;
                listControl.Items.Clear();
                listControl.SelectedValue = null;
                listControl.DataValueField = dataTable.Columns[0].ColumnName;
                listControl.DataTextField = dataTable.Columns[1].ColumnName;
                listControl.DataSource = dataView;
                listControl.DataBind();
                if (ctrl is DropDownList && (dataTable.Rows.Count != 1 || !listControl.CssClass.StartsWith("mandatory")))
                {
                    ListItem item = new ListItem("(none)", "");
                    listControl.Items.Insert(0, item);
                }
                if (dataTable.Rows.Count == 1 && listControl.CssClass.StartsWith("mandatory"))
                {
                    listControl.SelectedIndex = 0;
                    return;
                }
            }
        }

        public static void retrieve(object value, DropDownList ctrl, object[] param, DBConnect conn, int dbtimeout)
        {
            retrieve(value, ctrl.ID, ctrl, param, conn, dbtimeout);
        }

        public static void retrieve(object value, string ColumnName, DropDownList ctrl, object[] param, DBConnect conn, int dbtimeout)
        {
            retrieve(value, ColumnName, ctrl, param, conn, dbtimeout, true);
        }

        public static void retrieve(object value, string ColumnName, DropDownList ctrl, object[] param, DBConnect conn, int dbtimeout, bool hasactive)
        {
            if (param != null)
            {
                for (int i = 0; i < param.Length; i++)
                {
                    param[i] = toSql(param[i]);
                }
                reff(ctrl, ctrl.Attributes["cascadequery"], param, conn, dbtimeout, hasactive);
            }
            retrieve(value, ColumnName, ctrl);
        }

        public static void retrieve(object value, Control ctrl, string prefix)
        {
            retrieve(value, ctrl.ID.Substring(prefix.Length), ctrl);
        }

        public static void retrieve(object value, Control ctrl)
        {
            retrieve(value, ctrl.ID, ctrl);
        }

        public static void retrieve(object value, string ColumnName, Control ctrl)
        {
            if (value is DataTable)
            {
                if ((value as DataTable).Rows.Count > 0)
                {
                    value = (value as DataTable).Rows[0][ColumnName];
                }
                else
                {
                    value = null;
                }
            }
            else if (value is DBConnect)
            {
                if ((value as DBConnect).GetFieldValue(ColumnName) == "")
                {
                    value = null;
                }
                else
                {
                    value = (value as DBConnect).GetNativeFieldValue(ColumnName);
                }
            }
            setvalue(ctrl, value);
        }

        public static void setvalue(object obj, object value)
        {
            if (value is DBNull)
            {
                value = null;
            }
            Type type = obj.GetType();
            if (type.Name == "CheckBoxList")
            {
                CheckBoxList checkBoxList = (CheckBoxList)obj;
                string[] array = new string[0];
                if (value != null && value is string)
                {
                    array = value.ToString().Split(new char[]
                    {
                        ','
                    });
                }
                foreach (ListItem listItem in checkBoxList.Items)
                {
                    listItem.Selected = false;
                    string[] array2 = array;
                    for (int i = 0; i < array2.Length; i++)
                    {
                        string a = array2[i];
                        if (a == listItem.Value)
                        {
                            listItem.Selected = true;
                        }
                    }
                }
                return;
            }
            PropertyInfo property = type.GetProperty("SelectedValue");
            if (property != null)
            {
                try
                {
                    property.SetValue(obj, value, null);
                }
                catch
                {
                }
                return;
            }
            property = type.GetProperty("Checked");
            if (property != null)
            {
                if (value == null)
                {
                    property.SetValue(obj, false, null);
                    return;
                }
                property.SetValue(obj, (value is string) ? ((string)value == "1") : ((bool)value), null);
                return;
            }
            else
            {
                property = type.GetProperty("Value");
                if (property != null)
                {
                    #region dsppk
                    /*if (obj is TXT_CURRENCY)
                    {
                        if (value != null)
                        {
                            try
                            {
                                (obj as TXT_CURRENCY).Value = Convert.ToDouble(value);
                                return;
                            }
                            catch
                            {
                                (obj as TXT_CURRENCY).Text = "";
                                return;
                            }
                        }
                        (obj as TXT_CURRENCY).Text = "";
                        return;
                    }
                    if (obj is TXT_DECIMAL)
                    {
                        if (value != null)
                        {
                            try
                            {
                                (obj as TXT_DECIMAL).Value = Convert.ToDouble(value);
                                return;
                            }
                            catch
                            {
                                (obj as TXT_DECIMAL).Text = "";
                                return;
                            }
                        }
                        (obj as TXT_DECIMAL).Text = "";
                        return;
                    }
                    if (obj is TXT_NUMBER)
                    {
                        if (value != null)
                        {
                            try
                            {
                                (obj as TXT_NUMBER).Value = Convert.ToInt32(value);
                                return;
                            }
                            catch
                            {
                                (obj as TXT_NUMBER).Text = "";
                                return;
                            }
                        }
                        (obj as TXT_NUMBER).Text = "";
                        return;
                    }*/
                    #endregion

                    if (value != null && property.PropertyType.ToString() == "System.String")
                    {
                        value = value.ToString();
                    }
                    property.SetValue(obj, value, null);
                    return;
                }
                property = type.GetProperty("Text");
                if (property != null)
                {
                    if (value != null)
                    {
                        if (value is DateTime)
                        {
                            value = ((DateTime)value).ToString("d MMMM yyyy");
                        }
                        else if (value is double)
                        {
                            value = ((double)value).ToString("###,##0.##");
                        }
                        else if (value is float)
                        {
                            value = ((float)value).ToString("###,##0.##");
                        }
                        else
                        {
                            value = value.ToString();
                        }
                    }
                    property.SetValue(obj, value, null);
                }
                return;
            }
        }

        private static string FixupUrl(string Url, string ApplicationPath)
        {
            if (Url.StartsWith("~"))

                return (ApplicationPath +
                        Url.Substring(1)).Replace("//", "/");

            return Url;
        }

        public static object getvalue(object obj)
        {
            if (obj is System.Web.UI.Control || obj is System.Web.UI.UserControl || obj is System.Windows.Forms.Control || obj is System.Windows.Forms.UserControl)
            {
                Type type = obj.GetType();
                PropertyInfo property = type.GetProperty("SelectedValue");
                if (property != null)
                {
                    return property.GetValue(obj, null);
                }
                property = type.GetProperty("Checked");
                if (property != null)
                {
                    return property.GetValue(obj, null);
                }
                property = type.GetProperty("Value");
                if (property != null)
                {
                    return property.GetValue(obj, null);
                }
                property = type.GetProperty("Text");
                if (property != null)
                {
                    return property.GetValue(obj, null);
                }
            }
            return obj;
        }

        public static object[] getaram(object[] param)
        {
            if (param == null)
            {
                return null;
            }
            object[] array = new object[param.Length];
            for (int i = 1; i <= param.Length; i++)
            {
                array[i - 1] = getvalue(param[i - 1]);
            }
            return array;
        }
    }
    #endregion
}
