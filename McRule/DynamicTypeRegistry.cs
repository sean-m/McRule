
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;

public class DynamicTypeRegistry
{

	public static Func<T, T> DynamicSelector<T>(params string[] properties)
	{
		var param = Expression.Parameter(typeof(T), "x");
		var constructor = Expression.New(typeof(T));

		// property initializers from list of property names
		var bindings = properties.Select(p => p.Trim())
			.Select(x =>
			{
				var sourceProp = typeof(T).GetProperty(x);
				var sourceValue = Expression.Property(param, sourceProp);
				return Expression.Bind(sourceProp, sourceValue);
			}
		);

		var initializedObject = Expression.MemberInit(constructor, bindings);
		var lambda = Expression.Lambda<Func<T, T>>(initializedObject, param);

		return lambda.Compile();
	}


	protected class McDynamicType {
		public string name { get; set;}
		public TypeBuilder tb {get; set;}
		Type _builtType;
		public Type builtType
		{
			get {
				if (_builtType == null) { _builtType = tb.CreateType(); } 
				return _builtType;
			}
		} 
		public string[] properties {get; set;}
				
		public Type buildForType {get; set;}
		
		public Func<T,T> GetDynamicSelector<T>() {
			return DynamicSelector<T>(properties);
		}
	}
	
	AssemblyBuilder dynAsm { get; set; }
	ModuleBuilder dynModule { get; set; }

	Dictionary<string, McDynamicType> registeredTypes = new Dictionary<string, McDynamicType>();


	public DynamicTypeRegistry()
	{
		dynAsm = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("McDynamicAssembly"), AssemblyBuilderAccess.Run);
		dynModule = dynAsm.DefineDynamicModule("McTypes");
	}

	public Func<T, T> DynamicSelector<T>(string dynName)
	{
		McDynamicType dynType = null;
		var sourceType = typeof(T);

		if (registeredTypes.TryGetValue(dynName, out dynType))
		{
			return dynType.GetDynamicSelector<T>();
		}
		return null;
	}

	public dynamic CopyFromSourceObject<T>(T source, string dynName)
	{
		McDynamicType dynType = null;
		var sourceType = typeof(T);
		
		if (registeredTypes.TryGetValue(dynName, out dynType))
		{
			var resultType = dynType.builtType;
			var result = dynAsm.CreateInstance(dynType.name);
	
			foreach (var p in dynType.properties) {
				var prop = resultType.GetProperty(p);
				prop.SetValue(result, sourceType.GetProperty(p).GetValue(source));
			}
			return result;
		}
		return null;
	}

	public void BuildDynamicSubType<T>(string shortName, IEnumerable<string> properties)
	{
		var requestedType = typeof(T);
		var props = properties.ToArray();
		Array.Sort(props);
		var dynTypeName = shortName ?? $"{requestedType.Name}_{String.Join('-',props)}"; // TODO make this a concatination of the policy ids for a given predicate and type.
		var dynType = dynModule.DefineType(dynTypeName);

		foreach (var p in properties)
		{
			var reflectedProperty = requestedType.GetProperty(p);

			if (reflectedProperty == null) continue;

			AddProperty(dynType, p, reflectedProperty.PropertyType);
		}

		var builtType = new McDynamicType();
		builtType.tb = dynType;
		builtType.buildForType = typeof(T);
		builtType.name = dynTypeName;
		builtType.properties = props;
		
		registeredTypes.Add(dynTypeName, builtType);
	}

	public Type GetDynamicObjectByTypeName(string Name)
	{
		McDynamicType result = null;
		if (registeredTypes.TryGetValue(Name, out result))
		{
			return result.builtType;
		}
		return typeof(Object);
	}

	private void AddProperty(TypeBuilder typeBuilder, string propertyName, Type propertyType)
	{
		const MethodAttributes getSetAttr = MethodAttributes.Public | MethodAttributes.HideBySig;

		// Backing field
		FieldBuilder field = typeBuilder.DefineField("_" + propertyName, typeof(string), FieldAttributes.Private);
		PropertyBuilder property = typeBuilder.DefineProperty(propertyName, PropertyAttributes.None, propertyType,
			new[] { propertyType });

		// Getter
		MethodBuilder getMethodBuilder = typeBuilder.DefineMethod("get_value", getSetAttr, propertyType,
			Type.EmptyTypes);
		ILGenerator getIl = getMethodBuilder.GetILGenerator();
		getIl.Emit(OpCodes.Ldarg_0);
		getIl.Emit(OpCodes.Ldfld, field);
		getIl.Emit(OpCodes.Ret);

		// Setter
		MethodBuilder setMethodBuilder = typeBuilder.DefineMethod("set_value", getSetAttr, null,
			new[] { propertyType });
		ILGenerator setIl = setMethodBuilder.GetILGenerator();
		setIl.Emit(OpCodes.Ldarg_0);
		setIl.Emit(OpCodes.Ldarg_1);
		setIl.Emit(OpCodes.Stfld, field);
		setIl.Emit(OpCodes.Ret);

		property.SetGetMethod(getMethodBuilder);
		property.SetSetMethod(setMethodBuilder);
	}
}
