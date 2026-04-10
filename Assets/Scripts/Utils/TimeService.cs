using System;
using System.Globalization;
using UnityEngine;

public static class TimeService
{
    public const int DailyResetHourLocal = 5;
    public const double MaxOfflineElapsedCapSeconds = 60d * 60d * 24d * 7d;
    private const double SuspiciousElapsedThresholdSeconds = 60d * 60d * 24d * 30d;

    public static DateTime GetUtcNow()
    {
        return DateTime.UtcNow;
    }

    public static DateTime GetLocalNow()
    {
        return DateTime.Now;
    }

    public static bool TryParseUtcTimestamp(string timestamp, out DateTime parsedUtc)
    {
        parsedUtc = default;
        if (string.IsNullOrWhiteSpace(timestamp))
        {
            return false;
        }

        if (!DateTime.TryParse(timestamp, null, System.Globalization.DateTimeStyles.RoundtripKind, out DateTime parsed))
        {
            return false;
        }

        parsedUtc = parsed.Kind == DateTimeKind.Utc ? parsed : parsed.ToUniversalTime();
        return true;
    }

    public static double GetOfflineElapsedSeconds(string lastSeenUtc, double maxSeconds)
    {
        return GetElapsedSafe(lastSeenUtc, maxSeconds).TotalSeconds;
    }

    public static TimeSpan GetElapsedSafe(string lastSeenUtc, double maxSeconds = MaxOfflineElapsedCapSeconds)
    {
        if (!TryParseUtcTimestamp(lastSeenUtc, out DateTime lastSeen))
        {
            return TimeSpan.Zero;
        }

        double elapsedSeconds = (GetUtcNow() - lastSeen).TotalSeconds;
        double sanitizedSeconds = SanitizeElapsedSeconds(elapsedSeconds, maxSeconds);
        return TimeSpan.FromSeconds(sanitizedSeconds);
    }

    public static double SanitizeElapsedSeconds(double elapsedSeconds, double maxSeconds)
    {
        if (double.IsNaN(elapsedSeconds) || double.IsInfinity(elapsedSeconds))
        {
            return 0d;
        }

        if (elapsedSeconds < 0d)
        {
            Debug.LogWarning($"Ignoring negative elapsed time: {elapsedSeconds:0.##}s");
            return 0d;
        }

        if (elapsedSeconds > SuspiciousElapsedThresholdSeconds)
        {
            Debug.LogWarning($"Suspiciously large elapsed time detected: {elapsedSeconds:0.##}s");
        }

        double effectiveCap = MaxOfflineElapsedCapSeconds;
        if (maxSeconds > 0d)
        {
            effectiveCap = Math.Min(effectiveCap, maxSeconds);
        }

        elapsedSeconds = Math.Min(elapsedSeconds, effectiveCap);
        return Math.Max(0d, elapsedSeconds);
    }

    public static int GetResetBucket(DateTime localNow, int resetHourLocal = DailyResetHourLocal)
    {
        DateTime effectiveDate = localNow;
        if (effectiveDate.TimeOfDay < TimeSpan.FromHours(resetHourLocal))
        {
            effectiveDate = effectiveDate.AddDays(-1);
        }

        return effectiveDate.Year * 10000 + effectiveDate.Month * 100 + effectiveDate.Day;
    }

    public static string GetDailyResetBucket(DateTime localNow, int resetHourLocal = DailyResetHourLocal)
    {
        return FormatResetBucket(GetResetBucket(localNow, resetHourLocal));
    }

    public static int GetCurrentResetBucketLocal(int resetHourLocal = DailyResetHourLocal)
    {
        return GetResetBucket(GetLocalNow(), resetHourLocal);
    }

    public static bool IsSameResetWindow(int firstResetBucket, int secondResetBucket)
    {
        return NormalizeResetBucket(firstResetBucket) > 0 &&
               NormalizeResetBucket(firstResetBucket) == NormalizeResetBucket(secondResetBucket);
    }

    public static bool ShouldRunDailyReset(int lastCompletedResetBucket, int observedResetBucket)
    {
        int normalizedLastBucket = NormalizeResetBucket(lastCompletedResetBucket);
        int normalizedObservedBucket = NormalizeResetBucket(observedResetBucket);
        return normalizedObservedBucket > normalizedLastBucket;
    }

    public static int GetEffectiveResetBucket(int lastCompletedResetBucket, int observedResetBucket)
    {
        int normalizedLastBucket = NormalizeResetBucket(lastCompletedResetBucket);
        int normalizedObservedBucket = NormalizeResetBucket(observedResetBucket);
        return Math.Max(normalizedLastBucket, normalizedObservedBucket);
    }

    public static string FormatResetBucket(int resetBucket)
    {
        if (!TryParseResetBucket(resetBucket.ToString(CultureInfo.InvariantCulture), out int normalizedBucket))
        {
            return string.Empty;
        }

        string normalizedText = normalizedBucket.ToString("D8", CultureInfo.InvariantCulture);
        return $"{normalizedText.Substring(0, 4)}-{normalizedText.Substring(4, 2)}-{normalizedText.Substring(6, 2)}";
    }

    public static bool TryParseResetBucket(string rawValue, out int resetBucket)
    {
        resetBucket = 0;
        if (string.IsNullOrWhiteSpace(rawValue))
        {
            return false;
        }

        string trimmed = rawValue.Trim();
        if (DateTime.TryParseExact(trimmed, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime parsedDate))
        {
            resetBucket = parsedDate.Year * 10000 + parsedDate.Month * 100 + parsedDate.Day;
            return true;
        }

        if (!int.TryParse(trimmed, NumberStyles.Integer, CultureInfo.InvariantCulture, out int numericBucket))
        {
            return false;
        }

        string candidate = numericBucket.ToString("D8", CultureInfo.InvariantCulture);
        if (!DateTime.TryParseExact(candidate, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime parsedBucketDate))
        {
            return false;
        }

        resetBucket = parsedBucketDate.Year * 10000 + parsedBucketDate.Month * 100 + parsedBucketDate.Day;
        return true;
    }

    public static int NormalizeResetBucket(int resetBucket)
    {
        return TryParseResetBucket(resetBucket.ToString(CultureInfo.InvariantCulture), out int normalizedBucket)
            ? normalizedBucket
            : 0;
    }
}
