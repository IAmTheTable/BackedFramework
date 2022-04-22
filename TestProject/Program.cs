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
        public void func()
        {
            base.Response.Redirect("https://youtube.com");
        }
    }
}