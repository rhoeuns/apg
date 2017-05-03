﻿using System;

namespace APG {

	public interface ChatterInterface {

		int Count();

		string GetName( int id );

		string GetMessage( int id );

		void Clear();

		void ClearOlderThan( int maxLifeTime );

		void SetMessageEventFunction( Action<string,string> messageFunction );

		void SetOnSubscribe( Action<string> onSubscribeFunc );

		void SetOnDonate( Action<string, int> onDonateFunc );

		// if a moderator speaks up in chat - how can we tell who is a moderator?

		// if someone speaks up (?)

	}
}