namespace Moth.Tasks
{
    using System;

    /// <summary>
    /// Represents a unit type.
    /// </summary>
    public struct Unit : IEquatable<Unit>
    {
        /// <summary>
        /// Compares this instance of <see cref="Unit"/> with another instance.
        /// </summary>
        /// <param name="other">Other instance of <see cref="Unit"/>.</param>
        /// <returns>Always returns <see langword="true"/>.</returns>
        public readonly bool Equals (Unit other) => true;
    }
}
