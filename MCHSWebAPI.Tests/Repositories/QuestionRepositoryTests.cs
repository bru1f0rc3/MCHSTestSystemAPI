using MCHSWebAPI.Repositories;

namespace MCHSWebAPI.Tests.Repositories;

public class QuestionRepositoryTests
{
    [Fact]
    public void QuestionRepository_ShouldHaveCorrectInterface()
    {
        // Arrange & Act
        var type = typeof(QuestionRepository);
        var interfaces = type.GetInterfaces();

        // Assert
        interfaces.Should().Contain(typeof(IQuestionRepository));
    }
}
