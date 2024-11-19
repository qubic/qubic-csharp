using li.qubic.crypt;
using li.qubic.lib;

namespace li.qubic.test
{
    [TestClass]
    public class LibraryTest
    {
        private QubicHelper _helper = new QubicHelper();

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

            var identity = _helper.GetIdentityFromSeed(seed);

            Assert.IsNotNull(identity);

            Assert.AreEqual("UGQLSPXWWQORKDDJNOQVYRPYPWKDYLBCTOJCQTPRJFUXGTQXJAVACKSDDNMA", identity);

        }

        [TestMethod]
        public void TestIdToBinary()
        {
            var qcrypt = new QubicCrypt();
            var seed = "prwutqifhtqxjrhpliuhzvezyobjwilejelewskiykvogmvlgolkqie";
            var pubKey = qcrypt.GetPublicKey(seed);
            var id = "UGQLSPXWWQORKDDJNOQVYRPYPWKDYLBCTOJCQTPRJFUXGTQXJAVACKSDDNMA";
            var convertedPubKey = _helper.GetPublicKeyFromIdentity(id);

            Assert.IsTrue(pubKey.SequenceEqual(convertedPubKey));

        }
    }
}