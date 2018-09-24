using System;
using System.Configuration;
using AzurePlayground.Model;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace AzurePlayground.EmailPusher
{
    public static class DequeueEmployees
    {
        [FunctionName("DequeueEmployees")]
        public static void Run([QueueTrigger("employee-emails", Connection = "EmployeeQueue")]string message, TraceWriter log)
        {
            var apiKey = ConfigurationManager.AppSettings.Get("SendGridApiKey");

            if(string.IsNullOrWhiteSpace(apiKey))
            {
                log.Error($"API key was not set");
                return;
            }

            if(!Int32.TryParse(message, out var employeeId))
            {
                log.Error($"Queue item {message} is not an integer");
                return;
            }

            Employee employee;
            using (NorthwindContext northwindContext = new NorthwindContext())
            {
                employee = northwindContext.Employees.Find(employeeId);
            }

            if(employee == null)
            {
                log.Error($"Could not find employee with id {employeeId}");
                return;
            }

            var client = new SendGridClient(apiKey);

            var msg = new SendGridMessage()
            {
                From = new EmailAddress("testsender@azuretraining.westfaliausa.com", "Test Sender"),
                Subject = "Employee Notification",
                PlainTextContent = $"Someone has notified you about employee, {employee.FirstName} {employee.LastName}."
            };

            msg.AddTo(new EmailAddress("jpavoncello@westfaliausa.com", "Josh P."));

            var task = client.SendEmailAsync(msg);

            task.Wait();

            if(task.Exception != null)
            {
                log.Error($"An exception was thrown while sending the email: {task.Exception.Message}");
                return;
            }

            var response = task.Result;

            if (response.StatusCode != System.Net.HttpStatusCode.OK)
            {
                log.Error($"An error occurred while sending the email, status code = {response.StatusCode}");
                log.Info($"Response body:{System.Environment.NewLine}{response.Body}");
                return;
            }

            log.Info($"C# Queue trigger function processed successfully");
        }
    }
}
