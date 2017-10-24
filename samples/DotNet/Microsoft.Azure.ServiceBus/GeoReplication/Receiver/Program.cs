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
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Microsoft.Azure.ServiceBus;
    using Microsoft.Azure.ServiceBus.Core;

    public class Program : IConnectionStringSample
    {
        public async Task Run(string connectionString)
        {

            try
            {
                // Create a primary and secondary queue client.
                var primaryQueueClient = new QueueClient(connectionString, Sample.BasicQueueName);
                var secondaryQueueClient = new QueueClient(connectionString, Sample.BasicQueue2Name);


                this.RegisterMessageHandler(
                    primaryQueueClient,
                    secondaryQueueClient,
                    async m => { await Console.Out.WriteLineAsync(m.MessageId); });


                Console.WriteLine("Waiting for messages, press ENTER to exit.\n");
                Console.ReadLine();
            }
            catch (Exception e)
            {
                Console.WriteLine("Unexpected exception {0}", e);
                throw;
            }
        }

        void RegisterMessageHandler(
            QueueClient primaryQueueClient,
            QueueClient secondaryQueueClient,
            Func<Message, Task> handlerCallback,
            int maxDeduplicationListLength = 256)
        {
            var receivedMessageList = new List<string>();
            var receivedMessageListLock = new object();

            Func<QueueClient, int, Func<Message, Task>, Message, Task> callback = async (qc, maxCount, fwd, message) =>
            {
                // Detect if a message with an identical ID has been received through the other queue.
                bool duplicate;
                lock (receivedMessageListLock)
                {
                    duplicate = receivedMessageList.Remove(message.MessageId);
                    if (!duplicate)
                    {
                        receivedMessageList.Add(message.MessageId);
                        if (receivedMessageList.Count > maxCount)
                        {
                            receivedMessageList.RemoveAt(0);
                        }
                    }
                }
                if (!duplicate)
                {
                    await fwd(message);
                }
            };

            primaryQueueClient.RegisterMessageHandler((msg, ct) => callback(primaryQueueClient, maxDeduplicationListLength, handlerCallback, msg),
                new MessageHandlerOptions((e) => LogMessageHandlerException(e)) { AutoComplete = true, MaxConcurrentCalls = 1 });
            secondaryQueueClient.RegisterMessageHandler((msg, ct) => callback(secondaryQueueClient, maxDeduplicationListLength, handlerCallback, msg),
                new MessageHandlerOptions((e) => LogMessageHandlerException(e)) { AutoComplete = true, MaxConcurrentCalls = 1 });
        }



        private Task LogMessageHandlerException(ExceptionReceivedEventArgs e)
        {
            Console.WriteLine("Exception: \"{0}\" {0}", e.Exception.Message, e.ExceptionReceivedContext.EntityPath);
            return Task.CompletedTask;
        }
    }
}