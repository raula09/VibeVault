using System.Net;
using System.Text.RegularExpressions;

namespace VibeVault;

internal sealed record GoogleDriveDownloadResult(
    IReadOnlyList<string> DownloadedPaths,
    int TotalFiles,
    int FailedDownloads,
    string? Error);

internal static class GoogleDriveFolderDownloader
{
    private sealed record FolderRef(string Id, string? ResourceKey, string OriginalLink);
    private sealed record DriveFile(string Id, string Name);
    private sealed record FolderListResult(IReadOnlyList<DriveFile> Files, string? Error);

    private static readonly HttpClient Http = new(new HttpClientHandler
    {
        AllowAutoRedirect = true,
        AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate | DecompressionMethods.Brotli
    });

    static GoogleDriveFolderDownloader()
    {
        Http.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (X11; Linux x86_64) VibeVault/1.0");
        Http.DefaultRequestHeaders.Accept.ParseAdd("text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");
        Http.DefaultRequestHeaders.AcceptLanguage.ParseAdd("en-US,en;q=0.8");
    }

    public static async Task<GoogleDriveDownloadResult> DownloadMp3FilesAsync(
        string folderLink,
        string targetDir,
        CancellationToken cancellationToken = default)
    {
        var folderRef = TryExtractFolderRef(folderLink);
        if (folderRef is null || string.IsNullOrWhiteSpace(folderRef.Id))
            return new GoogleDriveDownloadResult([], 0, 0, "invalid google drive folder link");

        Directory.CreateDirectory(targetDir);

        var listing = await ListFolderFilesAsync(folderRef, cancellationToken).ConfigureAwait(false);
        var files = listing.Files;
        if (files.Count == 0)
            return new GoogleDriveDownloadResult(
                [],
                0,
                0,
                listing.Error is null
                    ? "could not read files from this folder link (check share settings and link type)"
                    : $"could not read files from this folder link ({listing.Error})");

        var downloadableFiles = files
            .GroupBy(f => f.Id, StringComparer.Ordinal)
            .Select(g => g.First())
            .ToList();

        if (downloadableFiles.Count == 0)
            return new GoogleDriveDownloadResult([], 0, 0, "no downloadable files found in the shared folder");

        var downloaded = new List<string>(downloadableFiles.Count);
        var failed = 0;
        foreach (var file in downloadableFiles)
        {
            var ok = await TryDownloadFileAsync(file, targetDir, cancellationToken).ConfigureAwait(false);
            if (ok is null)
            {
                failed++;
                continue;
            }

            downloaded.Add(ok);
        }

        return new GoogleDriveDownloadResult(downloaded, downloadableFiles.Count, failed, null);
    }

    private static async Task<FolderListResult> ListFolderFilesAsync(FolderRef folderRef, CancellationToken cancellationToken)
    {
        var suffix = string.IsNullOrWhiteSpace(folderRef.ResourceKey)
            ? string.Empty
            : $"&resourcekey={Uri.EscapeDataString(folderRef.ResourceKey)}";

        var urls = new[]
        {
            folderRef.OriginalLink,
            $"https://drive.google.com/embeddedfolderview?id={folderRef.Id}{suffix}#list",
            $"https://drive.google.com/drive/folders/{folderRef.Id}" +
                (string.IsNullOrWhiteSpace(folderRef.ResourceKey) ? string.Empty : $"?resourcekey={Uri.EscapeDataString(folderRef.ResourceKey)}"),
            $"https://drive.google.com/drive/u/0/folders/{folderRef.Id}" +
                (string.IsNullOrWhiteSpace(folderRef.ResourceKey) ? string.Empty : $"?resourcekey={Uri.EscapeDataString(folderRef.ResourceKey)}")
        };

        var merged = new List<DriveFile>();
        string? lastError = null;
        foreach (var url in urls)
        {
            string html;
            try
            {
                using var req = new HttpRequestMessage(HttpMethod.Get, url);
                using var res = await Http.SendAsync(req, cancellationToken).ConfigureAwait(false);
                if (!res.IsSuccessStatusCode) continue;
                html = await res.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                lastError = ex.GetType().Name;
                continue;
            }

            merged.AddRange(ParseDriveFilesFromHtml(html));
            if (merged.Count > 0) break;
        }

        if (merged.Count == 0)
            return new FolderListResult([], lastError);

        return new FolderListResult(merged
            .GroupBy(x => x.Id, StringComparer.Ordinal)
            .Select(g => g.First())
            .ToList(), null);
    }

