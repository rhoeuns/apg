using UnityEngine;
using System.IO;
using System.Collections.Generic;
using System;
using System.Text;
using APG;

namespace APG {

	public interface AudienceSysInterface {
		void RegisterHandler( AudiencePlayerEventHandler events );
		int PlayerCount();
		string PlayerName( int playerNumber );
		List<int> PlayerInput( int playerNumber );

		bool AddPlayer( string playerName );
		bool SetPlayerInput( string playerName, List<int> input );

		ChatterInterface Chatters();
	}

	public interface ChatterInterface {
		int Count();
		string GetName( int id );
		string GetMessage( int id );
		void Clear();
		void ClearOlderThan( int maxLifeTime );
		void SetMessageEventFunction( Action<string,string> messageFunction );
	}

	public class AudiencePlayerEventHandler {
		public Action<string> onJoin = playerName => { };
		public Action<List<int>> onInput = inputList => { };
		public Func<string> updateClient = () => "";
	}

	// __________________________________________________________________

	public class Chatter {
		static readonly int maxIRCMsgLength = 512;
		public string name;
		public StringBuilder msg = new StringBuilder( maxIRCMsgLength + 1 );
		public int time;
		public Chatter( string _name, string _msg, int _time ) {
			name = _name;
			msg.Append(_msg );
			time = _time;
		}
	}

	public class ChatSys : ChatterInterface {
		List<Chatter> chatters = new List<Chatter>();
		Dictionary<string, int> chatterID = new Dictionary<string, int>();
		AudiencePlayersSys apgSys;
		Action<string, string> customMsgEventFunction = (name, message) => { };

		public ChatSys( AudiencePlayersSys src ) {
			apgSys = src;
		}

		public int Count() {
			return chatters.Count;
		}
		public string GetName( int id ) {
			if( id < 0 || id >= chatters.Count || chatters[id] == null )return "";
			return chatters[id].name;
		}
		public string GetMessage( int id ) {
			if( id < 0 || id >= chatters.Count || chatters[id] == null )return "";
			return chatters[id].msg.ToString();
		}
		public void Clear() {
			chatters = new List<Chatter>();
			chatterID = new Dictionary<string, int>();
		}
		public void ClearOlderThan( int maxLifeTime ) {
			var newChatters = new List<Chatter>();
			var newChatterID = new Dictionary<string, int>();

			for( var k = 0; k < chatters.Count; k++ ) {
				var chatter = chatters[k];
				if( apgSys.time - chatter.time < maxLifeTime ){
					newChatterID[chatter.name] = newChatters.Count;
					newChatters.Add(chatter);
				}
			}
			chatters = newChatters;
			chatterID = newChatterID;
		}
		public void SetMessageEventFunction( Action<string,string> messageFunction ) {
			if( messageFunction == null )return;
			customMsgEventFunction = messageFunction;
		}
		public void Log( string name, string msg, int time ) {
			customMsgEventFunction( name, msg );
			if(chatterID.ContainsKey(name) == false) {
				chatterID[name] = chatters.Count;
				chatters.Add( new Chatter( name, msg, time ) );
			}
			else {
				var chatter = chatters[chatterID[name]];
				chatter.msg.Length = 0;
				chatter.msg.Append( msg );
				chatter.time = time;
			}
		}
	}

	public class AudiencePlayersSys : AudienceSysInterface {

		List<AudiencePlayerEventHandler> playerEvents = new List<AudiencePlayerEventHandler>();
		List<string> playerNames = new List<string>();
		List<List<int>> playerInput = new List<List<int>>();

		AudiencePlayerEventHandler nullEvents = new AudiencePlayerEventHandler();

		public Dictionary<string, int> playerMap = new Dictionary<string, int>();
		public int activePlayers = 0;

		ChatSys chatSys;

		public AudiencePlayersSys() {
			chatSys = new ChatSys( this );
		}

		public int time = 0;

		public void RegisterHandler( AudiencePlayerEventHandler events ) {
			playerEvents.Add( events );
		}
		public int PlayerCount() {
			return activePlayers;
		}
		public void Update() {
			time++;
		}
		bool ValidPlayer( int playerNumber ) {
			if( playerNumber < 0 || playerNumber >= playerEvents.Count || playerEvents[playerNumber] == null )return false;
			return true;
		}

