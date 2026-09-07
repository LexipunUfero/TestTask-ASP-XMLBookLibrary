using System.Runtime.Serialization;
using System.Text.Json;
using System.Timers;
using XMLBookLibrary.Configs;
using XMLBookLibrary.Models;
using XMLBookLibrary.Models.Constants;
using XMLBookLibrary.Models.InternalModels;
using XMLBookLibrary.XmlDataManagement.FileOrganization.FileTree;

namespace XMLBookLibrary.XmlDataManagement;

public class XmlBooksLibrary : IDisposable
{
    private Dictionary<int, string> cachedBooks = new Dictionary<int, string>();

    private FileConfigs fileConfigs = new();
    private XmlFilesFactory filesFactory = new();
    private Configurations configs { get; set; }
    private FileTree bookLibrary;
    private FileTree authorLibrary;

    private int lastBookIndex = 0;
    private int lastAuthorIndex = 0;
    private int lastBookId = 0;

    public XmlBooksLibrary(Configurations? configs = null)
    {
        configs ??= new Configurations();

        configs.PathToSave ??= Path.Combine(Directory.GetCurrentDirectory(), "library");

        Directory.CreateDirectory(Path.Combine(configs.PathToSave, fileConfigs.authorDirectory));
        Directory.CreateDirectory(Path.Combine(configs.PathToSave, fileConfigs.bookDirectory));

        configs.FileSizeMb ??= 5;
        configs.CachedBooksLimit ??= 200;

        this.configs = configs;

        var indexFile = Path.Combine(configs.PathToSave, fileConfigs.indexFile);
        if (!File.Exists(indexFile))
        {
            File.Create(Path.Combine(configs.PathToSave, fileConfigs.indexFile)).Close();
            return;
        }

        if (!File.Exists(indexFile))
        {
            File.Create(indexFile).Close();
        }

        FileSystemIndexDTO initialData = null;
        using (var reader = new StreamReader(indexFile))
        {
            var data = reader.ReadToEnd();

            if (data.Length > 0)
            {
                initialData = JsonSerializer.Deserialize<FileSystemIndexDTO>(data);
            }
            else
            {
                initialData = new FileSystemIndexDTO();
            }
        }

        if (initialData.Books.Count > 0)
        {
            bookLibrary = new FileTree(0, initialData.Books[0].From[0], initialData.Books[0].File);
            if (!File.Exists(Path.Combine(Path.Combine(configs.PathToSave, fileConfigs.bookDirectory),
                    bookLibrary.FilePath)))
            {
                File.Create(Path.Combine(Path.Combine(configs.PathToSave, fileConfigs.bookDirectory),
                    bookLibrary.FilePath)).Close();
            }

            foreach (var book in initialData.Books)
            {
                bookLibrary.Add(book);
            }
        }

        if (initialData.Authors.Count > 0)
        {
            authorLibrary = new FileTree(0, initialData.Authors[0].From[0], initialData.Authors[0].File);
            if (!File.Exists(Path.Combine(Path.Combine(configs.PathToSave, fileConfigs.authorDirectory),
                    bookLibrary.FilePath)))
            {
                File.Create(Path.Combine(Path.Combine(configs.PathToSave, fileConfigs.authorDirectory),
                    bookLibrary.FilePath)).Close();
            }

            foreach (var author in initialData.Authors)
            {
                authorLibrary.Add(author);
            }
        }


        lastBookIndex = initialData.LastBookFileIndex;
        lastAuthorIndex = initialData.LastAuthorFileIndex;
        lastBookId = initialData.LastBookId;
    }

    private string InsertIntoNodes(FileTree originalTree, string title, string path, ref int lastIndex)
    {
        var node = originalTree.Add(title);
        var fileInfo = new FileInfo(Path.Combine(path, node.FilePath));
        var fileSize = fileInfo.Length / ByteSizes.MB;
        var isLastFile = lastIndex == int.Parse(node.FilePath);

        switch (isLastFile)
        {
            case true when fileSize > configs.FileSizeMb:
                ++lastIndex;
                node.SetFilePath(lastIndex.ToString());
                break;
            case false when fileSize > configs.FileSizeMb * 1.6:
                ++lastIndex;
                var oldFile = node.FilePath;
                var splitNode = node.Split(lastIndex.ToString());

                var name = string.Concat(splitNode.GetName());
                if (originalTree == bookLibrary)
                {
                    var movedBooks = filesFactory.Split<Book>(oldFile, lastIndex.ToString(), path,
                        (source) => source.Title.Equals(name));

                    foreach (var authorData in movedBooks.GroupBy(el => el.Author).OrderBy(el => el.Key))
                    {
                        var authorFileName = authorLibrary.GetFile(authorData.Key);

                        var authorsPath = Path.Combine(configs.PathToSave, fileConfigs.authorDirectory, authorFileName);
                        var authors = filesFactory.Deserialize<AuthorData>(authorsPath);
                        var targetAuthor = authors.First(el => el.Name.Equals(authorData.Key));

                        foreach (var book in authorData)
                        {
                            var authorBook = targetAuthor.Books.First(el => el.Id == book.Id);

                            authorBook.FilePath = lastIndex.ToString();
                        }

                        filesFactory.Serialize(authors, authorsPath);
                    }
                }
                else
                {
                    filesFactory.Split<AuthorData>(oldFile, lastIndex.ToString(), path,
                        (source) => source.Name.Equals(name));
                }

                break;
        }

        return node.FilePath;
    }

