﻿using UnityEngine;
using System.Collections.Generic;
using System;
using V3 = UnityEngine.Vector3;
using APG;

public class Props:MonoBehaviour {
}

public class PropSys {
	GameSys gameSys;
	Props theProps;
	public List<Ent> propList = new List<Ent>();
	public PropSys(Props props, GameSys theGameSys) {
		gameSys = theGameSys;
		theProps = props;
	}
}