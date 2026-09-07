namespace XMLBookLibrary.Models.InternalModels;

internal class AuthorData
{
    public string Name { get; set; }
    public List<AuthorBookData> Books { get; set; } = new List<AuthorBookData>();
}