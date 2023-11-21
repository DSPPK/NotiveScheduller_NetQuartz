using System;
using System.Data;
using System.Collections.Specialized;
using System.Data.SqlClient;
using System.Reflection;
using System.Collections;

namespace Notif.Services.Worker.Db
{
    public class DBConnect : IDisposable
    {
        private SqlConnection _conn = null;
        private SqlDataReader _reader = null;
        private SqlTransaction _tran = null;

        private object[] _colvalues;
        private string[] _coldatatypes;
        private string[] _colnames;

        public DBConnect(string connstr)
        {
            DBConnection(connstr);
        }

        private void DBConnection(string connstr)
        {
            this._conn = new SqlConnection(DBReadCnnStr(connstr));
        }

        public void DBCloseConnection()
        {
            this._conn.Close();
        }

        public bool ReaderHasRows
        {
            get
            {
                return this._reader.HasRows;
            }
        }

        public bool ReaderIsClosed
        {
            get
            {
                return this._reader.IsClosed;
            }
        }

        public string DBReadCnnStr(string connstr)
        {
            string result = connstr;
            return result;
        }

        public bool DBChkConnection(string cnnStr)
        {
            bool result;

            using (SqlConnection sqlConnection = new SqlConnection(cnnStr))
            {
                try
                {
                    sqlConnection.Open();
                }
                catch (System.Exception)
                {
                    result = false;
                    return result;
                }
                finally
                {
                    sqlConnection.Close();
                }
                result = true;
            }
            return result;
        }

        public int ExecTran(string query, object[] param, int timeout)
        {
            return ExecTran(query, param, timeout, BlankToNull: true);
        }

        public int ExecTran(string query, object[] param, int timeout, bool BlankToNull)
        {
            if (_conn.State != ConnectionState.Open)
            {
                _conn.Open();
            }

            try
            {
                if (this._reader != null)
                    if (!_reader.IsClosed)
                        _reader.Close();
            }
            catch
            {
            }

            try
            {
                if (_tran == null)
                {
                    _tran = _conn.BeginTransaction();
                }

                SqlCommand sqlCommand = new SqlCommand(getCommandQuery(query, param, BlankToNull), _conn, _tran);
                if (timeout > 0)
                {
                    sqlCommand.CommandTimeout = timeout;
                }

                return sqlCommand.ExecuteNonQuery();
            }
            catch (SqlException ex)
            {
                string text = getCommandQuery(query, param, BlankToNull);
                if (timeout > 0)
                {
                    text = text + "; Timeout was set to: " + timeout;
                }

                throw new ApplicationException(ex.Message + " Last Query: " + text);
            }
        }

        public bool ExecTran_Commit()
        {
            try
            {
                _tran.Commit();
                _tran.Dispose();
                _tran = null;
            }
            catch
            {
                return false;
            }

            return true;
        }

        public bool ExecTran_Rollback()
        {
            try
            {
                _tran.Rollback();
                _tran.Dispose();
                _tran = null;
            }
            catch
            {
                return false;
            }

            return true;
        }

