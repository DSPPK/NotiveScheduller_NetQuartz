using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
namespace Notif.Services.Worker.Scheduler
{
    /// <summary>
    /// Factory class to create Quartz server implementations from.
    /// </summary>
    public class QuartzServerFactory
    {
        private static readonly ILog logger = LogManager.GetLogger(typeof(QuartzServerFactory));

        /// <summary>
        /// Creates a new instance of an Quartz.NET server core.
        /// </summary>
        /// <returns></returns>
        public static IQuartzServer CreateServer()
        {
            string typeName = QuartzConfiguration.ServerImplementationType;

            Type t = Type.GetType(typeName, true);

            logger.Debug("Creating new instance of server type 'NotificationMessageService'");
            IQuartzServer retValue = (IQuartzServer)Activator.CreateInstance(t);
            logger.Debug("Instance successfully created");
            return retValue;
        }
    }
}
