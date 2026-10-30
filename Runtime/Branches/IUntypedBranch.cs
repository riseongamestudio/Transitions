namespace RiseOn.Transitions {
    /// <summary>
    /// What the Editor reaches on a branch without knowing its types: the object it reaches, for Capture and the preview.
    /// </summary>
    internal interface IUntypedBranch : IUntypedNodes {
        string Path { get; }

        /// <summary>
        /// The object the path reaches from <paramref name="target"/> now, read through reflection, so it leaves nothing behind for the transitions running;<br/>
        /// null when it reaches nothing.
        /// </summary>
        object Reach(object target);
    }
}