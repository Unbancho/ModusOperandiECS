using System;
using System.Runtime.Serialization;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;

namespace ModusOperandi.Messaging
{
    [Flags]
    public enum MessageFlags
    {
        None = 0
    }

    [Serializable]
    public class Message : ISerializable
    {
        public override string ToString() { return Text; }
        public Dictionary<string, object> PackedObjects;

        public object this[string name]
        {
            get { return PackedObjects[name]; }
            set { PackObject(value, name); }
        }

        public object Sender;

        public MessageFlags Flag;

        private MessageFlags[] _flagsArray;
        private MessageFlags[] FlagsArray
        {
            get { return _flagsArray; }
            set { _flagsArray = value; foreach (var f in value) { Flag |= f; } }
        }

        public string Text;

        public Message(string text = null, params Enum[] flags)
        {
            PackedObjects = new Dictionary<string, object>();
            Text = text == null ? String.Empty : text;
            FlagsArray = Array.ConvertAll(flags, f => (MessageFlags)f);
        }

        public void PackObject<T>(T obj, string name = null)
        {
            if (name == null)
            {
                name = obj.GetType().Name;
            }
            PackedObjects[name] = obj;
        }

        public object UnpackObject(string name, bool pop = false)
        {
            object obj = PackedObjects[name];
            if (pop) PackedObjects.Remove(name);
            return obj;
        }

        public object Pop(string name = null)
        {
            if (name == null)
            {
                foreach (KeyValuePair<string, object> entry in PackedObjects)
                {
                    return (obj: UnpackObject(entry.Key, pop: true), name: entry.Key);
                }
                throw new EmptyMessageException();
            }
            return UnpackObject(name, pop: true);
        }

        public bool TryPop(out object result, string name = null)
        {
            result = null;
            try
            {
                result = Pop(name);
                return true;
            }
            catch (Exception e) when (e is EmptyMessageException || e is KeyNotFoundException)
            {
                return false;
            }
        }

        public IEnumerable<Enum> GetFlags(bool enumerable = true)
        {
            if (!enumerable) yield return Flag;
            foreach (var flag in FlagsArray)
            {
                yield return flag;
            }
        }

        public void SetFlags<T>(T flags)
        {
            List<MessageFlags> newFlags = new List<MessageFlags>();
            foreach (var f in Enum.GetValues(flags.GetType()))
            {
                if ((flags as Enum).HasFlag((Enum)f))
                    newFlags.Add((MessageFlags)f);
            }
            FlagsArray = newFlags.ToArray();
        }

        public void SetFlags(Enum[] flags)
        {
            FlagsArray = Array.ConvertAll(flags, item => (MessageFlags)item); ;
        }

        public bool HasFlag(Enum flag)
        {
            return Flag.HasFlag((MessageFlags)flag);
        }

        protected Message(SerializationInfo info, StreamingContext context)
        {
            PackedObjects = new Dictionary<string, object>();
            foreach (SerializationEntry entry in info)
            {
                PackObject(entry.Value, entry.Name);
            }
            Text = (string)Pop("Text");
            FlagsArray = (MessageFlags[])Pop("FlagsArray");
            Flag = (MessageFlags)Pop("Flag");
        }

        public virtual void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            foreach (KeyValuePair<string, object> entry in PackedObjects)
            {
                info.AddValue(entry.Key, entry.Value);
            }
            info.AddValue("Text", Text);
            info.AddValue("FlagsArray", FlagsArray);
            info.AddValue("Flag", Flag);
        }
    }

    [Serializable]
    public class Package
    {
        public string Name { get; set; }
        public object Object { get; set; }
        public PropertyInfo PropertyInfo { get; set; }

        public Package(object obj, PropertyInfo propertyInfo, string name)
        {
            Object = obj;
            Name = name;
        }
    }

    public abstract class MessageProcessor<T>
    {
        public abstract Dictionary<T, MethodInfo> ProcessingMethods { get; set; }

        public virtual void ProcessMessage(Message message)
        {
            object[] parametersToPass = new object[] { message };
            foreach (var flag in message.GetFlags())
            {
                T e = (T)Convert.ChangeType(flag, typeof(T));
                if (ProcessingMethods.TryGetValue(e, out MethodInfo method))
                    method.Invoke(this, parametersToPass);
            }
        }
    }

    public class EmptyMessageException : Exception { }
    public class InvalidFlagType : Exception
    {
        public InvalidFlagType(Type type) : base($"Invalid type for flag: {type}. Must be Enum Flag.") { }
    }

}