        public SqlParameterCollection ExecProc(string spname, int timeout, ArrayList paramname, ArrayList paramtype, ArrayList paramdir, ArrayList paramvalue, ArrayList paramsize)
        {
            if (paramname.Count != paramtype.Count || paramtype.Count != paramdir.Count || paramdir.Count != paramvalue.Count)
            {
                return null;
            }

            if (_conn.State != ConnectionState.Open)
            {
                _conn.Open();
            }

            try
            {
                if (this._reader != null)
                    if (!_reader.IsClosed)
                        _reader.Close();
            }
            catch
            {
            }

            SqlCommand sqlCommand;
            try
            {
                sqlCommand = new SqlCommand(spname, _conn);
                sqlCommand.CommandType = CommandType.StoredProcedure;
                for (int i = 0; i < paramname.Count; i++)
                {
                    SqlParameter sqlParameter = sqlCommand.Parameters.Add((string)paramname[i], (SqlDbType)paramtype[i]);
                    sqlParameter.Direction = (ParameterDirection)paramdir[i];
                    sqlParameter.Value = paramvalue[i];
                    if (paramsize.Count == paramname.Count)
                    {
                        sqlParameter.Size = (int)paramsize[i];
                    }
                }

                if (timeout > 0)
                {
                    sqlCommand.CommandTimeout = timeout;
                }

                sqlCommand.ExecuteNonQuery();
            }
            catch (SqlException ex)
            {
                string text = " Last Query: <executing storeprocedure " + spname + ">";
                if (timeout > 0)
                {
                    text = text + "; Timeout was set to: " + timeout;
                }

                throw new ApplicationException(ex.Message + text);
            }

            return sqlCommand.Parameters;
        }


        public SqlParameterCollection ExecProc(string spname, int timeout, ArrayList paramname, ArrayList paramtype, ArrayList paramdir, ArrayList paramvalue)
        {
            ArrayList paramsize = new ArrayList();
            return ExecProc(spname, timeout, paramname, paramtype, paramdir, paramvalue, paramsize);
        }

        public bool hasRow()
        {
            bool flag = _reader.Read();
            if (flag)
            {
                try
                {
                    int fieldCount = _reader.FieldCount;
                    _colnames = new string[fieldCount];
                    _colvalues = new object[fieldCount];
                    _coldatatypes = new string[fieldCount];
                    for (int i = 0; i < fieldCount; i++)
                    {
                        _colnames[i] = _reader.GetName(i);
                        _colvalues[i] = _reader[i];
                        _coldatatypes[i] = _reader.GetDataTypeName(i);
                    }
                    return flag;
                }
                catch
                {
                    _colnames = new string[0];
                    _colvalues = new object[0];
                    _coldatatypes = new string[0];
                    return false;
                }
            }
            _colnames = new string[0];
            _colvalues = new object[0];
            _coldatatypes = new string[0];
            return flag;
        }

        public DataSet SetData(string SQLQuery, object[] param, int timeout)
        {
            try
            {
                SqlConnection.ClearAllPools();
            }
            catch
            {

            }

            try
            {

                if (this._conn.State == ConnectionState.Open)
                {
                    this._conn.Close();
                }
                this._conn.Open();
            }
            catch
            {

            }
            SqlCommand sqlCommand = new SqlCommand(this.getCommandQuery(SQLQuery, param, true), _conn);
            if (timeout > 0)
            {
                sqlCommand.CommandTimeout = timeout;
            }
            SqlDataAdapter sqlDataAdapter = new SqlDataAdapter(sqlCommand);
            DataSet dataSet = new DataSet();
            sqlDataAdapter.Fill(dataSet);
            DBCloseConnection();
            return dataSet;
        }

        public DataTable GetDataTable(string query, object[] param, int dbtimeout)
        {
            return GetDataTable(query, param, dbtimeout, _conn.ConnectionString, true, false);
        }

        public DataTable GetDataTable(string query, object[] param, int dbtimeout, string connstr, bool BlankToNull, bool withSchema)
        {
            DataTable dataTable = null;
            if (this._conn.State != ConnectionState.Open)
            {
                this._conn.Open();
            }
            try
            {
                if (this._reader != null)
                    if (!this._reader.IsClosed)
                        this._reader.Close();
            }
            catch
            {
            }
            try
            {
                SqlCommand sqlCommand = new SqlCommand(this.getCommandQuery(query, param, BlankToNull), this._conn);
                if (dbtimeout > 0)
                {
                    sqlCommand.CommandTimeout = dbtimeout;
                }
                SqlDataAdapter sqlDataAdapter = new SqlDataAdapter(sqlCommand);
                dataTable = new DataTable();
                if (withSchema)
                {
                    sqlDataAdapter.FillSchema(dataTable, SchemaType.Mapped);
                }
                sqlDataAdapter.Fill(dataTable);
                sqlDataAdapter.Dispose();
            }
            catch (SqlException ex)
            {
                string text = this.getCommandQuery(query, param, BlankToNull);
                if (dbtimeout > 0)
                {
                    text = text + "; Timeout was set to: " + dbtimeout.ToString();
                }
                throw new ApplicationException(ex.Message + " Last Query: " + text);
            }

            return dataTable;
        }


