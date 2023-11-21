using System;
using Notif.Services.Worker.Scheduler;
using Topshelf;

namespace Notif.Services.Worker
{
    class Program
    {
        static void Main(string[] args)
        {
            log4net.Config.XmlConfigurator.Configure();

            // change from service account's dir to more logical one
            System.IO.Directory.SetCurrentDirectory(System.AppDomain.CurrentDomain.BaseDirectory);

            HostFactory.Run(x =>
            {
                x.RunAsLocalSystem();

                x.SetDescription(QuartzConfiguration.ServiceDescription);
                x.SetDisplayName(QuartzConfiguration.ServiceDisplayName);
                x.SetServiceName(QuartzConfiguration.ServiceName);

                x.Service(factory =>
                {
                    QuartzServer server = new QuartzServer();
                    server.Initialize();
                    return server;
                });
            });
        }
    }
}
