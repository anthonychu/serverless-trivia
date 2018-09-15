using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json;

namespace ServerlessTrivia
{
    public class Guess : TableEntity
    {
        [JsonProperty("clueId")]
        public string ClueId { get; set; }
        
        [JsonProperty("sessionId")]
        public string SessionId { get; set; }

        [JsonProperty("value")]
        public string Value { get; set; }
    }
}