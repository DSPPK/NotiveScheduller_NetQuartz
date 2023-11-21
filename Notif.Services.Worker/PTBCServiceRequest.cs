using log4net;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using Topshelf;
using DMS.Combank.Cons.Worker.Operations.Sms;
using DMS.Combank.Cons.Worker.Model;
using System.Web.Script.Serialization;
using System.Configuration;

namespace DMS.Combank.Cons.Worker
{
    class PTBCServiceRequest
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(Program));
        static string PostHttp<T>(string requestData, string uri, int timeOut, string ProxyAddress, string ProxyUser, string ProxyPWD)
        {

            System.Net.ServicePointManager.ServerCertificateValidationCallback += (s, cert, chain, sslPolicyErrors) => true;
            try
            {
                log.Info("SMS Engine SVC: requestData: " + requestData);
                byte[] data = System.Text.Encoding.ASCII.GetBytes(requestData);

                log.Info("SMS Engine SVC: delegate ServicePointManager");
                System.Net.ServicePointManager.ServerCertificateValidationCallback +=
                    delegate(object sender, System.Security.Cryptography.X509Certificates.X509Certificate certificate,
                                            System.Security.Cryptography.X509Certificates.X509Chain chain,
                                            System.Net.Security.SslPolicyErrors sslPolicyErrors)
                    {
                        return true; // **** Always accept
                    };

                #region add by lutvi post with proxy
                string urlProxy = string.Empty;
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(uri);
                IWebProxy proxy = request.Proxy;

                if (proxy != null)
                {
                    urlProxy = proxy.GetProxy(request.RequestUri).ToString();
                }
                else
                {
                    urlProxy = "Proxy is null, no proxy will be used";
                }

                log.Info("SMS Engine SVC: Check Proxy Address from default : " + urlProxy);

                if (ProxyAddress != null && ProxyAddress != "")
                {
                    WebProxy myProxy = new WebProxy();
                    Uri newUri = new Uri(ProxyAddress);
                    //// Associate the newUri object to 'myProxy' object so that new myProxy settings can be set.
                    myProxy.Address = newUri;
                    //// Create a NetworkCredential object and associate it with the 
                    //// Proxy property of request object.
                    myProxy.Credentials = new NetworkCredential(ProxyUser.TrimStart().TrimEnd(), ProxyPWD.TrimStart().TrimEnd());
                    request.Proxy = myProxy;
                }
                

                int timeout = timeOut;
                request.Method = WebRequestMethods.Http.Post;
                request.ContentType = "application/json";
                request.KeepAlive = false;
                request.Timeout = timeout;
                #endregion

                log.Debug("SMS Engine SVC: Ready for stream");
                using (Stream stream = request.GetRequestStream())
                {
                    stream.Write(data, 0, data.Length);
                }
                log.Debug("SMS Engine SVC: Stream Finish");

                using (var response = (HttpWebResponse)request.GetResponse())
                {
                    using (StreamReader reader = new StreamReader(response.GetResponseStream()))
                    {
                        string result = reader.ReadToEnd();
                        log.Debug("SMS Engine SVC: response: " + result);
                        result = result.Replace("trxid=", "").Replace("&status=", ";");

                        //var ser = new ServiceStack.Text.JsonSerializer<T>();
                        //T resp = ser.DeserializeFromString(result);

                        return result;
                    }
                }
            }
            catch (Exception ex)
            {
                log.Error("SMS Engine SVC: Error in SMS Engine Service=> " + ex.Message + " Proxy Configuration=> User:" + ProxyUser + ";PWD:" + ProxyPWD + ";Address:" + ProxyAddress + " => req data:" + requestData, ex);
                //log.Error("PostHttp<T>: " + ex.Message);
                return string.Empty;
            }
        }

        internal static ModelResData SendSMS(ModelReqData data, string URL, string ProxyAddress, string ProxyPWD, string ProxyUser)
        {
            
            
            ModelResData resp = new ModelResData();
            try
            {
                var str = new JavaScriptSerializer().Serialize(data);
                int timeout = 22000;
                try
                {
                    timeout = int.Parse(ConfigurationManager.AppSettings["dbtimeoutsms"]);
                }
                catch { }
                var respnd = PostHttp<JsData>(str, URL, timeout, ProxyAddress, ProxyUser, ProxyPWD).Split(';');
                
                resp.TRXID = respnd[0];
                resp.STATUS = respnd[1];
            }
            catch {
                //throw;
            }
            return resp;
        }
    }
}
