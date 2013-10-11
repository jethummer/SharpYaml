﻿using System;

namespace YamlDotNet.Serialization.Serializers
{
	public class ChainedSerializer : IYamlSerializable
	{
		private readonly IYamlSerializable next;

		public ChainedSerializer(IYamlSerializable next)
		{
			if (next == null) throw new ArgumentNullException("next");
			this.next = next;
		}

		public virtual ValueOutput ReadYaml(SerializerContext context, object value, ITypeDescriptor typeDescriptor)
		{
			return next.ReadYaml(context, value, typeDescriptor);
		}

		public virtual void WriteYaml(SerializerContext context, ValueInput input, ITypeDescriptor typeDescriptor)
		{
			next.WriteYaml(context, input, typeDescriptor);
		}
	}
}