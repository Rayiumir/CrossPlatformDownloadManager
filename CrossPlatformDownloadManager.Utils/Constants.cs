﻿using CrossPlatformDownloadManager.Utils.Enums;

namespace CrossPlatformDownloadManager.Utils;

public static class Constants
{
    // File Size
    public const long KB = 1024;
    public const long MB = KB * 1024;
    public const long GB = MB * 1024;
    public const long TB = GB * 1024;

    // Turn off computer modes
    public static readonly List<string> TurnOffComputerModes = Enum
        .GetNames(typeof(TurnOffComputerMode))
        .Select(n =>
        {
            if (n.Equals(Enum.GetName(TurnOffComputerMode.Shutdown)))
                n = "Shut down";

            return n;
        })
        .ToList();

    // Speed limiter units
    public static readonly List<string> SpeedLimiterUnits = ["KB", "MB"];

    // Times of day
    public static readonly List<string> TimesOfDay = ["AM", "PM"];

    // General category title
    public const string GeneralCategoryTitle = "General";

    // Unknown file type
    public const string UnknownFileType = "Unknown";

    public const string NewCategoryIcon =
        "M500.3 7.3C507.7 13.3 512 22.4 512 32l0 144c0 26.5-28.7 48-64 48s-64-21.5-64-48s28.7-48 64-48l0-57L352 90.2 352 208c0 26.5-28.7 48-64 48s-64-21.5-64-48s28.7-48 64-48l0-96c0-15.3 10.8-28.4 25.7-31.4l160-32c9.4-1.9 19.1 .6 26.6 6.6zM74.7 304l11.8-17.8c5.9-8.9 15.9-14.2 26.6-14.2l61.7 0c10.7 0 20.7 5.3 26.6 14.2L213.3 304l26.7 0c26.5 0 48 21.5 48 48l0 112c0 26.5-21.5 48-48 48L48 512c-26.5 0-48-21.5-48-48L0 352c0-26.5 21.5-48 48-48l26.7 0zM192 408a48 48 0 1 0 -96 0 48 48 0 1 0 96 0zM478.7 278.3L440.3 368l55.7 0c6.7 0 12.6 4.1 15 10.4s.6 13.3-4.4 17.7l-128 112c-5.6 4.9-13.9 5.3-19.9 .9s-8.2-12.4-5.3-19.2L391.7 400 336 400c-6.7 0-12.6-4.1-15-10.4s-.6-13.3 4.4-17.7l128-112c5.6-4.9 13.9-5.3 19.9-.9s8.2 12.4 5.3 19.2zm-339-59.2c-6.5 6.5-17 6.5-23 0L19.9 119.2c-28-29-26.5-76.9 5-103.9c27-23.5 68.4-19 93.4 6.5l10 10.5 9.5-10.5c25-25.5 65.9-30 93.9-6.5c31 27 32.5 74.9 4.5 103.9l-96.4 99.9z";

    public const string UnfinishedCategoryHeaderTitle = "Unfinished";
    public const string FinishedCategoryHeaderTitle = "Finished";

    public static readonly List<string> DuplicateDownloadLinkActions =
    [
        "Show the dialog and let me choose",
        "Add the duplicate with a number after its file name",
        "Add the duplicate and overwrite existing file",
        "if download file complete, show download complete dialog. Otherwise resume it"
    ];

    public static readonly List<int> MaximumConnectionsCounts =
    [
        1,
        2,
        4,
        8,
        16,
        32,
    ];

    public static readonly List<string> ProxyTypes = Enum
        .GetNames(typeof(ProxyType))
        .Select(pt =>
        {
            if (pt.Equals(Enum.GetName(ProxyType.Socks5)))
                pt = "Socks 5";
            
            return pt;
        })
        .ToList();

    public const string DefaultDownloadQueueTitle = "Main Queue";
    
    // Listening urls
    public const string CheckFileTypeSupportUrl = "http://localhost:5000/cdm/download/check/";
    public const string AddDownloadFileUrl = "http://localhost:5000/cdm/download/add/";
}