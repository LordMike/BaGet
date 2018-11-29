using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BaGet.Core.Services
{
    public class FileSymbolStorageService : ISymbolStorageService
    {
        // See: https://github.com/dotnet/corefx/blob/master/src/Common/src/CoreLib/System/IO/Stream.cs#L35
        private const int DefaultCopyBufferSize = 81920;

        private readonly string _storePath;

        public FileSymbolStorageService(string storePath)
        {
            _storePath = storePath ?? throw new ArgumentNullException(nameof(storePath));
        }

        public async Task SavePortablePdbContentAsync(
            string filename,
            string key,
            Stream pdbStream,
            CancellationToken cancellationToken)
        {
            var path = GetPathForKey(filename, key);

            Directory.CreateDirectory(Path.GetDirectoryName(path));

            using (var fileStream = File.Open(path, FileMode.CreateNew))
            {
                await pdbStream.CopyToAsync(fileStream, DefaultCopyBufferSize, cancellationToken);
            }
        }

        public Task<Stream> GetPortablePdbContentStreamOrNullAsync(string filename, string key)
        {
            Stream result = null;
            try
            {
                result = File.Open(
                    GetPathForKey(filename, key),
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.Read);
            }
            catch (DirectoryNotFoundException)
            {
            }
            catch (FileNotFoundException)
            {
            }

            return Task.FromResult(result);
        }

        private string GetPathForKey(string filename, string key)
        {
            // Ensure the filename doesn't try to escape out of the current directory.
            var tempPath = Path.GetDirectoryName(Path.GetTempPath());
            var expandedPath = Path.GetDirectoryName(Path.Combine(tempPath, filename));
            
            if (expandedPath != tempPath)
            {
                throw new ArgumentException(nameof(filename));
            }

            if (!key.All(char.IsLetterOrDigit))
            {
                throw new ArgumentException(nameof(key));
            }

            return Path.Combine(
                _storePath,
                filename.ToLowerInvariant(),
                key.ToLowerInvariant());
        }
    }
}