        public DataTable GetDataTable(string query, object[] param, int dbtimeout, string constr)
        {
            return this.GetDataTable(query, param, dbtimeout, true, false);
        }

        public DataTable GetDataTable(string query, object[] param, int dbtimeout, bool Schema)
        {
            return this.GetDataTable(query, param, dbtimeout, true, Schema);
        }

        public DataTable GetDataTable(string query, object[] param, int timeout, bool BlankToNull, bool withSchema)
        {
            DataTable dataTable = null;
            try
            {
                if (_conn.State != ConnectionState.Open)
                {
                    _conn.Open();
                }
            }
            catch { }
            try
            {
                if (this._reader != null)
                    if (!_reader.IsClosed)
                        _reader.Close();
            }
            catch
            {
            }
            try
            {

                SqlCommand sqlCommand = new SqlCommand(getCommandQuery(query, param, BlankToNull), _conn);
                if (timeout > 0)
                {
                    sqlCommand.CommandTimeout = timeout;
                }
                SqlDataAdapter sqlDataAdapter = new SqlDataAdapter(sqlCommand);
                dataTable = new DataTable();
                if (withSchema)
                {
                    sqlDataAdapter.FillSchema(dataTable, SchemaType.Mapped);
                }
                sqlDataAdapter.Fill(dataTable);
                sqlDataAdapter.Dispose();
                return dataTable;
            }
            catch (SqlException ex)
            {
                string text = getCommandQuery(query, param, BlankToNull);
                if (timeout > 0)
                {
                    text = text + "; Timeout was set to: " + timeout.ToString();
                }
                InvalidOperationException innerException = new InvalidOperationException(ex.Message + " Last Query: " + text, ex);
                throw new ApplicationException(ex.Message, innerException);
            }
        }

        private string modifyData(string SQLQuery, SqlConnection DBCons)
        {
            try
            {
                SqlConnection.ClearAllPools();
            }
            catch
            {
            }
            SqlCommand sqlCommand = new SqlCommand(SQLQuery, DBCons);
            try
            {
                if (DBCons.State == ConnectionState.Open)
                {
                    DBCons.Close();
                }
                DBCons.Open();
            }
            catch
            {

            }
            try
            {
                sqlCommand.ExecuteNonQuery();
                sqlCommand.Dispose();
                _conn.Close();
            }
            catch (SqlException ex)
            {
                return ex.Message;
            }
            return "";
        }

        private string parseProcedure(string cmdText, string[] arrProcVar, string[] arrDat)
        {
            SqlCommand sqlCommand = new SqlCommand();
            sqlCommand.Connection = this._conn;
            sqlCommand.CommandType = CommandType.StoredProcedure;
            sqlCommand.CommandText = cmdText;
            string result;
            if (arrProcVar.Length > 0)
            {
                for (int i = 0; i < arrProcVar.Length; i++)
                {
                    SqlParameter sqlParameter = new SqlParameter(arrProcVar[i].ToString(), arrDat[i].ToString());
                    sqlParameter.Direction = ParameterDirection.Input;
                    sqlCommand.Parameters.Add(sqlParameter);
                }
                if (this._conn.State == ConnectionState.Open)
                {
                    this._conn.Close();
                }
                this._conn.Open();
                try
                {
                    sqlCommand.ExecuteNonQuery();
                }
                catch (System.Exception ex)
                {
                    result = "ex:" + ex.Message;
                    return result;
                }
                finally
                {
                    this._conn.Close();
                }
                result = "";
            }
            else
            {
                result = "ex:Empty Parameter";
            }
            return result;
        }

