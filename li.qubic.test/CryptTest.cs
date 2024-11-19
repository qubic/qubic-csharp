using li.qubic.crypt;
using li.qubic.lib;
using li.qubic.lib.Helper;
using System.Text;

namespace li.qubic.test
{
    [TestClass]
    public class CryptTest
    {
        private QubicCrypt _helper = new QubicCrypt();

        [TestMethod]
        public void TestRandomSeedGen()
        {
            var seed = _helper.GenerateRandomSeed();

            Assert.IsNotNull(seed);

            Assert.IsTrue(seed.Length == 55);
            Assert.IsTrue(seed.Length == "prwutqifhtqxjrhpliuhzvezyobjwilejelewskiykvogmvlgolkqie".Length);

            Console.WriteLine(seed);
        }

        [TestMethod]
        public void TestCreateIdentity()
        {
            var seed = "prwutqifhtqxjrhpliuhzvezyobjwilejelewskiykvogmvlgolkqie";

            Assert.IsNotNull(seed);

            var identity = _helper.GetIdentityFromPublicKey(_helper.GetPublicKey(seed));

            Assert.IsNotNull(identity);

            Assert.AreEqual("UGQLSPXWWQORKDDJNOQVYRPYPWKDYLBCTOJCQTPRJFUXGTQXJAVACKSDDNMA", identity);

        }

        [TestMethod]
        public void TestTxVerify()
        {
            var data = "kI/AipQwMxYyXirbHbpNYYlPnizvXcKX6QGxTsaq2UcBAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAEBCDwAAAAAA7Wq6AAIAcACQj8CKlDAzFjJeKtsduk1hiU+eLO9dwpfpAbFOxqrZR5CPwIqUMDMWMl4q2x26TWGJT54s713Cl+kBsU7GqtlHkI/AipQwMxYyXirbHbpNYYlPnizvXcKX6QGxTsaq2UdRWAAAAAAAAAEAAAAAAAAAqEUrmfteVXmTls6us2MzXhupSmsKxFm0MnMgolgvWCsOmWytIZpZWkO28BCMHWEvJj5zWoxUkseqE4fhcEsMAA==";
            var binData = Convert.FromBase64String(data);

            var baseTx = Marshalling.Deserialize<SignedTransaction>(binData);

            var verified = _helper.Verify(baseTx.transaction.sourcePublicKey, binData);

            Assert.IsTrue(verified, "Invalid Signature");
        }

        [TestMethod]
        public void TestSignOnly()
        {
            
            var seed = "prwutqifhtqxjrhpliuhzvezyobjwilejelewskiykvogmvlgolkqie";
            var message = "Hallo";
            var binMessage = Encoding.ASCII.GetBytes(message);
            var signature = _helper.Sign(seed, binMessage);

            var signedPacket = binMessage.Concat(signature).ToArray();
            var signedPacketAsBase64 = Convert.ToBase64String(signedPacket);

            Assert.AreEqual("SGFsbG9w3PrF3AvP1/epGGEbt79ZtwuDUP1UrxUKQSxw8Un31EKICNOIoqmuC9W/52M8Xg5islHGdAuPwOCS3OBjHwgA", signedPacketAsBase64);
        }

        [TestMethod]
        public void TestSignAndVerify()
        {
            var seed = "prwutqifhtqxjrhpliuhzvezyobjwilejelewskiykvogmvlgolkqie";
            var pubKey = _helper.GetPublicKey(seed);
            var id = "UGQLSPXWWQORKDDJNOQVYRPYPWKDYLBCTOJCQTPRJFUXGTQXJAVACKSDDNMA";
            var message = "Hallo";
            var binMessage = Encoding.ASCII.GetBytes(message);
            var signature = _helper.Sign(seed, binMessage);

            var signedPacket = binMessage.Concat(signature).ToArray();
            var signedPacketAsBase64 = Convert.ToBase64String(signedPacket);

            var verified = _helper.Verify(pubKey, signedPacket);

            Assert.IsTrue(verified, "Invalid Signature");
        }

    }
}