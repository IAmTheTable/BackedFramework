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
        public void Func()
        {
            //base.Response.Redirect("https://evit.instructure.com");
            base.Response.SendFile(true, "what");
        }
    }

    [Route("/test", BackedFramework.Resources.HTTP.HTTPMethods.GET)]
    public class Home : BaseController
    {
        public void Index()
        {
            base.Response.Content = "Hello World.";
            base.Response.Finalize();
        }
        
        public void NotIndex()
        {
            base.Response.Content = "Not Hello World.";
            base.Response.Finalize();
        }
    }
}