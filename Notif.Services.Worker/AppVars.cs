
using System;
using System.Configuration;
using System.IO;
using System.Security.Cryptography;
using System.Threading;
using Notif.Services.Worker.Db;

namespace Notif.Services.Worker
{
    public class AppVars
    {
        private static bool _debugfile = false;
        private static int _maxtracelevel = 0;
        private static bool _debugwriterbusy = false;
        private static string _logfile, _trclogfile;
        private static bool _debugdb = false;

        //db vars
        private static int _dbtimeout = 0;
        private static string[] _arrconnstr;
        private static string[] _arrtypestr;

        private static byte[] rgbIV = System.Text.Encoding.ASCII.GetBytes("oiwehioplsiajsdv");
        private static byte[] key = System.Text.Encoding.ASCII.GetBytes("kajsdoaioalptljsadrq2jflkasd23jd");

        #region public properties
        public static string[] connstr
        {
            get { return _arrconnstr; }
        }

        public static string[] type
        {
            get { return _arrtypestr; }
        }

        public static int dbtimeout
        {
            get { return int.Parse(ConfigurationManager.AppSettings["dbtimeout"]); }
        }
        #endregion

        private static string DecryptString(string EncryptedText)
        {
            string retval;
            byte[] encryptedTextBytes = Convert.FromBase64String(EncryptedText);

            using (MemoryStream ms = new MemoryStream())
            {
                SymmetricAlgorithm rijn = SymmetricAlgorithm.Create();
                using (CryptoStream cs = new CryptoStream(ms, rijn.CreateDecryptor(key, rgbIV), CryptoStreamMode.Write))
                {
                    cs.Write(encryptedTextBytes, 0, encryptedTextBytes.Length);

                    cs.Close();
                }
                retval = System.Text.Encoding.UTF8.GetString(ms.ToArray());
            }

            return retval;
        }

        public static string decryptMyStr(string encryptedStr)
        {
            string decryptedStr = "";

            decryptedStr = DecryptString(encryptedStr);

            return decryptedStr;
        }

        public static string decryptConnStr(string encryptedConnStr)
        {
            if (encryptedConnStr == null || encryptedConnStr.Trim() == "")
                return "";

            string connStr, encpwd, decpwd = "";
            int pos1, pos2;
            pos1 = encryptedConnStr.IndexOf("pwd=");
            pos2 = encryptedConnStr.IndexOf(";", pos1 + 4);
            encpwd = encryptedConnStr.Substring(pos1 + 4, pos2 - pos1 - 4);
            decpwd = decryptMyStr(encpwd);
            connStr = encryptedConnStr.Replace(encpwd, decpwd);
            return connStr;
        }

        static AppVars()
        {
            ReloadConfig();
        }

        public static void ReloadConfig()
        {
            if (ConfigurationManager.AppSettings["debugfile"] != null)
                _debugfile = ConfigurationManager.AppSettings["debugfile"] == "on";
            if (ConfigurationManager.AppSettings["debugdb"] != null)
                _debugdb = ConfigurationManager.AppSettings["debugdb"] == "on";
            if (ConfigurationManager.AppSettings["LogFilename"] != null)
                _logfile = ConfigurationManager.AppSettings["LogFilename"];
            if (ConfigurationManager.AppSettings["tracelevel"] != null)
                try
                {
                    _maxtracelevel = int.Parse(ConfigurationManager.AppSettings["tracelevel"]);
                }
                catch { }
            if (ConfigurationManager.AppSettings["TraceLogFilename"] != null)
                _trclogfile = ConfigurationManager.AppSettings["TraceLogFilename"];

            if (ConfigurationManager.AppSettings["dbtimeout"] != null)
                try
                {
                    _dbtimeout = dbtimeout;
                }
                catch { }

            int pathcount = 0, idp = 0;

            foreach (string key in ConfigurationManager.AppSettings)
            {
                if (key.ToLower().StartsWith("type_"))
                    if (ConfigurationManager.AppSettings[key] != null && ConfigurationManager.AppSettings[key].Trim() != "")
                        pathcount++;
            }
            _arrtypestr = new string[pathcount];

            foreach (string key in ConfigurationManager.AppSettings)
            {
                if (key.ToLower().StartsWith("type_"))
                {
                    if (ConfigurationManager.AppSettings[key] != null && ConfigurationManager.AppSettings[key].Trim() != "")
                    {
                        _arrtypestr[idp] = ConfigurationManager.AppSettings[key];
                        idp++;
                    }
                }
            }
        }
        // BELUM TERPAKAI
        #region log support
        public static void putMessage(string sender, string msg)
        {
            putMessage(sender, msg, "", null, null);
        }

