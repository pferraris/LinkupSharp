using LinkupSharp;
using LinkupSharpTestModel;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace LinkupSharpTests
{
    [TestClass]
    public class ClientConnectionTests
    {
        private static string endpoint = "tcp://localhost:5650/";

        [TestMethod]
        public void ConnectionSuccessfully()
        {
            using (var server = new TestServer(endpoint))
            using (var client = new TestClient())
            {
                client.Connect(endpoint).Wait();
                Assert.IsTrue(client.IsConnected);
            }
        }

        [TestMethod]
        public void ConnectionFailed()
        {
            using (var server = new TestServer(endpoint))
            using (var client = new TestClient())
            {
                client.Connect(endpoint.Replace("5650", "6650")).Wait();
                Assert.IsFalse(client.IsConnected);
            }
        }

        [TestMethod]
        public void Disconnection()
        {
            using (var server = new TestServer(endpoint))
            using (var client = new TestClient())
            {
                client.Connect(endpoint).Wait();
                Assert.IsTrue(client.IsConnected);
                client.Disconnect().Wait();
                Assert.IsFalse(client.IsConnected);
            }
        }

        [TestMethod]
        public void SignInSuccessfully()
        {
            using (var server = new TestServer(endpoint))
            using (var client = new TestClient())
            {
                client.Connect(endpoint).Wait();
                client.SignIn("pablo@tests").Wait();
                Assert.IsTrue(client.IsAuthenticated);
                Console.WriteLine(client.Id);
            }
        }

        [TestMethod]
        public void SignInFailed()
        {
            using (var server = new TestServer(endpoint))
            using (var client = new TestClient())
            {
                client.Connect(endpoint).Wait();
                client.SignIn("pablo@other").Wait();
                Assert.IsFalse(client.IsAuthenticated);
            }
        }

        [TestMethod]
        public void ReceiveContacts()
        {
            using (var server = new TestServer(endpoint))
            using (var client = new TestClient())
            {
                client.Connect(endpoint).Wait();
                client.SignIn("pablo@tests").Wait();
                var packet = client.Receive().Result;
                Assert.IsTrue(packet.Is<Id[]>());
                var actual = packet.GetContent<Id[]>();
                var expected = new Id[] { "pablo@tests" };
                CollectionAssert.AreEqual(expected, actual);
            }
        }

        [TestMethod]
        public void ReceiveMessage()
        {
            using (var server = new TestServer(endpoint))
            using (var client1 = new TestClient())
            using (var client2 = new TestClient())
            {
                client1.Connect(endpoint).Wait();
                client2.Connect(endpoint).Wait();
                client1.SignIn("pablo@tests").Wait();
                client2.SignIn("otro@tests").Wait();
                var message = new Message("This is a test.");
                client1.Send(new Packet(message) { Recipient = "otro@tests" }).Wait();
                client2.Receive().Wait();
                var packet = client2.Receive().Result;
                Assert.IsTrue(packet.Is<Message>());
                var actual = packet.GetContent<Message>();
                var expected = message;
                Assert.AreEqual(expected.Text, actual.Text);
            }
        }
    }
}
