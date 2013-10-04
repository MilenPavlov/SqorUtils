using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Sqor.Utils.Generics;
using Sqor.Utils.Strings;
using Sqor.Utils.Json;
using System.Globalization;

namespace Sqor.Utils.Json
{
	public class JsonObjectSerializer : IJsonSerializer
	{
//        private static readonly DateTime unixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        internal const string dateFormat = "yyyy-MM-dd HH:mm:ss";

		public JsonValue Parse(string input)
		{
			return JsonValue.Parse(input);
		}		

		public T Parse<T>(string input)
		{
			var jsonValue = JsonValue.Parse(input);
			return (T)ConvertJsonObjectToType(jsonValue, typeof(T));
		}

		public string Serialize(object o)
		{
            using (var s = new StringWriter()) 
            {
                ConvertObjectToJsonValue(o).Save(s);
                return s.ToString();
            }
		}
        
        public static DateTime? ParseDate(string s)
        {
            if (s == "0000-00-00T00:00:00Z")
                return null;
            if (s.EndsWith("Z"))
                return DateTime.ParseExact(s, @"yyyy-MM-dd\THH:mm:ss\Z", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal);
            else
                return DateTime.ParseExact(s, dateFormat, null);
        }

		private object ConvertJsonObjectToType(JsonValue graph, Type type) 
		{
			if (type == typeof(string)) 
			{
				return (string)graph;
			}
            else if (type.IsEnum)
            {
                return Enum.Parse(type, (string)graph);
            }
			else if (type == typeof(bool)) 
			{
				return (bool)graph;
			}
			else if (type == typeof(int))
			{
				return (int)graph;
			}
			else if (type == typeof(long)) 
			{
				return (long)graph;
			}
			else if (type == typeof(float)) 
			{
				return (float)graph;
			}
			else if (type == typeof(double))
			{
				return (double)graph;
			}
            else if (type == typeof(decimal)) 
            {
                return (decimal)graph;
            }
            else if (type == typeof(DateTime) || type == typeof(DateTime?))
            {
                var s = (string)graph;
                if (s == null)
                    return null;
                return ParseDate(s);
            }
//            else if (type == typeof(DateTime) || type == typeof(DateTime?))
//            {
//                var s = (string)graph;
//                if (s == null)
//                    return null;
//
//                s = s.ChopStart("/Date(");
//                s = s.ChopEnd(")/");
//                var seconds = (int)(long.Parse(s) / 1000);
//                var date = unixEpoch.AddSeconds(seconds);
//                return date;
//            }
            else if (type.IsGenericDictionary())
            {
                var dictionary = (IDictionary)Activator.CreateInstance(type);
                var jsonObject = (JsonObject)graph;
                foreach (var item in jsonObject)
                {
                    dictionary[item.Key] = item.Value;
                }
                return dictionary;
            }
			else if (type.IsArray)
			{
                if (graph is JsonArray)
                {
                    var array = (JsonArray)graph;
                    var result = Array.CreateInstance(type.GetElementType(), array.Count);
                    for (int i = 0; i < result.Length; i++)
                    {
                        var jsonValue = array[i];
                        result.SetValue(ConvertJsonObjectToType(jsonValue, type.GetElementType()), i);
                    }
                    return result;
                }
                else if (graph is JsonPrimitive || ((JsonPrimitive)graph).Value == null)
                {
                    return null;
                }
                else
                {
                    throw new InvalidOperationException("Expected an array or the null value, but found: " + graph);
                }
			}
			else if (type.IsGenericList())
			{
				var array = (JsonArray)graph;
				var elementType = type.GetListElementType();
				var listType = typeof(List<>).MakeGenericType(elementType);
				var list = (IList)Activator.CreateInstance(listType);
				for (int i = 0; i < array.Count; i++)
				{
					var jsonValue = array[i];
					list.Add(ConvertJsonObjectToType(jsonValue, elementType));
				}
				return list;
			}
			else 
			{
				var result = Activator.CreateInstance(type);
				var jsonObject = (JsonObject)graph;
				foreach (var property in type.GetProperties().Where(x => JsonAttribute.IsSerialized(x))) 
				{
                    try 
                    {
                        var keyName = JsonAttribute.GetKey(property);
                        var jsonValue = jsonObject[keyName];
                        if (jsonValue != null)
                        {
                            var value = ConvertJsonObjectToType(jsonValue, property.PropertyType);
                            property.SetValue(result, value, null);
                        }
                    }
                    catch (Exception e) 
                    {
                        throw new InvalidOperationException("Error deserializing property " + property.Name, e);
                    }
				}
				return result;
			}
		}

