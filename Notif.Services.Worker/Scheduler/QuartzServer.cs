using System;
using System.Configuration;
using Notif.Services.Worker.Jobs;
using log4net;
using Quartz;
using Quartz.Impl;
using Topshelf;

namespace Notif.Services.Worker.Scheduler
{
    class QuartzServer : ServiceControl, IQuartzServer
    {
        private readonly ILog logger;
        private ISchedulerFactory schedulerFactory;
        private IScheduler scheduler;

        /// <summary>
        /// Initializes a new instance of the <see cref="QuartzServer"/> class.
        /// </summary>
        public QuartzServer()
        {
            logger = LogManager.GetLogger(GetType());
        }

        /// <summary>
        /// Initializes the instance of the <see cref="QuartzServer"/> class.
        /// </summary>
        public virtual void Initialize()
        {
            try
            {
                schedulerFactory = CreateSchedulerFactory();
                scheduler = GetScheduler();
            }
            catch
            {
                logger.Error("Server initialization failed: NotificationMessageService");
                throw;
            }
        }

        /// <summary>
        /// Gets the scheduler with which this server should operate with.
        /// </summary>
        /// <returns></returns>
        protected virtual IScheduler GetScheduler()
        {
            return schedulerFactory.GetScheduler();
        }

        /// <summary>
        /// Returns the current scheduler instance (usually created in <see cref="Initialize" />
        /// using the <see cref="GetScheduler" /> method).
        /// </summary>
        protected virtual IScheduler Scheduler
        {
            get { return scheduler; }
        }

        /// <summary>
        /// Creates the scheduler factory that will be the factory
        /// for all schedulers on this instance.
        /// </summary>
        /// <returns></returns>
        protected virtual ISchedulerFactory CreateSchedulerFactory()
        {
            return new StdSchedulerFactory();
        }

        /// <summary>
        /// Starts this instance, delegates to scheduler.
        /// </summary>
        public virtual void Start()
        {
            try
            {
                AppVars.ReloadConfig();

                scheduler.Start();

                string cronExpression = string.Format(
                    ConfigurationManager.AppSettings["SchedulerCronExpressionFormat"],
                    ConfigurationManager.AppSettings["SchedulerTime"]);

                #region EMAILJOB
                var job = JobBuilder.Create(typeof(SendNotifJob))
                    .WithIdentity("SendMailJob", "EmailJobGroup")
                    .Build();

                var trigger = TriggerBuilder.Create()
                    .WithIdentity("EmailJobTrigger", "EmailJobTriggerGroup")
                    .WithCronSchedule(cronExpression)
                    .Build();

                scheduler.ScheduleJob(job, trigger);
                #endregion
            }
            catch (Exception ex)
            {
                logger.Fatal(string.Format("Scheduler start failed: {0}", "NotificationMessageService"), ex);
                throw;
            }

            logger.Info("Scheduler started successfully");
        }

        /// <summary>
        /// Stops this instance, delegates to scheduler.
        /// </summary>
        public virtual void Stop()
        {
            try
            {
                scheduler.Shutdown(true);
            }
            catch (Exception ex)
            {
                logger.Error(string.Format("Scheduler stop failed: {0}", "NotificationMessageService"), ex);
                throw;
            }

            logger.Info("Scheduler shutdown complete");
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public virtual void Dispose()
        {
            // no-op for now
        }

        /// <summary>
        /// Pauses all activity in scheduler.
        /// </summary>
        public virtual void Pause()
        {
            scheduler.PauseAll();
        }

        /// <summary>
        /// Resumes all activity in server.
        /// </summary>
        public void Resume()
        {
            scheduler.ResumeAll();
        }

        /// <summary>
        /// TopShelf's method delegated to <see cref="Start()"/>.
        /// </summary>
        public bool Start(HostControl hostControl)
        {
            Start();
            return true;
        }

        /// <summary>
        /// TopShelf's method delegated to <see cref="Stop()"/>.
        /// </summary>
        public bool Stop(HostControl hostControl)
        {
            Stop();
            return true;
        }

        /// <summary>
        /// TopShelf's method delegated to <see cref="Pause()"/>.
        /// </summary>
        public bool Pause(HostControl hostControl)
        {
            Pause();
            return true;
        }

        /// <summary>
        /// TopShelf's method delegated to <see cref="Resume()"/>.
        /// </summary>
        public bool Continue(HostControl hostControl)
        {
            Resume();
            return true;
        }
    }
}
