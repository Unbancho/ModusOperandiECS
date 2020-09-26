using JetBrains.Annotations;

namespace ModusOperandi.ECS.Utils
{
    [PublicAPI]
    public unsafe struct FixedString32
    {
        public const int MaxSize = 32;
        // TODO: What I wanted to do simply does not work, we need a miracle.
        private fixed char _string[MaxSize];

        public FixedString32(string s)
        {
            String = s;
        }

        public string String
        {
            get
            {
                fixed (char* s = _string)
                    return new string(s);
            }

            set
            {
                if (value == null)
                {
                    for (var i = 0; i < MaxSize; i++) _string[i] = (char) 0;
                    return;
                }

                fixed (char* p = value)
                {
                    var len = value.Length;
                    if (len > MaxSize) len = MaxSize;
                    for (var i = 0; i < len; i++) _string[i] = p[i];
                    for (var i = len; i < MaxSize; i++) _string[i] = (char) 0;
                }
            }

        }

        public override string ToString() => String;
        public static implicit operator string(FixedString32 fs) => fs.ToString();
        public static explicit operator FixedString32(string s) => new FixedString32(s);
    }
}