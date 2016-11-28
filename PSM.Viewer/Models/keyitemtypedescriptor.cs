/// <copyright file="keyitemtypedescriptor.cs" company="Baker Hughes Incorporated">
/// Copyright (c) 2015 All Rights Reserved
/// </copyright>
/// <author>Odd Marthon Lende</author>
/// 
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace PSM.Viewer.Models
{

    [TypeDescriptionProvider(typeof(KeyItem.KeyItemTypeDescriptionProvider))]
    public partial class KeyItem : ICustomTypeDescriptor
    {

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="property"></param>
        /// <param name="value"></param>
        public delegate T Setter<T>(KeyItemDynamicPropertyDescriptor<T> sender, KeyItem key, T value);

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="property"></param>
        /// <returns></returns>
        public delegate T Getter<T>(KeyItemDynamicPropertyDescriptor<T> sender, KeyItem key);

        /// <summary>
        /// 
        /// </summary>
        public class KeyItemTypeDescriptionProvider : TypeDescriptionProvider
        {

            /// <summary>
            /// The default provider
            /// </summary>
            private static readonly TypeDescriptionProvider _default = TypeDescriptor.GetProvider(typeof(KeyItem));
            
            /// <summary>
            /// Constructor
            /// </summary>
            public KeyItemTypeDescriptionProvider() : base(_default)
            {
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="objectType"></param>
            /// <param name="instance"></param>
            /// <returns></returns>
            public override ICustomTypeDescriptor GetTypeDescriptor(Type objectType, object instance)
            {
                ICustomTypeDescriptor defaultDescriptor = base.GetTypeDescriptor(objectType, instance);
                return instance == null ? defaultDescriptor :
                    new KeyItemTypeDescriptor((KeyItem)instance);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public class KeyItemTypeDescriptor : ICustomTypeDescriptor
        {
            /// <summary>
            /// 
            /// </summary>
            private KeyItem _instance;

            /// <summary>
            /// Constructor
            /// </summary>
            /// <param name="instance"></param>
            public KeyItemTypeDescriptor(KeyItem instance)
            {
                _instance = instance;
            }

            /// <summary>
            /// 
            /// </summary>
            /// <returns></returns>
            public AttributeCollection GetAttributes()
            {
                return TypeDescriptor.GetAttributes(this, true);
            }

            /// <summary>
            /// 
            /// </summary>
            /// <returns></returns>
            public string GetClassName()
            {
                return TypeDescriptor.GetClassName(this, true);
            }

            /// <summary>
            /// 
            /// </summary>
            /// <returns></returns>
            public string GetComponentName()
            {
                return TypeDescriptor.GetComponentName(this, true);
            }

            /// <summary>
            /// 
            /// </summary>
            /// <returns></returns>
            public TypeConverter GetConverter()
            {
                return TypeDescriptor.GetConverter(this, true);
            }

            /// <summary>
            /// 
            /// </summary>
            /// <returns></returns>
            public EventDescriptor GetDefaultEvent()
            {
                return TypeDescriptor.GetDefaultEvent(this, true);
            }

            /// <summary>
            /// 
            /// </summary>
            /// <returns></returns>
            public PropertyDescriptor GetDefaultProperty()
            {
                return TypeDescriptor.GetDefaultProperty(this, true);
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="editorBaseType"></param>
            /// <returns></returns>
            public object GetEditor(Type editorBaseType)
            {
                return TypeDescriptor.GetEditor(this, editorBaseType, true);
            }

            /// <summary>
            /// 
            /// </summary>
            /// <returns></returns>
            public EventDescriptorCollection GetEvents()
            {
                return TypeDescriptor.GetEvents(this, true);
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="attributes"></param>
            /// <returns></returns>
            public EventDescriptorCollection GetEvents(Attribute[] attributes)
            {
                return TypeDescriptor.GetEvents(this, attributes, true);
            }

            /// <summary>
            /// 
            /// </summary>
            /// <returns></returns>
            public PropertyDescriptorCollection GetProperties()
            {
                return this.GetProperties(new Attribute[0]);
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="attributes"></param>
            /// <returns></returns>
            public PropertyDescriptorCollection GetProperties(Attribute[] attributes)
            {
                List<PropertyDescriptor> descriptors = new List<PropertyDescriptor>();
                object context = _instance.Context;

                if (context is Type)
                {
                    Type t = (context as Type);

                    while (t != null)
                    {
                        if (KeyItem._property.ContainsKey(t))
                        {
                            descriptors.AddRange(KeyItem._property[t].Values);
                        }

                        t = t.BaseType;
                    }
                }
                else
                {

                    if (!KeyItem._property.ContainsKey(context))
                        return new PropertyDescriptorCollection(new PropertyDescriptor[0]);

                    descriptors.AddRange(KeyItem._property[context].Values);
                }

                return new PropertyDescriptorCollection(descriptors.ToArray());
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="pd"></param>
            /// <returns></returns>
            public object GetPropertyOwner(PropertyDescriptor pd)
            {
                return _instance;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public class KeyItemDynamicPropertyDescriptor<T> : PropertyDescriptor
        {
            /// <summary>
            /// The default value
            /// </summary>
            private T _default;

            /// <summary>
            /// The getter
            /// </summary>
            private Getter<T> _get;

            /// <summary>
            /// The setter
            /// </summary>
            private Setter<T> _set;

           /// <summary>
            /// Constructor
            /// </summary>
            /// <param name="dataType"></param>
            /// <param name="name"></param>
            public KeyItemDynamicPropertyDescriptor(string name, T defaultValue, Attribute[] attributes = null) : base(name, attributes)
            {
                _default = defaultValue;
            }

            /// <summary>
            /// Constructor
            /// </summary>
            /// <param name="name"></param>
            /// <param name="get"></param>
            /// <param name="set"></param>
            /// <param name="defaultValue"></param>
            /// <param name="attributes"></param>
            public KeyItemDynamicPropertyDescriptor(string name, Getter<T> get, Setter<T> set, T defaultValue, Attribute[] attributes = null) : this(name, defaultValue, attributes)
            {
                _get = get;
                _set = set;
            }

            /// <summary>
            /// 
            /// </summary>
            public override Type ComponentType
            {
                get
                {
                    return typeof(KeyItem);
                }
            }

            /// <summary>
            /// 
            /// </summary>
            public override bool IsReadOnly
            {
                get
                {
                    return false;
                }
            }

            /// <summary>
            /// 
            /// </summary>
            public override Type PropertyType
            {
                get
                {
                    return typeof(T);
                }
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="component"></param>
            /// <returns></returns>
            public override bool CanResetValue(object component)
            {
                return false;
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="component"></param>
            /// <returns></returns>
            public override object GetValue(object component)
            {

                if (_get != null)
                    return _get(this, (component as KeyItem));

                return GetValue((component as KeyItem));
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="component"></param>
            /// <returns></returns>
            public T GetValue(KeyItem component)
            {
                var properties = component._properties;
                return properties.ContainsKey(this) ? (T)properties[this] : _default;
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="component"></param>
            public override void ResetValue(object component)
            {
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="component"></param>
            /// <param name="value"></param>
            public override void SetValue(object component, object value)
            {
                
                if (_set != null)
                    value = _set(this, component as KeyItem, (T)value);

                SetValue(component as KeyItem, (T)value);
               
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="component"></param>
            /// <param name="value"></param>
            public void SetValue(KeyItem component, T value)
            {
                var properties = (component as KeyItem)._properties;

                if (!properties.ContainsKey(this))
                    properties.Add(this, value);
                else
                    properties[this] = value;
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="component"></param>
            /// <returns></returns>
            public override bool ShouldSerializeValue(object component)
            {
                return false;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public AttributeCollection GetAttributes()
        {
            return TypeDescriptor.GetAttributes(this, true);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public string GetClassName()
        {
            return TypeDescriptor.GetClassName(this, true);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public string GetComponentName()
        {
            return TypeDescriptor.GetComponentName(this, true);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public TypeConverter GetConverter()
        {
            return TypeDescriptor.GetConverter(this, true);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public EventDescriptor GetDefaultEvent()
        {
            return TypeDescriptor.GetDefaultEvent(this, true);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public PropertyDescriptor GetDefaultProperty()
        {
            return TypeDescriptor.GetDefaultProperty(this, true);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="editorBaseType"></param>
        /// <returns></returns>
        public object GetEditor(Type editorBaseType)
        {
            return TypeDescriptor.GetEditor(this, editorBaseType, true);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public EventDescriptorCollection GetEvents()
        {
            return TypeDescriptor.GetEvents(this, true);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="attributes"></param>
        /// <returns></returns>
        public EventDescriptorCollection GetEvents(Attribute[] attributes)
        {
            return TypeDescriptor.GetEvents(this, attributes, true);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public PropertyDescriptorCollection GetProperties()
        {
            return this.GetProperties(new Attribute[0]);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="attributes"></param>
        /// <returns></returns>
        public PropertyDescriptorCollection GetProperties(Attribute[] attributes)
        {

            List<PropertyDescriptor> descriptors = new List<PropertyDescriptor>();
            
            if(Context is Type)
            {
                Type t = (Context as Type);

                while(t != null)
                {
                    if(KeyItem._property.ContainsKey(t))
                    {
                        descriptors.AddRange(KeyItem._property[t].Values);
                    }

                    t = t.BaseType;
                }
            }
            else
            {

                if (!KeyItem._property.ContainsKey(Context))
                    return new PropertyDescriptorCollection(new PropertyDescriptor[0]);

                descriptors.AddRange(KeyItem._property[Context].Values);
            }

            return new PropertyDescriptorCollection(descriptors.ToArray());
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="pd"></param>
        /// <returns></returns>
        public object GetPropertyOwner(PropertyDescriptor pd)
        {
            return this;
        }
    }
}
