/* CRoundStarter.cs

Free to use as is in any way you want with no warranty.

Coded by xfileFIN

Some helpers are taken from Insane Limits plugin.

*/

using System;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Threading;

using PRoCon.Core;
using PRoCon.Core.Plugin;
using PRoCon.Core.Players;

namespace PRoConEvents
{
    public class CRoundStarter : PRoConPluginAPI, IPRoConPluginInterface
    {
        #region Enums
        public enum ChatRange
        {
            GLOBAL,
            TEAM,
            SQUAD
        }
        public enum CommandPrefixes
        {
            GLOBAL,
            PRIVATE,
            ADMIN,
            NONE
        } 
        #endregion

        #region Variables
        private bool fIsEnabled;
        private int fDebugLevel;

        private bool _debugMode;
        private bool _autoStartAfterDelay;
        private bool _autoStart;
        private int _autoStartDelay;
        private string _cancelCommand;

        private List<CPlayerInfo> _currentPlayers;

        private string server_host { get; set; }
        private string server_port { get; set; }
        #endregion

        #region Constructor
        public CRoundStarter() {
            this.fIsEnabled = false;
            this.fDebugLevel = 2;
            this._debugMode = false;

            this._autoStartAfterDelay = false;
            this._autoStart = false;
            this._autoStartDelay = 20;
            this._cancelCommand = "cancelRS";
            this._currentPlayers = new List<CPlayerInfo>();
        } 
        #endregion

        #region Base Helpers
        public enum MessageType { Warning, Error, Exception, Normal };

        public String FormatMessage(String msg, MessageType type) {
            String prefix = "[^bRound Starter^n] ";

            if (type.Equals(MessageType.Warning))
                prefix += "^1^bWARNING^0^n: ";
            else if (type.Equals(MessageType.Error))
                prefix += "^1^bERROR^0^n: ";
            else if (type.Equals(MessageType.Exception))
                prefix += "^1^bEXCEPTION^0^n: ";

            return prefix + msg;
        }

        public void LogWrite(String msg) {
            this.ExecuteCommand("procon.protected.pluginconsole.write", msg);
        }

        public void ConsoleWrite(string msg, MessageType type) {
            LogWrite(FormatMessage(msg, type));
        }
        public void ConsoleWrite(string msg) {
            ConsoleWrite(msg, MessageType.Normal);
        }
        public void ConsoleWarn(String msg) {
            ConsoleWrite(msg, MessageType.Warning);
        }
        public void ConsoleError(String msg) {
            ConsoleWrite(msg, MessageType.Error);
        }
        public void ConsoleException(String msg) {
            ConsoleWrite(msg, MessageType.Exception);
        }

        public void DebugWrite(string msg, int level) {
            if (fDebugLevel >= level) ConsoleWrite(msg, MessageType.Normal);
        }

        public void ServerCommand(params String[] args) {
            List<string> list = new List<string>();
            list.Add("procon.protected.send");
            list.AddRange(args);
            this.ExecuteCommand(list.ToArray());
        }
        #endregion

