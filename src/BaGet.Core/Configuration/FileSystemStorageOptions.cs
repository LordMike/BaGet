namespace BaGet.Core.Configuration
{
    public class FileSystemStorageOptions : StorageOptions
    {
        /// <summary>
        /// The path to store packages' contents.
        /// </summary>
        public string Path { get; set; }

        /// <summary>
        /// The path to store symbols' contents.
        /// </summary>
        public string SymbolPath { get; set; }
    }
}
