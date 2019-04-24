# Plumb.Cacher

[![Build Status](https://travis-ci.org/TwitchBronBron/Plumb.Cacher.svg?branch=master)](https://travis-ci.org/TwitchBronBron/Plumb.Cacher)
[![AppVeyor Build status](https://ci.appveyor.com/api/projects/status/knn6laqdeusq053g?svg=true)](https://ci.appveyor.com/project/TwitchBronBron/plumb-cacher)
[![Coverage Status](https://coveralls.io/repos/github/TwitchBronBron/Plumb.Cacher/badge.svg?branch=master)](https://coveralls.io/github/TwitchBronBron/Plumb.Cacher?branch=master)
[![Nuget Version](https://img.shields.io/nuget/v/Plumb.Cacher.svg)](https://www.nuget.org/packages/Plumb.Cacher/)

A .NET thread-safe in-memory cache that provides expiration for cache items. 


# Usage

```csharp
using Plumb.Cacher;
class VideoLibrary {
    //create a new static cache property with a default cache timeout of 
    //5 minutes (in milliseconds)
    static Cache cache = new Cache(300000);

    public List<Movie> GetAllMovies(){
        //provide the key, and a resolver function.
        //If the cache has the value, it is immediately returned.
        //If the cache does NOT have the value (either because it never had it
        //or because this cache item expired), the factory function is called,
        //and then the value is returned. 
        return cache.Resolve("allMovies", ()=>{
            //get the movies from your database somehow...
            return Db.GetAllMovies();
        });
    }

    public List<Movie> GetMoviesForActor(int actorId){
        return cache.Resolve("moviesForActor-" + actorId, ()=>{
            //get the movies from your database somehow...
            return Db.GetMoviesForActor(actorId);
        }, 
        //override the default cache timeout for this specific cache item
        20000);
    }

    public List<Movie> GetMoviesForYear(int year){
        return cache.Resolve("moviesForYear-" + year, ()=>{
            //get the movies from your database somehow...
            return Db.GetMoviesForYear(year);
        }, 
        //set the timeout to null, which will keep this item in cache forever
        null);
    }
}
```
    


