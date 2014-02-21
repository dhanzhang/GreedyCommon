using PostHelpr.Controllers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using Microsoft.VisualStudio.TestTools.UnitTesting.Web;
using System.Web.Mvc;
using System.Web.Routing;
using System.Collections.Generic;

using Moq;

namespace TestProject1
{


    /// <summary>
    ///这是 HomeControllerTest 的测试类，旨在
    ///包含所有 HomeControllerTest 单元测试
    ///</summary>
    [TestClass()]
    public class HomeControllerTest
    {


        private TestContext testContextInstance;

        /// <summary>
        ///获取或设置测试上下文，上下文提供
        ///有关当前测试运行及其功能的信息。
        ///</summary>
        public TestContext TestContext
        {
            get
            {
                return testContextInstance;
            }
            set
            {
                testContextInstance = value;
            }
        }

        #region 附加测试特性
        // 
        //编写测试时，还可使用以下特性:
        //
        //使用 ClassInitialize 在运行类中的第一个测试前先运行代码
        //[ClassInitialize()]
        //public static void MyClassInitialize(TestContext testContext)
        //{
        //}
        //
        //使用 ClassCleanup 在运行完类中的所有测试后再运行代码
        //[ClassCleanup()]
        //public static void MyClassCleanup()
        //{
        //}
        //
        //使用 TestInitialize 在运行每个测试前先运行代码
        //[TestInitialize()]
        //public void MyTestInitialize()
        //{
        //}
        //
        //使用 TestCleanup 在运行完每个测试后运行代码
        //[TestCleanup()]
        //public void MyTestCleanup()
        //{
        //}
        //
        #endregion




        /// <summary>
        ///PSave 的测试
        ///</summary>
        // TODO: 确保 UrlToTest 特性指定一个指向 ASP.NET 页的 URL(例如，
        // http://.../Default.aspx)。这对于在 Web 服务器上执行单元测试是必需的，
        //无论要测试页、Web 服务还是 WCF 服务都是如此。
        [TestMethod()]
        //    [HostType("ASP.NET")]
        ////     [AspNetDevelopmentServerHost("C:\\Users\\dhanzhang\\Documents\\Visual Studio 2010\\Projects\\GreedyCommon\\PostHelpr", "/")]
        [UrlToTest("http://www.gyh.cn/Home/Save")]
        public void PSaveTest()
        {
            HomeController target = new HomeController(); // TODO: 初始化为适当的值
            string username = "dhz";
            string userpwd = "123";
            var expected = target.Save(username, userpwd);


        }
        public class MockHttpSession : System.Web.HttpSessionStateBase
        {
            private readonly Dictionary<string, object> sessionStorage = new Dictionary<string, object>();

            public override object this[string name]
            {
                get { return sessionStorage.ContainsKey(name) ? sessionStorage[name] : null; }
                set { sessionStorage[name] = value; }
            }

            public override void Remove(string name)
            {
                sessionStorage.Remove(name);
            }
        }
        /// <summary>
        ///CtxSave 的测试
        ///</summary>

        [TestMethod()]
        //    [HostType("ASP.NET")]
        // [AspNetDevelopmentServerHost("C:\\Users\\dhanzhang\\Documents\\Visual Studio 2010\\Projects\\GreedyCommon\\PostHelpr", "/")]
        [UrlToTest("http://www.gyh.cn/home/ctxsave")]
        public void CtxSaveTest()
        {
            HomeController target = new HomeController(); // TODO: 初始化为适当的值
            var mockHttpContext = new Mock<System.Web.HttpContextBase>();
           
            var request = new Mock<System.Web.HttpRequestBase>();
            request.SetupGet(r => r["uname"]).Returns("dhz");
            request.SetupGet(r => r["pwd"]).Returns("pwd");
            request.SetupGet(r => r["f1"]).Returns("f1");
            request.SetupGet(r => r["f2"]).Returns("f2");

            request.SetupGet(x => x.Headers).Returns(new System.Net.WebHeaderCollection {
                    {"X-Requested-With", "XMLHttpRequest"}
            }); 

            var server = new Mock<System.Web.HttpServerUtilityBase>();
            //server.Setup(x => x.MapPath("~/cc.jpg")).Returns(@"d:\igo\cc.jpg");
            //server.Setup(x => x.MapPath("~/cd.jpg")).Returns(@"d:\igo\cd.jpg"); 
            server.Setup(i => i.MapPath(It.IsAny<String>())).Returns((String a) => a.Replace("~/", @"d:\igo\").Replace("/", @"\")); 
            var response = new Mock<System.Web.HttpResponseBase>();
            response.Setup(r => r.ApplyAppPathModifier(It.IsAny<string>())).Returns((String url) => url); 
            var session = new Mock<System.Web.HttpSessionStateBase>(); 
            mockHttpContext.Setup(c => c.Request).Returns(request.Object);
            mockHttpContext.Setup(c => c.Response).Returns(response.Object);
            mockHttpContext.Setup(c => c.Server).Returns(server.Object);
            mockHttpContext.Setup(x => x.Session).Returns(session.Object); 
            target.ControllerContext = MvcContextMockFactory.CreateControllerContext(target, mockHttpContext.Object);
            //target.HttpContext.Request.ad
            //target.Request["pwd"] = "134";
            //target.Request["f1"] = "134";
            //target.Request["f2"] = "134";
            var actual = target.CtxSave();
            Assert.IsTrue(actual != null);

        }
    }
}
