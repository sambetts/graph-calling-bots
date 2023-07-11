namespace Engine;


public class MeetingState
{
    public DateTime Created { get; set; }
    public string MeetingUrl { get; set; } = string.Empty;

    public bool IsMeetingCreated => !string.IsNullOrEmpty(MeetingUrl);
    public List<NumberCallState> Numbers { get; set; } = new();
}

public class NumberCallState
{
    public string Number { get; set; } = null!;

    public static bool IsValidNumber(string number)
    {
        return !string.IsNullOrEmpty(number);
    }
}
