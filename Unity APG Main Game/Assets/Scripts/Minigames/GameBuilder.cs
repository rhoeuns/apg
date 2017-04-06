﻿using UnityEngine;
using System;
using v3 = UnityEngine.Vector3;
using APG;

public class GameBuilder:MonoBehaviour {
	public GameObject basicSpriteObject;

	public GameObject sky, ground, overlay1, overlay2;

	public IncomingWaveHUD incomingWaveHUD;
	public Backgrounds backgrounds;
	public Foes foes;
	public Players players;
	public Treats treats;
	public Props props;
	public Reacts reacts;

	public TwitchGameLogicChat gameLogicChat;
	public APGBasicGameLogic basicGameLogic;

	FullGame fullGame;

	void Start() {
		Application.runInBackground = true;
		fullGame = new FullGame( this );
		fullGame.Init( this );
	}

	void Update() {
		fullGame.RunUpdate( transform );
	}
}

class FullGame {

	float tick = 0;

	GameSys gameSys;
	FoeSys foeSys;
	PlayerSys playerSys = new PlayerSys();
	AudiencePlayerSys audiencePlayerSys = new AudiencePlayerSys();
	TreatSys treatSys;
	PropSys propSys;
	ReactSys reactSys = new ReactSys();
	SpawnSys spawnSys = new SpawnSys();
	WaveHUD  waveHUD;

	bool pauseLatch = false;
	bool isPaused = false;

	void InitSpawns() {

		var turnEnd= new SpawnEntry { icon = assets.incomingWaveHUD.phaseDivider, spawn = () => { }, message="", scale=5 };
		for( var k = 1; k < 10; k++ )spawnSys.Add(45*k, turnEnd );

		var smallPositive = new SpawnEntry[] { treatSys.balloonClusterLeft, treatSys.balloonClusterBottomLeft, treatSys.balloonClusterRight, treatSys.balloonClusterBottomRight };
		var smallPositiveUnbiased = new SpawnEntry[] { treatSys.balloonClusterBottom };
		var bigPositive = new SpawnEntry[] { treatSys.balloonGridLeft, treatSys.balloonGridRight };
		var bigPositiveUnbiased = new SpawnEntry[] { treatSys.balloonGridAll, treatSys.balloonGridCenter };

		var normalFoes = new SpawnEntry[] { foeSys.beardGuy, foeSys.microwaveGuy, foeSys.mustacheGuy, foeSys.plantGuy, foeSys.trashGuy };

		Func<SpawnEntry[], SpawnEntry> rs = choices => choices[ rd.i(0, choices.Length) ];

		Action<SpawnEntry[], int, int> addTwo = (choices, time, timeAdd) => {
			var id= rd.i(0,choices.Length);
			var id2=(id + choices.Length/2)%choices.Length;
			spawnSys.Add(time, choices[id]);
			spawnSys.Add(time+timeAdd, choices[id2]);
		};

		addTwo( smallPositive, 6, 6 );
		spawnSys.Add(18, rs(smallPositiveUnbiased));

		spawnSys.Add(20, rs(normalFoes));

		addTwo( smallPositive, 24, 6 );

		addTwo( smallPositive, 46, 6 );

		spawnSys.Add(60, rs(normalFoes));

		spawnSys.Add(68, rs(smallPositiveUnbiased));

		spawnSys.Add(80, rs(normalFoes));

		spawnSys.Add(100, rs(bigPositiveUnbiased));

		spawnSys.Add(120, rs(normalFoes));

		addTwo( bigPositive, 140, 6 );

		spawnSys.Add(160, rs(normalFoes));

		//spawnSys.Add(300, () => { }, boulder);

		spawnSys.Sort();
	}

	v3[] buildingPos;

	float baseCameraZ;

