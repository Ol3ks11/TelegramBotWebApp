using Newtonsoft.Json;
using System;

public class Location
{
    public Location()
    {
        locationName = "N/A";
        UNLocationCode = "N/A";
    }

    [JsonProperty("locationName")]
    public string locationName { get; set; }

    [JsonProperty("UNLocationCode")]
    public string UNLocationCode { get; set; }

    [JsonProperty("facilitySMDGCode")]
    public string facilitySMDGCode { get; set; }

    public string locationFlag { get; set; }
}
