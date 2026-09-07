using System.Linq.Expressions;
using System.Text;
using XMLBookLibrary.Models;

namespace XMLBookLibrary.XmlDataManagement;
/*
 * <objectName>
 * <property1> value </property1>
 */

internal class XmlFilesFactory
{
    private Dictionary<Type, XmlSerilizer> serilizers = new();
    private Dictionary<Type, XmlDeserializer> deserilizers = new();


    internal void Serialize<T>(List<T> datas, string path)
    {
        var type = typeof(T);
        if (!serilizers.ContainsKey(type))
        {
            serilizers.Add(type, new XmlSerilizer(type));
        }

        var serilizer = serilizers[type];
        var dataToWrite = serilizer.Get(datas);

        using var writer = new StreamWriter(path);

        writer.Write(dataToWrite);
    }

    internal List<T> Split<T>(string originFile, string newFile, string path, Predicate<T> predicate)
        where T : class, new()
    {
        var type = typeof(T);
        var data = deserilizers[type].Deserialize<T>(Path.Combine(path, originFile));

        var index = data.FindIndex(predicate);


        var dataToWrite = serilizers[type].Get(data.Take(index + 1).ToList());

        using (var writer = new StreamWriter(Path.Combine(path, originFile)))
        {
            writer.Write(dataToWrite);
        }

        File.Create(Path.Combine(path, newFile)).Close();
        dataToWrite = serilizers[type].Get(data.Skip(index + 1).ToList());
        using (var writer = new StreamWriter(Path.Combine(path, newFile)))
        {
            writer.Write(dataToWrite);
        }

        return data.Skip(index + 1).ToList();
    }

    internal List<T> Deserialize<T>(string path)
        where T : class, new()
    {
        var info = new FileInfo(path);
        if (info.Length == 0)
        {
            return new List<T>();
        }

        if (!deserilizers.ContainsKey(typeof(T)))
        {
            deserilizers.Add(typeof(T), new XmlDeserializer(typeof(T)));
        }

        var text = "";
        using (var reader = new StreamReader(path))
        {
            text = reader.ReadToEnd();
        }

        return deserilizers[typeof(T)].Deserialize<T>(text);
    }

    public void InsertLast<T>(string path, T book)
    {
        if (!serilizers.ContainsKey(typeof(T)))
        {
            serilizers.Add(typeof(T), new XmlSerilizer(typeof(T)));
        }

        string closingTag = "</" + typeof(T).Name + "s>";
        var newRecord = serilizers[typeof(T)].Get(book);
        newRecord += closingTag;
        var bytesToWrite = Encoding.UTF8.GetBytes(newRecord.ToString());

        var info = new FileInfo(path);

        if (info.Length == 0)
        {
            Serialize(new List<T>() { book }, path);
            return;
        }

        using (var stream = new FileStream(path, FileMode.Open, FileAccess.ReadWrite))
        {
            stream.Seek(-Encoding.UTF8.GetBytes(closingTag).Length, SeekOrigin.End);
            stream.Write(bytesToWrite, 0, bytesToWrite.Length);
        }
    }
}