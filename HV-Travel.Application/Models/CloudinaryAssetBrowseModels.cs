namespace HVTravel.Application.Models;

public class CloudinaryAssetBrowseRequest
{
    public string Folder { get; set; } = string.Empty;
    public string Search { get; set; } = string.Empty;
    public string Cursor { get; set; } = string.Empty;
    public int MaxResults { get; set; } = 24;
}

public class CloudinaryAssetBrowseResult
{
    public List<CloudinaryAssetBrowseItem> Items { get; set; } = [];
    public string NextCursor { get; set; } = string.Empty;
    public string Folder { get; set; } = string.Empty;
}

public class CloudinaryAssetBrowseItem
{
    public string SecureUrl { get; set; } = string.Empty;
    public string PublicId { get; set; } = string.Empty;
    public string ThumbnailUrl { get; set; } = string.Empty;
    public string Format { get; set; } = string.Empty;
    public string SizeLabel { get; set; } = string.Empty;
    public int Width { get; set; }
    public int Height { get; set; }
    public string CreatedAt { get; set; } = string.Empty;
    public string Folder { get; set; } = string.Empty;
}
