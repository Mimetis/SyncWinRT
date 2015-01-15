using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Synchronization.Services;
using System.IO;

namespace SyncServiceLibTest
{
    [TestClass]
    public class SyncServiceTest
    {
        public class SyncServiceTestClass : SyncService<TestEntity>
        {
            protected override void OnBeginSyncRequest()
            {
                // raise an exception first
                throw new SyncServiceException("Test Exception");

                // base.OnBeginSyncRequest();
            }

            public static void InitializeService(Microsoft.Synchronization.Services.ISyncServiceConfiguration config)
            {
                // TODO: MUST set these values
                config.ServerConnectionString = "test";

                config.SetEnableScope("TestScope");

                //config.AddFilterParameterConfiguration("Owner_Id", "[FTasks]", "@Owner_Id", typeof(System.String));
            }
        }

        [TestMethod]
        [ExpectedException(typeof(SyncServiceException))]
        public void EventTest()
        {
            var svc = new SyncServiceTestClass();
            var stream = new MemoryStream();
            svc.ProcessRequestForMessage(stream);
        }
    }
}