    private static List<DriveFile> ParseDriveFilesFromHtml(string html)
    {
        var list = new List<DriveFile>();
        if (string.IsNullOrWhiteSpace(html)) return list;

        var anchorsDouble = Regex.Matches(
            html,
            "<a[^>]+href=\"([^\"]+)\"[^>]*>(.*?)</a>",
            RegexOptions.IgnoreCase | RegexOptions.Singleline);

        foreach (Match anchor in anchorsDouble)
        {
            if (anchor.Groups.Count < 3) continue;
            var href = WebUtility.HtmlDecode(anchor.Groups[1].Value);
            var rawLabel = Regex.Replace(anchor.Groups[2].Value, "<.*?>", string.Empty);
            var label = WebUtility.HtmlDecode(rawLabel).Trim();
            var fileId = TryExtractFileId(href);
            if (string.IsNullOrWhiteSpace(fileId)) continue;
            list.Add(new DriveFile(fileId, string.IsNullOrWhiteSpace(label) ? fileId : label));
        }

        var anchorsSingle = Regex.Matches(
            html,
            "<a[^>]+href='([^']+)'[^>]*>(.*?)</a>",
            RegexOptions.IgnoreCase | RegexOptions.Singleline);

        foreach (Match anchor in anchorsSingle)
        {
            if (anchor.Groups.Count < 3) continue;
            var href = WebUtility.HtmlDecode(anchor.Groups[1].Value);
            var rawLabel = Regex.Replace(anchor.Groups[2].Value, "<.*?>", string.Empty);
            var label = WebUtility.HtmlDecode(rawLabel).Trim();
            var fileId = TryExtractFileId(href);
            if (string.IsNullOrWhiteSpace(fileId)) continue;
            list.Add(new DriveFile(fileId, string.IsNullOrWhiteSpace(label) ? fileId : label));
        }

        var rawPatterns = new[]
        {
            "/file/d/([a-zA-Z0-9_-]{20,})",
            "\\\\/file\\\\/d\\\\/([a-zA-Z0-9_-]{20,})",
            "https:\\\\\\/\\\\\\/drive\\.google\\.com\\\\/file\\\\/d\\\\/([a-zA-Z0-9_-]{20,})\\\\/view",
            "[?&]id=([a-zA-Z0-9_-]{20,})",
            "\"docid\":\"([a-zA-Z0-9_-]{20,})\"",
            "\"id\":\"([a-zA-Z0-9_-]{20,})\""
        };

        foreach (var pattern in rawPatterns)
        {
            var matches = Regex.Matches(html, pattern, RegexOptions.IgnoreCase);
            foreach (Match m in matches)
            {
                if (m.Groups.Count < 2) continue;
                var id = m.Groups[1].Value;
                if (string.IsNullOrWhiteSpace(id)) continue;
                list.Add(new DriveFile(id, id));
            }
        }

        var ivd = Regex.Match(
            html,
            "window\\['_DRIVE_ivd'\\]\\s*=\\s*'(?<payload>.*?)';",
            RegexOptions.IgnoreCase | RegexOptions.Singleline);
        if (ivd.Success)
        {
            var payload = ivd.Groups["payload"].Value;
            var ivdMatches = Regex.Matches(
                payload,
                "https:\\\\\\/\\\\\\/drive\\.google\\.com\\\\/file\\\\/d\\\\/([a-zA-Z0-9_-]{20,})\\\\/view",
                RegexOptions.IgnoreCase);
            foreach (Match m in ivdMatches)
            {
                if (m.Groups.Count < 2) continue;
                var id = m.Groups[1].Value;
                if (string.IsNullOrWhiteSpace(id)) continue;
                list.Add(new DriveFile(id, id));
            }
        }

        return list;
    }

