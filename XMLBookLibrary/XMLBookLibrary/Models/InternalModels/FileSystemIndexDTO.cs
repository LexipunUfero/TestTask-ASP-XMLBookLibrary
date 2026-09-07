namespace XMLBookLibrary.Models.InternalModels;

internal class FileSystemIndexDTO
{
    internal List<FileDataContainer> Authors { get; set; } = new List<FileDataContainer>();
    internal List<FileDataContainer> Books { get; set; } = new List<FileDataContainer>();
    
    internal int LastBookFileIndex { get; set; }
    internal int LastAuthorFileIndex { get; set; }
    internal int LastBookId { get; set; }
}