    public void AddBook(Book book)
    {
        book.Id = ++lastBookId;
        if (cachedBooks.Count > configs.CachedBooksLimit)
        {
            cachedBooks.Remove(cachedBooks.First().Key);
        }

        if (bookLibrary is null)
        {
            bookLibrary = new FileTree(0, book.Title[0], "0");
            if (!File.Exists(Path.Combine(Path.Combine(configs.PathToSave, fileConfigs.bookDirectory),
                    bookLibrary.FilePath)))
            {
                File.Create(Path.Combine(Path.Combine(configs.PathToSave, fileConfigs.bookDirectory),
                    bookLibrary.FilePath)).Close();
            }
        }

        if (authorLibrary is null)
        {
            authorLibrary = new FileTree(0, book.Author[0], "0");
            if (!File.Exists(Path.Combine(Path.Combine(configs.PathToSave, fileConfigs.authorDirectory),
                    bookLibrary.FilePath)))
            {
                File.Create(Path.Combine(Path.Combine(configs.PathToSave, fileConfigs.authorDirectory),
                    bookLibrary.FilePath)).Close();
            }
        }

        var bookFileName = InsertIntoNodes(bookLibrary, book.Title,
            Path.Combine(configs.PathToSave, fileConfigs.bookDirectory),
            ref lastBookIndex);
        var authorFileName = InsertIntoNodes(authorLibrary, book.Author,
            Path.Combine(configs.PathToSave, fileConfigs.authorDirectory),
            ref lastAuthorIndex);

        bookLibrary = bookLibrary.GetInitial();
        authorLibrary = authorLibrary.GetInitial();

        cachedBooks.Add(book.Id, bookFileName);
        filesFactory.InsertLast(Path.Combine(configs.PathToSave, fileConfigs.bookDirectory, bookFileName), book);
        var authorsPath = Path.Combine(configs.PathToSave, fileConfigs.authorDirectory, authorFileName);
        var authors = filesFactory.Deserialize<AuthorData>(authorsPath);

        var targetAuthor = authors.FirstOrDefault(el => el.Name.Equals(book.Author));

        if (targetAuthor is null)
        {
            targetAuthor ??= new AuthorData()
            {
                Name = book.Author,
            };

            authors.Add(targetAuthor);
        }

        targetAuthor.Books.Add(new AuthorBookData()
        {
            FilePath = bookFileName,
            Id = book.Id,
        });

        filesFactory.Serialize(authors, authorsPath);
    }

    public void UpdateBook(Book book)
    {
        if (cachedBooks.ContainsKey(book.Id))
        {
            var books = filesFactory.Deserialize<Book>(Path.Combine(configs.PathToSave, fileConfigs.bookDirectory,
                cachedBooks[book.Id]));

            var targetBook = books.FirstOrDefault(el => el.Id == book.Id);

            if (targetBook != null)
            {
                targetBook.PageCount = book.PageCount;

                if (book.Title != targetBook.Title)
                {
                    var orderedBooks = books.OrderBy(el => el.Title).ToList();
                    if (book.Title.CompareTo(orderedBooks.First().Title) > 0
                        && book.Title.CompareTo(orderedBooks.Last().Title) < 0)
                    {
                        targetBook.Title = book.Title;
                    }
                }

                if (book.Author != targetBook.Author)
                {
                    var pathToAuthor = authorLibrary.GetFile(targetBook.Author);

                    var authors = filesFactory.Deserialize<AuthorData>(Path.Combine(configs.PathToSave,
                        fileConfigs.authorDirectory,
                        pathToAuthor));

                    var orderedauthors = authors.OrderBy(el => el.Name).ToList();

                    var targetAuthor = authors.FirstOrDefault(el => el.Name == targetBook.Author);
                    bool isDraft = false;
                    if (targetAuthor != null)
                    {
                        targetAuthor.Books.Remove(targetAuthor.Books.First(el => el.Id == book.Id));

                        if (targetAuthor.Books.Count == 0)
                        {
                            authors.Remove(targetAuthor);
                            isDraft = true;
                        }
                    }

                    if (book.Author.CompareTo(orderedauthors.First().Name) > 0
                        && book.Author.CompareTo(orderedauthors.Last().Name) < 0)
                    {
                        authors.Add(new AuthorData()
                        {
                            Name = book.Author,
                            Books = new List<AuthorBookData>()
                            {
                                new AuthorBookData()
                                {
                                    Id = book.Id,
                                    FilePath = cachedBooks[book.Id]
                                }
                            },
                        });

                        targetBook.Author = book.Author;
                        isDraft = true;
                    }

                    if (isDraft)
                    {
                        filesFactory.Serialize(authors,
                            Path.Combine(configs.PathToSave, fileConfigs.authorDirectory, pathToAuthor));
                    }
                }
            }
        }

        var files = Directory.GetFiles(Path.Combine(configs.PathToSave, fileConfigs.bookDirectory));

        foreach (var file in files)
        {
            var books = filesFactory.Deserialize<Book>(file);

            var targetBook = books.FirstOrDefault(el => el.Id == book.Id);

            if (targetBook is null)
            {
                var orderedBooks = books.OrderBy(el => el.Title).ToList();
                if (book.Title.CompareTo(orderedBooks.First().Title) > 0
                    && book.Title.CompareTo(orderedBooks.Last().Title) < 0)
                {
                    filesFactory.InsertLast(file, book);
                    break;
                }

                continue;
            }

            targetBook.PageCount = book.PageCount;

            if (targetBook.Title != book.Title)
            {
                var orderedBooks = books.OrderBy(el => el.Title).ToList();
                if (book.Title.CompareTo(orderedBooks.First().Title) > 0
                    && book.Title.CompareTo(orderedBooks.Last().Title) < 0)
                {
                    targetBook.Title = book.Title;
                    targetBook.Author = book.Author;
                    filesFactory.Serialize(books, file);
                    break;
                }

                books.Remove(targetBook);
                filesFactory.Serialize(books, file);
            }
        }
    }

