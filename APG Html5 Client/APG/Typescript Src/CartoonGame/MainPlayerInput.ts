﻿cacheImages('assets/imgs', ['ClientUI4.png']);
cacheSounds('assets/snds/fx', ['strokeup2.mp3', 'strokeup.mp3','strokeup4.mp3']);
cacheJSONs(['TestActions.json']);

interface RoundUpdate {
	round: number;
	time: number;
}

interface EmptyParms {
}

interface SelectionParms {
	choices: number[];
}

interface PlayerUpdate{
	nm:string;
	hp:number;
	money:number;
}


function MainPlayerInput(sys: APGSys): void {
	var fontName: string = "Caveat Brush";

	var actions: any = sys.JSONAssets['TestActions.json'];

	var endOfRoundSound: Phaser.Sound = sys.g.add.audio('assets/snds/fx/strokeup4.mp3', 1, false);
	var warningSound: Phaser.Sound = sys.g.add.audio('assets/snds/fx/strokeup.mp3', 1, false);


	function makeButtonSet(baseX: number, baseY: number, xAdd: number, yAdd: number, size: number, highlightColor: string, baseColor: string, setToolTip: (str: string) => void, setOption: (val: number) => void, buttonsInit: ActionEntry[]): ButtonCollection {
		return new ButtonCollection(sys, baseX, baseY, xAdd, yAdd, size, highlightColor, baseColor, setToolTip, setOption, buttonsInit);
	}
	function addActionSet(setToolTip: (str: string) => void): ButtonCollection {
		var o = [];
		for (var j = 0; j < actions.length; j++)o.push(new ActionEntry(actions[j].name, ""));
		return makeButtonSet(200, 120, 81, 0, 28, '#F00000', '#200000', setToolTip, v => { }, o);
	}
	function addActions(srcChoices: number[], setToolTip: (str: string) => void): ButtonCollection[] {
		var choiceLeft: number = 200, choiceUp: number = 170;
		var curCollection: number = 0;
		function add(choiceSet: ActionEntry[]): ButtonCollection {
			var id: number = curCollection;
			curCollection++;
			return makeButtonSet(choiceLeft, choiceUp, 0, 40, 22, '#F00000', '#200000', setToolTip, v => srcChoices[id] = v, choiceSet);
		}
		function st(name: string, tip: string): ActionEntry { return new ActionEntry(name, tip); }

		var o = [];
		for (var j = 0; j < actions.length; j++) {
			var p = [];
			for (var k = 0; k < actions[j].choices.length; k++) {
				var r = actions[j].choices[k];
				p.push(st(r.name, r.tip));
			}
			o.push(add(p));
		}
		return o;
	}

	var timer: number = 0;
	var roundNumber: number = 1;
	var choices: number[] = [1, 1, 1, 1, 1, 1];
	var myStats: PlayerUpdate = { nm: "", hp: 3, money: 0 };

	sys.handlers = new APGSubgameMessageHandler()
		.add<RoundUpdate>("time", p => {
			timer = p.time;
			roundNumber = p.round;
			if (timer < 6) { warningSound.play('', 0, 1 - (timer * 15) / 100); }
		})
		.add<PlayerUpdate>("pl", p => {
			if (p.nm != sys.playerName) return;
			myStats = p;
		})
		.add<SelectionParms>("submit", p => {
			sys.sendMessageToServer<SelectionParms>("upd", { choices: choices });
		});

	var toolTip: string = "";
	function setToolTip(str: string): void { toolTip = str; }

	var tick: number = 0, choiceLeft: number = 50, choiceUp: number = 118, tabButtons: ButtonCollection, choiceButtons: ButtonCollection[], bkg = new Image(); bkg.src = 'ClientUI4.png';
	var labelColor: string = '#608080';
	var roundLabel: enttx, toolTipLabel: enttx, nextChoiceLabel: enttx;
	var lastRoundUpdate: number = 0;

	new ent(sys.w, 0, 0, 'assets/imgs/ClientUI4.png', {
		upd: e => {
			if (roundNumber != lastRoundUpdate) {
				roundLabel.text = "Choices for Round " + roundNumber;
				lastRoundUpdate = roundNumber;
			}
			tabButtons.update(true);
			for (var j: number = 0; j < choiceButtons.length; j++)choiceButtons[j].update(tabButtons.selected == j);
			toolTipLabel.text = toolTip;
			nextChoiceLabel.text = "" + timer;
		}
	});
	roundLabel = new enttx(sys.w, 220, 25, "Choices for Round ", { font: '54px ' + fontName, fill: '#688' });
	toolTipLabel = new enttx(sys.w, 340, 150, "ToolTip", { font: '20px ' + fontName, fill: '#233', wordWrap: true, wordWrapWidth: 330 });
	nextChoiceLabel = new enttx(sys.w, 650, 350, "", { font: '40px ' + fontName, fill: '#688' });
	tabButtons = addActionSet(setToolTip);
	choiceButtons = addActions(choices, setToolTip);

	function category(msg: string, x: number, y: number ):void {
		new enttx(sys.w, x, y, msg, { font: '18px ' + fontName, fill: '#433' });
	}

	function inCategory(x: number, y: number, add: number, labels: string[]): void {
		for (var k = 0; k < labels.length; k++) {
			new enttx(sys.w, x, y + k*add, labels[k], { font: '14px ' + fontName, fill: '#211' });
		}
	}

	category("RESOURCES", 40, 100);
	inCategory(50, 120, 16, ["Health:", "Gold:", "Tacos:", "Silver:"]);

	category("STATS", 40, 120 + 64 + 8);
	inCategory(50, 120 + 64 + 8+20, 16, ["Defense:", "Action+", "Heal+", "Item Get+", "Work+"]);

	category("SELECTED CHOICES", 40, 300);
	inCategory(50, 320, 16, ["Move:", "Action:", "Stance:", "Item:"]);

}