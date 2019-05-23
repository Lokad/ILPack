using System;
using System.Reflection;
using System.Reflection.Metadata;

namespace Lokad.ILPack.Metadata
{
    internal abstract class DefinitionMetadata<TEntity, THandle>
    {
        protected DefinitionMetadata(TEntity entity, THandle handle)
        {
            IsEmitted = false;
            Entity = entity;
            Handle = handle;
        }

        public bool IsEmitted { get; private set; }
        public TEntity Entity { get; }
        public THandle Handle { get; }

        public void MarkAsEmitted()
        {
            IsEmitted = true;
        }
    }

    internal class FieldDefinitionMetadata : DefinitionMetadata<FieldInfo, FieldDefinitionHandle>
    {
        public FieldDefinitionMetadata(FieldInfo method, FieldDefinitionHandle handle) : base(method, handle)
        {
        }
    }

    internal class PropertyDefinitionMetadata : DefinitionMetadata<PropertyInfo, PropertyDefinitionHandle>
    {
        public PropertyDefinitionMetadata(PropertyInfo property, PropertyDefinitionHandle propertyHandle) : base(
            property, propertyHandle)
        {
        }
    }

    internal class EventDefinitionMetadata : DefinitionMetadata<EventInfo, EventDefinitionHandle>
    {
        public EventDefinitionMetadata(EventInfo ev, EventDefinitionHandle eventHandle) : base(
            ev, eventHandle)
        {
        }
    }

    internal class MethodBaseDefinitionMetadata : DefinitionMetadata<MethodBase, MethodDefinitionHandle>
    {
        public MethodBaseDefinitionMetadata(MethodBase method, MethodDefinitionHandle handle) : base(method, handle)
        {
        }
    }

    internal class TypeDefinitionMetadataOffset
    {
        public int TypeIndex { get; set; }
        public int FieldIndex { get; set; }
        public int PropertyIndex { get; set; }
        public int MethodIndex { get; set; }
        public int EventIndex { get; set; }
    }

    internal class TypeDefinitionMetadata : DefinitionMetadata<Type, EntityHandle>
    {
        public TypeDefinitionMetadata(Type type, EntityHandle handle) : base(type, handle)
        {
        }
    }
}