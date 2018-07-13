using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json;

namespace ServerlessTrivia
{
    public class Guess : TableEntity
    {
        [JsonProperty("clueId")]
        public string ClueId
        {
            get { return PartitionKey; }
            set { PartitionKey = value;}
        }
        
        [JsonProperty("sessionId")]
        public string SessionId
        {
            get { return RowKey; }
            set { RowKey = value;}
        }

        [JsonProperty("value")]
        public string Value { get; set; }
    }
}