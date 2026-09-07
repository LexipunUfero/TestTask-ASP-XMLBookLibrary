using XMLBookLibrary.Models;
using XMLBookLibrary.XmlDataManagement;

namespace Tests;

public class OperationTests
{
    private readonly List<Book> testBooks = new List<Book>()
        {
            new Book()
            {
                Author = "Hans Christian Andersen",
                PageCount = 32,
                Title = "The Little Mermaid"
            },
            new Book()
            {
                Author = "Hans Christian Andersen",
                PageCount = 28,
                Title = "The Ugly Duckling"
            },
            new Book()
            {
                Author = "Hans Christian Andersen",
                PageCount = 45,
                Title = "The Snow Queen"
            },
            new Book()
            {
                Author = "C.S. Lewis",
                PageCount = 206,
                Title = "The Lion, the Witch and the Wardrobe"
            },
            new Book()
            {
                Author = "C.S. Lewis",
                PageCount = 223,
                Title = "Prince Caspian"
            },
            new Book()
            {
                Author = "J.R.R. Tolkien",
                PageCount = 310,
                Title = "The Hobbit"
            },
            new Book()
            {
                Author = "J.R.R. Tolkien",
                PageCount = 423,
                Title = "The Fellowship of the Ring"
            },
            new Book()
            {
                Author = "Lucy Maud Montgomery",
                PageCount = 320,
                Title = "Anne of Green Gables"
            },
            new Book()
            {
                Author = "L. Frank Baum",
                PageCount = 154,
                Title = "The Wonderful Wizard of Oz"
            },
            new Book()
            {
                Author = "Hans Christian Andersen",
                PageCount = 32,
                Title = "The Little Mermaid"
            },
            new Book()
            {
                Author = "Lewis Carroll",
                PageCount = 200,
                Title = "Alice's Adventures in Wonderland"
            },
            new Book()
            {
                Author = "Lewis Carroll",
                PageCount = 224,
                Title = "Through the Looking-Glass"
            },
            new Book()
            {
                Author = "E.B. White",
                PageCount = 184,
                Title = "Charlotte's Web"
            },
            new Book()
            {
                Author = "Rudyard Kipling",
                PageCount = 224,
                Title = "The Jungle Book"
            },
            new Book()
            {
                Author = "J.M. Barrie",
                PageCount = 190,
                Title = "Peter Pan"
            },
        };
    [Fact]
    public void AddCheck()
    {
        var path = "./AddCheck";
        Directory.CreateDirectory(path);
        Directory.Delete(path,true);
        
        var library = new XmlBooksLibrary(new()
        {
            PathToSave = path
        });
        
        
        foreach (var book in testBooks)
        {
            library.AddBook(book);
        }

        var addedBooks = library.GetBooks(null, null);
        
        Assert.Equal(addedBooks.Count, testBooks.Count);
        Assert.True(addedBooks.All(added=> testBooks.FirstOrDefault(el=>el.Id == added.Id && el.Title == added.Title
            && el.Author == added.Author && el.PageCount == added.PageCount) != null));

    }
    
    [Fact]
    public void UpdateCheck()
    {
        var path = "./updateCheck";
        Directory.CreateDirectory(path);
        Directory.Delete(path,true);
        
        var library = new XmlBooksLibrary(new()
        {
            PathToSave = path
        });
        
        
        foreach (var book in testBooks)
        {
            library.AddBook(book);
        }

        var books = library.GetBooks("Anne of", null, null);
        var resultBook = books.First();
        resultBook.Title = "The Fellowship of the Ring2";
        resultBook.Author = "KMap";
        library.UpdateBook(resultBook);
    
       var checkBooks = library.GetBooks("The Fellowship of the Ring", null, null);
       
       Assert.Equal(2, checkBooks.Count);
       
       resultBook= checkBooks.FirstOrDefault(el=>el.Author=="KMap");
       Assert.NotNull(resultBook);
    }
}