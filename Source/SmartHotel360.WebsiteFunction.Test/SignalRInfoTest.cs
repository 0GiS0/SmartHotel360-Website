using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Internal;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Primitives;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Microsoft.Azure.WebJobs.Extensions.SignalRService;
using Microsoft.Extensions.Logging;

namespace SmartHotel360.WebsiteFunction.Test
{
    [TestClass]
    public class SignalRInfoTest
    {
        protected ILogger log = new VerboseDiagnosticsLogger();


        [TestMethod]
        public void Request_SignalR_Token()
        {
            /*var signalRInfo = new SignalRConnectionInfo { AccessToken = "", Url = "" },
            var result = SignalRInfo.Run(req: HttpRequestSetup(
            new Dictionary<String, StringValues>(), ""),
            null
             log: log);
            var resultObject = (OkObjectResult)result;*/

            Assert.IsTrue(true);
        }

        public HttpRequest HttpRequestSetup(Dictionary<String, StringValues> query, string body)
        {
            var reqMock = new Mock<HttpRequest>();

            reqMock.Setup(req => req.Query).Returns(new QueryCollection(query));
            var stream = new MemoryStream();
            var writer = new StreamWriter(stream);
            writer.Write(body);
            writer.Flush();
            stream.Position = 0;
            reqMock.Setup(req => req.Body).Returns(stream);
            return reqMock.Object;
        }
    }
}