    private static async Task<string?> TryDownloadFileAsync(
        DriveFile file,
        string targetDir,
        CancellationToken cancellationToken)
    {
        var directUrl = $"https://drive.google.com/uc?export=download&id={file.Id}";
        using var firstRequest = new HttpRequestMessage(HttpMethod.Get, directUrl);
        using var firstResponse = await Http.SendAsync(firstRequest, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
        if (!firstResponse.IsSuccessStatusCode) return null;

        var contentType = firstResponse.Content.Headers.ContentType?.MediaType ?? string.Empty;
        if (contentType.Contains("text/html", StringComparison.OrdinalIgnoreCase))
        {
            var html = await firstResponse.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            var confirm = TryExtractConfirmUrl(html);
            if (confirm is null) return null;

            var confirmUrl = confirm.StartsWith("http", StringComparison.OrdinalIgnoreCase)
                ? confirm
                : "https://drive.google.com" + confirm;

            using var secondRequest = new HttpRequestMessage(HttpMethod.Get, confirmUrl);
            using var secondResponse = await Http.SendAsync(secondRequest, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
            if (!secondResponse.IsSuccessStatusCode) return null;

            return await SaveContentAsync(secondResponse, file.Name, targetDir, cancellationToken).ConfigureAwait(false);
        }

        return await SaveContentAsync(firstResponse, file.Name, targetDir, cancellationToken).ConfigureAwait(false);
    }

    private static async Task<string?> SaveContentAsync(
        HttpResponseMessage response,
        string originalName,
        string targetDir,
        CancellationToken cancellationToken)
    {
        var headerName = response.Content.Headers.ContentDisposition?.FileNameStar
            ?? response.Content.Headers.ContentDisposition?.FileName;
        var chosenName = !string.IsNullOrWhiteSpace(headerName)
            ? headerName.Trim().Trim('"')
            : originalName;

        var safeName = SanitizeFileName(chosenName);
        if (string.IsNullOrWhiteSpace(safeName))
            safeName = "downloaded-file";

        var finalPath = BuildUniquePath(targetDir, safeName);
        await using var outFile = File.Create(finalPath);
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        await stream.CopyToAsync(outFile, cancellationToken).ConfigureAwait(false);
        await outFile.FlushAsync(cancellationToken).ConfigureAwait(false);

        var info = new FileInfo(finalPath);
        return info.Length == 0 ? null : finalPath;
    }

    private static string BuildUniquePath(string dir, string fileName)
    {
        var baseName = Path.GetFileNameWithoutExtension(fileName);
        var ext = Path.GetExtension(fileName);
        var path = Path.Combine(dir, fileName);
        var suffix = 1;

        while (File.Exists(path))
        {
            path = Path.Combine(dir, $"{baseName}_{suffix}{ext}");
            suffix++;
        }

        return path;
    }

    private static string SanitizeFileName(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var chars = name.Select(ch => invalid.Contains(ch) ? '_' : ch).ToArray();
        return new string(chars).Trim();
    }

    private static string? TryExtractConfirmUrl(string html)
    {
        var href = Regex.Match(
            html,
            "href=\"([^\"]*uc\\?export=download[^\"]*confirm[^\"]*)\"",
            RegexOptions.IgnoreCase);

        if (href.Success && href.Groups.Count > 1)
            return WebUtility.HtmlDecode(href.Groups[1].Value);

        var form = Regex.Match(
            html,
            "action=\"([^\"]*/uc[^\"]*)\"",
            RegexOptions.IgnoreCase);
        var confirm = Regex.Match(html, "name=\"confirm\" value=\"([^\"]+)\"", RegexOptions.IgnoreCase);
        var id = Regex.Match(html, "name=\"id\" value=\"([^\"]+)\"", RegexOptions.IgnoreCase);
        if (form.Success && confirm.Success && id.Success)
        {
            var action = WebUtility.HtmlDecode(form.Groups[1].Value);
            return $"{action}?export=download&confirm={confirm.Groups[1].Value}&id={id.Groups[1].Value}";
        }

        return null;
    }

    private static FolderRef? TryExtractFolderRef(string link)
    {
        var normalized = NormalizePotentialPaste(link);
        if (string.IsNullOrWhiteSpace(normalized)) return null;

        string? resourceKey = null;
        var rk = Regex.Match(normalized, "[?&]resourcekey=([^&]+)", RegexOptions.IgnoreCase);
        if (rk.Success && rk.Groups.Count > 1)
            resourceKey = Uri.UnescapeDataString(rk.Groups[1].Value);

        var byPath = Regex.Match(normalized, "/folders/([a-zA-Z0-9_-]+)", RegexOptions.IgnoreCase);
        if (byPath.Success) return new FolderRef(byPath.Groups[1].Value, resourceKey, normalized);

        var byParam = Regex.Match(normalized, "[?&]id=([a-zA-Z0-9_-]+)", RegexOptions.IgnoreCase);
        if (byParam.Success) return new FolderRef(byParam.Groups[1].Value, resourceKey, normalized);

        return null;
    }

    private static string NormalizePotentialPaste(string? input)
    {
        if (string.IsNullOrWhiteSpace(input)) return string.Empty;

        var text = input
            .Replace("\u001b[200~", string.Empty, StringComparison.Ordinal)
            .Replace("\u001b[201~", string.Empty, StringComparison.Ordinal)
            .Replace("\u200B", string.Empty, StringComparison.Ordinal)
            .Replace("\u200C", string.Empty, StringComparison.Ordinal)
            .Replace("\u200D", string.Empty, StringComparison.Ordinal)
            .Trim();

        var url = Regex.Match(text, @"https?://\S+", RegexOptions.IgnoreCase);
        if (url.Success)
            return url.Value.TrimEnd('.', ',', ';', ')', ']', '>');

        return text;
    }

    private static string? TryExtractFileId(string href)
    {
        if (string.IsNullOrWhiteSpace(href)) return null;

        var byPath = Regex.Match(href, "/file/d/([a-zA-Z0-9_-]+)", RegexOptions.IgnoreCase);
        if (byPath.Success) return byPath.Groups[1].Value;

        var byParam = Regex.Match(href, "[?&]id=([a-zA-Z0-9_-]+)", RegexOptions.IgnoreCase);
        if (byParam.Success) return byParam.Groups[1].Value;

        return null;
    }
}