		public AudiencePlayerEventHandler GetPlayerEvents( int playerNumber ) {
			if( !ValidPlayer( playerNumber ) )return nullEvents;
			return playerEvents[playerNumber];
		}
		public string PlayerName( int playerNumber ) {
			if( !ValidPlayer( playerNumber ) )return "";
			return playerNames[playerNumber];
		}
		public List<int> PlayerInput( int playerNumber ) {
			if( !ValidPlayer( playerNumber ) )return null;
			return playerInput[playerNumber];
		}
		public bool AddPlayer( string playerName ) {
			if( playerMap.ContainsKey(playerName) == true)return false;

			GetPlayerEvents( activePlayers ).onJoin(playerName);
			playerNames.Add( playerName );
			playerMap[playerName] = activePlayers;
			activePlayers++;
			return true;
		}
		public bool SetPlayerInput( string user, List<int> parms ) {
			if(playerMap.ContainsKey(user) == false) return false;

			var id = playerMap[user];
			GetPlayerEvents( id ).onInput(parms);
			playerInput[id] = parms;
			return true;
		}
		public void LogChat( string name, string msg) {
			chatSys.Log( name, msg, time );
		}
		public ChatterInterface Chatters() {
			return chatSys;
		}
	}

	class APGBasicGameLogic {
		int ticksPerSecond = 60;
		float nextAudienceTimer;
		float nextAudiencePlayerChoice;
		int roundNumber = 1;
		int secondsPerChoice = 20;

		public void Start() {
			nextAudiencePlayerChoice = ticksPerSecond * secondsPerChoice;
			nextAudienceTimer = ticksPerSecond * 10;
		}
		void InviteAudience( IRCNetworking network, AudiencePlayersSys apgSys, int maxPlayers ) {
			nextAudienceTimer--;
			if(apgSys.activePlayers < maxPlayers) {
				if(nextAudienceTimer <= 0) {
					if(apgSys.activePlayers == 0) {
						network.InviteEmptyGame();
					}
					else {
						network.InvitePartiallyFullGame();
					}
					nextAudienceTimer = ticksPerSecond * 30;
				}
			}
			else {
				if(nextAudienceTimer <= 0) {
					network.InviteFullGame();
					nextAudienceTimer = ticksPerSecond * 60;
				}
			}
		}
		void RunPlayerChoice( IRCNetworking network, AudiencePlayersSys apgSys ) {
			nextAudiencePlayerChoice--;
			if(nextAudiencePlayerChoice <= 0) {
				nextAudiencePlayerChoice = ticksPerSecond * secondsPerChoice;
				// update game state
				network.RequestPlayersUpdate();
				roundNumber++;
				network.UpdateTime( (int)(nextAudiencePlayerChoice/60), roundNumber);
				foreach(var key in apgSys.playerMap.Keys) {
					network.UpdatePlayer( key, apgSys.GetPlayerEvents( apgSys.playerMap[key] ).updateClient());
				}
				//apgSys.onUpdate()
			}
			else if((nextAudiencePlayerChoice % (ticksPerSecond * 5) == 0) || (nextAudiencePlayerChoice % (ticksPerSecond * 1) == 0 && nextAudiencePlayerChoice < (ticksPerSecond * 5))) {
				//apgSys.onUpdate()
				network.UpdateTime( (int)(nextAudiencePlayerChoice/60), roundNumber);
			}
		}
		public void Update( IRCNetworking network, AudiencePlayersSys apgSys, int maxPlayers ) {
			InviteAudience( network, apgSys, maxPlayers );
			RunPlayerChoice( network, apgSys );
		}
	}

	public interface IRCNetworking {
		void RequestPlayersUpdate();
		void UpdateTime( int time, int roundNumber );
		void UpdatePlayer( string key, string updateString );
		void InviteEmptyGame();
		void InvitePartiallyFullGame();
		void InviteFullGame();
		AudienceSysInterface GetAudienceSys();
	}
}

[RequireComponent(typeof(TwitchIRC))]
[RequireComponent(typeof(TwitchIRCLogic))]
public class TwitchGameLogicChat:MonoBehaviour, IRCNetworking {

	//___________________________________________

	public int maxPlayers = 20;
	public string LogicOauth;
	public string LogicChannelName;
	public string ChatOauth;
	public string ChatChannelName;

	//___________________________________________

	string launchGameLink = "GAME LAUNCHING LINK NOT SET";

	TwitchIRC IRC;
	TwitchIRCLogic IRCLogic;
	AudiencePlayersSys apgSys = new AudiencePlayersSys();

	// This, right here, needs to be much better thought through.
	APGBasicGameLogic gameLogic = new APGBasicGameLogic();

	//___________________________________________

	public void RequestPlayersUpdate() {
		IRCLogic.SendMsg("s");
	}
	public void UpdateTime( int time, int roundNumber ) {
		IRCLogic.SendMsg( "t " + time + " " + roundNumber );
	}
	public void UpdatePlayer( string key, string updateString ) {
		IRCLogic.SendMsg("u "+key+" "+updateString);
	}
	
