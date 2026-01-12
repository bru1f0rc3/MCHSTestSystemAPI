using MCHSWebAPI.Repositories;

namespace MCHSWebAPI.Tests.Repositories;

public class ReportRepositoryTests
{
    [Fact]
    public void ReportRepository_ShouldHaveCorrectInterface()
    {
        // Arrange & Act
        var type = typeof(ReportRepository);
        var interfaces = type.GetInterfaces();

        // Assert
        interfaces.Should().Contain(typeof(IReportRepository));
    }
}
