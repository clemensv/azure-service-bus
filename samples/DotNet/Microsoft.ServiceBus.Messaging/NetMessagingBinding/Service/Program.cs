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
    using System.ServiceModel;
    using System.Threading.Tasks;
    using Microsoft.ServiceBus;

    public class Program : Sample
    {
        public async Task Run(string connectionString)
        {
            try
            {
                var sbb = new ServiceBusConnectionStringBuilder(connectionString);

                Console.Title = "Service";
                Console.WriteLine("Ready to receive messages from {0}...", BasicQueueName);

                // Creating the service host object as defined in config
                using (var serviceHost = new ServiceHost(typeof (OnewayService), new Uri(sbb.GetAbsoluteRuntimeEndpoints()[0], BasicQueueName)))
                {
                    var authBehavior = new TransportClientEndpointBehavior(TokenProvider.CreateSharedAccessSignatureTokenProvider(sbb.SharedAccessKeyName, sbb.SharedAccessKey));
                    foreach (var ep in serviceHost.Description.Endpoints)
                    {
                        ep.EndpointBehaviors.Add(authBehavior);
                    }

                    // Subscribe to the faulted event.
                    serviceHost.Faulted += serviceHost_Faulted;

                    // Start service
                    serviceHost.Open();

                    Console.WriteLine("\nPress [Enter] to Close the ServiceHost.");
                    Console.ReadLine();

                    // Close the service
                    serviceHost.Close();
                }
            }
            catch (Exception exception)
            {
                Console.WriteLine("Exception occurred: {0}", exception);
                
                Console.WriteLine("\nPress [Enter] to exit.");
                Console.ReadLine();
            }
        }

        static void serviceHost_Faulted(object sender, EventArgs e)
        {
            Console.WriteLine("Fault occured. Aborting the service host object ...");
            ((ServiceHost) sender).Abort();
        }

        static void Main(string[] args)
        {
            var app = new Program();
            app.RunSample(args, app.Run);
        }
    }
}