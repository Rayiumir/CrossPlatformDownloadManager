﻿using System.Collections.ObjectModel;
using Avalonia.Platform;
using Newtonsoft.Json;

namespace CrossPlatformDownloadManager.Utils;

public static class ExtensionMethods
{
    public static bool IsNullOrEmpty(this string? value)
    {
        value = value?.Trim();
        return string.IsNullOrEmpty(value);
    }

    public static T? ConvertFromJson<T>(this string? json)
    {
        return json.IsNullOrEmpty() ? default : JsonConvert.DeserializeObject<T>(json!);
    }

    public static string? ConvertToJson(this object? value)
    {
        return JsonConvert.SerializeObject(value);
    }

    public static ObservableCollection<T> ToObservableCollection<T>(this IEnumerable<T>? items)
    {
        return items == null ? [] : new ObservableCollection<T>(items);
    }

    public static string ToFileSize(this double bytes)
    {
        if (bytes <= 0)
            return string.Empty;

        var tb = bytes / Constants.TB;
        if (tb > 1)
            return $"{tb:N2} TB";

        var gb = bytes / Constants.GB;
        if (gb > 1)
            return $"{gb:N2} GB";

        var mb = bytes / Constants.MB;
        if (mb > 1)
            return $"{mb:N2} MB";

        var kb = bytes / Constants.KB;
        if (kb > 1)
            return $"{kb:N2} KB";

        return $"{bytes:N2} Byte" + (bytes > 1 ? "s" : "");
    }

    public static string ToFileSize(this double? bytes)
    {
        bytes ??= 0;
        return bytes.Value.ToFileSize();
    }

    public static string ToFileSize(this long bytes)
    {
        return ((double)bytes).ToFileSize();
    }

    public static string ToFileSize(this long? bytes)
    {
        bytes ??= 0;
        return bytes.Value.ToFileSize();
    }

    public static string ToFileSize(this float bytes)
    {
        return ((double)bytes).ToFileSize();
    }

    public static string ToFileSize(this float? bytes)
    {
        bytes ??= 0;
        return bytes.Value.ToFileSize();
    }

    public static bool CheckUrlValidation(this string? url)
    {
        if (url.IsNullOrEmpty())
            return false;

        return Uri.TryCreate(url, UriKind.Absolute, out var uriResult) &&
               (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
    }

    public static T? OpenJsonAsset<T>(this Uri? uri)
    {
        if (uri == null)
            return default;

        using var stream = AssetLoader.Open(uri);
        using var reader = new StreamReader(stream);
        var json = reader.ReadToEnd();
        reader.Close();
        stream.Close();

        return json.ConvertFromJson<T>();
    }

    public static string? GetFileName(this string? url)
    {
        if (url.IsNullOrEmpty())
            return null;

        url = url!.Replace('\\', '/').Trim();

        var uri = new Uri(url);
        var fileName = string.Empty;
        if (uri.IsFile)
            fileName = Path.GetFileName(uri.LocalPath);

        var tempBaseUri = new Uri("https://localhost/temp");
        if (fileName.IsNullOrEmpty())
        {
            if (!Uri.TryCreate(url, UriKind.Absolute, out uri))
                uri = new Uri(tempBaseUri, url);

            fileName = Path.GetFileName(uri.LocalPath);
        }

        if (fileName.IsNullOrEmpty())
        {
            var startIndex = url.LastIndexOf('/') + 1;
            var path = url.Substring(startIndex);
            if (path.Contains('.'))
            {
                var endIndex = path.LastIndexOf('.');
                if (path.Substring(endIndex).Contains('?'))
                {
                    endIndex = path.LastIndexOf('?');
                    fileName = path.Substring(0, endIndex);
                }
                else
                {
                    fileName = path;
                }
            }
            else
            {
                fileName = null;
            }
        }

        if (fileName.IsNullOrEmpty())
            return fileName;

        if (fileName!.Contains('/'))
            fileName = fileName.Substring(fileName.LastIndexOf('/') + 1);

        if (fileName.Contains('?'))
            fileName = fileName.Substring(0, fileName.IndexOf('?'));

        return fileName;
    }

    public static bool HasFileExtension(this string? fileName)
    {
        if (fileName.IsNullOrEmpty())
            return false;

        return !Path.GetExtension(fileName!).IsNullOrEmpty();
    }

    public static string GetShortTime(this TimeSpan? time)
    {
        if (time == null)
            return "00 : 00";

        var seconds = time.Value.TotalSeconds;

        var hours = seconds / 3600;
        seconds %= 3600;

        var minutes = seconds / 60;
        seconds %= 60;

        return hours > 1 ? $"{hours:00} : {minutes:00} : {seconds:00}" : $"{minutes:00} : {seconds:00}";
    }

    public static T? DeepCopy<T>(this T? obj)
    {
        var json = obj.ConvertToJson();
        return json.ConvertFromJson<T>();
    }
}