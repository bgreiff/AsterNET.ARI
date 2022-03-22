/*
 * SimpleBridge AsterNET.ARI Bridge Sample
 * Copyright Ben Merrills (ben at mersontech co uk), all rights reserved.
 * https://asternetari.codeplex.com/
 * https://asternetari.codeplex.com/license
 * 
 * No Warranty. The Software is provided "as is" without warranty of any kind, either express or implied, 
 * including without limitation any implied warranties of condition, uninterrupted use, merchantability, 
 * fitness for a particular purpose, or non-infringement.
 * 
 * Extensions.conf exmaple setup
 *   exten => 7002,1,Noop()
 *   same => n,Stasis(bridge_test)
 *   same => n,hangup()
 *
 */

using AsterNET.ARI.Models;
using System;

namespace AsterNET.ARI.SimpleBridge
{
    class Program
    {
        public static AriClient ActionClient;
        public static Bridge SimpleBridge;
        //private const string AsterIP = "10.255.173.58";


        private const string AppName = "simpleconf";
        private const string AsterIP = "192.168.155.242";        
        private const int AsterPort = 8088;
        private const string ARIUser = "ari_user";
        private const string ARIPassword = "bBSrUuwwLTVBMC1OiH";

        static void Main(string[] args)
        {
            try
            {
                // Create a message actionClient to receive events on
                StasisEndpoint ep = new StasisEndpoint(AsterIP, AsterPort, ARIUser, ARIPassword);
                Console.WriteLine("Endpoint {0}", ep.AriEndPoint);
                ActionClient = new AriClient(ep, AppName);

                ActionClient.OnStasisStartEvent += c_OnStasisStartEvent;
                ActionClient.OnStasisEndEvent += c_OnStasisEndEvent;

                ActionClient.Connect();

                // Create simple bridge
                SimpleBridge = ActionClient.Bridges.Create("mixing", Guid.NewGuid().ToString(), AppName);

                // subscribe to bridge events
                ActionClient.Applications.Subscribe(AppName, "bridge:" + SimpleBridge.Id);
                Console.WriteLine("Bridge {0}", SimpleBridge.Id);

                // start MOH on bridge
                ActionClient.Bridges.StartMoh(SimpleBridge.Id, "default");

                var done = false;
                while (!done)
                {
                    var lastKey = Console.ReadKey();
                    switch(lastKey.KeyChar.ToString())
                    {
                        case "*":
                            Console.WriteLine("* Pressed");
                            done = true;
                            break;
                        case "1":
                            Console.WriteLine("1 Pressed");
                            ActionClient.Bridges.StopMoh(SimpleBridge.Id);
                            break;
                        case "2":
                            Console.WriteLine("2 Pressed");
                            ActionClient.Bridges.StartMoh(SimpleBridge.Id, "default");
                            break;
                        case "3":
                            // Mute all channels on bridge
                            Console.WriteLine("3 Pressed");
                            var bridgeMute = ActionClient.Bridges.Get(SimpleBridge.Id);
                            foreach (var chan in bridgeMute.Channels)
                                ActionClient.Channels.Mute(chan, "in");
                            break;
                        case "4":
                            // Unmute all channels on bridge
                            Console.WriteLine("4 Pressed");
                            var bridgeUnmute = ActionClient.Bridges.Get(SimpleBridge.Id);
                            foreach (var chan in bridgeUnmute.Channels)
                                ActionClient.Channels.Unmute(chan, "in");
                            break;
                    }
                }

                ActionClient.Bridges.Destroy(SimpleBridge.Id);
                ActionClient.Disconnect();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                Console.ReadKey();
            }
        }

        static void c_OnStasisEndEvent(object sender, AsterNET.ARI.Models.StasisEndEvent e)
        {
            Console.WriteLine("End Event");
            // remove from bridge
            ActionClient.Bridges.RemoveChannel(SimpleBridge.Id, e.Channel.Id);

            // hangup
            ActionClient.Channels.Hangup(e.Channel.Id, "normal");
        }

        static void c_OnStasisStartEvent(object sender, AsterNET.ARI.Models.StasisStartEvent e)
        {
            Console.WriteLine("Start Event");
            // answer channel
            ActionClient.Channels.Answer(e.Channel.Id);

            // add to bridge
            ActionClient.Bridges.AddChannel(SimpleBridge.Id, e.Channel.Id, "member");
        }
    }
}
