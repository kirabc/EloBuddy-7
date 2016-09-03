using System;
using System.Collections.Generic;
using System.Linq;
using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Events;
using EloBuddy.SDK.Menu;
using EloBuddy.SDK.Menu.Values;
using EloBuddy.SDK.Rendering;

namespace ToxicBuddy
{
    class Program : Words
    {
        private static void Main(string[] args) { Loading.OnLoadingComplete += OnLoad; }
        public static AIHeroClient myhero { get { return ObjectManager.Player; } }
        private static Menu menu;
        private static bool MutedTeam = false, Disabled = false;
        static Dictionary<string, int> TeamToxicCount = new Dictionary<string, int>();

        public static void OnLoad(EventArgs args)
        {
            Chat.Print("<font color='#90D4BB'>Toxic</font><font color='#F00E59'>Buddy</font> : Loaded!");
            Chat.Print("<font color='#7752FF'>By </font><font color='#0FA348'>Toyota</font><font color='#7752FF'>7</font><font color='#FF0000'> <3 </font>");

            Menu();

            Chat.OnInput += OnInput;
            Chat.OnMessage += OnMessage;
            Game.OnTick += OnTick;

            foreach (var mate in EntityManager.Heroes.Allies)
            {
                TeamToxicCount.Add(mate.ChampionName, 0);
            }
        }

        private static void OnTick(EventArgs args)
        {        
            foreach (AIHeroClient ally in EntityManager.Heroes.Allies)
            {
                if (TeamToxicCount[ally.ChampionName] >= 10) Chat.Say("/mute " + ally.Name);               
            }

            if (menu["MUTE"].Cast<KeyBind>().CurrentValue) MuteAll();

            if (menu["DISABLE"].Cast<CheckBox>().CurrentValue) Disabled = true;
        }

        private static void OnInput(ChatInputEventArgs args)
        {
            if (Disabled == true)
            {
                Chat.Print("Your Chat Is Permanently Disabled!");
                args.Process = false;
                return;
            }

            var msg = args.Input;

            if (WordList.Any(x => msg.ToLower().Contains(x)))
            {
                args.Process = false;
                Chat.Print("Being Toxic Wont Help Your Team!");
            }

            if (msg.Contains(".block "))
            {
                args.Process = false;

                AddCommand(msg);
            }
        }

        private static void MuteAll()
        {
            if (MutedTeam)
            {
                Chat.Print("Teammates Where Already Muted!");
                return;
            }
            else
            {
                foreach (var ally in EntityManager.Heroes.Allies)
                {
                    Chat.Say("/mute " + ally.Name);
                }
                MutedTeam = true;
                menu["MUTE"].Cast<KeyBind>().CurrentValue = false;
            }                       
        }

        private static void AddCommand(string msg)
        {
            bool IsCommand = false;

            char[] command = { '.', 'b', 'l', 'o', 'c', 'k', ' ' };

            for (int i = 0; i < 6; i++)
            {
                if (msg.ElementAt(i) == command[i])
                {
                    IsCommand = true;
                    continue;
                }
                else
                {
                    IsCommand = false;
                    break;
                }
            }

            if (!IsCommand) return;

            if (!WordList.Contains(msg.Remove(0, 7)))
            {
                WordList.Add(msg.Remove(0, 7));
                Core.DelayAction(delegate
                {
                    if (WordList.Contains(msg.Remove(0, 7)))
                    {
                        Chat.Print("Successfuly Added Word To The List!");                       
                    }
                    else
                    {
                        Chat.Print("Failed To Add New Word To The List!");                        
                    }
                }, 50);
                return;
            }
            else
            {
                Chat.Print("This Word Is Already In The List!");
            }

            return;
        }

        private static void OnMessage(Obj_AI_Base sender, ChatMessageEventArgs args)
        {
            if (!menu["BLOCKTEAM"].Cast<CheckBox>().CurrentValue) return;

            var ally = sender as AIHeroClient;

            if (!sender.IsMe && sender.IsAlly && WordList.Any(x => args.Message.ToLower().Contains(x)))
            {
                TeamToxicCount[ally.ChampionName]++;

                if (TeamToxicCount[ally.ChampionName] == 9) Chat.Print(ally.ChampionName + " Will Get Muted If He Says Another Bad Word!");
            }
        }
    

        private static void Menu()
        {
            menu = MainMenu.AddMenu("ToxicBuddy", "toxicmenu");
            menu.AddGroupLabel("Time For You To Become A Better Person :3");
            menu.AddSeparator();
            menu.Add("BLOCKTEAM", new CheckBox("Block Toxic Teammates"));
            menu.AddSeparator();
            menu.Add("MUTE", new KeyBind("Mute Teammates Forever!!!!11",false, KeyBind.BindTypes.PressToggle, 'M'));
            menu.AddLabel("Can Only Be Used Once ^^^");
            menu.AddSeparator();
            menu.Add("DISABLE", new CheckBox("Permanently Disable Chat",false));
            menu.AddLabel("WARNING! CANNOT BE UNDONE ^^^");
        }
    }
}