        public SqlDataReader ExecReader(string query, object[] param, int dbtimeout)
        {
            return this.ExecReader(query, param, dbtimeout, false);
        }

        public SqlDataReader ExecReader(string query, object[] param, int dbtimeout, bool BlankToNull)
        {
            if (_conn.State == ConnectionState.Open)
            {
                _conn.Close();
            }
            _conn.Open();
            try
            {
                if (this._reader != null)
                    if (!this._reader.IsClosed)
                        this._reader.Close();
            }
            catch
            {
            }
            try
            {
                SqlCommand sqlCommand = new SqlCommand(this.getCommandQuery(query, param, BlankToNull), _conn);
                if (dbtimeout > 0)
                {
                    sqlCommand.CommandTimeout = dbtimeout;
                }
                this._reader = sqlCommand.ExecuteReader();
            }
            catch (SqlException ex)
            {
                string text = this.getCommandQuery(query, param, BlankToNull);
                if (dbtimeout > 0)
                {
                    text = text + "; Timeout was set to: " + dbtimeout.ToString();
                }
                InvalidOperationException innerException = new InvalidOperationException(ex.Message + " Last Query: " + text, ex);
                throw new ApplicationException(ex.Message, innerException);
            }
            return this._reader;
        }
        public int ExecuteNonQuery(string query, object[] param, int timeout, bool BlankToNull)
        {
            if (_conn.State != ConnectionState.Open)
            {
                _conn.Open();
            }

            try
            {
                if (this._reader != null)
                    if (!_reader.IsClosed)
                        _reader.Close();
            }
            catch
            {
            }

            try
            {
                SqlCommand sqlCommand = new SqlCommand(getCommandQuery(query, param, BlankToNull), _conn);
                if (timeout > 0)
                {
                    sqlCommand.CommandTimeout = timeout;
                }

                return sqlCommand.ExecuteNonQuery();
            }
            catch (SqlException ex)
            {
                string text = getCommandQuery(query, param, BlankToNull);
                if (timeout > 0)
                {
                    text = text + "; Timeout was set to: " + timeout;
                }

                throw new ApplicationException(ex.Message + " Last Query: " + text);
            }
        }
        public int ExecNonQuery(string query, object[] param, int timeout)
        {
            return ExecuteNonQuery(query, param, timeout, BlankToNull: true);
        }
        public int ExecNonQuery(string query, object[] param, int timeout, bool BlankToNull)
        {
            return ExecuteNonQuery(query, param, timeout, BlankToNull);
        }
        private string getCommandQuery(string query, object[] paramorig, bool BlankToNull)
        {
            object[] array = this.getparam(paramorig);
            if (array != null)
            {
                if (query.ToLower().StartsWith("exec") && query.IndexOf("@") == -1)
                {
                    for (int i = 1; i <= array.Length; i++)
                    {
                        query = query + " @" + i.ToString() + ",";
                    }
                    query = query.Substring(0, query.Length - 1);
                }
                query = query.Replace("@", "#4$#");
                for (int j = array.Length; j > 0; j--)
                {
                    if (array[j - 1] != null)
                    {
                        string key;
                        switch (key = array[j - 1].GetType().Name.ToLower())
                        {
                            case "int32":
                            case "int64":
                                query = query.Replace("#4$#" + j.ToString(), array[j - 1].ToString());
                                goto IL_472;
                            case "single":
                            case "double":
                                {
                                    double num2 = (double)array[j - 1];
                                    long num3 = (long)num2;
                                    double num4 = num2 - (double)num3;
                                    string text = num3.ToString();
                                    try
                                    {
                                        if (num4 < 0.0)
                                        {
                                            num4 *= -1.0;
                                        }
                                        text = text + "." + num4.ToString().Substring(2);
                                    }
                                    catch
                                    {
                                    }
                                    query = query.Replace("#4$#" + j.ToString(), text);
                                    goto IL_472;
                                }
                            case "boolean":
                                if ((bool)array[j - 1])
                                {
                                    query = query.Replace("#4$#" + j.ToString(), "1");
                                    goto IL_472;
                                }
                                query = query.Replace("#4$#" + j.ToString(), "0");
                                goto IL_472;
                            case "dbnull":
                                query = query.Replace("#4$#" + j.ToString(), "NULL");
                                goto IL_472;
                            case "datetime":
                                {
                                    DateTime dateTime = (DateTime)array[j - 1];
                                    string newValue = string.Concat(new string[]
                            {
                                "'",
                                dateTime.Year.ToString(),
                                "/",
                                dateTime.Month.ToString(),
                                "/",
                                dateTime.Day.ToString(),
                                " ",
                                dateTime.Hour.ToString(),
                                ":",
                                dateTime.Minute.ToString(),
                                ":",
                                dateTime.Second.ToString(),
                                ".",
                                dateTime.Millisecond.ToString(),
                                "'"
                            });
                                    if (dateTime.Day == 1 && dateTime.Month == 1 && dateTime.Year == 1900)
                                    {
                                        newValue = "NULL";
                                    }
                                    query = query.Replace("#4$#" + j.ToString(), newValue);
                                    goto IL_472;
                                }
                        }
                        string text2 = array[j - 1].ToString().Replace("'", "''").Replace("&nbsp;", " ");
                        if (BlankToNull)
                        {
                            text2 = "'" + text2.Trim() + "'";
                            if (text2 == "''")
                            {
                                text2 = "NULL";
                            }
                            query = query.Replace("#4$#" + j.ToString(), text2);
                        }
                        else
                        {
                            query = query.Replace("#4$#" + j.ToString(), "'" + text2 + "'");
                        }
                    }
                    else
                    {
                        query = query.Replace("#4$#" + j.ToString(), "NULL");
                    }
                IL_472:;
                }
            }
            return query.Replace("#4$#", "@").Trim();
        }

