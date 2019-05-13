﻿using System;
using System.Collections.Generic;
using Sandbox.ModAPI;
using System.Text.RegularExpressions;
using DarkHelmet.Game;

namespace DarkHelmet.UI
{
    /// <summary>
    /// Stores chat command name and action
    /// </summary>
    public class Command
    {
        public readonly string cmdName;
        public readonly Func<string[], bool> action;
        public readonly bool takesArgs;

        public Command(string cmdName, Func<string[], bool> argAction)
        {
            this.cmdName = cmdName.ToLower();
            action = argAction;
            takesArgs = true;
        }

        public Command(string cmdName, Action action)
        {
            this.cmdName = cmdName.ToLower();
            this.action = (string[] args) => { action(); return true; };
            takesArgs = false;
        }
    }

    /// <summary>
    /// Manages chat commands; singleton
    /// </summary>
    internal sealed class CmdManager : ModBase.Component<CmdManager>
    {
        public static string Prefix { get { return prefix; } set { if (value != null && value.Length > 0) prefix = value; } }

        private static string prefix = "/cmd";
        private static readonly Regex cmdParser;
        private static readonly List<Command> commands;

        static CmdManager()
        {
            commands = new List<Command>();
            cmdParser = new Regex(@"((\s*?[\s,;|]\s*?)(\w+))+");
        }

        /// <summary>
        /// Instantiates commands and regex
        /// </summary>
        public CmdManager()
        {
            MyAPIGateway.Utilities.MessageEntered += MessageHandler;
        }

        protected override void BeforeClose()
        {
            MyAPIGateway.Utilities.MessageEntered -= MessageHandler;
        }

        public static void AddCommands(IEnumerable<Command> newCommands)
        {
            commands.AddRange(newCommands);
        }

        /// <summary>
        /// Recieves chat commands and attempts to execute them.
        /// </summary>
        private void MessageHandler(string message, ref bool sendToOthers)
        {
            string cmdName;
            string[] matches;
            bool cmdFound = false;
            message = message.ToLower();

            if (message.StartsWith(Prefix))
            {
                sendToOthers = false;

                if (TryParseCommand(message, out matches))
                {
                    cmdName = matches[0];
                    
                    foreach (Command cmd in commands)
                        if (cmd.cmdName == cmdName)
                        {
                            cmdFound = true;

                            if (cmd.takesArgs)
                            {
                                if (matches.Length > 1)
                                    cmd.action(Utilities.GetSubarray(matches, 1));
                                else
                                    ModBase.SendChatMessage("Invalid Command. This command requires an argument.");
                            }
                            else
                                cmd.action(null);

                            break;
                        }
                }
                
                if (!cmdFound)
                    ModBase.SendChatMessage("Command not recognised.");
            }
        }

        /// <summary>
        /// Parses list of arguments and their associated command name.
        /// </summary>
        public bool TryParseCommand(string cmd, out string[] matches)
        {
            Match match = cmdParser.Match(cmd);
            CaptureCollection captures = match.Groups[3].Captures;
            matches = new string[captures.Count];

            for (int n = 0; n < captures.Count; n++)
                matches[n] = captures[n].Value;

            return matches.Length > 0; 
        }       
    }
}