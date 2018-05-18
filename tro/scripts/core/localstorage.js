function StoredFavourites() {
	this.getFavouritesData = function (favouriteName, maxFavourites) {
		var favouritesKey = this.getFavouritesKey(favouriteName);

		if (!favouritesKey)
			return null;

		favouritesData = {};
		favouritesData.name = favouriteName;

		var storedData = localStorage.getItem(favouritesKey);

		if (storedData) {
			storedData = JSON.parse(storedData);
		}

		// If stored data doesn't contain a valid array, create one.
		if (Object.prototype.toString.call(storedData) !== '[object Array]') {
			storedData = new Array();
		}

		favouritesData.favourites = storedData;

		if (maxFavourites)
			favouritesData.maxFavourites = maxFavourites;

		favouritesData.addFavourite = function (name, displayname, maxFavourites) {
			if (!maxFavourites)
				maxFavourites = this.maxFavourites;

			if (!maxFavourites)
				throw ("Max favourites value not found in either favourite object or in the parameter.");

			var favourite = {
				name: name,
				displayname: displayname
			};

			// Remove similar favourite from list
			for (var i = this.favourites.length - 1; i >= 0 ; i--) {
				var existingFavourite = this.favourites[i];
				if (existingFavourite.name == favourite.name) {
					this.favourites.splice(i, 1);
				}
			}

			this.favourites.unshift(favourite);

			if (this.favourites.length > maxFavourites)
				this.favourites.length = maxFavourites;

			localStorage.setItem(favouritesKey, JSON.stringify(this.favourites));
		};

		return favouritesData;
	};


	this.getFavouritesKey = function (favouriteName) {

		var userId = $("body").attr("data-user");

		if (!userId || !favouriteName) {
			console.log("required infromation missing to enable relation dropdown favourites. UserId: " + userId + ", favourite name: " + favouriteName);
			return null;
		}

		return userId + "__favourites_" + favouriteName;
	}
}

var storedFavourites = new StoredFavourites();