	public void Init( MonoBehaviour src ) {
		gameSys = new GameSys(assets.basicSpriteObject, src.transform);
		reactSys.Init( gameSys, assets.reacts );
		foeSys = new FoeSys(assets.foes, gameSys, playerSys, audiencePlayerSys);
		treatSys = new TreatSys	( assets.treats, gameSys, reactSys );
		propSys = new PropSys( assets.props, gameSys );
		
		
		playerSys.Setup(gameSys, assets.players, foeSys, reactSys);
		audiencePlayerSys.Setup(gameSys, assets.players, foeSys, assets.basicGameLogic.GetPlayers(), playerSys, assets.gameLogicChat.GetAudienceSys() );
		buildingPos = assets.backgrounds.Setup(gameSys);
		InitSpawns();

		waveHUD = new WaveHUD( gameSys, assets.incomingWaveHUD, src, spawnSys );
	}

	v3 lastLookAtPos = new v3(0,0,0);
	v3 lastLookFromPos = new v3(0,0,0);

	public void RunUpdate( Transform transform ) {
		tick++;
		foeSys.tick = tick;


		v3 lookPos;
		if(playerSys.player2Ent == null) {
			lookPos = playerSys.playerEnt.pos;
		}
		else {
			lookPos = (playerSys.playerEnt.pos + playerSys.player2Ent.pos)/2;
		}
		lookPos.y -= 10f;

		var pauseRatio = assets.basicGameLogic.BetweenRoundPauseRatio();

		if( pauseRatio > 0 ) {
			var id = (int)( pauseRatio * buildingPos.Length );
			if( id < 0 )id = 0;
			if( id >= buildingPos.Length )id = buildingPos.Length-1;
			var newLookPos = buildingPos[ id ];

			newLookPos.y -= 10f;

			nm.ease( ref lastLookAtPos, newLookPos, .3f );
			lookPos = lastLookAtPos;

			nm.ease( ref lastLookFromPos, new v3(lookPos.x * .1f, lookPos.y * .1f, -8), .2f );

			transform.LookAt(new v3(transform.position.x * .9f + .1f * lookPos.x, transform.position.y * .9f + .1f * lookPos.y, lookPos.z));
			transform.position = lastLookFromPos;
		}
		else {
			lastLookAtPos = lookPos;

			nm.ease( ref lastLookFromPos, new v3(lookPos.x * .03f, lookPos.y * .03f, -10), .3f );

			transform.LookAt(new v3(transform.position.x * .97f + .03f * lookPos.x, transform.position.y * .97f + .03f * lookPos.y, lookPos.z));
			transform.position = lastLookFromPos;
		}

		if( Input.GetKey(KeyCode.Escape) ) {
			if( !pauseLatch ) {
				isPaused = !isPaused;
				pauseLatch = true;
			}
		}
		else pauseLatch = false;

		gameSys.Update( isPaused || pauseRatio > 0 );

		if(!isPaused && pauseRatio == 0 ) {
			spawnSys.Update((int)tick);
		}

		assets.ground.transform.position = new Vector3(transform.position.x, transform.position.y - 6, assets.ground.transform.position.z);
		assets.sky.transform.position = new Vector3(transform.position.x, transform.position.y, assets.sky.transform.position.z);

		assets.overlay1.transform.position = new Vector3(transform.position.x, transform.position.y, assets.overlay1.transform.position.z);
		assets.overlay2.transform.position = new Vector3(transform.position.x, transform.position.y, assets.overlay2.transform.position.z);

		assets.overlay1.GetComponent<SpriteRenderer>().color = new Color(1f, 1f, .9f, .15f + .11f * Mathf.Cos(tick * .01f + 73.0f) + .13f * Mathf.Cos(tick * .0073f + 13.0f));
		assets.overlay2.GetComponent<SpriteRenderer>().color = new Color(1f, 1f, .9f, .12f + .09f * Mathf.Cos(tick * .0083f + 173.0f) + .11f * Mathf.Cos(tick * .0063f + 23.0f));

		assets.overlay1.transform.localEulerAngles = new Vector3(0, 0, .2f + 21f * Mathf.Cos(tick * .01f + 73.0f) + 16f * Mathf.Cos(tick * .0053f + 13.0f));
		assets.overlay2.transform.localEulerAngles = new Vector3(0, 0, .2f + 11f * Mathf.Cos(tick * .0311f + 173.0f) + 23f * Mathf.Cos(tick * .0073f + 213.0f));
	}

	GameBuilder assets;

	public FullGame( GameBuilder builder ) {
		assets = builder;
	}
}