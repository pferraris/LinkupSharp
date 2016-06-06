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
    }
}