		private JsonValue ConvertObjectToJsonValue(object graph) 
		{
            if (graph == null)
            {
                return new JsonPrimitive();
            }
            
			var type = graph.GetType();
            if (graph is JsonValue)
            {
                return (JsonValue)graph;
            }
			else if (graph is string)
			{
				return (string)graph;
			}
            else if (graph is Enum)
            {
                return graph.ToString();
            }
			else if (type == typeof(bool)) 
			{
				return (bool)graph;
			}
			else if (type == typeof(int)) 
			{
				return (int)graph;
			}
			else if (type == typeof(long)) 
			{
				return (long)graph;
			}
			else if (type == typeof(float)) 
			{
				return (float)graph;
			}
			else if (type == typeof(double)) 
			{
				return (double)graph;
			}
            else if (type == typeof(decimal))
            {
                return (decimal)graph;
            }
            else if (type == typeof(DateTime))
            {
                var date = (DateTime)graph;
                var s = date.ToString(dateFormat);
                return s;
//                var seconds = (int)(date - unixEpoch).TotalSeconds;
//                var s = "/Date(" + seconds + ")/";
//                return s;
            }
			else if (type.IsArray) 
			{
				var array = (Array)graph;
				var elements = new List<JsonValue>();

				for (int i = 0; i < array.Length; i++)
				{
					var value = array.GetValue(i);
					var jsonValue = ConvertObjectToJsonValue(value);
					elements.Add(jsonValue);
				}

				var result = new JsonArray(elements);
				return result;
			}
			else if (type.IsGenericList())
			{
				var list = (IList)graph;
				var elements = new List<JsonValue>();
				
				foreach (var value in list)
				{
				    var jsonValue = ConvertObjectToJsonValue(value);
				    elements.Add(jsonValue);
				}
				
				var result = new JsonArray(elements);
				return result;
			}
            else if (graph is IDictionary)
            {
                var dict = (IDictionary)graph;
                var values = new List<KeyValuePair<string, JsonValue>>();
                
                foreach (string key in dict.Keys)
                {
                    try 
                    {
                        var value = dict[key];
                        var jsonValue = ConvertObjectToJsonValue(value);
                        values.Add(new KeyValuePair<string, JsonValue>(key, jsonValue));
                    }
                    catch (Exception e)
                    {
                        throw new InvalidOperationException("Error serializing property " + key, e);
                    }
                }
                
                var result = new JsonObject(values);
                return result;
            }
			else 
			{
				var values = new List<KeyValuePair<string, JsonValue>>();

				foreach (var property in type.GetProperties().Where(x => JsonAttribute.IsSerialized(x)))
				{
                    try 
                    {
                        var value = property.GetValue(graph, null);
                        var jsonValue = ConvertObjectToJsonValue(value);
                        var keyName = JsonAttribute.GetKey(property);
                        values.Add(new KeyValuePair<string, JsonValue>(keyName, jsonValue));
                    }
                    catch (Exception e)
                    {
                        throw new InvalidOperationException("Error serializing property " + property.Name, e);
                    }
				}

				var result = new JsonObject(values);
				return result;
			}
		}
	}
}
