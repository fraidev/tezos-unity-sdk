using System.Collections;
using System.Collections.Generic;
using Netezos.Encoding;
using Netezos.Forging.Models;
using TezosSDK.Helpers;

namespace TezosSDK.Tezos.Gas
{
    public class Operation
    {
        public string destination { get; set; }
        public Parameters parameters { get; set; }
    }
     
    public class GasStation : HttpClient
    {
        public GasStation(string gasUrl) : base(gasUrl)
        {
        }
        
        public IEnumerator PostOperations<T>(string sender, IList<Operation> operations)
        {
            var data = new
            {
                sender,
                operations
            };
            return PostJson<T>($"operation/", data);
        }
    }
}