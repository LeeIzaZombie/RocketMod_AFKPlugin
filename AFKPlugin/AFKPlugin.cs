using Rocket.API;
using Rocket.RocketAPI.Events;
using Rocket.Unturned;
using Rocket.Unturned.Commands;
using Rocket.Unturned.Player;
using Rocket.Unturned.Plugins;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace AFKPlugin
{
    public class AFKPlugin_ : RocketPlugin<AFKConfig>
    {

        public static readonly List<string> AFK = new List<string>();
        public static List<string> tempAFK = new List<string>();
        private static readonly Dictionary<string, Thread> check_threads = new Dictionary<string, Thread>();
        public static Dictionary<string, DateTime> last_moved = new Dictionary<string, DateTime>();
        private static List<string> playerList = new List<string>();

        protected override void Load()
        {
            Rocket.Unturned.Events.RocketPlayerEvents.OnPlayerUpdatePosition += onPlayerPosition;
            Rocket.Unturned.Events.RocketServerEvents.OnPlayerDisconnected += onPlayerDisconnect;
            Rocket.Unturned.Events.RocketServerEvents.OnPlayerConnected += onPlayerConnect;
            //Rocket.Unturned.Events.RocketPlayerEvents.OnPlayerUpdateFood += RocketPlayerEvents_OnPlayerUpdateFood;
            //Rocket.Unturned.Events.RocketPlayerEvents.OnPlayerUpdateWater += RocketPlayerEvents_OnPlayerUpdateWater;
            foreach (string player in playerList)
            {
                RocketPlayer p = RocketPlayer.FromName(player);
                if (!last_moved.ContainsKey(p.SteamName))
                    last_moved.Add(p.SteamName, DateTime.Now);
                else
                    last_moved[p.SteamName] = DateTime.Now;
                if (!check_threads.ContainsKey(p.SteamName))
                {
                    Thread t = new Thread(new ThreadStart(() =>
                    {
                        while (true)
                        {
                            Thread.Sleep(1000);
                            playerCheck(p);
                        }
                    }))
                    {
                        IsBackground = true
                    };
                    check_threads.Add(p.SteamName, t);
                    t.Start();
                }
            }
        }

       

        private void onPlayerConnect(RocketPlayer p)
        {
            playerList.Add(p.CharacterName);
            if (!last_moved.ContainsKey(p.SteamName))
                last_moved.Add(p.SteamName, DateTime.Now);
            else
                last_moved[p.SteamName] = DateTime.Now;
            if (!check_threads.ContainsKey(p.SteamName))
            {
                Thread t = new Thread(new ThreadStart(() =>
                {
                    while (true)
                    {
                        Thread.Sleep(1000);
                        playerCheck(p);
                    }
                }))
                {
                    IsBackground = true
                };
                check_threads.Add(p.SteamName, t);
                t.Start();
            }
        }

        private void onPlayerDisconnect(RocketPlayer p)
        {
            playerList.Remove(p.CharacterName);
            if (last_moved.ContainsKey(p.SteamName))
                last_moved.Remove(p.SteamName);
            if (check_threads.ContainsKey(p.SteamName))
            {
                check_threads[p.SteamName].Abort();
                check_threads.Remove(p.SteamName);
            }
            if (AFK.Contains(p.SteamName))
                AFK.Remove(p.SteamName);
            if (tempAFK.Contains(p.SteamName))
                tempAFK.Remove(p.SteamName);
        }

        private void onPlayerPosition(RocketPlayer p, Vector3 position)
        {
            try
            {
                if (p.Position != position)
                {
                    last_moved[p.SteamName] = DateTime.Now;
                    if (AFK.Contains(p.SteamName))
                    {
                        AFK.Remove(p.SteamName);
                        RocketChat.Say(p.CharacterName + " is no longer AFK.", Color.yellow);
                    }
                    else if (tempAFK.Contains(p.SteamName))
                    {
                        tempAFK.Remove(p.SteamName);
                        RocketChat.Say(p.CharacterName + " is no longer AFK.", Color.yellow);
                    }
                }
            }
            catch
            {
            }
        }

        private void playerCheck(RocketPlayer p)
        {

            try
            {
                if (DateTime.Now.Subtract(last_moved[p.SteamName]).TotalMinutes >= (this.Configuration.mins))
                {
                    if (!AFK.Contains(p.SteamName))
                    {
                        if (this.Configuration.kick)
                        {
                            RocketChat.Say(p.CharacterName + " is being kicked for being AFK for " + (this.Configuration.mins) + " mins.", Color.red);
                            p.Kick("Kicked for being AFK.");
                        }
                        else
                        {
                            AFK.Add(p.SteamName);
                            RocketChat.Say(p.CharacterName + " is now AFK: " + (this.Configuration.mins) + " mins.", Color.yellow);
                        }
                    }
                    else if (tempAFK.Contains(p.SteamName))
                    {
                        if (this.Configuration.kick)
                        {
                            RocketChat.Say(p.CharacterName + " is being kicked for being AFK for " + (this.Configuration.mins) + " mins.", Color.red);
                            p.Kick("Kicked for being AFK.");
                        }
                        else
                        {
                            RocketChat.Say(p.CharacterName + " is now AFK: " + (this.Configuration.mins) + " mins.", Color.red);
                        }
                    }
                }
            }
            catch
            {
            }
        }
    }

    public class CommandAFK : IRocketCommand
    {
        public bool RunFromConsole
        {
            get { return false; }
        }

        public string Name
        {
            get { return "afk"; }
        }

        public void Execute(RocketPlayer caller, string[] command)
        {
            if (AFKPlugin_.tempAFK.Contains(caller.SteamName))
            {
                RocketChat.Say(caller.CharacterName + " is no longer AFK.", Color.yellow);
                AFKPlugin_.tempAFK.Remove(caller.SteamName);
                AFKPlugin_.last_moved.Remove(caller.SteamName);
            }
            else
            {
                RocketChat.Say(caller.CharacterName + " is now AFK.", Color.yellow);
                AFKPlugin_.tempAFK.Add(caller.SteamName);
                AFKPlugin_.last_moved.Remove(caller.SteamName);
                AFKPlugin_.last_moved.Add(caller.SteamName, DateTime.Now);
            }
        }

        public string Help
        {
            get { return "/afk - Away From Keyboard, show everyone you'll be busy."; }
        }
    }

   
    public class AFKConfig : IRocketPluginConfiguration
    {
        public int mins;
        public bool kick;
        public IRocketPluginConfiguration DefaultConfiguration
        {
            get
            {
                return new AFKConfig()
                {
                    mins = 5,
                    kick = false
                };
            }
        }
    }
    
}
