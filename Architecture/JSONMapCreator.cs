using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Json;
using System.Reflection;
using Digger.Mobs;

namespace Digger.Architecture
{
	/// <summary>
	/// <para>Class that enables you to create maps from JsonFile.</para>
	/// <para>You capable of specifying Object coordinates, type and other properties,
	/// determined by the object you want to create (ex: Turret have field "direction" and "frequency").</para>
	/// <para>You can also create blocks of similar type and specify their properties like this:
	/// "chunk" (any case) : { "x":_x, "y":_y, "width":_width, "height":_height, "type":_type, properties of an object}.
	/// There values that starts with "_" are actual values.
	/// "type" - is a ClassName for an object you want to create</para>
	///
	/// <para>Map file is a common Json file.
	/// It should contain at least 2 fields: width and height of the map.
	/// You can also specify, whether the map have wall on the edge by value of "walls" : true of false.</para>
	/// </summary>
	public static class JsonMapCreator
	{
		public static GameObject[,] CreateMap(string mapFile)
		{
			var parser = new JsonTextParser();
			var mapCode = File.ReadAllText(mapFile);
			var obj = parser.Parse(mapCode);
			var mainCollection = (JsonCollection) obj;
			var width = -1;
			var height = -1;
			var walls = true;

			var gameObjects = new List<PreparedObject>();
			var chunks = new List<Chunk>();
			
			foreach (var jsonObj in mainCollection)
			{
				if (jsonObj is JsonNumericValue n)
				{
					if (n.Name == "width")
					{
						width = (int) n.Value;
					}

					if (n.Name == "height")
					{
						height = (int) n.Value;
					}
				}

				if (jsonObj is JsonLiteralValue b)
				{
					if (b.Name == "walls")
					{
						switch (b.Value)
						{
							case JsonAllowedLiteralValues.False:
								walls = false;
								break;
							case JsonAllowedLiteralValues.True:
								walls = true;
								break;
							default:
								throw new ArgumentOutOfRangeException();
						}
					}
				}

				if (jsonObj is JsonObjectCollection o)
				{
					var name = "";
					if (o.Name.ToLower() == "chunk")
					{
						chunks.Add(new Chunk(o));
					}
					foreach (var field in o)
					{
						if (field is JsonStringValue s)
						{
							if (s.Name == "type")
							{
								name = s.Value;
							}
						}
					}

					if (name != "")
					{
						var type = Assembly
							.GetExecutingAssembly()
							.GetTypes()
							.FirstOrDefault(z => z.Name == name);

						if (type == null)
						{
							throw new Exception($"Can't find type '{name}'");
						}

						var method = type.GetMethods().FirstOrDefault(m => { return m.Name == "FromJsonObject"; });
						if (method != null)
						{
							gameObjects.Add(method.Invoke(null, new object[] {o}) as PreparedObject);
						}
						else
						{
							gameObjects.Add(GameObject.FromJsonObject(o));
						}
					}
					else
					{
						o.Add(new JsonStringValue("type","Terrain"));
						gameObjects.Add(GameObject.FromJsonObject(o));
					}
				}
			}

			if (width == -1 || height == -1)
			{
				throw new ArgumentException();
			}

			var map = new GameObject[height,width];
			
			if (walls)
			{
				for (var i = 0; i < width; i++)
				{
					map[0,i] = new Wall();
					map[height-1,i] = new Wall();
				}
				for (var i = 0; i < height; i++)
				{
					map[i,0] = new Wall();
					map[i,width-1] = new Wall();
				}
			}

			for (int i = 0; i < chunks.Count; i++)
			{
				var chunk = chunks[i];
				map = chunk.Print(map);
			}
			foreach (var prepObj in gameObjects)
			{
				map[prepObj._x, prepObj._y] = prepObj._obj;
			}

			return map;
		}
	}
}