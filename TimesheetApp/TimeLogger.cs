using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TimesheetApp.DBAccess;
using TimesheetApp.Interfaces;
using TimesheetApp.Util;

namespace TimesheetApp
{
    public class TimeLogger
    {
        ITask task;
        IEmailSender emailSender;
        IErrorLogger errorLogger;
        IUserLogger userLogger;
        ITaskManager taskManager;

        public TimeLogger(
           ITask task,
           IEmailSender emailSender,
           IErrorLogger errorLogger,
           IUserLogger userLogger,
           ITaskManager taskManager)


        {
            this.task = task;
            this.emailSender = emailSender;
            this.errorLogger = errorLogger;
            this.userLogger = userLogger;
            this.taskManager = taskManager;
        }

        public void LogTime(int hours, int minutes, string description)
        {
            try
            {
                //dobaviti podatke o trenutno logovanom korisniku (username, email)
                string userName = userLogger.GetLoggedUserName();
                string userEmail = userLogger.GetLoggedUserEmail(userName);

                //dobaviti podatke o projektu i aktivnosti (task) na projektu za koju se loguje vreme
                int taskId = taskManager.GetTaskId(userName, userEmail);

                //logovati vreme - sacuvati podatke u bazi
                task.TaskId = taskId;
                task.Hours = hours;
                task.Minutes = minutes;
                task.Description = description;
                bool saved = task.SaveToDB();
                if (saved)
                {
                    //poslati mejl obavestenja o logovanom vremenu
                    emailSender.SendEmail(userEmail, 
                                         "Time logged successfully",
                                         hours + " hours and " + minutes + " minutes successfully logged to task with ID=" + taskId);
                }
                else
                {
                    throw new Exception("Failed to save data to database");
                }                                               
            }
            catch (Exception ex) //obrada gresaka
            {
                errorLogger.LogError(ex);
                throw ex;
            }
        }
    }
}
