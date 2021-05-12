using System;

namespace ModusOperandi.ECS.Archetypes
{
    public readonly struct Archetype : IEquatable<Archetype>
    {
        public ulong Signature { get; }
        public ulong AntiSignature { get; }

        private readonly int[] _indices;
        public Span<int> Indices => _indices;
        private readonly int[] _antiIndices;
        public Span<int> AntiIndices => _antiIndices;

        public static Span<int> CalculateIndices(ulong sig)
        {
            if (sig == 0) return Array.Empty<int>();
            Span<int> indices = new int[(int) Math.Log2(sig) + 1];
            var counter = 0;
            for (var i = 0; 1u << i <= sig; i++)
            {
                if (((1u << i) & sig) == 0) continue;
                indices[counter++] = i;
            }

            return indices.Slice(0, counter);
        }

        public Archetype(ulong signature, ulong antiSignature)
        {
            Signature = signature;
            AntiSignature = antiSignature;
            _indices = CalculateIndices(signature).ToArray();
            _antiIndices = CalculateIndices(antiSignature).ToArray();
        }

        public bool Equals(Archetype other)
        {
            return Signature == other.Signature && AntiSignature == other.AntiSignature;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hash = 17;
                hash = hash * 23 + Signature.GetHashCode();
                hash = hash * 23 + AntiSignature.GetHashCode();
                return hash;
            }
        }
    }
}