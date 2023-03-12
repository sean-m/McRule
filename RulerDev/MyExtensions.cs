using System;
using Newtonsoft.Json;

namespace RulerDev;

public static class MyExtensions {
    
    public static object Dump(this object thing, params string[]? msgs) {
        if (msgs != null && msgs.Length > 0) {
            Console.WriteLine("> {0}\n", string.Join(", ", msgs));
        }

        if (thing is String) {
            Console.WriteLine(thing);
        } else {
            Console.WriteLine(JsonConvert.SerializeObject(thing, Formatting.Indented));
        }
        return thing;
    }

}