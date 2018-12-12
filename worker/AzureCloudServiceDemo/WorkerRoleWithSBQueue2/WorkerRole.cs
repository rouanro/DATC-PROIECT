using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.ServiceBus;

namespace WorkerRoleWithSBQueue2
{
    public class WorkerRole : RoleEntryPoint
    {
        // The name of your queue  
        const string QueueName = "ProcessingQueue";

        // QueueClient is thread-safe. Recommended that you cache   
        // rather than recreating it on every request  
        QueueClient Client;
        ManualResetEvent CompletedEvent = new ManualResetEvent(false);

        public override void Run()
        {

            string connectionString = CloudConfigurationManager.GetSetting("Microsoft.ServiceBus.ConnectionString");
            var namespaceManager = NamespaceManager.CreateFromConnectionString(connectionString);
            Client = QueueClient.CreateFromConnectionString(connectionString, QueueName);
            BrokeredMessage message = Client.Receive();
            string body = message.GetBody<string>();

            using (MailMessage mm = new MailMessage("beniaminmaghis@gmail.com", "benimagh@yahoo.com"))
            {
                mm.Subject = "Testing Worker Role";
                mm.Body = body;
                mm.IsBodyHtml = false;
                SmtpClient smtp = new SmtpClient();
                smtp.Host = "smtp.gmail.com";
                smtp.EnableSsl = true;
                NetworkCredential NetworkCred = new NetworkCredential("beniaminmaghis@gmail.com", "politm12");
                smtp.UseDefaultCredentials = true;
                smtp.Credentials = NetworkCred;
                smtp.Port = 587;
                smtp.Send(mm);
            }
            message.Complete();
            message.Abandon();

        }

        public override bool OnStart()
        {
            // Set the maximum number of concurrent connections   
            ServicePointManager.DefaultConnectionLimit = 12;

            // Create the queue if it does not exist already  

            string connectionString = CloudConfigurationManager.GetSetting("Microsoft.ServiceBus.ConnectionString");
            var namespaceManager = NamespaceManager.CreateFromConnectionString(connectionString);
            if (!namespaceManager.QueueExists(QueueName))
            {
                namespaceManager.CreateQueue(QueueName);
            }

            // Initialize the connection to Service Bus Queue  
            Client = QueueClient.CreateFromConnectionString(connectionString, QueueName);
            BrokeredMessage message = new BrokeredMessage("Am from Azure Service bus Queue as a worker roles");
            Client.SendAsync(message);
            return base.OnStart();
        }

        public override void OnStop()
        {
            // Close the connection to Service Bus Queue  
            Client.Close();
            CompletedEvent.Set();
            base.OnStop();
        }
    }
}
