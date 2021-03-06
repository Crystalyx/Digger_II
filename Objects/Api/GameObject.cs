using System;
using System.Linq;
using System.Net.Json;
using System.Reflection;
using Digger.Architecture;
using Digger.Map;

namespace Digger.Objects.Api
{
	public abstract class GameObject
	{
		public abstract CreatureCommand Update(int x, int y);
		public abstract string GetImageFileName();
		public abstract int GetDrawingPriority();
		public abstract bool IsSolidObject();

		public virtual bool CanBeTaken()
		{
			return false;
		}
		
		public virtual bool CanBePlaced(int x,int y)
		{
			return Game._map[x,y]==null || (Game._map[x,y].CanBeDisplacedBy(this) && Game._map[x,y].CanBeReplacedBy(this));
		}

		public virtual bool IsFlammable(GameObject fireSource)
		{
			return false;
		}

		public virtual bool CanBeDisplacedBy(GameObject obj)
		{
			return false;
		}
		public virtual bool CanBeReplacedBy(GameObject obj)
		{
			return false;
		}
		
		public abstract bool DestroyedInConflict(GameObject conflictedGameObject, params int[] coords);

		public static PreparedObject FromJsonObject(JsonObjectCollection jsonObject)
		{
			int x = -1;
			int y = -1;
			string name = "";
			foreach (var obj in jsonObject)
			{
				if (obj is JsonNumericValue n)
				{
					if (n.Name == "x")
					{
						x =(int) n.Value;
					}
					if (n.Name == "y")
					{
						y =(int) n.Value;
					}
				}

				if (obj is JsonStringValue s)
				{
					if (s.Name == "type")
					{
						name = s.Value;
					}
				}
			}
			var type = Assembly
				.GetExecutingAssembly()
				.GetTypes()
				.FirstOrDefault(z => z.Name == name);

			if (type == null)
			{
				throw new Exception($"Can't find type '{name}'");
			}

			var gameObj = (GameObject) Activator.CreateInstance(type);

			if (x == -1 || y == -1)
			{
				throw new ArgumentException();
			}
			return new PreparedObject(gameObj,x,y);
		}
	}
}