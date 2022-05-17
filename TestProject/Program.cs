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
                RootDirectory = @"root",
                UseMultiThreading = true,
                MaxThreads = 1,
                DynamicBuffers = true,
                ReadBuffer = 10,
                WriteBuffer = 10,
            });

            while (true)
            {
                Console.Title = $"{BackedServer.Instance.Config.ServerName} | ThreadCount: {ThreadPool.ThreadCount} | PendingThreads: {ThreadPool.PendingWorkItemCount}";
            }
        }
    }

    public class Base : BaseController
    {
        [Route("/", BackedFramework.Resources.HTTP.HTTPMethods.GET)]
        public void Index(string name = "World!")
        {
            //base.Response.Write($"Hello {name}");
            // base.Response.FinishRequest();
            base.Response.SendFile(true, "index.html");
        }

        [Route("/post", BackedFramework.Resources.HTTP.HTTPMethods.GET)]
        public void Post()
        {
            base.Response.Write($"<script>alert('{File.ReadAllText(BackedServer.Instance.Config.RootDirectory + "/dynamic.txt")}');</script>");
            base.Response.FinishRequest();
        }

        [Route("/post", BackedFramework.Resources.HTTP.HTTPMethods.POST)]
        public void Post(string data)
        {
            
            File.WriteAllBytes(BackedServer.Instance.Config.RootDirectory + "/image.png", base.Request.);
            base.Response.Redirect("post");
        }

        [Route("/img", BackedFramework.Resources.HTTP.HTTPMethods.GET)]
        public void ImageRequest()
        {
            base.Response.SendRawFile(true, $"{base.Request.Path}");
            Console.WriteLine($"Client requested: {base.Request.Path}");
        }

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
        [Route("/cool/index/", BackedFramework.Resources.HTTP.HTTPMethods.GET)]
        public void Index()
        {
            base.Response.SendFile(true, "index.html");
        }

        public void NotIndex()
        {
            base.Response.Content = "Not Hello World.";
            base.Response.FinishRequest();
        }

        [Route("/notadmin", BackedFramework.Resources.HTTP.HTTPMethods.POST)]
        public void admin(string name)
        {
            base.Response.Content = $"You queried for {name}";
            base.Response.FinishRequest();
        }
    }
}