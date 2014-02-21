using HttpClient;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Specialized;
using System.IO;
namespace TestProject1
{


    /// <summary>
    ///这是 HttpClientPostTest 的测试类，旨在
    ///包含所有 HttpClientPostTest 单元测试
    ///</summary>
    [TestClass()]
    public class HttpClientPostTest
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
        ///Post 的测试
        ///</summary>
        [TestMethod()]
        public void PostTest()
        {
            string url = @"http://www.gyh.cn/Home/PSave";
            //string username, string userpwd
            //NameValueCollection nv = new NameValueCollection(2);
            //nv.Add("username", "dhz1234234");
            //nv.Add("userpwd", "123435");
            var actual = HttpClient.Post.Execute(url, null);
            Assert.IsTrue(actual.IsOk);
        }
        [TestMethod()]
        public void UploadFileTest()
        {
            string url = @"http://www.gyh.cn/Home/Upload";
            // Upload(string username, string userpwd, HttpPostedFileBase imgfile, HttpPostedFileBase icofile)
            NameValueCollection nv = new NameValueCollection(2);
            nv.Add("username", "dhz1234234");
            nv.Add("userpwd", "123435");
            var aa = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "aa.jpg");
            var bb = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "bb.jpg");
            using (var stream2 = File.Open(aa, FileMode.Open))
            using (var stream3 = File.Open(bb, FileMode.Open))
            {
                UploadFile[] files = new UploadFile[]{
                 new UploadFile (){
                      ContentType= "image/jpg",
                       Filename="aa.jpg", Name = "imgfile", Stream= stream2
                 },
                 new UploadFile (){
                      ContentType= "image/jpg",
                       Filename="bb.jpg", Name = "icofile", Stream= stream2
                 }
            };
                var actual = HttpClient.Post.Execute(url, nv, files);
                Assert.IsTrue(actual.IsOk);
            }

        }
    }
}
