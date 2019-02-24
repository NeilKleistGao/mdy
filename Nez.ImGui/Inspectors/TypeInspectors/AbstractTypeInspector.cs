using System;
using System.Reflection;
using ImGuiNET;

namespace Nez.ImGuiTools.TypeInspectors
{
	public abstract class AbstractTypeInspector
	{
		protected int _scopeId = NezImGui.SetScopeId();

		protected object _target;
		protected string _name;
		protected Type _valueType;
		protected Func<object> _getter;
		protected Action<object> _setter;
		protected MemberInfo _memberInfo;
		protected bool _isReadOnly;
		protected string _tooltip;

		
		/// <summary>
		/// used to prep the inspector
		/// </summary>
		public virtual void initialize()
		{
			_tooltip = _memberInfo.getCustomAttribute<TooltipAttribute>()?.tooltip;
		}

		/// <summary>
		/// used to draw the UI for the Inspector. Calls either drawMutable or drawReadOnly depending on the _isReadOnly bool
		/// </summary>
		public void draw()
		{
			ImGui.PushID( _scopeId );
			if( _isReadOnly )
			{
				ImGui.PushStyleVar( ImGuiStyleVar.Alpha, ImGui.GetStyle().Alpha * 0.5f );
				drawReadOnly();
				ImGui.PopStyleVar();
			}
			else
			{
				drawMutable();
			}
			ImGui.PopID();
		}

		public abstract void drawMutable();

		/// <summary>
		/// default implementation disables the next widget and calls through to drawMutable. If specialy drawing needs to
		/// be done (such as a multi-widget setup) this can be overridden.
		/// </summary>
		public virtual void drawReadOnly()
		{
			if( _isReadOnly )
				NezImGui.DisableNextWidget();
			drawMutable();
		}


		/// <summary>
		/// if there is a tooltip and the item is hovered this will display it
		/// </summary>
		protected void handleTooltip()
		{
			if( !string.IsNullOrEmpty( _tooltip ) && ImGui.IsItemHovered() )
			{
				ImGui.BeginTooltip();
				ImGui.Text( _tooltip );
				ImGui.EndTooltip();
			}
		}

		#region Set target methods

		public void setTarget( object target, FieldInfo field )
		{
			_target = target;
			_memberInfo = field;
			_name = field.Name;
			_valueType = field.FieldType;
			_isReadOnly = field.IsInitOnly;

			_getter = () =>
			{
				return field.GetValue( target );
			};

			if( !_isReadOnly )
			{
				_setter = ( val ) =>
				{
					field.SetValue( target, val );
				};
			}
		}

		public void setTarget( object target, PropertyInfo prop )
		{
			_memberInfo = prop;
			_target = target;
			_name = prop.Name;
			_valueType = prop.PropertyType;
			_isReadOnly = !prop.CanWrite;

			_getter = () =>
			{
				return ReflectionUtils.getPropertyGetter( prop ).Invoke( target, null );
			};

			if( !_isReadOnly )
			{
				_setter = ( val ) =>
				{
					ReflectionUtils.getPropertySetter( prop ).Invoke( target, new object[] { val } );
				};
			}
		}

		/// <summary>
		/// this version will first fetch the struct before getting/setting values on it when invoking the getter/setter
		/// </summary>
		/// <returns>The struct target.</returns>
		/// <param name="target">Target.</param>
		/// <param name="structName">Struct name.</param>
		/// <param name="field">Field.</param>
		public void setStructTarget( object target, AbstractTypeInspector parentInspector, FieldInfo field )
		{
			_target = target;
			_memberInfo = field;
			_name = field.Name;
			_valueType = field.FieldType;
			_isReadOnly = field.IsInitOnly || parentInspector._isReadOnly;

			_getter = () =>
			{
				var structValue = parentInspector.getValue();
				return field.GetValue( structValue );
			};

			if( !_isReadOnly )
			{
				_setter = ( val ) =>
				{
					var structValue = parentInspector.getValue();
					field.SetValue( structValue, val );
					parentInspector.setValue( structValue );
				};
			}
		}

		/// <summary>
		/// this version will first fetch the struct before getting/setting values on it when invoking the getter/setter
		/// </summary>
		/// <returns>The struct target.</returns>
		/// <param name="target">Target.</param>
		/// <param name="structName">Struct name.</param>
		/// <param name="field">Field.</param>
		public void setStructTarget( object target, AbstractTypeInspector parentInspector, PropertyInfo prop )
		{
			_target = target;
			_memberInfo = prop;
			_name = prop.Name;
			_valueType = prop.PropertyType;
			_isReadOnly = !prop.CanWrite || parentInspector._isReadOnly;

			_getter = () =>
			{
				var structValue = parentInspector.getValue();
				return ReflectionUtils.getPropertyGetter( prop ).Invoke( structValue, null );
			};

			if( !_isReadOnly )
			{
				_setter = ( val ) =>
				{
					var structValue = parentInspector.getValue();
					prop.SetValue( structValue, val );
					parentInspector.setValue( structValue );
				};
			}
		}

		public void setTarget( object target, MethodInfo method )
		{
			_memberInfo = method;
			_target = target;
			_name = method.Name;
		}
	
		#endregion


		#region Get/set values

		protected T getValue<T>()
		{
			return (T)_getter.Invoke();
		}

		protected object getValue()
		{
			return _getter.Invoke();
		}

		protected void setValue( object value )
		{
			_setter.Invoke( value );
		}

		#endregion

	}
}