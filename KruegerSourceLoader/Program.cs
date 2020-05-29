using System;
using System.Linq;
using System.Threading.Tasks;

namespace KrügerSourceLoader {

  internal class Program {

    #region Private Methods

    private static async Task Main(string[] args) {
      Console.CancelKeyPress += Console_CancelKeyPress;
      var hostUrl = "http://hpc.uni-due.de/lnc/cpp/";
      if (args.Length == 2) {
        hostUrl = args[1];
      }
      string project;
      if (args.FirstOrDefault() is string p) {
        project = p;
        await new SourceLoader(project, hostUrl, onFinishFile).StartAsync();
        return;
      }
      else {
        Console.WriteLine("A downloader for the source files provided by Jens Krüger");
        Console.WriteLine("Possible projects are eg. \"Raytrace\", \"OpenGL\"");
        while (true) {
          Console.Write("Please insert project name: ");
          var input = Console.ReadLine();
          if (!string.IsNullOrWhiteSpace(input)) {
            project = input;

            break;
          }
        }
      }
      await new SourceLoader(project, hostUrl, onFinishFile).StartAsync();
      Console.WriteLine("Finished downloading. Press any key to exit.");
      Console.ReadKey();
    }

    private static void onFinishFile(string file) {
      Console.WriteLine($"Saved: {file}");
    }

    private static void Console_CancelKeyPress(object sender, ConsoleCancelEventArgs e) {
      Environment.Exit(0);
    }

    #endregion Private Methods
  }
}