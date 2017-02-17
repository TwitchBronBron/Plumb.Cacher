# Plumb.Cacher
A .NET thread-safe in-memory cache that provides expiration for cache items.


# Usage

```csharp
using Plumb.Cacher;
class VideoLibrary{
    //create a new static cache property with a default cache timeout of 
    //5 minutes (in milliseconds)
    static Cache cache = new Cache(300000);

    public List<Movie> GetAllMovies(){
        //provide the key, and a resolver function.
        //If the cache has the value, it is immediately returned.
        //If the cache does NOT have the value (either because it never had it
        //or because this cache item expired), the factory function is called,
        //and then the value is returned. 
        return cache.Resolve("allActors", ()=>{
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
    


