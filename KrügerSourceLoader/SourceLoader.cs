using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace KrügerSourceLoader {

  public class SourceLoader {

    #region Public Constructors

    public SourceLoader(string project, string hostUrl, Action<string> onFinishFile = null, ConcurrentBag<string> loadedFiles = null) {
      OnFinishFile = onFinishFile;
      LoadedFiles = loadedFiles ?? LoadedFiles;
      Project = project;
      if (hostUrl != null) {
        if (hostUrl.EndsWith("/")) {
          HostUrl = hostUrl;
        }
        else {
          HostUrl = hostUrl + "/";
        }
      }
      var linkOptions = new DataflowLinkOptions { PropagateCompletion = true };
      var blockOptions = new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = ExecutionDataflowBlockOptions.Unbounded, EnsureOrdered = false };

      LoadBlock = new TransformBlock<CodeFile, CodeFile>(OnLoadBlock, blockOptions);
      ParseBlock = new TransformBlock<CodeFile, CodeFile>(OnParseBlock, blockOptions);
      SaveBlock = new ActionBlock<CodeFile>(OnSaveBlock, blockOptions);

      LoadBlock.LinkTo(ParseBlock, linkOptions, (cf) => cf != null);
      LoadBlock.LinkTo(DataflowBlock.NullTarget<CodeFile>());
      ParseBlock.LinkTo(SaveBlock, linkOptions);
    }

    #endregion Public Constructors

    #region Public Methods

    public async Task StartAsync(IEnumerable<string> files) {
      foreach (var file in files) {
        LoadBlock.Post(new CodeFile(file));
      }
      LoadBlock.Complete();
      await SaveBlock.Completion;
    }

    public async Task StartAsync(string entryFile = "makefile") {
      await StartAsync(new[] { entryFile });
    }

    #endregion Public Methods

    #region Private Methods

    private async Task<CodeFile> OnLoadBlock(CodeFile file) {
      if (!LoadedFiles.Contains(file.RelativeUrl)) {
        LoadedFiles.Add(file.RelativeUrl);
        try {
          var responseMessage = await httpClient.GetAsync($"{HostUrl}{Project}/{file.RelativeUrl}");
          if (responseMessage.IsSuccessStatusCode) {
            file.Source = await responseMessage.Content.ReadAsStringAsync();
            return file;
          }
          else {
            if (file.FileName == "makefile") {
              await new SourceLoader(Project, HostUrl, OnFinishFile, LoadedFiles).StartAsync("main.cpp");
            }
          }
        }
        catch (HttpRequestException) {
          if (file.FileName == "makefile") {
            await new SourceLoader(Project, HostUrl, OnFinishFile, LoadedFiles).StartAsync("main.cpp");
          }
        }
      }
      return null;
    }

    private async Task<CodeFile> OnParseBlock(CodeFile file) {
      var includes = file.GetIncludes().Select(i => i.Replace("\\", "/")).Except(LoadedFiles).ToArray();
      if (includes.Length > 0) {
        var loader = new SourceLoader(Project, HostUrl, OnFinishFile, LoadedFiles);
        await loader.StartAsync(includes);
      }
      return file;
    }

    private void OnSaveBlock(CodeFile obj) {
      try {
        var fileinfo = new FileInfo(Path.Combine(OutputFoler, $"{Project}{Path.DirectorySeparatorChar}{obj.RelativePath}"));
        fileinfo.Directory.Create();
        File.WriteAllText(fileinfo.FullName, obj.Source);
        OnFinishFile?.Invoke($"{Project}{Path.DirectorySeparatorChar}{obj.RelativePath}");
      }
      catch (Exception e) {
      }
    }

    #endregion Private Methods

    #region Private Fields

    private static HttpClient httpClient = new HttpClient();
    private TransformBlock<CodeFile, CodeFile> LoadBlock;
    private TransformBlock<CodeFile, CodeFile> ParseBlock;
    private ActionBlock<CodeFile> SaveBlock;
    private string HostUrl;
    private string Project;
    private string OutputFoler = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
    private Action<string> OnFinishFile;
    private ConcurrentBag<string> LoadedFiles = new ConcurrentBag<string>();

    #endregion Private Fields
  }
}