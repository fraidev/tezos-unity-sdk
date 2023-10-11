using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Mime;
using System.Text.Json;
using Beacon.Sdk.Beacon.Sign;
using Netezos.Encoding;
using TezosSDK.Helpers;

namespace TezosSDK.Tezos.Gas
{
    // TODO: better types
    public class Extension
    {
        public string admin { get; set; }
        public string counter { get; set; }
        public int permits { get; set; }
        public int extension { get; set; }
        public string max_expiry { get; set; }
        public int user_expiries { get; set; }
        public string default_expiry { get; set; }
        public int permit_expiries { get; set; }
    }

    public class Storage
    {
        public int ledger { get; set; }
        public int metadata { get; set; }
        public Extension Extension { get; set; }
        public int operators { get; set; }
        public int token_metadata { get; set; }
    }

    public class PermitOperation
    {
        public string publicKey { get; set; }
        public string signature { get; set; }
        public string transferHash { get; set; }
    }

    public class Permit
    {
        public string bytes { get; set; }
        public string transfer_hash { get; set; }
    }

    public class PermitContract : HttpClient
    {
        // private const string chainId = "NetXdQprcVkpaWU";

        private ITezos tezos;
        private string address;

        public PermitContract(string address, ITezos tezos, string baseUrl) : base(baseUrl)
        {
            this.address = address;
            this.tezos = tezos;
        }

        //public int GetCounter()
        public IEnumerator GetCounter(Action<int> callback)
        {
            // TODO: make this via rpc. it's using tzkt for now
            var contract = this.tezos.API.GetContractStorage<Storage>(this.address);
            return new CoroutineWrapper<Storage>(contract, result =>
            {
                var counter = result.Extension.counter;
                var counterInt = int.Parse(counter);
                callback(counterInt);
            });
        }

        public IEnumerator GeneratePermit(string destination, int tokenId, int amount, Action<Permit> callback)
        {
            return GetCounter(counter =>
                {
                    var transferData = tezos.TokenContract.TransferParams("transfer", destination, tokenId, amount)
                        .ToJson();
                    
                    const SignPayloadType transferType = SignPayloadType.operation;

                    var payload = NetezosExtensions.GetPayloadString(transferType, transferData);

                    //TODO: is this correct?
                    // const byts = packDataBytes(transfer_data, transfer_type).bytes;
                    // const blak = blake2b(32);
                    // const transfer_hash = blak.update(hex2buf(byts)).digest('hex');
                    var payloadBytes = Hex.Parse(payload);
                    var transferHash = Netezos.Utils.Blake2b.GetDigest(payloadBytes);

                    // const permit_data = [
                    //   [
                    //     {"string": chain_id},
                    //     {"string": this.address}
                    //   ],
                    //   [
                    //     {"int": counter},
                    //     {"bytes": transfer_hash}
                    //   ]
                    // ];
 
                    
                    // var permit_data = 


                    var permit = new Permit()
                    {
                        bytes = "",
                        transfer_hash = Hex.Convert(transferHash)
                    };
                    
                    callback(permit);
                }
            );
        }

        public IMicheline PermitCall(PermitOperation op)
        {
            var input = new MichelinePrim
            {
                Prim = PrimType.Pair,
                Args = new List<IMicheline>
                {
                    new MichelinePrim()
                    {
                        Prim = PrimType.Pair,
                        Args = new List<IMicheline>
                        {
                            new MichelineString(op.publicKey),
                            new MichelineString(op.signature),
                        }
                    },
                    new MichelineString(op.transferHash)
                }
            }.ToJson();

            return tezos.TokenContract.GetContractScript().BuildParameter(
                entrypoint: "permit",
                value: input);
        }
    }
}