using System.Reflection;

namespace CommonUtils;

public class Resources
{

    public static Stream ReadResource(string resourcePath, Assembly assembly)
    {
        // Format: "{Namespace}.{Folder}.{filename}.{Extension}"
        var stream = assembly.GetManifestResourceStream(resourcePath);
        if (stream != null)
        {
            return stream;
        }
        else
        {
            throw new ArgumentOutOfRangeException(nameof(resourcePath), $"No resource found by name '{resourcePath}'");
        }
    }
}
