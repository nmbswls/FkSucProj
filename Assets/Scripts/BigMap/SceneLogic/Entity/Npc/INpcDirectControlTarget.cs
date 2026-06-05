namespace My.Map.Entity
{
    public interface INpcDirectControlTarget
    {
        bool CanAcceptDirectControl(int playerId);
        void OnDirectControlBegin(int playerId);
        void OnDirectControlEnd(int playerId);
    }
}
