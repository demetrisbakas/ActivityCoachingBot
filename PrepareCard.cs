using System.IO;

namespace AdaptiveCardPromptSample.Welcome
{
    public static class PrepareCard
    {
        public static string ReadCard(string fileName)
        {
            string[] BuildPath = { ".", "Cards", fileName };
            var filePath = Path.Combine(BuildPath);
            var fileRead = File.ReadAllText(filePath);
            return fileRead;
        }

    }
}
