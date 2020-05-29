using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;

namespace KrügerSourceLoader {

  public class CodeFile {

    #region Public Properties

    public string Source { get; set; }

    public string FileName { get; }

    public string RelativePath => RelativeUrl.Replace('/', Path.DirectorySeparatorChar);

    public string RelativeUrl { get; } = string.Empty;

    #endregion Public Properties

    #region Public Constructors

    public CodeFile(string relativepath) {
      var parts = relativepath.Split('/');
      for (int i = 0; i < parts.Length; i++) {
        if (i == parts.Length - 1) {
          FileName = parts[i];
          RelativeUrl += $"{parts[i]}";
        }
        else {
          RelativeUrl += $"{parts[i]}/";
        }
      }
    }

    #endregion Public Constructors

    #region Public Methods

    public string GetAbsoluteFilePath(string root) => Path.Combine(Path.Combine(root, RelativePath), FileName);

    public IEnumerable<string> GetIncludes() {
      if (Source != null) {
        if (FileName.ToLower() == "makefile") {
          var match = makefileRegex.Value.Match(Source);
          if (match != default) {
            return match.Groups[1].Value.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
          }
        }
        else {
          return IncludeRegex.Value.Matches(Source).OfType<Match>()
            .Concat(commonfileRegex.Value.Matches(Source).OfType<Match>()
            .Where(m => {
              return commonFileExtensions.Value.Contains(m.Groups[4].Value.ToLower().Trim().Remove(0, 1));
            }))
            .Select((m) => m.Groups[1].Value);
        }
      }
      return Enumerable.Empty<string>();
    }

    #endregion Public Methods

    #region Private Fields

    private static ThreadLocal<Regex> IncludeRegex = new ThreadLocal<Regex>(() => new Regex("#include\\s*\"((\\w*[\\/\\\\])*(\\w*(\\.\\w *)?))\""));
    private static ThreadLocal<Regex> makefileRegex = new ThreadLocal<Regex>(() => new Regex("[Ss][Rr][Cc]\\s*=\\s*((\\w*.?\\w*\\s*?)*)"));
    private static ThreadLocal<Regex> commonfileRegex = new ThreadLocal<Regex>(() => new Regex("\"((\\w*[\\/\\\\])*(\\w*(\\.\\w*)))\""));

    private static ThreadLocal<List<string>> commonFileExtensions = new ThreadLocal<List<string>>(() => new List<string> {
    "bmp",
    "glsl",
    "jpeg",
    "mp3",
    "mp4",
    "png",
    "tar",
    "tar.gz",
    "txt",
    "xml",
    "zip",
    });

    #endregion Private Fields
  }
}