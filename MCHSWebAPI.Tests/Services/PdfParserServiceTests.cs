using MCHSWebAPI.Services.TestService;

namespace MCHSWebAPI.Tests.Services;

public class PdfParserServiceTests
{
    private readonly PdfParserService _service = new();

    [Fact]
    public async Task ParseTestFromPdfAsync_WithBrokenPdf_Throws()
    {
        using var stream = new MemoryStream(new byte[] { 0x25, 0x50, 0x44, 0x46 });

        await Assert.ThrowsAnyAsync<Exception>(() => _service.ParseTestFromPdfAsync(stream));
    }

    [Fact]
    public async Task ParseTestFromPdfAsync_WithEmptyStream_Throws()
    {
        using var stream = new MemoryStream(Array.Empty<byte>());

        await Assert.ThrowsAnyAsync<Exception>(() => _service.ParseTestFromPdfAsync(stream));
    }
}
