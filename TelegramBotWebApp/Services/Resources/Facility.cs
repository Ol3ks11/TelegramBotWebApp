using Newtonsoft.Json;
public class Facility
{

    [JsonProperty("portName")]
    public string portName { get; set; }

    [JsonProperty("locationName")]
    public string locationName { get; set; }

    [JsonProperty("locationType")]
    public string locationType { get; set; }

    [JsonProperty("countryName")]
    public string countryName { get; set; }

    [JsonProperty("carrierTerminalCode")]
    public string carrierTerminalCode { get; set; }

    public string emoji { get; set; }

    
}