	public void InviteEmptyGame() {
		IRC.SendMsg("Up to 20 audience members can play this game!  To join this game, go to " + launchGameLink);
	}
	public void InvitePartiallyFullGame() {
		IRC.SendMsg("" + apgSys.activePlayers + " of " + maxPlayers + " are now playing this game!  To join, go to " + launchGameLink);
	}
	public void InviteFullGame() {
		IRC.SendMsg("The current game is now full!  To get in line for the next game, go to " + launchGameLink);
	}

	//___________________________________________

	void LoadDebugOauths() {
		try { 
			// format of this file, for debugging purposes: logic_channel_oauth chat_channel_oauth
			var sr = new StreamReader(@"C:\APG\apg_debug_oauths.txt");
			if( sr != null ) {
				/*
				 * Settings:
				 * Chat oauth
				 * Logic oauth
				 * Client-ID
				 * redirectURI
				 * chat name
				 * logic name
				 * 
				 */

				var fileContents = sr.ReadToEnd();
				var vals = fileContents.Split(new char[] { ' ' });
				Debug.Log( "Setting oauths to " + vals[0] + " " + vals[1] + " " + vals[2] );
				LogicOauth = vals[0];
				ChatOauth = vals[1];
				launchGameLink = vals[2];
				launchGameLink = launchGameLink.Replace( "STATE_PARMS_HERE", "B+"+ChatChannelName+"+"+LogicChannelName );
				sr.Close();
			}
		}
		catch { }
	}
	public string GetLogicOauth() {
		LoadDebugOauths();
		return LogicOauth;
	}
	public string GetChatOauth() {
		LoadDebugOauths();
		return ChatOauth;
	}

	void InitIRCChat() {
		IRC = this.GetComponent<TwitchIRC>();
		//IRC.SendCommand("CAP REQ :twitch.tv/tags"); //register for additional data such as emote-ids, name color etc.
		IRC.messageRecievedEvent.AddListener(msg => {
			int msgIndex = msg.IndexOf("PRIVMSG #");
			string msgString = msg.Substring(msgIndex + ChatChannelName.Length + 11);
			string user = msg.Substring(1, msg.IndexOf('!') - 1);
			apgSys.LogChat( user, msgString );
		});
	}

	Dictionary<string, Action<string, string[]>> clientCommands = new Dictionary<string, Action<string, string[]>>();

	// what messages can come from clients?
	void InitIRCLogicChannel() {
		IRCLogic = this.GetComponent<TwitchIRCLogic>();
		IRCLogic.messageRecievedEvent.AddListener(msg => {
			int msgIndex = msg.IndexOf("PRIVMSG #");
			string msgString = msg.Substring(msgIndex + LogicChannelName.Length + 11);
			string user = msg.Substring(1, msg.IndexOf('!') - 1);

			var fullMsg = msgString.Split(new char[] { ' ' });
			/*if(clientCommands.ContainsKey(fullMsg[0]) == true) {
				clientCommands[fullMsg[0]](user, fullMsg );
			}*/

			if(fullMsg[0] == "join") {
				// need game logic to determine if this player should be allowed to join
				// need multiple ways to join, too - join in different roles
				if( apgSys.AddPlayer(user ))IRCLogic.SendMsg("join "+user);
			}
			if(fullMsg[0] == "upd") {
				// need a better way to handle updates - different kinds of update types?  json-esque?
				var parms = new List<int>();
				for(var k = 1; k < fullMsg.Length; k++) parms.Add(Int32.Parse(fullMsg[k]));
				apgSys.SetPlayerInput( user, parms );
			}
			// register these as a dictionary instead?
			// need a way add a custom handler for unrecognized messages
		});

		/*clientCommands["join"] = (user, fullMsg) =>{
			// need game logic to determine if this player should be allowed to join
			// need multiple ways to join, too - join in different roles
			if( apgSys.AddPlayer( user ))IRCLogic.SendMsg("join "+user);
		};
		clientCommands["upd"] = (user, fullMsg) => {
			var parms = new List<int>();
			for(var k = 1; k < fullMsg.Length; k++) parms.Add(Int32.Parse(fullMsg[k]));
			apgSys.SetPlayerInput( user, parms );
		};*/
	}

	//_______________________________________________________

	public AudienceSysInterface GetAudienceSys() {
		return apgSys;
	}
	public void Start() {
		gameLogic.Start();
		InitIRCChat();
		InitIRCLogicChannel();
	}
	void Update() {
		apgSys.Update();
		gameLogic.Update(this, apgSys, maxPlayers );
	}
}