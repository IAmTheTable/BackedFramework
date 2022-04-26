using BackedFramework.Api.Routing;
using BackedFramework.Controllers;
using BackedFramework.Server;

namespace TestProject
{
    public class Program
    {
        public static void Main(string[] args)
        {
            BackedServer.Initialize(new()
            {
                ApiPath = "/api",

                ListeningPort = 80,
                ApiVersion = "v4.20",
                RootDirectory = "root",
                UseMultiThreading = true,
                MaxThreads = -1,
                DynamicBuffers = true
            });

            BackedServer.Instance.Config.ReadBuffer = 9;
        }
    }


    public class Index : BaseController
    {
        [Route("/home", BackedFramework.Resources.HTTP.HTTPMethods.GET)]
        public void Func()
        {
            base.Response.SendFile(true, "what");
        }
    }


    // base test for testing the routing.
    [Route("/test", BackedFramework.Resources.HTTP.HTTPMethods.GET)]
    public class Home : BaseController
    {
        [Route("/cool", BackedFramework.Resources.HTTP.HTTPMethods.GET)]
        public void Index()
        {
            base.Response.SendFile(true, "index.html");
        }

        public void NotIndex()
        {
            base.Response.Content = "Not Hello World.";
            base.Response.Finalize();
        }

        [Route("/notadmin", BackedFramework.Resources.HTTP.HTTPMethods.POST)]
        public void admin()
        {
            base.Response.StatusCode = System.Net.HttpStatusCode.NotAcceptable;
            base.Response.Content = "this is the administrator page.";
            base.Response.Finalize();
        }
    }
}