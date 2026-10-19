namespace HPKata.AutoCad.Commands
{
    /// <summary>
    /// Specifies rebar bar mark (STT) notation sequencing mode in active drawing session.
    /// </summary>
    public enum MarkNotationMode
    {
        /// <summary>
        /// Standard sequential integer marks (e.g., 1, 2, 3...) - configured via 'stt1'.
        /// </summary>
        Sequential,

        /// <summary>
        /// Secondary suffix marks (e.g., 1a, 1b...) - configured via 'stt0'.
        /// </summary>
        Suffix
    }
}
