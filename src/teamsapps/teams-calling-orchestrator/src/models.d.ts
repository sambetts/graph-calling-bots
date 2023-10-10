
interface BotRequest{
    Attendees : Attendee[]
}

interface Attendee
{
    Id: string,
    DisplayId: string?,
    Type: number
}