    public List<Book> GetBooks(int? from = null, int? to = null)
    {
        var fileData = authorLibrary?.GetData() ?? new List<FileDataContainer>();
        List<AuthorData> selectedAuthors = new();

        foreach (var item in fileData)
        {
            var authors =
                filesFactory.Deserialize<AuthorData>(Path.Combine(configs.PathToSave, fileConfigs.authorDirectory,
                    item.File));

            if (from.HasValue)
            {
                if (from > authors.Count)
                {
                    from = -authors.Count;
                    continue;
                }

                selectedAuthors.AddRange(authors.Skip(from.Value));
                from = null;
                continue;
            }

            if (to.HasValue)
            {
                if (to < authors.Count)
                {
                    selectedAuthors.AddRange(authors.Take(to.Value));
                    break;
                }

                to -= authors.Count;
            }

            selectedAuthors.AddRange(authors);
        }

        var selectedBooks = new List<Book>();
        foreach (var files in selectedAuthors.SelectMany(el => el.Books).GroupBy(el => el.FilePath))
        {
            var books = filesFactory.Deserialize<Book>(Path.Combine(configs.PathToSave, fileConfigs.bookDirectory,
                files.Key));

            selectedBooks.AddRange(books.Where(el => files.Any(fileToBook => fileToBook.Id == el.Id)));

            if (books.Count + cachedBooks.Count - 1 > configs.CachedBooksLimit)
            {
                for (int i = 0; i < books.Count; ++i)
                {
                    if (cachedBooks.Count == 0)
                    {
                        break;
                    }

                    cachedBooks.Remove(cachedBooks.First().Key);
                }
            }

            foreach (var book in books)
            {
                cachedBooks[book.Id] = files.Key;
            }
        }

        var result = selectedBooks.OrderBy(el => el.Title).ThenBy(el => el.Author).ToList();


        return result;
    }

    public List<Book> GetBooks(string subTitile, int? from = null, int? to = null)
    {
        var pathToBook = bookLibrary.GetFile(subTitile);

        var books = filesFactory.Deserialize<Book>(Path.Combine(configs.PathToSave, fileConfigs.bookDirectory,
            pathToBook));

        if (books.Count + cachedBooks.Count - 1 > configs.CachedBooksLimit)
        {
            for (int i = 0; i < books.Count; ++i)
            {
                if (cachedBooks.Count == 0)
                {
                    break;
                }

                cachedBooks.Remove(cachedBooks.First().Key);
            }
        }

        foreach (var book in books)
        {
            cachedBooks[book.Id] = pathToBook;
        }

        return books.Where(el => el.Title.Contains(subTitile)).Skip(from ?? 0).Take(to ?? books.Count).ToList();
    }

    public void Load(string path)
    {
        var data = filesFactory.Deserialize<Book>(path);

        foreach (var book in data)
        {
            AddBook(book);
        }
    }


    public void Save(string path)
    {
        var books = GetBooks();

        filesFactory.Serialize(books, path);
    }

    ~XmlBooksLibrary()
    {
        ReleaseUnmanagedResources();
    }

    private void ReleaseUnmanagedResources()
    {
        var indexFile = Path.Combine(configs.PathToSave!, fileConfigs.indexFile);
        var data = new FileSystemIndexDTO
        {
            LastAuthorFileIndex = lastAuthorIndex,
            LastBookFileIndex = lastBookIndex,
            LastBookId = lastBookId,
            Authors = authorLibrary.GetData(),
            Books = bookLibrary.GetData(),
        };

        using var reader = new StreamWriter(indexFile);
        reader.Write(JsonSerializer.Serialize(data));
    }

    public void Dispose()
    {
        ReleaseUnmanagedResources();
        GC.SuppressFinalize(this);
    }
}