using System;
using System.IO;
using System.Text;
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

        public async Task SavePortablePdbContentAsync(string key, Stream pdbStream, CancellationToken cancellationToken)
        {
            var path = GetPathForKey(key);
            using (var fileStream = File.Open(path, FileMode.CreateNew))
            {
                await pdbStream.CopyToAsync(fileStream, DefaultCopyBufferSize, cancellationToken);
            }
        }

        public Task<Stream> GetPortablePdbContentStreamAsync(string key)
        {
            var path = GetPathForKey(key);
            var result = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read);

            return Task.FromResult((Stream)result);
        }

        private string GetPathForKey(string key)
        {
            string fileName;
            var bytes = Encoding.UTF8.GetBytes(key);
            using (var hash = System.Security.Cryptography.SHA512.Create())
            {
                var hashedBytes = hash.ComputeHash(bytes);

                fileName = BitConverter.ToString(hashedBytes).Replace("-", "");
            }

            return Path.Combine(_storePath, fileName);
        }
    }
}
