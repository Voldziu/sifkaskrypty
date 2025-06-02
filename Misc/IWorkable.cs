public interface IWorkable
{
    bool IsWorked { get; }
    bool CanBeWorked();
    void SetWorked(bool worked, string cityId = "");
}