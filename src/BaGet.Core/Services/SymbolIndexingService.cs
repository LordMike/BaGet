using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NuGet.Packaging;

namespace BaGet.Core.Services
{
    // Based off: https://github.com/NuGet/NuGetGallery/blob/master/src/NuGetGallery/Services/SymbolPackageUploadService.cs
    // Based off: https://github.com/NuGet/NuGet.Jobs/blob/master/src/Validation.Symbols/SymbolsValidatorService.cs#L44
    public class SymbolIndexingService : ISymbolIndexingService
    {
        private static readonly HashSet<string> ValidSymbolPackageContentExtensions = new HashSet<string>
        {
            ".pdb",
            ".nuspec",
            ".xml",
            ".psmdcp",
            ".rels",
            ".p7s"
        };

        private readonly IPackageService _packages;
        private readonly ISymbolStorageService _storage;
        private readonly ILogger<SymbolIndexingService> _logger;

        public SymbolIndexingService(
            IPackageService packages,
            ISymbolStorageService storage,
            ILogger<SymbolIndexingService> logger)
        {
            _packages = packages ?? throw new ArgumentNullException(nameof(packages));
            _storage = storage ?? throw new ArgumentNullException(nameof(storage));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<SymbolIndexingResult> IndexAsync(Stream stream, CancellationToken cancellationToken)
        {
            try
            {
                using (var symbolPackage = new PackageArchiveReader(stream, leaveStreamOpen: true))
                {
                    // Ensure a corresponding NuGet package exists.
                    var packageId = symbolPackage.NuspecReader.GetId();
                    var packageVersion = symbolPackage.NuspecReader.GetVersion();

                    var package = await _packages.FindOrNullAsync(packageId, packageVersion, includeUnlisted: true);
                    if (package == null)
                    {
                        return SymbolIndexingResult.PackageNotFound;
                    }

                    var symbolPackageFiles = (await symbolPackage.GetFilesAsync(cancellationToken)).ToList();
                    if (!AreSymbolFilesValid(symbolPackageFiles))
                    {
                        return SymbolIndexingResult.InvalidSymbolPackage;
                    }

                    // TODO: Validate that all PDBs have a corresponding DLL. See: https://github.com/NuGet/NuGet.Jobs/blob/master/src/Validation.Symbols/SymbolsValidatorService.cs#L170

                    foreach (var symbolFile in symbolPackageFiles)
                    {
                        if (Path.GetExtension(symbolFile) != ".pdb") continue;

                        using (var pdbStream = await symbolPackage.GetStreamAsync(symbolFile, cancellationToken))
                        {
                            var pdbKey = BuildPortablePDBKey(pdbStream, symbolFile);

                            pdbStream.Position = 0;

                            await _storage.SavePortablePdbContentAsync(pdbKey, pdbStream, cancellationToken);
                        }
                    }

                    return SymbolIndexingResult.Success;
                }
            }
            catch
            {
                return SymbolIndexingResult.InvalidSymbolPackage;
            }
        }

        private bool AreSymbolFilesValid(IReadOnlyList<string> entries)
        {
            // TODO: Validate that all PDBs are portable. See: https://github.com/NuGet/NuGetGallery/blob/master/src/NuGetGallery/Services/SymbolPackageService.cs#L174
            bool IsValidSymbolFileInfo(FileInfo file)
            {
                if (string.IsNullOrEmpty(file.Name)) return false;
                if (string.IsNullOrEmpty(file.Extension)) return false;
                if (!ValidSymbolPackageContentExtensions.Contains(file.Extension)) return false;

                return true;
            }

            return entries.Select(e => new FileInfo(e)).All(IsValidSymbolFileInfo);
        }

        private string BuildPortablePDBKey(Stream pdbStream, string pdbPath)
        {
            // See: https://github.com/dotnet/symstore/blob/master/docs/specs/SSQP_Key_Conventions.md#portable-pdb-signature
            using (var pdbReaderProvider = MetadataReaderProvider.FromPortablePdbStream(pdbStream, MetadataStreamOptions.LeaveOpen))
            {
                var pdbReader = pdbReaderProvider.GetMetadataReader();

                var pdbSignature = new BlobContentId(pdbReader.DebugMetadataHeader.Id).Guid;
                var pdbFileName = Path.GetFileName(pdbPath).ToLowerInvariant();
                var pdbAge = "FFFFFFFF";

                return $"{pdbFileName}/{pdbSignature.ToString("N")}{pdbAge}/{pdbFileName}";
            }
        }
    }
}
