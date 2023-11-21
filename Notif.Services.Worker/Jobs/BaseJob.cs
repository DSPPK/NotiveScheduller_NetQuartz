using log4net;
using Quartz;
using System;
using System.Configuration;
using System.IO;

namespace Notif.Services.Worker.Jobs
{
    [DisallowConcurrentExecution]
    public abstract class BaseJob : IJob
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(BaseJob));

        public abstract int Checking { get; }
        public abstract void Execute(IJobExecutionContext context);
        public static string GetDirectory()
        {
            DirectoryInfo myDirectory = new DirectoryInfo(Environment.CurrentDirectory);
            string path = CommonTools.StringNull(ConfigurationManager.AppSettings["pathstr"]);
            if (path == "")
                path = myDirectory.Parent.Parent.FullName;
            return path;
        }

        public static string ReNameMethod(string methodName)
        {
            return System.Text.RegularExpressions.Regex.Replace(methodName, "(?<=.)([A-Z])", "_$0"
                , System.Text.RegularExpressions.RegexOptions.Compiled).ToLower();
        }
    }
}