        public string Save(NameValueCollection FieldNameNVC, NameValueCollection FieldKeyNVC, string TableName, int dbtimeout, string constr)
        {

            SqlConnection DBCons = new SqlConnection(DBReadCnnStr(constr));

            string text = "";
            string result = "";
            try
            {
                string text2 = " WHERE 1=1";
                for (int i = 0; i < FieldKeyNVC.Count; i++)
                {
                    string key = FieldKeyNVC.GetKey(i);
                    string text3 = FieldKeyNVC[i];
                    string text4 = text2;
                    text2 = text4 + " AND " + key + " = " + text3;
                }
                DataTable dataTable = GetDataTable("SELECT top 1 1 FROM " + TableName + text2, null, dbtimeout, constr);
                if (dataTable.Rows.Count > 0)
                {
                    string text5 = "";
                    for (int i = 0; i < FieldNameNVC.Count; i++)
                    {
                        string key = FieldNameNVC.GetKey(i);
                        string text3 = FieldNameNVC[i];
                        if (i == 0)
                        {
                            text5 = key + " = " + text3;
                        }
                        else
                        {
                            string text4 = text5;
                            text5 = text4 + ", " + key + " = " + text3;
                        }
                    }
                    text = modifyData("UPDATE " + TableName + " SET " + text5 + text2, DBCons);
                }
                else
                {
                    NameValueCollection nameValueCollection = new NameValueCollection();
                    for (int i = 0; i < FieldKeyNVC.Count; i++)
                    {
                        string key = FieldKeyNVC.GetKey(i);
                        string text3 = FieldNameNVC[key] = FieldKeyNVC[i];
                    }
                    for (int i = 0; i < FieldNameNVC.Count; i++)
                    {
                        string key = FieldNameNVC.GetKey(i);
                        string text3 = nameValueCollection[key] = FieldNameNVC[i];
                    }
                    string text8 = "";
                    string text9 = "";
                    for (int i = 0; i < nameValueCollection.Count; i++)
                    {
                        string key = nameValueCollection.GetKey(i);
                        string text3 = nameValueCollection[i];
                        if (i == 0)
                        {
                            text8 = key;
                            text9 = text3;
                        }
                        else
                        {
                            text8 = text8 + "," + key;
                            text9 = text9 + "," + text3;
                        }
                    }
                    text = modifyData("INSERT INTO " + TableName + " (" + text8 + " ) VALUES (" + text9 + ")", DBCons);
                }
            }
            catch (Exception ex)
            {
                result = ex.Message.ToString();
            }

            result = text;

            return result;
        }

