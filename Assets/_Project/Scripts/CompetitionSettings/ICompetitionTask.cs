/// <summary>
/// Interface for competition scoring tasks. Implement StartScript/StopScript for enable/disable.
/// </summary>
public interface ICompetitionTask
{
    void StartScript();
    void StopScript();
}
