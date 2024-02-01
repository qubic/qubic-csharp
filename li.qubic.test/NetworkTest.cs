using li.qubic.lib;
using li.qubic.lib.Helper;
using li.qubic.lib.Network;
using Newtonsoft.Json;

namespace li.qubic.test
{
    [TestClass]
    public class NetworkTest
    {
        private QubicHelper _helper = new QubicHelper();
        private string testNodeIp = "91.208.92.59";

        [TestMethod]
        public void TestGetTickInfo()
        {
            var requestor = new QubicRequestor(testNodeIp);

            var tickInfo = requestor.GetTickInfo().GetAwaiter().GetResult();


            Assert.IsNotNull(tickInfo);
            Console.WriteLine(JsonConvert.SerializeObject(tickInfo));
        }

        [TestMethod]
        public void TestGetEntity()
        {
            var id = "UGQLSPXWWQORKDDJNOQVYRPYPWKDYLBCTOJCQTPRJFUXGTQXJAVACKSDDNMA";

            var requestor = new QubicRequestor(testNodeIp);

            var header = new RequestResponseHeader(true)
            {
                type = (short)QubicPackageTypes.REQUEST_ENTITY,
                size = 40
            };
            var p = new RequestedEntity()
            {
                publicKey = _helper.GetPublicKeyFromIdentity(id)
            };
            var packet = Marshalling.Serialize(header).Concat(Marshalling.Serialize(p)).ToArray();

            requestor.GetDataPackageFromPeer<RespondedEntity>(packet, (short)QubicPackageTypes.RESPOND_ENTITY, (r) =>
            {
                Assert.IsNotNull(r);
                Console.WriteLine(JsonConvert.SerializeObject(r));
            });


        }
    }
}