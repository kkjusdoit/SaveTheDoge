namespace SaveTheDoge
{
    public interface IGameOverTarget
    {
        bool IsEliminated { get; }
        void Eliminate(string reason);
    }
}
