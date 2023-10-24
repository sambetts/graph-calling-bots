import { MeetingInfo, ChatInfo } from "@microsoft/microsoft-graph-types";

interface BotRequest{
    Attendees : AttendeeCallInfo[],
    JoinMeetingInfo?: JoinMeetingInfo;
    MessageUrl: string
}

interface AttendeeCallInfo
{
    Id: string,
    DisplayName: string?,
    Type: number
}

interface JoinMeetingInfo
{
    JoinUrl? : string;
}
