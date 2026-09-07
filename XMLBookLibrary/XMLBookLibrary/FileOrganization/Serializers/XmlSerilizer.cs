using System.Collections;
using System.Linq.Expressions;
using System.Text;
using XMLBookLibrary.Configs;

namespace XMLBookLibrary.XmlDataManagement;

internal class XmlSerilizer
{
    private static XmlFormatter formatter = new();
    private string name;
    private Dictionary<string, Func<object, string>> properties = new();

    private string GetObjectBody(object source)
    {
        StringBuilder result = new();

        foreach (var property in properties)
        {
            result.AppendFormat(formatter.Row, property.Key, property.Value(source));
        }

        return string.Format(formatter.Row, name, result.ToString());
    }

    private LambdaExpression GetInnerBody(Type type)
    {
        var properties = type.GetProperties();

        var parameter = Expression.Parameter(typeof(object), "source");
        var toStringMethod = typeof(object).GetMethod(nameof(object.ToString), Type.EmptyTypes);
        var arrayFormatterMethod =
            typeof(string).GetMethod(nameof(string.Format), new[] { typeof(string), typeof(string) });
        var formatterMethod =
            typeof(string).GetMethod(nameof(string.Format), new[] { typeof(string), typeof(string), typeof(string) });
        var format = Expression.Constant(formatter.Item);
        var rowFormat = Expression.Constant(formatter.Row);
        LambdaExpression resultExpression = null;
        List<Expression> rows = new List<Expression>();
        foreach (var property in properties)
        {
            var nameConstant = Expression.Constant(property.Name);
            var castToT = Expression.Convert(parameter, type);
            var propertyExpression = Expression.Property(castToT, property.Name);
            var convertedResultExpresion = Expression.Call(propertyExpression, toStringMethod);

            var resultCall = Expression.Call(formatterMethod!, rowFormat, nameConstant, convertedResultExpresion);

            rows.Add(resultCall);
        }

        var conctatination =
            typeof(string).GetMethod(nameof(string.Concat), rows.Select(row => typeof(string)).ToArray());
        
        var temp = Expression.Call(conctatination, rows);
        temp = Expression.Call(arrayFormatterMethod!, format, temp);
        resultExpression = Expression.Lambda<Func<object, string>>(temp, parameter);

        return resultExpression;
    }

    internal XmlSerilizer(Type type)
    {
        this.name = type.Name;
        var localProperties = type.GetProperties();

        var parameter = Expression.Parameter(typeof(object), "source");
        var formatterMethod = typeof(string).GetMethod(nameof(string.Format),
            new[] { typeof(string), typeof(string), typeof(string) });
        var castArray = typeof(Enumerable).GetMethod(nameof(Enumerable.Cast)).MakeGenericMethod(typeof(object));
        var selectMethod = typeof(Enumerable).GetMethods()
            .First(m => m.Name == nameof(Enumerable.Select)
                        && m.GetParameters().Length == 2
                        && m.GetParameters()[1].ParameterType.GetGenericTypeDefinition() == typeof(Func<,>))
            .MakeGenericMethod(typeof(object), typeof(string));
        var toArrayMethod = typeof(Enumerable).GetMethod(nameof(Enumerable.ToArray)).MakeGenericMethod(typeof(string));
        var nameConst = Expression.Constant(name);
        var formatterConst = Expression.Constant(formatter.Row);
        var concatMethod = typeof(string).GetMethod(nameof(string.Concat), new[] { typeof(string[]) });
        foreach (var property in localProperties)
        {
            var propertyNameConst = Expression.Constant(property.Name);
            var castToT = Expression.Convert(parameter, type);
            var propertyExpression = Expression.Property(castToT, property.Name);
            MethodCallExpression resultCall = null;
            if (property.PropertyType.IsGenericType)
            {
                var method = Expression.Call(castArray, propertyExpression);
                method = Expression.Call(selectMethod, method,
                    GetInnerBody(property.PropertyType.GetGenericArguments()[0]));
                method = Expression.Call(toArrayMethod, method);
                resultCall = Expression.Call(concatMethod!, method);
            }
            else
            {
                var toStringMethod = property.PropertyType.GetMethod(nameof(object.ToString), Type.EmptyTypes);
                var convertedResultExpresion = Expression.Call(propertyExpression, toStringMethod);
                resultCall = convertedResultExpresion;
            }

            var lambda =
                Expression.Lambda<Func<object, string>>(resultCall, parameter);
            this.properties.Add(property.Name, lambda.Compile());
        }
    }

    internal string Get(object source)
    {
        var result = GetObjectBody(source);

        return result;
    }

    internal StringBuilder Get<T>(List<T> datas)
    {
        StringBuilder result = new();

        result.Append(formatter.initialRow);

        var arrayBody = new StringBuilder();
        foreach (var item in datas)
        {
            arrayBody.Append(GetObjectBody(item));
        }

        result.AppendFormat(formatter.Row, string.Concat(name, "s"), arrayBody.ToString());
        return result;
    }
}