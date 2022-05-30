using System.Collections.Generic;

public static class Extensions
{
    public static List<string> SplitOnWords(this string inputText, int chunkLength)
    {
        List<string> chunkList = new List<string>();

        while (inputText.Length > 0)
        {
            if (inputText == null || inputText.Length < chunkLength || inputText.LastIndexOf(" ", chunkLength) == -1)
            {
                chunkList.Add(inputText);
                break;
            }

            string chunk = inputText.Substring(0, inputText.LastIndexOf(" ", chunkLength));
            chunkList.Add(chunk);

            inputText = inputText.Substring(chunk.Length + 1);
        }

        return chunkList;
    }
}