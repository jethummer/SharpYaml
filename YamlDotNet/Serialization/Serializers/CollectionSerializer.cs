﻿using System;
using System.Collections;
using YamlDotNet.Events;
using YamlDotNet.Serialization.Descriptors;

namespace YamlDotNet.Serialization.Serializers
{
	internal class CollectionSerializer : ObjectSerializer
	{
		public CollectionSerializer()
		{
		}

		public override IYamlSerializable TryCreate(SerializerContext context, ITypeDescriptor typeDescriptor)
		{
			return typeDescriptor is CollectionDescriptor ? this : null;
		}

		protected override bool CheckIsSequence(ITypeDescriptor typeDescriptor)
		{
			var collectionDescriptor = (CollectionDescriptor)typeDescriptor;

			// If the dictionary is pure, we can directly output a sequence instead of a mapping
			return collectionDescriptor.IsPureCollection || collectionDescriptor.HasOnlyCapacity;
		}

		public override void ReadItem(SerializerContext context, object thisObject, ITypeDescriptor typeDescriptor)
		{
			var collectionDescriptor = (CollectionDescriptor)typeDescriptor;

			if (CheckIsSequence(collectionDescriptor))
			{
				ReadPureCollectionItems(context, thisObject, typeDescriptor);
			}
			else
			{
				var keyEvent = context.Reader.Peek<Scalar>();
				if (keyEvent != null)
				{
					if (keyEvent.Value == context.Settings.SpecialCollectionMember)
					{
						var reader = context.Reader;
						reader.Parser.MoveNext();

						// Read inner sequence
						reader.Expect<SequenceStart>();
						ReadPureCollectionItems(context, thisObject, typeDescriptor);
						reader.Expect<SequenceEnd>();
						return;
					}
				}

				base.ReadItem(context, thisObject, typeDescriptor);
			}
		}

		protected override SequenceStyle GetSequenceStyle(SerializerContext context, object thisObject, ITypeDescriptor typeDescriptor)
		{
			var collection = thisObject as ICollection;
			return collection == null || collection.Count >= context.Settings.LimitFlowSequence ? SequenceStyle.Block : SequenceStyle.Flow;
		}

		public override void WriteItems(SerializerContext context, object thisObject, ITypeDescriptor typeDescriptor)
		{
			var collectionDescriptor = (CollectionDescriptor)typeDescriptor;
			if (CheckIsSequence(collectionDescriptor))
			{
				WritePureCollectionItems(context, thisObject, typeDescriptor);
			}
			else
			{
				// Serialize Dictionary members
				foreach (var member in typeDescriptor.Members)
				{
					if (member.Name == "Capacity" && !context.Settings.EmitCapacityForList)
					{
						continue;
					}

					// Emit the key name
					WriteKey(context, member.Name);

					var memberValue = member.Get(thisObject);
					var memberType = member.Type;
					context.WriteYaml(memberValue, memberType);
				}

				WriteKey(context, context.Settings.SpecialCollectionMember);

				context.Writer.Emit(new SequenceStartEventInfo(thisObject, thisObject.GetType()));
				WritePureCollectionItems(context, thisObject, typeDescriptor);
				context.Writer.Emit(new SequenceEndEventInfo(thisObject, thisObject.GetType()));
			}
		}

		private void ReadPureCollectionItems(SerializerContext context, object thisObject, ITypeDescriptor typeDescriptor)
		{
			var list = thisObject as IList;
			if (list == null)
			{
				throw new InvalidOperationException("Cannot deserialize list to type [{0}]".DoFormat(typeDescriptor.Type));
			}

			var collectionDescriptor = (CollectionDescriptor)typeDescriptor;
			var reader = context.Reader;

			while (!reader.Accept<SequenceEnd>())
			{
				var valueResult = context.ReadYaml(null, collectionDescriptor.ElementType);
	
				// Handle aliasing
				if (valueResult.IsAlias)
				{
					context.AddAliasBinding(valueResult.Alias, deferredValue => list.Add(deferredValue));
				}
				else
				{
					list.Add(valueResult.Value);
				}
			}
		}

		private void WritePureCollectionItems(SerializerContext context, object thisObject, ITypeDescriptor typeDescriptor)
		{
			var collection = (IEnumerable)thisObject;
			var collectionDescriptor = (CollectionDescriptor)typeDescriptor;

			foreach (var item in collection)
			{
				context.WriteYaml(item, collectionDescriptor.ElementType);
			}
		}
	}
}