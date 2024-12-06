namespace Moth.Tasks
{
    using System;

    /// <summary>
    /// Represents a unit type. This type is used to represent the absence of a value.
    /// </summary>
    public struct Unit : IEquatable<Unit>
    {
        public bool Equals (Unit other) => true;
    }
}
