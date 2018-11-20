using System.IO;
using System.Threading;
using System.Threading.Tasks;
using BaGet.Core.Entities;
using NuGet.Versioning;

namespace BaGet.Core.Services
{
    /// <summary>
    /// Stores packages' content. Packages' state are stored by the
    /// <see cref="IPackageService"/>.
    /// </summary>
    public interface ISymbolStorageService
    {
        /// <summary>
        /// Persist a portable PDB's content to storage. This operation MUST fail if a PDB
        /// with the same key but different content has already been stored.
        /// </summary>
        /// <param name="key">The portable PDB's SSQP key.</param>
        /// <param name="pdbStream">The PDB's content stream.</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task SavePortablePdbContentAsync(
            string key,
            Stream pdbStream,
            CancellationToken cancellationToken);

        /// <summary>
        /// Retrieve a portable PDB's content stream.
        /// </summary>
        /// <param name="key">The portable PDB's SSQP key.</param>
        /// <returns>The portable PDB's stream.</returns>
        Task<Stream> GetPortablePdbContentStreamAsync(string key);
    }
}
