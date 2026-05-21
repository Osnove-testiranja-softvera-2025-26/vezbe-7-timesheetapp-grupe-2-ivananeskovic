using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TimesheetApp.Test
{
    
        [TestFixture]
        public class TimeLoggerTests
        {
            // Helper fake implementations
            class FakeTask : ITask
            {
                public int TaskId { get; set; }
                public int Hours { get; set; }
                public int Minutes { get; set; }
                public string Description { get; set; }

                public bool SaveToDBCalled = false;
                public bool SaveResult = true;

                public bool SaveToDB()
                {
                    SaveToDBCalled = true;
                    return SaveResult;
                }
            }

            class FakeEmailSender : IEmailSender
            {
                public string To;
                public string Title;
                public string Body;
                public bool Sent = false;

                public void SendEmail(string to, string title, string body)
                {
                    Sent = true;
                    To = to;
                    Title = title;
                    Body = body;
                }
            }

            class FakeErrorLogger : IErrorLogger
            {
                public Exception LoggedException;
                public void LogError(Exception error)
                {
                    LoggedException = error;
                }
            }

            class FakeUserLogger : IUserLogger
            {
                public string UserName = "john";
                public string Email = "john@example.com";
                public string GetLoggedUserName() => UserName;
                public string GetLoggedUserEmail(string userName) => Email;
            }

            class FakeUserLoggerThrowsOnEmail : IUserLogger
            {
                public string GetLoggedUserName() => "mike";
                public string GetLoggedUserEmail(string userName) => throw new Exception("Failed to get user email");
            }

            class FakeTaskManager : ITaskManager
            {
                public int TaskId = 42;
                public int GetTaskId(string loggedUserName, string loggedUserEmail) => TaskId;
            }

            [Test]
            public void LogTime_SuccessfulLogging_SavesAndSendsEmail()
            {
                var task = new FakeTask { SaveResult = true };
                var email = new FakeEmailSender();
                var error = new FakeErrorLogger();
                var user = new FakeUserLogger { UserName = "alice", Email = "alice@company.com" };
                var tm = new FakeTaskManager { TaskId = 7 };

                var logger = new TimeLogger(task, email, error, user, tm);

                logger.LogTime(2, 30, "Worked on feature X");

                Assert.IsTrue(task.SaveToDBCalled, "Task.SaveToDB should be called");
                Assert.IsTrue(email.Sent, "Email should be sent on successful save");
                Assert.AreEqual("alice@company.com", email.To);
                StringAssert.Contains("Time logged successfully", email.Title);
                StringAssert.Contains("2 hours and 30 minutes", email.Body);
                Assert.IsNull(error.LoggedException);
            }

            [Test]
            public void LogTime_SaveFails_ErrorLoggedAndExceptionThrown()
            {
                var task = new FakeTask { SaveResult = false };
                var email = new FakeEmailSender();
                var error = new FakeErrorLogger();
                var user = new FakeUserLogger();
                var tm = new FakeTaskManager();

                var logger = new TimeLogger(task, email, error, user, tm);

                var ex = Assert.Throws<Exception>(() => logger.LogTime(1, 0, "desc"));
                Assert.AreEqual("Failed to save data to database", ex.Message);
                Assert.IsNotNull(error.LoggedException);
                Assert.AreEqual("Failed to save data to database", error.LoggedException.Message);
                Assert.IsFalse(email.Sent, "Email should not be sent when save fails");
            }

            [Test]
            public void LogTime_GetUserEmailFails_ErrorLoggedAndRethrown()
            {
                var task = new FakeTask { SaveResult = true };
                var email = new FakeEmailSender();
                var error = new FakeErrorLogger();
                var user = new FakeUserLoggerThrowsOnEmail();
                var tm = new FakeTaskManager();

                var logger = new TimeLogger(task, email, error, user, tm);

                var ex = Assert.Throws<Exception>(() => logger.LogTime(3, 15, "work"));
                Assert.AreEqual("Failed to get user email", ex.Message);

                Assert.IsNotNull(error.LoggedException);
                Assert.AreEqual("Failed to get user email", error.LoggedException.Message);
                Assert.IsFalse(email.Sent, "Email should not be sent when getting user email fails");
            }

            [Test]
            public void LogTime_GetTaskFails_ErrorLoggedAndRethrown()
            {
                var task = new FakeTask { SaveResult = true };
                var email = new FakeEmailSender();
                var error = new FakeErrorLogger();
                var user = new FakeUserLogger { UserName = "sam", Email = "sam@org.com" };

                var tm = new FakeTaskManagerThrows();

                var logger = new TimeLogger(task, email, error, user, tm);

                var ex = Assert.Throws<Exception>(() => logger.LogTime(4, 0, "desc"));
                Assert.AreEqual("Failed to get the task info", ex.Message);
                Assert.IsNotNull(error.LoggedException);
                Assert.AreEqual("Failed to get the task info", error.LoggedException.Message);
            }

            class FakeTaskManagerThrows : ITaskManager
            {
                public int GetTaskId(string loggedUserName, string loggedUserEmail)
                {
                    throw new Exception("Failed to get the task info");
                }
            }
        }
    }


