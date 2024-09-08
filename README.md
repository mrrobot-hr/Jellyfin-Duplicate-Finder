This plugin will search through your library and find duplicate movies by their IMDB id. After the search it will select the movie version with best resolution and keep that version of the movie.

It will delete all other copies.

If there is only one version of the movie it will not touch it.

After deletion of the movie plugin will delete the same folder if the size of it is less than 20 MB, just to clean up any residual subtitles and empty folders.

Sometimes the IMDB is missing or missinterpreted for some movies by the Jellyfin scraper. This might cause you loosing some files that were wrongly recognized under a same invalid IMDB id.
