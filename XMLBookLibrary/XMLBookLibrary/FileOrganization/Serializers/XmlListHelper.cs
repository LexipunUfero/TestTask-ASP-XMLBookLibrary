using System.Xml;

namespace XMLBookLibrary.XmlDataManagement;

public static class XmlListHelper
{
    
    public static TElement DeserializeOneElement<TElement>(XmlReader reader)
        where TElement : new()
    {
        var obj = new TElement();

        if (reader.IsEmptyElement)
        {
            reader.Read();
            return obj;
        }

        reader.ReadStartElement();

        while (reader.NodeType != XmlNodeType.EndElement)
        {
            if (reader.NodeType == XmlNodeType.Element)
            {
                string fieldName = reader.Name;
                var property = typeof(TElement).GetProperty(fieldName);

                if (property != null)
                {
                    string value = reader.ReadElementContentAsString();
                    property.SetValue(obj, Convert.ChangeType(value, property.PropertyType));
                }
                else
                {
                    reader.Skip();
                }
            }
            else
            {
                reader.Read();
            }
        }

        reader.ReadEndElement();
        return obj;
    }
    
    public static List<TElement> ReadList<TElement>(
        XmlReader reader)
        where TElement : new()
    {
        var result = new List<TElement>();

        string containerTag = reader.Name;

        if (reader.IsEmptyElement)
        {
            reader.Read();
            return result;
        }

        reader.ReadStartElement();

        while (reader.NodeType != XmlNodeType.EndElement)
        {
            if (reader.NodeType == XmlNodeType.Element && reader.Name == "Item")
            {
                result.Add(DeserializeOneElement<TElement>(reader));
            }
            else
            {
                reader.Skip();
            }
        }

        reader.ReadEndElement();
        return result;
    }
}