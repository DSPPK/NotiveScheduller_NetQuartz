using System;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Xml;
using System.Configuration;

namespace DMS.Mobile.WS.DB
{
    public class ACSDataConnection
    {
        public System.Data.SqlClient.SqlConnection cnACS;
        public void ACSConnection(string connstr)
        {
            this.cnACS = new System.Data.SqlClient.SqlConnection(ACSReadCnnStr(connstr));
        }
        public void ACSCloseConnection()
        {
            this.cnACS.Close();
        }
        public static string decryptConnStr(string encryptedConnStr)
        {
            string connStr, encpwd, decpwd = "";
            int pos1, pos2;
            pos1 = encryptedConnStr.IndexOf("pwd=");
            pos2 = encryptedConnStr.IndexOf(";", pos1 + 4);
            encpwd = encryptedConnStr.Substring(pos1 + 4, pos2 - pos1 - 4);
            for (int i = 2; i < encpwd.Length; i++)
            {
                char chr = (char)(encpwd[i] - 2);
                decpwd += new string(chr, 1);
            }
            connStr = encryptedConnStr.Replace(encpwd, decpwd);
            return connStr;
        }
        public string ACSReadCnnStr(string connstr)
        {
            //string result=(decryptConnStr(ConfigurationManager.AppSettings["eSecurityConnectString"]));
            string result = connstr; // sqldatabase
            return result;
        }
        public bool ACSChkConnection(string cnnStr)
        {
            bool result;
            using (System.Data.SqlClient.SqlConnection sqlConnection = new System.Data.SqlClient.SqlConnection(cnnStr))
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
        public System.Data.DataSet ACSGetData(string SQLQuery, string connstr)
        {
            ACSConnection(connstr);

            System.Data.SqlClient.SqlDataAdapter sqlDataAdapter = new System.Data.SqlClient.SqlDataAdapter(SQLQuery, this.cnACS);
            System.Data.DataSet dataSet = new System.Data.DataSet();
            sqlDataAdapter.Fill(dataSet);
            ACSCloseConnection();
            return dataSet;
        }
        public System.Data.DataTable ACSGetDataTable(string SQLQuery, string connstr)
        {
            try
            {
                System.Data.SqlClient.SqlConnection.ClearAllPools();
            }
            catch
            {

            }
            ACSConnection(connstr);
            try
            {
                this.cnACS.Open();
            }
            catch
            {
                this.cnACS.Close();
                this.cnACS.Open();
            }

            System.Data.SqlClient.SqlDataAdapter sqlDataAdapter = new System.Data.SqlClient.SqlDataAdapter(SQLQuery, this.cnACS);
            System.Data.DataSet dataSet = new System.Data.DataSet();
            sqlDataAdapter.Fill(dataSet);
            sqlDataAdapter.Dispose();
            this.cnACS.Close();
            return dataSet.Tables[0];
        }
        public string modifyData(string SQLQuery, string connstr)
        {
            try
            {
                System.Data.SqlClient.SqlConnection.ClearAllPools();
            }
            catch
            {

            }
            ACSConnection(connstr);
            System.Data.SqlClient.SqlCommand sqlCommand = new System.Data.SqlClient.SqlCommand(SQLQuery, this.cnACS);
            try
            {
                this.cnACS.Open();
            }
            catch
            {
                this.cnACS.Close();
                this.cnACS.Open();
            }
            string result;
            try
            {
                sqlCommand.ExecuteNonQuery();
                sqlCommand.Dispose();
                this.cnACS.Close();
            }
            catch (System.Exception ex)
            {
                result = ex.Message;
                return result;
            }
            result = "";
            return result;
        }
        public string parseProcedure(string cmdText, string[] arrProcVar, string[] arrDat)
        {
            System.Data.SqlClient.SqlCommand sqlCommand = new System.Data.SqlClient.SqlCommand();
            sqlCommand.Connection = this.cnACS;
            sqlCommand.CommandType = System.Data.CommandType.StoredProcedure;
            sqlCommand.CommandText = cmdText;
            string result;
            if (arrProcVar.Length > 0)
            {
                for (int i = 0; i < arrProcVar.Length; i++)
                {
                    System.Data.SqlClient.SqlParameter sqlParameter = new System.Data.SqlClient.SqlParameter(arrProcVar[i].ToString(), arrDat[i].ToString());
                    sqlParameter.Direction = System.Data.ParameterDirection.Input;
                    sqlCommand.Parameters.Add(sqlParameter);
                }
                if (this.cnACS.State.ToString() == "Open")
                {
                    this.cnACS.Close();
                }
                this.cnACS.Open();
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
                    this.cnACS.Close();
                }
                result = "";
            }
            else
            {
                result = "ex:Empty Parameter";
            }
            return result;
        }
        public string parseProcedure_NoOutput(string cmdText, string[] arrProcVar, string[] arrDat)
        {
            int num = arrProcVar.Length;
            string result;
            if (num > 0)
            {
                System.Data.SqlClient.SqlCommand sqlCommand = new System.Data.SqlClient.SqlCommand(cmdText, this.cnACS);
                sqlCommand.CommandType = System.Data.CommandType.StoredProcedure;
                for (int i = 0; i < num; i++)
                {
                    sqlCommand.Parameters.AddWithValue(arrProcVar[i].ToString(), arrDat[i].ToString());
                }
                if (this.cnACS.State.ToString() == "Open")
                {
                    this.cnACS.Close();
                }
                try
                {
                    this.cnACS.Open();
                    sqlCommand.ExecuteNonQuery();
                }
                catch (System.Exception ex)
                {
                    result = "Exception#" + ex.Message;
                }
                finally
                {
                    result = "Success#";
                    this.cnACS.Close();
                }
            }
            else
            {
                result = "Empty#Data Collection Is Null";
            }
            return result;
        }
        public string parseProcedure_WithOutput(string cmdText, string[] arrProcVar, string[] arrDat)
        {
            int num = arrProcVar.Length;
            string text = "";
            if (num > 0)
            {
                System.Data.SqlClient.SqlCommand sqlCommand = new System.Data.SqlClient.SqlCommand(cmdText, this.cnACS);
                sqlCommand.CommandType = System.Data.CommandType.StoredProcedure;
                for (int i = 0; i < num - 1; i++)
                {
                    sqlCommand.Parameters.AddWithValue(arrProcVar[i].ToString(), arrDat[i].ToString());
                }
                System.Data.SqlClient.SqlParameter sqlParameter = new System.Data.SqlClient.SqlParameter();
                sqlParameter.ParameterName = arrProcVar[num - 1].ToString();
                sqlParameter.Value = arrDat[num - 1].ToString();
                sqlParameter.Direction = System.Data.ParameterDirection.InputOutput;
                sqlParameter.DbType = System.Data.DbType.String;
                sqlParameter.Size = 5000;
                sqlCommand.Parameters.Add(sqlParameter);
                if (this.cnACS.State.ToString() == "Open")
                {
                    this.cnACS.Close();
                }
                try
                {
                    this.cnACS.Open();
                    sqlCommand.ExecuteNonQuery();
                    text = sqlCommand.Parameters[arrProcVar[num - 1].ToString()].Value.ToString();
                    text = "Success#" + text;
                }
                catch (System.Exception ex)
                {
                    text = "Exception#" + ex.Message;
                }
                finally
                {
                    this.cnACS.Close();
                }
            }
            else
            {
                text = "Empty#Data Collection Is Null";
            }
            return text;
        }
    }
}
