using li.qubic.lib;
using li.qubic.lib.Helper;
using li.qubic.lib.transfer;

namespace li.qubic.test
{
    [TestClass]
    public class TransferTest
    {
        private QubicHelper _helper = new QubicHelper();

        [TestMethod]
        public void TestTransactionFactorySendManyCreate()
        {
            var seed = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";

            var manyRecipients = new List<SendManyRecipient>()
            {
                new SendManyRecipient("QIMDCFEUDXXDAGPQFFHPPLFAHSPDXVMIFZVMPQVPRFITSWDDILMNJLWEBGZK", 1),
                new SendManyRecipient("QIMDCFEUDXXDAGPQFFHPPLFAHSPDXVMIFZVMPQVPRFITSWDDILMNJLWEBGZK", 1),
                new SendManyRecipient("QIMDCFEUDXXDAGPQFFHPPLFAHSPDXVMIFZVMPQVPRFITSWDDILMNJLWEBGZK", 1)
            };

            var tx = new TransferFactory(TransactionType.SendMany)
                            .SetReceiverIdentity(IdentityHelper.GenerateContractAddress(QubicLibConst.SC_QUTIL_CONTRACT_ID))
                            .AddSendManyRecipients(manyRecipients)
                            .BuildAndSign(seed);

            // try to verify signature
            var verified = _helper.VerifyQubicStruct(tx.Skip(8).ToArray(), tx.Length-8, _helper.GetPublicKeyFromIdentity(_helper.GetIdentityFromSeed(seed)));

            Assert.IsTrue(verified, "Signature cannot be verified");

            var parsedBaseTx = Marshalling.Deserialize<BaseTransaction>(tx.Skip(8).ToArray());

            Assert.IsTrue(parsedBaseTx.amount == 13, "Amount should be 13");

            // todo: test send many recipients

        }

    }
}