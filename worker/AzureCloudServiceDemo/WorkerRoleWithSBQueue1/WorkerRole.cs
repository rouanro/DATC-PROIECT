﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.ServiceRuntime;

namespace WorkerRoleWithSBQueue1
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
            Trace.WriteLine("Starting processing of messages");

            // Initiates the message pump and callback is invoked for each message that is received, calling close on the client will stop the pump.
            Client.OnMessage((receivedMessage) =>
                {
                    try
                    {
                        // Process the message
                        string[] info = { "", "" };
                        int i = 0;
                        Stream receivedMessageStream = receivedMessage.GetBody<Stream>();
                        StreamReader reader = new StreamReader(receivedMessageStream);
                        string receivedMessageString = reader.ReadToEnd();
                        string[] split = receivedMessageString.Split(new Char[] { ' ', ',', '.', ':', '\t' });
                        string text = System.IO.File.ReadAllText(@"C:\Users\Maghis\Desktop\Facultate\date.txt");
                        
                        foreach (string s in split)
                        {
                            if (s.Trim() != "")
                            {
                                info[i] = s;
                                i++;
                            }
                        }
                        text = text + "\r\ntemp: " + info[0];
                        text = text + " umid: " + info[1];
                        System.IO.File.WriteAllText(@"C:\Users\Maghis\Desktop\Facultate\date.txt", text);
                        Console.WriteLine("Received message: " + receivedMessage.ToString());
                        Trace.WriteLine("Processing Service Bus message: " + receivedMessage.SequenceNumber.ToString());
                    }
                    catch
                    {
                        // Handle any message processing specific exceptions here
                    }
                });

            CompletedEvent.WaitOne();
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