        public void Delete(NameValueCollection FieldKeyNVC, string TableName)
        {
            string text = " WHERE 1=1";
            for (int i = 0; i < FieldKeyNVC.Count; i++)
            {
                string key = FieldKeyNVC.GetKey(i);
                string text2 = FieldKeyNVC[i];
                string text3 = text;
                text = text3 + " AND " + key + " = " + text2;
            }
            modifyData("DELETE " + TableName + text, _conn);
        }

        private object getvalue(object obj)
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

        private object[] getparam(object[] param)
        {
            if (param == null)
            {
                return null;
            }
            object[] array = new object[param.Length];
            for (int i = 1; i <= param.Length; i++)
            {
                array[i - 1] = this.getvalue(param[i - 1]);
            }
            return array;
        }

        public int GetColIdx(string colname)
        {
            for (int i = 0; i < this._colnames.Length; i++)
            {
                if (this._colnames[i].ToLower() == colname.ToLower())
                {
                    return i;
                }
            }
            throw new ApplicationException("Invalid column name " + colname);
        }

        public string[] GetColDataTypes()
        {
            return this._coldatatypes;
        }

        public string GetColDataType(int idx)
        {
            return this._coldatatypes[idx];
        }

        public object[] GetFieldValues()
        {
            return this._colvalues;
        }

        public string GetFieldValue(int idx)
        {
            return this._colvalues[idx].ToString();
        }

        public string GetFieldValue(string colname)
        {
            return this._colvalues[this.GetColIdx(colname)].ToString();
        }

        public object GetNativeFieldValue(string colname)
        {
            return this.GetNativeFieldValue(this.GetColIdx(colname));
        }

        public object GetNativeFieldValue(int idx)
        {
            if (this._colvalues[idx].GetType().Name.ToLower() == "dbnull")
            {
                string a;
                if ((a = this._coldatatypes[idx]) != null)
                {
                    if (a == "datetime")
                    {
                        return new DateTime(1900, 1, 1);
                    }
                    if (a == "int")
                    {
                        int num = 0;
                        return num;
                    }
                    if (a == "float" || a == "double")
                    {
                        double num2 = 0.0;
                        return num2;
                    }
                    if (a == "bit")
                    {
                        return false;
                    }
                }
                return null;
            }
            return this._colvalues[idx];
        }

        public void CloseConnection()
        {
            ClearData();
            AbortTransaction();
            _conn.Close();
            _conn.Dispose();
        }
        public void AbortTransaction()
        {
            if (_tran != null)
            {
                try
                {
                    _tran.Rollback();
                }
                catch
                {
                }

                try
                {
                    _tran.Dispose();
                }
                catch
                {
                }

                _tran = null;
            }
        }
        public void ClearData()
        {
            try
            {
                if (this._reader != null)
                    if (!_reader.IsClosed)
                        _reader.Close();
                _reader = null;
            }
            catch
            {
            }
        }
        public void Dispose()
        {
            try
            {
                CloseConnection();
            }
            catch
            {
            }
        }
    }
}
