﻿//   
//   Copyright © Microsoft Corporation, All Rights Reserved
// 
//   Licensed under the Apache License, Version 2.0 (the "License"); 
//   you may not use this file except in compliance with the License. 
//   You may obtain a copy of the License at
// 
//   http://www.apache.org/licenses/LICENSE-2.0 
// 
//   THIS CODE IS PROVIDED *AS IS* BASIS, WITHOUT WARRANTIES OR CONDITIONS
//   OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING WITHOUT LIMITATION
//   ANY IMPLIED WARRANTIES OR CONDITIONS OF TITLE, FITNESS FOR A
//   PARTICULAR PURPOSE, MERCHANTABILITY OR NON-INFRINGEMENT.
// 
//   See the Apache License, Version 2.0 for the specific language
//   governing permissions and limitations under the License. 

namespace MessagingSamples
{
    using System;
    using System.Threading.Tasks;
    using Microsoft.Azure.ServiceBus;
    using Microsoft.Azure.ServiceBus.Core;
    using System.Text;

    public class Program : IConnectionStringSample
    {
        public async Task Run(string connectionString)
        {
            try
            {
                // Create a primary and secondary queue client.
                var primaryQueueClient = new QueueClient(connectionString, Sample.BasicQueueName);
                var secondaryQueueClient = new QueueClient(connectionString, Sample.BasicQueue2Name);
                Console.WriteLine("\nSending messages to primary and secondary queues...\n");

                for (var i = 1; i <= 5; i++)
                {
                    // Create brokered message.
                    var m1 = new Message(Encoding.UTF8.GetBytes("Message" + i))
                    {
                        MessageId = i.ToString(),
                        TimeToLive = TimeSpan.FromMinutes(2.0)
                    };

                    // Clone message so we can send clone to secondary in case sending to the primary fails.
                    var m2 = m1.Clone();

                    var exceptionCount = 0;

                    // send messages
                    var t1 = primaryQueueClient.SendAsync(m1);
                    var t2 = secondaryQueueClient.SendAsync(m2);

                    // collect result for primary queue
                    try
                    {
                        await t1;
                        Console.WriteLine("Message {0} sent to primary queue: Body = {1}", m1.MessageId, Encoding.UTF8.GetString(m1.Body));
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Unable to send message {0} to primary queue: Exception {1}", m1.MessageId, e);
                        exceptionCount++;
                    }

                    // collect result for secondary queue
                    try
                    {
                        await t2;
                        Console.WriteLine("Message {0} sent to secondary queue: Body = {1}", m2.MessageId, Encoding.UTF8.GetString(m2.Body));
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Unable to send message {0} to secondary queue: Exception {0}", m2.MessageId, e);
                        exceptionCount++;
                    }

                    // Throw exception if send operation on both queues failed.
                    if (exceptionCount > 1)
                    {
                        throw new Exception("Send Failure");
                    }
                }

                Console.WriteLine("\nAfter running the entire sample, press ENTER to clean up and exit.");
                Console.ReadLine();
            }
            catch (Exception e)
            {
                Console.WriteLine("Unexpected exception {0}", e);
                throw e;
            }
        }
    }
}