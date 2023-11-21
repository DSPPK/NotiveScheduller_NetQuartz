using log4net;
using System;
using System.Configuration;
using System.Net;
using System.Net.Mail;

namespace DMS.Interface
{
    /// <summary>
    /// An SMTP Email Sender using System.Net.Mail.
    /// </summary>
    public class Email2Sender
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(Email2Sender));
        public static void SendMail(string from, string to, string cc, string subj, string body, string svr)
        {
            SendMail(from, to, cc, subj, body, "", svr, "", "");
        }

        public static void SendMail(string from, string to, string cc, string subj, string body, string attcpath, string svr)
        {
            SendMail(from, to, cc, subj, body, attcpath, svr, "", "");
        }

        public static void SendMail(string from, string to, string cc, string subj, string body, string attcpath, string svr, string user, string pwd)
        {
            SendMail(from, to, cc, subj, body, attcpath, svr, 0, false, user, pwd);
        }

        public static void SendMail(string from, string to, string cc, string subj, string body, string attcpath,
            string svr, int port, bool ssl, string user, string pwd)
        {
            /*	Sample using GMail
             *		SendMail(<from>, <to>, <cc>, <subj>, <body>, <attcpath>, "smtp.gmail.com", 587, true, <user>, <pwd>);   //yupp port 587 for this type, not 465 as when we used System.Web.Mail
             */
            try
            {
                MailMessage message = new MailMessage(from, to);

                //string[] _to = to.Split(',');
                //for (int j = 0; j < _to.Length; x++)
                //{
                //    if (_to[j].Trim() != "")
                //        message.To.Add(new MailAddress(_to[j]));
                //}

                string[] _cc = cc.Split(',');
                for (int x = 0; x < _cc.Length; x++)
                {
                    if (_cc[x].Trim() != "")
                        message.CC.Add(new MailAddress(_cc[x]));
                }

                message.Subject = subj;
                message.IsBodyHtml = true;

                body = @"<html><body>" + body + "</body></html>";
                message.Body = body;

                //avoid spam email - add by lutvi
                message.BodyEncoding = System.Text.Encoding.GetEncoding("utf-8");
                System.Net.Mail.AlternateView plainView = System.Net.Mail.AlternateView.CreateAlternateViewFromString
                (System.Text.RegularExpressions.Regex.Replace(body, @"< (.|\n) *?>", string.Empty), null, "text/plain");
                System.Net.Mail.AlternateView htmlView = System.Net.Mail.AlternateView.CreateAlternateViewFromString(body, null, "text/html");
                //avoid spam email - add by lutvi

                if (attcpath != null && attcpath.Trim() != "")
                {
                    Attachment MyAttachment = new Attachment(attcpath, System.Net.Mime.MediaTypeNames.Application.Octet);
                    message.Attachments.Add(MyAttachment);
                    message.Priority = MailPriority.Normal;
                }

                int timeout = 22000;
                try
                {
                    timeout = int.Parse(ConfigurationManager.AppSettings["dbtimeoutemail"]);
                }
                catch { }

                SmtpClient client = new SmtpClient(svr, port);
                log.Info("Mail Service: server: " + svr + "Port: " + port);

                client.Timeout = timeout;
                client.UseDefaultCredentials = false;
                log.Info("Mail Service: UseDefaultCredentials: false");
                client.EnableSsl = ssl;
                client.ServicePoint.Expect100Continue = false;
                log.Info("Mail Service: TimeOut: " + timeout);
                log.Info("Mail Service: ssl: " + ssl);
                // Add credentials if the SMTP server requires them.
                if (user != null && user.Trim() != "")
                {
                    client.Credentials = new NetworkCredential(user, pwd);
                    log.Info("Mail Service: Create new Credential: " + user + " - " + pwd);

                    if (pwd == "" || pwd == null)
                    {
                        client.UseDefaultCredentials = true;
                        log.Info("Mail Service: UseDefaultCredentials: true");
                    }
                }

                client.DeliveryMethod = SmtpDeliveryMethod.Network;
                log.Info("Mail Service: sending to smtp server");
                client.Send(message);
                log.Info("Mail Service: finish send mail to smtp server");
            }
            catch (Exception ex)
            {
                log.Info("Mail Service: error from smtp server, message:" + ex.Message);
                throw ex;				//re-throw any exception to the calling class
            }
        }
    }
}