        public static void putTraceMessage(string sender, string msg, int tracelevel)
        {
            if (_maxtracelevel >= tracelevel)
                putMessage(sender, msg, "TRC" + tracelevel.ToString("00"), _trclogfile, null);
        }

        public static void putErrMessage(DBConnect conn, string sender, string msg)
        {
            putMessage(sender, msg, "ERROR", null, conn);
        }

        public static void putMessage(string sender, string msg, string type)
        {
            putTraceMessage(sender, msg, 3);
            putMessage(sender, msg, type, null, null);
        }

        public static void putMessage(string sender, string msg, string type, string logfile, DBConnect conn)
        {
            if (logfile == null || logfile.Trim() == "")
                logfile = _logfile;

            if (!_debugfile || logfile == null || logfile.Trim() == "")
                return;

            string now = DateTime.Now.ToString();
            if (type == null) type = "";
            while (type.Length < 5) type += " ";
            if (type.Length > 5) type = type.Substring(0, 5);

            if (sender == null) sender = "";
            while (sender.Length < 20) sender += " ";
            if (sender.Length > 20) sender = sender.Substring(0, 20);

            int waitloopcounter = 0;
            while (_debugwriterbusy)
            {
                waitloopcounter++;
                if (waitloopcounter > 10000)
                {
                    criticalError("putMessage", " | OrigSender::" + sender + " | " + msg, type, "LogBusy");
                    return;
                }
                Thread.Sleep(100);
                if (!_debugwriterbusy)
                    break;
            }

            if (_debugfile && logfile != null && logfile.Trim() != "")
            {
                _debugwriterbusy = true;                ///lock the file!!!

                try
                {
                    FileInfo f = new FileInfo(logfile);
                    string mylogfile = f.FullName.Replace(f.Extension, "") + DateTime.Now.ToString("MMM") + f.Extension;
                    f = new FileInfo(mylogfile);
                    if (f.Exists && f.CreationTime.Year != DateTime.Now.Year)
                        f.Delete();

                    using (StreamWriter writer = new StreamWriter(mylogfile, true))
                    {
                        writer.WriteLine(now + " |" + type + "| " + sender + " | " + msg);
                        writer.Flush();
                    }
                }
                catch (Exception ex)
                {
                    criticalError("putMessage", " | OrigSender::" + sender + " | " + msg, type, ex.Message);
                }

                _debugwriterbusy = false;                ///release the file....
            }
        }

        public static void criticalError(string sender, string msg, string type, string errmsg)
        {
            string now = DateTime.Now.ToString();
            try
            {
                using (StreamWriter errwriter = new StreamWriter("Critical.err.log", true))
                {
                    errwriter.WriteLine(now + " |" + type + "| " + sender + " | ERROR::" + errmsg + " | ORIGMSG::" + msg);
                    errwriter.Flush();
                }
            }
            catch
            {
                try
                {
                    using (StreamWriter errwriter2 = new StreamWriter("Critical.err2.log", true))         //attempt 2nd trial in case file was locked...
                    {
                        errwriter2.WriteLine(now + " |" + type + "| " + sender + " | ERROR::" + errmsg + " | ORIGMSG::" + msg);
                        errwriter2.Flush();
                    }
                }
                catch { }
            }
        }
        #endregion
    }
}
