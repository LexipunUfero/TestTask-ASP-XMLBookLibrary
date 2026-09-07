using System.Linq.Expressions;
using System.Xml;

namespace XMLBookLibrary.XmlDataManagement;

internal class XmlDeserializer
{
    private string name;
    private Dictionary<string, Action<object, XmlReader>> properties = new();

    private bool IsNumericType(Type type)
    {
        type = Nullable.GetUnderlyingType(type) ?? type; // разворачиваем Nullable<T>

        return Type.GetTypeCode(type) switch
        {
            TypeCode.Byte or TypeCode.SByte or
                TypeCode.UInt16 or TypeCode.UInt32 or TypeCode.UInt64 or
                TypeCode.Int16 or TypeCode.Int32 or TypeCode.Int64 or
                TypeCode.Decimal or TypeCode.Double or TypeCode.Single => true,
            _ => false
        };
    }


    internal XmlDeserializer(Type type)
    {
        name = type.Name;

        var properties = type.GetProperties();

        var parameter = Expression.Parameter(typeof(XmlReader), "reader");
        var sourceParameter = Expression.Parameter(typeof(object), "source");
        var readElement = typeof(XmlReader).GetMethod(nameof(XmlReader.ReadElementContentAsString), Type.EmptyTypes);

        foreach (var property in properties)
        {
            var castedSource = Expression.Convert(sourceParameter, type);
            var propertyExpresion = Expression.Property(castedSource, property);
            BinaryExpression setter = null;
            if (property.PropertyType == typeof(string))
            {
                setter = Expression.Assign(propertyExpresion, Expression.Call(parameter, readElement));
            }
            else if (property.PropertyType == typeof(bool)
                     || IsNumericType(property.PropertyType))
            {
                var parseMethod = property.PropertyType.GetMethod("Parse", new[] { typeof(string) });
                var dataToSet = Expression.Call(parseMethod!, Expression.Call(parameter, readElement));
                setter = Expression.Assign(propertyExpresion, dataToSet);
            }
            else if (property.PropertyType.IsGenericType)
            {
                var readListMethod = typeof(XmlListHelper)
                    .GetMethod(nameof(XmlListHelper.ReadList))
                    .MakeGenericMethod(property.PropertyType.GetGenericArguments()[0]);

                var dataToSet = Expression.Call(readListMethod, parameter);
                setter = Expression.Assign(propertyExpresion, dataToSet);
            }
            else
            {
                throw new NotSupportedException($"Property type {property.PropertyType} not supported");
            }

            var lambda = Expression.Lambda<Action<object, XmlReader>>(setter, true, sourceParameter, parameter);
            this.properties.Add(property.Name, lambda.Compile());
        }
    }

    internal List<T> Deserialize<T>(string xml)
        where T : class, new()
    {
        var result = new List<T>();

        using var reader = XmlReader.Create(new StringReader(xml), new XmlReaderSettings() { IgnoreWhitespace = true });
        T? currentObject = null;
        reader.Read();
        while (true)
        {
            if (reader.NodeType == XmlNodeType.Element)
            {
                if (reader.Name == name)
                {
                    currentObject = new T();
                    if (!reader.Read())
                    {
                        break;
                    }

                    continue;
                }

                if (currentObject is null
                    || !properties.ContainsKey(reader.Name))
                {
                    if (!reader.Read())
                    {
                        break;
                    }

                    continue;
                }

                properties[reader.Name](currentObject, reader);
                continue;
            }

            if (reader.NodeType == XmlNodeType.EndElement
                && reader.Name == name)
            {
                result.Add(currentObject!);

                currentObject = null;
            }

            if (!reader.Read())
            {
                break;
            }
        }

        return result;
    }
}