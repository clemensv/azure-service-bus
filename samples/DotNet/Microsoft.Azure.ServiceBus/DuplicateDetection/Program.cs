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

namespace DuplicateDetection
{
    using Microsoft.Azure.ServiceBus;
    using Microsoft.Azure.ServiceBus.Core;
    using System;
    using System.Threading.Tasks;

    public class Program : MessagingSamples.Sample
    {
        public async Task Run(string connectionString)
        {
            await Send(connectionString);
            await Receive(connectionString);
        }

        static async Task Send(string connectionString)
        {
            // Create communication objects to send and receive on the queue
            var sender = new MessageSender(connectionString, DupdetectQueueName);


            string messageId = Guid.NewGuid().ToString();
            // Send messages to queue
            Console.WriteLine("\tSending messages to {0} ...", sender.Path);
            var message = new Message
            {
                MessageId = messageId,
                TimeToLive = TimeSpan.FromMinutes(1)
            };
            await sender.SendAsync(message);
            Console.WriteLine("\t=> Sent a message with messageId {0}", message.MessageId);

            var message2 = new Message
            {
                MessageId = messageId,
                TimeToLive = TimeSpan.FromMinutes(1)
            };
            await sender.SendAsync(message2);
            Console.WriteLine("\t=> Sent a duplicate message with messageId {0}", message.MessageId);
            await sender.CloseAsync();
        }
        static async Task Receive(string connectionString)
        {
            var receiver = new MessageReceiver(connectionString, DupdetectQueueName, ReceiveMode.PeekLock);

            // Receive messages from queue
            var receivedMessageId = "";

            Console.WriteLine("\n\tWaiting up to 5 seconds for messages from {0} ...", receiver.Path);
            while (true)
            {
                var receivedMessage = await receiver.ReceiveAsync(TimeSpan.FromSeconds(5));

                if (receivedMessage == null)
                {
                    break;
                }
                Console.WriteLine("\t<= Received a message with messageId {0}", receivedMessage.MessageId);
                await receiver.CompleteAsync(receivedMessage.SystemProperties.LockToken);
                if (receivedMessageId.Equals(receivedMessage.MessageId, StringComparison.OrdinalIgnoreCase))
                {
                    throw new Exception("Received a duplicate messsage");
                }
                receivedMessageId = receivedMessage.MessageId;
            }
            Console.WriteLine("\tDone receiving messages from {0}", receiver.Path);

            await receiver.CloseAsync();
        }
       public static int Main(string[] args)
        {
            try
            {
                var app = new Program();
                app.RunSample(args, app.Run);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                return 1;
            }
            return 0;
        }
    }
}