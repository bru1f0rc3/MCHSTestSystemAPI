namespace MCHSWebAPI.Tests.Services;

public class PdfParserServiceTests
{
    private readonly PdfParserService _service;

    public PdfParserServiceTests()
    {
        _service = new PdfParserService();
    }

    [Fact]
    public async Task ParseTestFromPdfAsync_WithEmptyPdf_ThrowsInvalidOperation()
    {
        using var stream = new MemoryStream(new byte[] { 0x25, 0x50, 0x44, 0x46 }); // "%PDF" header
        await Assert.ThrowsAnyAsync<Exception>(() => _service.ParseTestFromPdfAsync(stream));
    }
}
