namespace CommonUtils;

public class DataValidation
{
    public static bool IsValidNumber(string number)
    {
        return !string.IsNullOrEmpty(number);
    }

    // Verify email address
    public static bool IsValidEmail(string email)
    {
        if (string.IsNullOrEmpty(email))
            return false;

        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch (Exception)
        {
            return false;
        }
    }
}
