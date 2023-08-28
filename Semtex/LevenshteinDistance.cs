namespace Semtex;

internal static class LevenshteinDistance
{
    internal static int Calculate(string left, string right)
    {
        int len1 = left.Length;
        int len2 = right.Length;
        int[,] dp = new int[len1 + 1, len2 + 1];

        for (int i = 0; i <= len1; i++)
        {
            dp[i, 0] = i;
        }

        for (int j = 0; j <= len2; j++)
        {
            dp[0, j] = j;
        }

        for (int i = 1; i <= len1; i++)
        {
            for (int j = 1; j <= len2; j++)
            {
                int cost = (left[i - 1] == right[j - 1]) ? 0 : 1;

                dp[i, j] = Math.Min(
                    Math.Min(
                        dp[i - 1, j] + 1,
                        dp[i, j - 1] + 1),
                    dp[i - 1, j - 1] + cost);
            }
        }

        return dp[len1, len2];

    }
}