        #region Plugin Information
        public string GetPluginName() {
            return "Round Starter";
        }
        public string GetPluginVersion() {
            return "0.0.0.2";
        }
        public string GetPluginAuthor() {
            return "xfileFIN";
        }
        public string GetPluginWebsite() {
            return "github.com/Razer2015";
        }
        public string GetPluginDescription() {
            return @"
<h2>Description</h2>
<p>Starts the next round in a given timespan!</p>

<h2>Commands</h2>
<p>@nextround switches to nextmap within 5 seconds. Following prefixes will work [!@#/]</p>

<h2>Settings</h2>
<h3>Global Settings</h3>
<p><b>Automatically early start next round</b> - Whether the automatic round start is on/off!</p>
<h3>Automatic Round Start</h3>
<p><b>Wait Time [Seconds]</b> - How many seconds to wait until automatically change!</p>
<p><b>Cancel Command</b> - Command to cancel automatic round start (Anyone can issue this)!</p>
<h3>Debug</h3>
<p><b>Debug mode</b> - If set to true, commands are not issued and are only displayed in the debug console.</p>

<h2>Development</h2>
<p>Developed by xfileFIN</p>

<h3>Changelog</h3>
<blockquote><h4>0.0.0.2 (02.04.2017)</h4>
	- Cleaned up the code ALOT<br/>
	- Improved performance<br/>
	- Changed some events since they were never fired in BF4<br/>
</blockquote>

<blockquote><h4>0.0.0.1 (04.03.2016)</h4>
	- initial version<br/>
</blockquote>
";
        }
        #endregion

        #region Plugin Variables
        public List<CPluginVariable> GetDisplayPluginVariables() {

            List<CPluginVariable> lstReturn = new List<CPluginVariable>();

            lstReturn.Add(new CPluginVariable("Debug|Debug level", this.fDebugLevel.GetType(), this.fDebugLevel));
            lstReturn.Add(new CPluginVariable("Debug|Debug mode", this._debugMode.GetType(), this._debugMode));
            lstReturn.Add(new CPluginVariable("1.1 Global Settings|Automatically early start next round", this._autoStart.GetType(), this._autoStart));
            lstReturn.Add(new CPluginVariable("1.2 Automatic Round Start|Wait Time [Seconds]", this._autoStartDelay.GetType(), this._autoStartDelay));
            lstReturn.Add(new CPluginVariable("1.2 Automatic Round Start|Cancel Command", this._cancelCommand.GetType(), this._cancelCommand));

            return lstReturn;
        }

        public List<CPluginVariable> GetPluginVariables() {
            return GetDisplayPluginVariables();
        }

        public void SetPluginVariable(string strVariable, string strValue) {
            if (Regex.Match(strVariable, @"Debug level").Success) {
                int tmp = 2;
                int.TryParse(strValue, out tmp);
                this.fDebugLevel = tmp;
            }
            else if (Regex.Match(strVariable, @"Debug mode").Success) {
                bool tmp = false;
                bool.TryParse(strValue, out tmp);
                this._debugMode = tmp;
            }
            else if (Regex.Match(strVariable, @"Automatically early start next round").Success) {
                bool tmp = false;
                bool.TryParse(strValue, out tmp);
                this._autoStart = tmp;
            }
            else if (strVariable.EndsWith("Wait Time [Seconds]")) {
                int tmp = 20;
                int.TryParse(strValue, out tmp);
                this._autoStartDelay = tmp;
            }
            else if (Regex.Match(strVariable, @"Cancel Command").Success) {
                this._cancelCommand = strValue;
            }
        }
        #endregion

        #region Plugin Loading
        public void OnPluginLoaded(string strHostName, string strPort, string strPRoConVersion) {
            server_host = strHostName;
            server_port = strPort;
            this.RegisterEvents(this.GetType().Name, "OnListPlayers","OnPlayerLeft","OnGlobalChat","OnTeamChat","OnSquadChat","OnRoundOverPlayers","OnRoundOverTeamScores","OnLevelLoaded");
        }
        #endregion

        #region Plugin Enable/Disable
        public void OnPluginEnable() {
            fIsEnabled = true;
            ConsoleWrite("^2Enabled!");
            ServerCommand("listPlayers", "all");
        }

        public void OnPluginDisable() {
            fIsEnabled = false;
            this._currentPlayers = null;
            ConsoleWrite("^1Disabled =(");
        }
        #endregion

        #region Server Events
        #region Player Events
        public override void OnListPlayers(List<CPlayerInfo> players, CPlayerSubset subset) {
            this._currentPlayers = players;
        }

        public override void OnPlayerLeft(CPlayerInfo playerInfo) {
            if (_currentPlayers == null)
                return;

            this._currentPlayers.Remove(playerInfo);
        }
        #endregion

        #region Chat
        public override void OnGlobalChat(string speaker, string message) {
            InGameCommands(speaker, message, ChatRange.GLOBAL, -1, -1);
        }

        public override void OnTeamChat(string speaker, string message, int teamId) {
            InGameCommands(speaker, message, ChatRange.TEAM, teamId, -1);
        }

        public override void OnSquadChat(string speaker, string message, int teamId, int squadId) {
            InGameCommands(speaker, message, ChatRange.SQUAD, teamId, squadId);
        }

        public override void OnPlayerChat(string speaker, string message, string targetPlayer) {
            //this.ExecuteCommand("procon.protected.chat.write", E(String.Format("{0} > {1} > {2}", speaker, targetPlayer, message)));
        }
        #endregion

        #region Round
        public override void OnRoundOverPlayers(List<CPlayerInfo> players) {
            this._currentPlayers = players;
        }

        public override void OnRoundOverTeamScores(List<TeamScore> teamScores) {
            if (!this._autoStart)
                return;

            try {
                Thread delayed = new Thread(new ThreadStart(delegate ()
                {
                    Thread.Sleep(10 * 1000);
                    this._autoStartAfterDelay = true;
                    if (!this._debugMode)
                        ServerCommand("admin.say", StripModifiers(E(String.Format("Switching to next round in {1} seconds (Use !{0} to cancel)", this._cancelCommand, this._autoStartDelay))), "all");
                    else
                        DebugWrite(StripModifiers(E(String.Format("^1(DEBUG):^n Switching to next round in {1} seconds (Use !{0} to cancel)", this._cancelCommand, this._autoStartDelay))), 1);

                    Thread.Sleep(this._autoStartDelay * 1000);
                    if (!this._autoStartAfterDelay || !this._autoStart) {
                        if (!this._autoStart)
                            if (!this._debugMode)
                                ServerCommand("admin.say", StripModifiers(E("Automatic next round start canceled!")), "all");
                            else
                                DebugWrite(StripModifiers(E("^1(DEBUG):^n Automatic next round start canceled!")), 1);
                        return;
                    }
                    else if (!this._debugMode) {
                        this._autoStartAfterDelay = false;
                        ServerCommand("mapList.runNextRound");
                    }
                    else
                        DebugWrite(StripModifiers(E("^1(DEBUG):^n mapList.runNextRound command issued")), 1);
                }));

                delayed.IsBackground = true;
                delayed.Name = "runNextRound";
                delayed.Start();
            }
            catch (Exception e) {
                DebugWrite(e.ToString(), 2);
            }
        }
        #endregion

        #region Level
        public override void OnLevelLoaded(string mapFileName, string gamemode, int roundsPlayed, int roundsTotal) {
            this._autoStartAfterDelay = false;
        }
        #endregion 
        #endregion

        #region Commands
        private void InGameCommands(string speaker, string message, ChatRange cRange, int teamId, int squadId) {
            if (speaker.Equals("Server") || speaker.Equals("SERVER") || String.IsNullOrEmpty(message))
                return;

            try {
                CommandPrefixes CommandPrefix = Prefix2Enum(ExtractCommandPrefix(message));
                String[] ProconAccounts = OnlineProconAccounts();
                Match cmd_nextRound = Regex.Match(message, @"[!@#/]nextround", RegexOptions.IgnoreCase);
                Match cmd_cancelRS = Regex.Match(message, @"[!@#/]" + _cancelCommand, RegexOptions.IgnoreCase);
                if (cmd_nextRound.Success) {
                    Boolean permission = false;

                    CPrivileges cpSpeakerPrivs = this.GetAccountPrivileges(speaker);
                    if (cpSpeakerPrivs.CanUseMapFunctions)
                        permission = true;

                    if (!permission) {
                        ServerCommand("admin.say", "Not enough privileges to issue this command!", "player", speaker);
                        return;
                    }

                    if (!this._debugMode)
                        ServerCommand("admin.say", StripModifiers(E("Switching to next round in 5 seconds")), "all");
                    else
                        DebugWrite(StripModifiers(E("^1(DEBUG):^n Switching to next round in 5 seconds")), 1);


                    Thread delayed = new Thread(new ThreadStart(delegate ()
                    {
                        Thread.Sleep(5 * 1000);
                        if (!this._debugMode)
                            ServerCommand("mapList.runNextRound");
                        else
                            DebugWrite(StripModifiers(E("^1(DEBUG):^n mapList.runNextRound command issued")), 1);
                    }));

                    delayed.IsBackground = true;
                    delayed.Name = "runNextRound";
                    delayed.Start();
                }
                else if (cmd_cancelRS.Success) {
                    if (!this._autoStartAfterDelay) {
                        if (!this._debugMode)
                            ServerCommand("admin.say", StripModifiers(E("Automatic next round start was already canceled!")), "all");
                        else
                            DebugWrite(StripModifiers(E("^1(DEBUG):^n Automatic next round start was already canceled!")), 1);
                    }
                    else {
                        this._autoStartAfterDelay = false;
                        if (!this._debugMode)
                            ServerCommand("admin.say", StripModifiers(E("Automatic next round start canceled!")), "all");
                        else
                            DebugWrite(StripModifiers(E("^1(DEBUG):^n Automatic next round start canceled!")), 1);
                    }
                }
            }
            catch (Exception e) {
                DebugWrite(e.ToString(), 2);
            }
        } 
        #endregion

        #region Helpers (Some from Insane Limits)
        public String StripModifiers(String text) {
            return Regex.Replace(text, @"\^[0-9a-zA-Z]", "");
        }
        public String E(String text) // Escape replacements
        {
            text = Regex.Replace(text, @"\\n", "\n");
            text = Regex.Replace(text, @"\\t", "\t");
            return text;
        }
        public String InGameCommand_Pattern = @"^\s*([@/!\?])\s*";
        public String ExtractCommandPrefix(String text) {
            String text_trimmed = String.Empty;
            if (text.Length > 1)
                text_trimmed = text.Substring(1);

            Match match = Regex.Match(text, InGameCommand_Pattern, RegexOptions.IgnoreCase);
            Match match2 = Regex.Match(text_trimmed, InGameCommand_Pattern, RegexOptions.IgnoreCase);

            if (match.Success)
                if (match.Groups[1].Value == "/")
                    if (match2.Success)
                        return match2.Groups[1].Value;
                    else
                        return match.Groups[1].Value;
                else
                    return match.Groups[1].Value;

            return String.Empty;
        }
        public CommandPrefixes Prefix2Enum(String prefix) {
            if (String.IsNullOrEmpty(prefix))
                return (CommandPrefixes.NONE);
            if (prefix.Equals('!'))
                return (CommandPrefixes.GLOBAL);
            if (prefix.Equals('@'))
                return (CommandPrefixes.PRIVATE);
            if (prefix.Equals('#'))
                return (CommandPrefixes.ADMIN);
            return (CommandPrefixes.NONE);
        }
        public String[] OnlineProconAccounts() {
            List<String> online_users = new List<String>();

            foreach (CPlayerInfo p in this._currentPlayers) {
                if (p == null)
                    continue;

                CPrivileges cpAccount = null;

                cpAccount = this.GetAccountPrivileges(p.SoldierName);

                if (cpAccount != null && cpAccount.PrivilegesFlags > 0) {
                    online_users.Add(p.SoldierName);
                }
            }
            return (online_users.ToArray());
        }
        #endregion

    } // end CRoundStarter

} // end namespace PRoConEvents



