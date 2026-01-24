using Newtonsoft.Json;
using System;

public class TransportCall
{
    public TransportCall()
    {
        location = new Location();
        timestamps = new List<Timestamp>();
    }
    [JsonProperty("location")]
    public Location location { get; set; }

    [JsonProperty("timestamps")]
    public List<Timestamp> timestamps { get; set; }
}
